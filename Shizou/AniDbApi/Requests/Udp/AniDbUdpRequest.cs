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
    public Dictionary<string, string> Params { get; } = new();

    public string? RequestText { get; private set; }

    public string? ResponseText { get; protected set; }
    public bool Errored { get; protected set; }
    public AniDbResponseCode? ResponseCode { get; protected set; }
    public string? ResponseCodeString { get; protected set; }
    public Encoding Encoding { get; } = Encoding.UTF8;

    public static string DataUnescape(string data)
    {
        return Regex.Replace(data, @"<br\s*/>", "\n").Replace('`', '\'').Replace('/', '|');
    }

    public abstract Task Process();

    public async Task SendRequest()
    {
        await BuildAndSendRequest();
        if (!Errored)
        {
            await ReceiveResponse();
            if (HandleSharedErrors())
            {
                Errored = false;
                ResponseCode = null;
                ResponseText = null;
                ResponseCodeString = null;
                await BuildAndSendRequest();
                if (!Errored)
                    await ReceiveResponse();
            }
        }
    }

    private async Task BuildAndSendRequest()
    {
        var requestBuilder = new StringBuilder(Command + " ");
        if (!new List<string> { "PING", "ENCRYPT", "AUTH", "VERSION" }.Contains(Command))
        {
            if (!await AniDbUdpState.Login())
            {
                Errored = true;
                ResponseCode = AniDbResponseCode.LoginFailed;
                return;
            }
            if (!string.IsNullOrWhiteSpace(AniDbUdpState.SessionKey))
            {
                Params["s"] = AniDbUdpState.SessionKey;
            }
            else
            {
                Errored = true;
                ResponseCode = AniDbResponseCode.InvalidSession;
                return;
            }
        }
        foreach (var (name, param) in Params)
            requestBuilder.Append($"{name}={Regex.Replace(HttpUtility.HtmlEncode(param), @"\r?\n|\r", "<br />")}&");
        // Removes the extra & at end of parameters
        requestBuilder.Length--;
        RequestText = requestBuilder.ToString();
        var dgramBytes = Encoding.GetBytes(RequestText);
        await AniDbUdpState.RateLimiter.EnsureRate();
        if (AniDbUdpState.Banned)
        {
            Logger.LogWarning("Banned, aborting UDP request: {requestText}", RequestText);
            Errored = true;
            return;
        }
        Logger.LogInformation("Sending AniDb UDP text: {requestText}", RequestText);
        try
        {
            await AniDbUdpState.UdpClient.SendAsync(dgramBytes, dgramBytes.Length);
        }
        catch (Exception ex)
        {
            Errored = true;
            Logger.LogError(ex, "Error sending data: {exceptionMsg}", ex.Message);
        }
    }

    private async Task ReceiveResponse()
    {
        try
        {
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
            Errored = true;
            Logger.LogError(ex, "Error receiving data: {exceptionMsg}", ex.Message);
        }
    }

    /// <summary>
    ///     For AniDB general errors
    /// </summary>
    /// <returns>True if need to retry</returns>
    private bool HandleSharedErrors()
    {
        switch (ResponseCode)
        {
            case null:
                Logger.LogWarning("Can't handle possible error, no error response code");
                Errored = true;
                throw new ProcessorPauseException("No response code from AniDB");
            case AniDbResponseCode.OutOfService:
                Logger.LogWarning("AniDB out of service or in maintenance");
                Errored = true;
                throw new ProcessorPauseException("AniDB out of service/maintenance");
            case AniDbResponseCode.ServerBusy:
                Logger.LogWarning("Server busy, try again later");
                Errored = true;
                throw new ProcessorPauseException("Server busy, try again later");
            case AniDbResponseCode.Banned:
                AniDbUdpState.Banned = true;
                AniDbUdpState.BanReason = ResponseText;
                Logger.LogWarning("Banned: {banReason}, waiting {hours}hr {minutes}min ({unbanTime})", AniDbUdpState.BanReason, AniDbUdpState.BanPeriod.Hours,
                    AniDbUdpState.BanPeriod.Minutes, DateTime.Now + AniDbUdpState.BanPeriod);
                Errored = true;
                break;
            case AniDbResponseCode.InvalidSession:
                Logger.LogWarning("Invalid session, reauth");
                AniDbUdpState.LoggedIn = false;
                Errored = true;
                return true;
            case AniDbResponseCode.LoginFirst:
                Logger.LogWarning("Not logged in, reauth");
                AniDbUdpState.LoggedIn = false;
                Errored = true;
                return true;
            case AniDbResponseCode.AccessDenied:
                Logger.LogError("Access denied");
                Errored = true;
                throw new ProcessorPauseException("Access was denied");
            case AniDbResponseCode.InternalServerError or (> AniDbResponseCode.ServerBusy and < (AniDbResponseCode)700):
                Logger.LogCritical("AniDB Server CRITICAL ERROR {errorCode} : {errorCodeStr}", ResponseCode, ResponseCodeString);
                Errored = true;
                throw new ProcessorPauseException($"Critical error with server {ResponseCode} {ResponseCodeString}");
            case AniDbResponseCode.UnknownCommand:
                Logger.LogError("Uknown command, {Command}, {RequestText}", Command, RequestText);
                Errored = true;
                throw new ProcessorPauseException($"Unknown AniDB command, investigate {Command}");
            case AniDbResponseCode.IllegalInputOrAccessDenied:
                Logger.LogError("Illegal input or access is denied, {Command}, {RequestText}", Command, RequestText);
                Errored = true;
                throw new ProcessorPauseException($"Illegal AniDB input, investigate {Command}");
            default:
                if (!Enum.IsDefined(typeof(AniDbResponseCode), ResponseCode))
                {
                    Logger.LogError("Response Code {ResponseCode} not found in enumeration: Code string: {codeString}", ResponseCode,
                        ResponseCodeString);
                    Errored = true;
                    throw new ProcessorPauseException($"Unknown response code: {ResponseCode}");
                }
                break;
        }
        return false;
    }
}
