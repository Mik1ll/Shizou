using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.Exceptions;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class UdpResponse
{
    public string ResponseText { get; init; } = string.Empty;
    public AniDbResponseCode ResponseCode { get; init; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string ResponseCodeText { get; init; } = string.Empty;
}

public abstract partial class AniDbUdpRequest<TResponse> : IAniDbUdpRequest<TResponse>
    where TResponse : UdpResponse, new()
{
    protected readonly AniDbUdpState AniDbUdpState;
    protected readonly ILogger<AniDbUdpRequest<TResponse>> Logger;
    private readonly UdpRateLimiter _rateLimiter;
    private readonly string[] _anonymousCmds = ["PING", "ENCODING", "ENCRYPT", "AUTH", "VERSION", "USER"];
    private string? _requestText;
    private string? _tag;

    protected AniDbUdpRequest(string command, ILogger<AniDbUdpRequest<TResponse>> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter)
    {
        Command = command;
        Logger = logger;
        AniDbUdpState = aniDbUdpState;
        _rateLimiter = rateLimiter;
    }

    protected string Command { get; set; }
    protected Dictionary<string, string> Args { get; } = [];
    protected bool ParametersSet { get; set; }
    protected Encoding Encoding { get; } = Encoding.UTF8;

    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="AniDbUdpRequestException"></exception>
    public async Task<TResponse?> ProcessAsync()
    {
        var response = await ProcessInnerAsync().ConfigureAwait(false);
        if (response is { } r)
            return CreateResponse(r.responseText, r.responseCode, r.responseCodeText);
        return null;
    }

    protected virtual TResponse CreateResponse(string responseText, AniDbResponseCode responseCode, string responseCodeText) =>
        new()
        {
            ResponseText = responseText,
            ResponseCode = responseCode,
            ResponseCodeText = responseCodeText,
        };

    private async Task<(string responseText, AniDbResponseCode responseCode, string responseCodeText)?> ProcessInnerAsync()
    {
        if (!ParametersSet)
            throw new ArgumentException($"Parameters not set before {nameof(ProcessAsync)} called");

        (string responseText, AniDbResponseCode responseCode, string responseCodeText)? response;
        await PrepareRequestAsync().ConfigureAwait(false);
        using (await _rateLimiter.AcquireAsync().ConfigureAwait(false))
        {
            await SendRequestAsync().ConfigureAwait(false);
            response = await ReceiveResponseAsync().ConfigureAwait(false);
        }

        var retry = HandleSharedErrors(response);
        if (!retry)
            return response;

        Logger.LogDebug("Error handled, retrying request");
        await PrepareRequestAsync().ConfigureAwait(false);
        using (await _rateLimiter.AcquireAsync().ConfigureAwait(false))
        {
            await SendRequestAsync().ConfigureAwait(false);
            response = await ReceiveResponseAsync().ConfigureAwait(false);
        }

        HandleSharedErrors(response);
        return response;
    }

    /// <exception cref="AniDbUdpRequestException"></exception>
    private async Task SendRequestAsync()
    {
        if (_requestText is null)
        {
            Logger.LogError("Request text was not set before sending");
            throw new AniDbUdpRequestException("Tried to send unprepared udp request text");
        }

        var dgramBytes = Encoding.GetBytes(_requestText);
        if (AniDbUdpState.Banned)
        {
            Logger.LogWarning("Banned, aborting UDP request: {RequestText}", _requestText);
            throw new AniDbUdpRequestException("Udp banned", AniDbResponseCode.Banned);
        }

        Logger.LogDebug("Sending AniDb UDP text: {RequestText}", _requestText);
        var addresses = await Dns.GetHostAddressesAsync(AniDbUdpState.ServerHost).ConfigureAwait(false);
        if (addresses.Length == 0)
        {
            Logger.LogError("Could not resolve anidb host");
            throw new AniDbUdpRequestException("Could not resolve AniDb Host");
        }

        var remoteEndPoint = new IPEndPoint(addresses[0], AniDbUdpState.ServerPort);
        await AniDbUdpState.UdpClient.SendAsync(dgramBytes, remoteEndPoint).ConfigureAwait(false);
    }

    /// <exception cref="AniDbUdpRequestException"></exception>
    private async Task PrepareRequestAsync()
    {
        Logger.LogTrace("Preparing {Command} request", Command);
        if (!_anonymousCmds.Contains(Command))
        {
            await AniDbUdpState.LoginAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(AniDbUdpState.SessionKey))
                throw new AniDbUdpRequestException("Session key was not found");
            Args["s"] = AniDbUdpState.SessionKey;
        }

        _requestText = Command;
        Args["tag"] = _tag = Guid.NewGuid().ToString("N")[..8];
        _requestText += ' ' + string.Join('&', Args.Select(kvp => $"{kvp.Key}={NewLineRegex().Replace(HttpUtility.HtmlEncode(kvp.Value), "<br />")}"));
    }


    /// <exception cref="AniDbUdpRequestException"></exception>
    private async Task<(string responseText, AniDbResponseCode responseCode, string responseCodeText)?> ReceiveResponseAsync()
    {
        Logger.LogTrace("Waiting to receive raw UDP response");
        var receiveTimeout = TimeSpan.FromSeconds(10);
        try
        {
            while (true)
            {
                using var cancelSource = new CancellationTokenSource(receiveTimeout);
                var receivedBytes = (await AniDbUdpState.UdpClient.ReceiveAsync(cancelSource.Token).ConfigureAwait(false)).Buffer;

                // If starts with two null bytes then zlib compressed (deflate)
                await using Stream memStream = receivedBytes is [0, 0, ..]
                    ? new ZLibStream(new MemoryStream(receivedBytes, 2, receivedBytes.Length - 2), CompressionMode.Decompress)
                    : new MemoryStream(receivedBytes);
                using var reader = new StreamReader(memStream, Encoding, false);
                var codeLine = await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(false) ?? string.Empty;
                var responseText = await reader.ReadToEndAsync(CancellationToken.None).ConfigureAwait(false);
                Logger.LogDebug("Received UDP response:\n{CodeLine}\n{ResponseText}", codeLine, responseText);
                var codeLineMatches = ReturnCodeRegex().Match(codeLine);
                if (!codeLineMatches.Success)
                {
                    Logger.LogError("AniDB response was malformed:\n{CodeLine}\n{ResponseText}", codeLine, responseText);
                    continue;
                }

                var responseTag = codeLineMatches.Groups["tag"].Value;
                var responseCode = (AniDbResponseCode)int.Parse(codeLineMatches.Groups["code"].Value);
                var responseCodeText = codeLineMatches.Groups["codetext"].Value;
                if (responseTag == _tag)
                    return (responseText, responseCode, responseCodeText);
                HandleSharedErrors((responseText, responseCode, responseCodeText));
                Logger.LogError("Tag {Tag} did not match returned response:\n{CodeLine}\n{ResponseText}", _tag, codeLine, responseText);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Failed to receive a response before timeout ({Timeout}s)", receiveTimeout.TotalSeconds);
            return null;
        }
    }

    /// <summary>
    ///     For AniDB general errors
    /// </summary>
    /// <returns>True if need to retry</returns>
    /// <exception cref="AniDbUdpRequestException"></exception>
    private bool HandleSharedErrors((string responseText, AniDbResponseCode responseCode, string responseCodeText)? response)
    {
        if (response is null)
            return true;
        var (responseText, responseCode, responseCodeText) = response.Value;
        switch (responseCode)
        {
            case AniDbResponseCode.OutOfService:
                Logger.LogWarning("AniDB out of service or in maintenance");
                throw new AniDbUdpRequestException("AniDB out of service/maintenance", responseCode);
            case AniDbResponseCode.ServerBusy:
                Logger.LogWarning("Server busy, try again later");
                throw new AniDbUdpRequestException("Server busy, try again later", responseCode);
            case AniDbResponseCode.Banned:
                AniDbUdpState.Banned = true;
                AniDbUdpState.BanReason = responseText;
                AniDbUdpState.SessionKey = null;
                AniDbUdpState.ResetBannedTimer();
                Logger.LogWarning("Banned: {BanReason}, waiting {Hours}hr {Minutes}min ({UnbanTime})", AniDbUdpState.BanReason, AniDbUdpState.BanPeriod.Hours,
                    AniDbUdpState.BanPeriod.Minutes, DateTimeOffset.Now + AniDbUdpState.BanPeriod);
                throw new AniDbUdpRequestException($"Udp banned, waiting until {DateTimeOffset.Now + AniDbUdpState.BanPeriod}", responseCode);
            case AniDbResponseCode.InvalidSession:
                Logger.LogWarning("Invalid session, reauth");
                AniDbUdpState.SessionKey = null;
                return true;
            case AniDbResponseCode.LoginFirst:
                Logger.LogWarning("Not logged in, reauth");
                AniDbUdpState.SessionKey = null;
                return true;
            case AniDbResponseCode.AccessDenied:
                Logger.LogError("Access denied");
                throw new AniDbUdpRequestException("Access was denied", responseCode);
            case AniDbResponseCode.InternalServerError or > AniDbResponseCode.ServerBusy and < (AniDbResponseCode)700:
                Logger.LogCritical("AniDB Server CRITICAL ERROR {ErrorCode} : {ErrorCodeStr}", responseCode, responseCodeText);
                throw new AniDbUdpRequestException($"Critical error with server {responseCode} {responseCodeText}", responseCode);
            case AniDbResponseCode.UnknownCommand:
                Logger.LogError("Uknown command, {Command}, {RequestText}", Command, _requestText);
                throw new AniDbUdpRequestException("Unknown AniDB command, check logs", responseCode);
            case AniDbResponseCode.IllegalInputOrAccessDenied:
                Logger.LogError("Illegal input or access is denied, {Command}, {RequestText}", Command, _requestText);
                throw new AniDbUdpRequestException("Illegal AniDB input, check logs", responseCode);
            default:
                if (!Enum.IsDefined(typeof(AniDbResponseCode), responseCode))
                {
                    Logger.LogError("Response Code {ResponseCode} not found in enumeration: Code string: {CodeString}", responseCode,
                        responseCodeText);
                    throw new AniDbUdpRequestException($"Unknown response code: {responseCode}: {responseCodeText}");
                }

                return false;
        }
    }

    [GeneratedRegex(@"\r?\n|\r")]
    private static partial Regex NewLineRegex();

    [GeneratedRegex("(?<tag>[0-9a-f]{8}) (?<code>[0-9]{3})(?: (?<codetext>.*))?")]
    private static partial Regex ReturnCodeRegex();
}
