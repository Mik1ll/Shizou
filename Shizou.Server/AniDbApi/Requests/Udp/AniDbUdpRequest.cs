using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
    public string ResponseCodeText { get; init; } = string.Empty;
}

public abstract class AniDbUdpRequest<TResponse> : IAniDbUdpRequest<TResponse>
    where TResponse : UdpResponse, new()
{
    protected readonly AniDbUdpState AniDbUdpState;
    protected readonly ILogger<AniDbUdpRequest<TResponse>> Logger;
    private readonly UdpRateLimiter _rateLimiter;
    private string? _requestText;

    protected AniDbUdpRequest(string command, ILogger<AniDbUdpRequest<TResponse>> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter)
    {
        Command = command;
        Logger = logger;
        AniDbUdpState = aniDbUdpState;
        _rateLimiter = rateLimiter;
    }

    protected string Command { get; set; }
    protected Dictionary<string, string> Args { get; } = new();
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

    protected virtual TResponse CreateResponse(string responseText, AniDbResponseCode responseCode, string responseCodeText)
    {
        return new TResponse
        {
            ResponseText = responseText,
            ResponseCode = responseCode,
            ResponseCodeText = responseCodeText
        };
    }

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
        if (retry)
        {
            Logger.LogDebug("Error handled, retrying request");
            using (await _rateLimiter.AcquireAsync().ConfigureAwait(false))
            {
                await SendRequestAsync().ConfigureAwait(false);
                response = await ReceiveResponseAsync().ConfigureAwait(false);
            }

            HandleSharedErrors(response);
        }

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
        if (!AniDbUdpState.UdpClient.Client.Connected)
            AniDbUdpState.Connect();
        await AniDbUdpState.UdpClient.SendAsync(dgramBytes, dgramBytes.Length).ConfigureAwait(false);
    }

    /// <exception cref="AniDbUdpRequestException"></exception>
    private async Task PrepareRequestAsync()
    {
        Logger.LogTrace("Preparing {Command} request", Command);
        var requestBuilder = new StringBuilder(Command + " ");
        if (!new List<string> { "PING", "ENCRYPT", "AUTH", "VERSION" }.Contains(Command))
        {
            await AniDbUdpState.LoginAsync().ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(AniDbUdpState.SessionKey))
                Args["s"] = AniDbUdpState.SessionKey;
            else
                throw new AniDbUdpRequestException("Failed to get new session");
        }

        foreach (var (name, param) in Args)
            requestBuilder.Append($"{name}={Regex.Replace(HttpUtility.HtmlEncode(param), @"\r?\n|\r", "<br />")}&");
        // Removes the extra & at end of parameters
        requestBuilder.Length--;
        _requestText = requestBuilder.ToString();
    }


    /// <exception cref="AniDbUdpRequestException"></exception>
    private async Task<(string responseText, AniDbResponseCode responseCode, string responseCodeText)?> ReceiveResponseAsync()
    {
        byte[] receivedBytes;
        Logger.LogTrace("Waiting to receive raw UDP response");
        var receiveTimeout = TimeSpan.FromSeconds(10);
        try
        {
            using var cancelSource = new CancellationTokenSource(receiveTimeout);
            receivedBytes = (await AniDbUdpState.UdpClient.ReceiveAsync(cancelSource.Token).ConfigureAwait(false)).Buffer;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Failed to receive a response before timeout ({Timeout}s)", receiveTimeout.TotalSeconds);
            return null;
        }

        Logger.LogTrace("Got raw UDP response");
        // Two null bytes and two bytes of Zlib header, seems to ignore trailer automatically
        await using Stream memStream = receivedBytes.Length > 2 && receivedBytes[0] == 0 && receivedBytes[1] == 0
            ? new DeflateStream(new MemoryStream(receivedBytes, 4, receivedBytes.Length - 4), CompressionMode.Decompress)
            : new MemoryStream(receivedBytes);
        using var reader = new StreamReader(memStream, Encoding);
        var codeLine = await reader.ReadLineAsync().ConfigureAwait(false);
        var responseText = await reader.ReadToEndAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(codeLine) || codeLine.Length <= 2)
            throw new AniDbUdpRequestException("AniDB response is empty");
        Logger.LogDebug("Received AniDB UDP response {CodeString}", codeLine);
        var responseCode = (AniDbResponseCode)int.Parse(codeLine[..3]);
        var responseCodeText = string.Empty;
        if (codeLine.Length >= 5)
            responseCodeText = codeLine[4..];
        return (responseText, responseCode, responseCodeText);
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
                AniDbUdpState.ResetBannedTimer();
                Logger.LogWarning("Banned: {BanReason}, waiting {Hours}hr {Minutes}min ({UnbanTime})", AniDbUdpState.BanReason, AniDbUdpState.BanPeriod.Hours,
                    AniDbUdpState.BanPeriod.Minutes, DateTimeOffset.Now + AniDbUdpState.BanPeriod);
                throw new AniDbUdpRequestException($"Udp banned, waiting until {DateTimeOffset.Now + AniDbUdpState.BanPeriod}", responseCode);
            case AniDbResponseCode.InvalidSession:
                Logger.LogWarning("Invalid session, reauth");
                AniDbUdpState.LoggedIn = false;
                return true;
            case AniDbResponseCode.LoginFirst:
                Logger.LogWarning("Not logged in, reauth");
                AniDbUdpState.LoggedIn = false;
                return true;
            case AniDbResponseCode.AccessDenied:
                Logger.LogError("Access denied");
                throw new AniDbUdpRequestException("Access was denied", responseCode);
            case AniDbResponseCode.InternalServerError or (> AniDbResponseCode.ServerBusy and < (AniDbResponseCode)700):
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
}
