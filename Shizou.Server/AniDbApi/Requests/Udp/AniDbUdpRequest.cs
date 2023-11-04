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

public abstract class AniDbUdpRequest : IAniDbUdpRequest
{
    private readonly UdpRateLimiter _rateLimiter;
    protected readonly AniDbUdpState AniDbUdpState;
    protected readonly ILogger<AniDbUdpRequest> Logger;
    private string? _requestText;

    protected AniDbUdpRequest(string command, ILogger<AniDbUdpRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter)
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
    public string? ResponseText { get; private set; }
    public AniDbResponseCode? ResponseCode { get; private set; }
    public string? ResponseCodeString { get; private set; }

    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="AniDbUdpRequestException"></exception>
    public async Task Process()
    {
        if (!ParametersSet)
            throw new ArgumentException($"Parameters not set before {nameof(Process)} called");
        await PrepareRequest();
        using (await _rateLimiter.AcquireAsync())
        {
            await SendRequest();
            await ReceiveResponse();
        }

        var retry = HandleSharedErrors();
        if (retry)
        {
            Logger.LogDebug("Error handled, retrying request");
            using (await _rateLimiter.AcquireAsync())
            {
                await SendRequest();
                await ReceiveResponse();
            }

            HandleSharedErrors();
        }

        await HandleResponse();
    }

    public static string DataUnescape(string data)
    {
        return Regex.Replace(data, @"<br\s*/>", "\n").Replace('`', '\'').Replace('/', '|');
    }


    protected abstract Task HandleResponse();

