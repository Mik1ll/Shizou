using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Exceptions;

namespace Shizou.AniDbApi.Requests.Udp;

public abstract class AniDbUdpRequest
{
    protected readonly AniDbUdpState AniDbUdpState;
    protected readonly ILogger<AniDbUdpRequest> Logger;

    protected AniDbUdpRequest(IServiceProvider provider, string command)
    {
        Command = command;
        Logger = (ILogger<AniDbUdpRequest>)provider.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        AniDbUdpState = provider.GetRequiredService<AniDbUdpState>();
    }

    public string Command { get; }
    public Dictionary<string, string> Args { get; } = new();

    public Encoding Encoding { get; } = Encoding.UTF8;
    public string? RequestText { get; private set; }

    public string? ResponseText { get; protected set; }
    public AniDbResponseCode? ResponseCode { get; protected set; }
    public string? ResponseCodeString { get; protected set; }

    public static string DataUnescape(string data)
    {
        return Regex.Replace(data, @"<br\s*/>", "\n").Replace('`', '\'').Replace('/', '|');
    }

    public abstract Task Process();

    public async Task HandleRequest()
    {
        if (!await BuildAndSendRequest())
            return;
        await ReceiveResponse();
        var retry = HandleSharedErrors();
        if (retry)
        {
            if (!await BuildAndSendRequest())
                return;
            await ReceiveResponse();
            HandleSharedErrors();
        }
    }

    /// <summary>
    ///     Create and send an UDP request to AniDb
    /// </summary>
    /// <returns>True if request sent</returns>
    /// <exception cref="ProcessorPauseException"></exception>
    private async Task<bool> BuildAndSendRequest()
    {
        var requestBuilder = new StringBuilder(Command + " ");
        if (!new List<string> { "PING", "ENCRYPT", "AUTH", "VERSION" }.Contains(Command))
        {
            if (!await AniDbUdpState.Login())
            {
                ResponseCode = AniDbResponseCode.LoginFailed;
                throw new ProcessorPauseException("Login failed");
            }
            if (!string.IsNullOrWhiteSpace(AniDbUdpState.SessionKey))
            {
                Args["s"] = AniDbUdpState.SessionKey;
            }
            else
            {
                ResponseCode = AniDbResponseCode.InvalidSession;
                throw new ProcessorPauseException("Failed to get new session");
            }
        }
        foreach (var (name, param) in Args)
            requestBuilder.Append($"{name}={Regex.Replace(HttpUtility.HtmlEncode(param), @"\r?\n|\r", "<br />")}&");
        // Removes the extra & at end of parameters
        requestBuilder.Length--;
        RequestText = requestBuilder.ToString();
        var dgramBytes = Encoding.GetBytes(RequestText);
        await AniDbUdpState.RateLimiter.EnsureRate();
        if (AniDbUdpState.Banned)
        {
            Logger.LogWarning("Banned, aborting UDP request: {requestText}", RequestText);
            throw new ProcessorPauseException("Udp banned");
        }
        Logger.LogInformation("Sending AniDb UDP text: {requestText}", RequestText);
        try
        {
            await AniDbUdpState.UdpClient.SendAsync(dgramBytes, dgramBytes.Length);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error sending data: {exceptionMsg}", ex.Message);
        }
        return false;
    }

    private async Task ReceiveResponse()
    {
        try
        {
            ResponseText = null;
            ResponseCode = null;
            ResponseCodeString = null;
            var receivedBytes = (await AniDbUdpState.UdpClient.ReceiveAsync()).Buffer;
            // Two null bytes and two bytes of Zlib header, seems to ignore trailer automatically
            using Stream memStream = receivedBytes.Length > 2 && receivedBytes[0] == 0 && receivedBytes[1] == 0
                ? new DeflateStream(new MemoryStream(receivedBytes, 4, receivedBytes.Length - 4), CompressionMode.Decompress)
                : new MemoryStream(receivedBytes);
            using var reader = new StreamReader(memStream, Encoding);
            var codeLine = reader.ReadLine();
            ResponseText = reader.ReadToEnd();
            if (string.IsNullOrWhiteSpace(codeLine) || codeLine.Length <= 2)
                throw new InvalidOperationException("AniDB response is empty");
            Logger.LogInformation("Received AniDB UDP response {codeString}", codeLine);
            ResponseCode = (AniDbResponseCode)int.Parse(codeLine[..3]);
            if (codeLine.Length >= 5)
                ResponseCodeString = codeLine[4..];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error receiving data: {exceptionMsg}", ex.Message);
        }
    }

    /// <summary>
    ///     For AniDB general errors
    /// </summary>
    /// <returns>True if need to retry</returns>
    /// <exception cref="ProcessorPauseException"></exception>
    private bool HandleSharedErrors()
    {
        switch (ResponseCode)
        {
            case null:
                Logger.LogError("Can't handle possible error, no error response code");
                throw new ProcessorPauseException("No response code from AniDB");
            case AniDbResponseCode.OutOfService:
                Logger.LogWarning("AniDB out of service or in maintenance");
                throw new ProcessorPauseException("AniDB out of service/maintenance");
            case AniDbResponseCode.ServerBusy:
                Logger.LogWarning("Server busy, try again later");
                throw new ProcessorPauseException("Server busy, try again later");
            case AniDbResponseCode.Banned:
                AniDbUdpState.Banned = true;
                AniDbUdpState.BanReason = ResponseText;
                Logger.LogWarning("Banned: {banReason}, waiting {hours}hr {minutes}min ({unbanTime})", AniDbUdpState.BanReason, AniDbUdpState.BanPeriod.Hours,
                    AniDbUdpState.BanPeriod.Minutes, AniDbUdpState.BanEndTime);
                throw new ProcessorPauseException($"Udp banned, waiting until {AniDbUdpState.BanEndTime}");
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
                throw new ProcessorPauseException("Access was denied");
            case AniDbResponseCode.InternalServerError or (> AniDbResponseCode.ServerBusy and < (AniDbResponseCode)700):
                Logger.LogCritical("AniDB Server CRITICAL ERROR {errorCode} : {errorCodeStr}", ResponseCode, ResponseCodeString);
                throw new ProcessorPauseException($"Critical error with server {ResponseCode} {ResponseCodeString}");
            case AniDbResponseCode.UnknownCommand:
                Logger.LogError("Uknown command, {Command}, {RequestText}", Command, RequestText);
                throw new ProcessorPauseException("Unknown AniDB command, check logs");
            case AniDbResponseCode.IllegalInputOrAccessDenied:
                Logger.LogError("Illegal input or access is denied, {Command}, {RequestText}", Command, RequestText);
                throw new ProcessorPauseException("Illegal AniDB input, check logs");
            default:
                if (!Enum.IsDefined(typeof(AniDbResponseCode), ResponseCode))
                {
                    Logger.LogError("Response Code {ResponseCode} not found in enumeration: Code string: {codeString}", ResponseCode,
                        ResponseCodeString);
                    throw new ProcessorPauseException($"Unknown response code: {ResponseCode}: {ResponseCodeString}");
                }
                break;
        }
        return false;
    }
}