    /// <summary>
    ///     Create and send an UDP request to AniDb
    /// </summary>
    /// <returns>True if request sent</returns>
    /// <exception cref="AniDbUdpRequestException"></exception>
    private async Task SendRequest()
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
        await AniDbUdpState.UdpClient.SendAsync(dgramBytes, dgramBytes.Length);
    }

    private async Task PrepareRequest()
    {
        Logger.LogTrace("Preparing {Command} request", Command);
        var requestBuilder = new StringBuilder(Command + " ");
        if (!new List<string> { "PING", "ENCRYPT", "AUTH", "VERSION" }.Contains(Command))
        {
            if (!await AniDbUdpState.Login())
            {
                ResponseCode = AniDbResponseCode.LoginFailed;
                throw new AniDbUdpRequestException("Login failed", ResponseCode);
            }

            if (!string.IsNullOrWhiteSpace(AniDbUdpState.SessionKey))
            {
                Args["s"] = AniDbUdpState.SessionKey;
            }
            else
            {
                ResponseCode = AniDbResponseCode.InvalidSession;
                throw new AniDbUdpRequestException("Failed to get new session", ResponseCode);
            }
        }

        foreach (var (name, param) in Args)
            requestBuilder.Append($"{name}={Regex.Replace(HttpUtility.HtmlEncode(param), @"\r?\n|\r", "<br />")}&");
        // Removes the extra & at end of parameters
        requestBuilder.Length--;
        _requestText = requestBuilder.ToString();
    }


    /// <exception cref="AniDbUdpRequestException"></exception>
    private async Task ReceiveResponse()
    {
        ResponseText = null;
        ResponseCode = null;
        ResponseCodeString = null;
        byte[] receivedBytes;
        Logger.LogTrace("Waiting to receive raw UDP response");
        var receiveTimeout = TimeSpan.FromSeconds(10);
        try
        {
            using var cancelSource = new CancellationTokenSource(receiveTimeout);
            receivedBytes = (await AniDbUdpState.UdpClient.ReceiveAsync(cancelSource.Token)).Buffer;
        }
        catch (OperationCanceledException)
        {
            ResponseCode = AniDbResponseCode.Timeout;
            Logger.LogWarning("Failed to receive a response before timeout ({Timeout}s)", receiveTimeout.TotalSeconds);
            return;
        }

        Logger.LogTrace("Got raw UDP response");
        // Two null bytes and two bytes of Zlib header, seems to ignore trailer automatically
        // ReSharper disable once UseAwaitUsing
        using Stream memStream = receivedBytes.Length > 2 && receivedBytes[0] == 0 && receivedBytes[1] == 0
            ? new DeflateStream(new MemoryStream(receivedBytes, 4, receivedBytes.Length - 4), CompressionMode.Decompress)
            : new MemoryStream(receivedBytes);
        using var reader = new StreamReader(memStream, Encoding);
        // ReSharper disable once MethodHasAsyncOverload
        var codeLine = reader.ReadLine();
        // ReSharper disable once MethodHasAsyncOverload
        ResponseText = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(codeLine) || codeLine.Length <= 2)
            throw new AniDbUdpRequestException("AniDB response is empty");
        Logger.LogDebug("Received AniDB UDP response {CodeString}", codeLine);
        ResponseCode = (AniDbResponseCode)int.Parse(codeLine[..3]);
        if (codeLine.Length >= 5)
            ResponseCodeString = codeLine[4..];
    }

    /// <summary>
    ///     For AniDB general errors
    /// </summary>
    /// <returns>True if need to retry</returns>
    /// <exception cref="AniDbUdpRequestException"></exception>
    private bool HandleSharedErrors()
    {
        switch (ResponseCode)
        {
            case null:
                Logger.LogError("Can't handle possible error, no error response code");
                throw new AniDbUdpRequestException("No response code from AniDB");
            case AniDbResponseCode.OutOfService:
                Logger.LogWarning("AniDB out of service or in maintenance");
                throw new AniDbUdpRequestException("AniDB out of service/maintenance", ResponseCode);
            case AniDbResponseCode.ServerBusy:
                Logger.LogWarning("Server busy, try again later");
                throw new AniDbUdpRequestException("Server busy, try again later", ResponseCode);
            case AniDbResponseCode.Banned:
                AniDbUdpState.Banned = true;
                AniDbUdpState.BanReason = ResponseText;
                AniDbUdpState.ResetBannedTimer();
                Logger.LogWarning("Banned: {BanReason}, waiting {Hours}hr {Minutes}min ({UnbanTime})", AniDbUdpState.BanReason, AniDbUdpState.BanPeriod.Hours,
                    AniDbUdpState.BanPeriod.Minutes, DateTimeOffset.Now + AniDbUdpState.BanPeriod);
                throw new AniDbUdpRequestException($"Udp banned, waiting until {DateTimeOffset.Now + AniDbUdpState.BanPeriod}", ResponseCode);
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
                throw new AniDbUdpRequestException("Access was denied", ResponseCode);
            case AniDbResponseCode.Timeout:
                return true;
            case AniDbResponseCode.InternalServerError or (> AniDbResponseCode.ServerBusy and < (AniDbResponseCode)700):
                Logger.LogCritical("AniDB Server CRITICAL ERROR {ErrorCode} : {ErrorCodeStr}", ResponseCode, ResponseCodeString);
                throw new AniDbUdpRequestException($"Critical error with server {ResponseCode} {ResponseCodeString}", ResponseCode);
            case AniDbResponseCode.UnknownCommand:
                Logger.LogError("Uknown command, {Command}, {RequestText}", Command, _requestText);
                throw new AniDbUdpRequestException("Unknown AniDB command, check logs", ResponseCode);
            case AniDbResponseCode.IllegalInputOrAccessDenied:
                Logger.LogError("Illegal input or access is denied, {Command}, {RequestText}", Command, _requestText);
                throw new AniDbUdpRequestException("Illegal AniDB input, check logs", ResponseCode);
            default:
                if (!Enum.IsDefined(typeof(AniDbResponseCode), ResponseCode))
                {
                    Logger.LogError("Response Code {ResponseCode} not found in enumeration: Code string: {CodeString}", ResponseCode,
                        ResponseCodeString);
                    throw new AniDbUdpRequestException($"Unknown response code: {ResponseCode}: {ResponseCodeString}");
                }

                return false;
        }
    }
}
