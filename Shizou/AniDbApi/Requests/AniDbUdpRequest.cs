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
using Shizou.CommandProcessors;

namespace Shizou.AniDbApi.Requests;

public abstract class AniDbUdpRequest
{
    protected readonly AniDbUdp AniDbUdp;
    protected readonly AniDbUdpProcessor UdpProcessor;
    protected readonly ILogger<AniDbUdpRequest> Logger;

    protected AniDbUdpRequest(IServiceProvider provider)
    {
        Logger = (ILogger<AniDbUdpRequest>)provider.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        AniDbUdp = provider.GetRequiredService<AniDbUdp>();
        UdpProcessor = provider.GetRequiredService<AniDbUdpProcessor>();
    }

    public abstract string Command { get; }
    public abstract Dictionary<string, string> Params { get; }

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

    private async Task BuildAndSendRequest()
    {
        var requestBuilder = new StringBuilder(Command + " ");
        if (!new List<string> { "PING", "ENCRYPT", "AUTH", "VERSION" }.Contains(Command))
        {
            if (!await AniDbUdp.Login())
            {
                Errored = true;
                ResponseCode = AniDbResponseCode.LoginFailed;
                return;
            }
            if (!string.IsNullOrWhiteSpace(AniDbUdp.SessionKey))
            {
                Params["s"] = AniDbUdp.SessionKey;
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
        await AniDbUdp.RateLimiter.EnsureRate();
        Logger.LogInformation("Sending AniDb UDP text: {requestText}", RequestText);
        try
        {
            await AniDbUdp.UdpClient.SendAsync(dgramBytes, dgramBytes.Length);
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
            var receivedBytes = (await AniDbUdp.UdpClient.ReceiveAsync()).Buffer;
            Stream memStream;
            if (receivedBytes.Length > 2 && receivedBytes[0] == 0 && receivedBytes[1] == 0)
                // Two null bytes and two bytes of Zlib header, seems to ignore trailer automatically
                memStream = new DeflateStream(new MemoryStream(receivedBytes, 4, receivedBytes.Length - 4), CompressionMode.Decompress);
            else
                memStream = new MemoryStream(receivedBytes);
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
                UdpProcessor.Pause("No response code from AniDB");
                Errored = true;
                break;

            case AniDbResponseCode.OutOfService:
                Logger.LogWarning("AniDB out of service or in maintenance");
                UdpProcessor.Pause("AniDB out of service/maintenance");
                Errored = true;
                break;
            case AniDbResponseCode.ServerBusy:
                Logger.LogWarning("Server busy, try again later");
                UdpProcessor.Pause("Server busy, try again later");
                Errored = true;
                break;
            case AniDbResponseCode.Banned:
                AniDbUdp.Banned = true;
                AniDbUdp.BanReason = ResponseText;
                Logger.LogWarning("Banned: {banReason}, waiting {hours}hr {minutes}min ({unbanTime})", AniDbUdp.BanReason, AniDbUdp.BanPeriod.Hours,
                    AniDbUdp.BanPeriod.Minutes, DateTime.Now + AniDbUdp.BanPeriod);
                Errored = true;
                break;
            case AniDbResponseCode.InvalidSession:
                Logger.LogWarning("Invalid session, reauth");
                AniDbUdp.LoggedIn = false;
                Errored = true;
                return true;
            case AniDbResponseCode.LoginFirst:
                Logger.LogWarning("Not logged in, reauth");
                AniDbUdp.LoggedIn = false;
                Errored = true;
                return true;
            case AniDbResponseCode.AccessDenied:
                Logger.LogError("Access denied");
                UdpProcessor.Pause("Access was denied");
                Errored = true;
                break;
            case AniDbResponseCode.InternalServerError or (> AniDbResponseCode.ServerBusy and < (AniDbResponseCode)700):
                Logger.LogCritical("AniDB Server CRITICAL ERROR {errorCode} : {errorCodeStr}", ResponseCode, ResponseCodeString);
                UdpProcessor.Pause($"Critical error with server {ResponseCode} {ResponseCodeString}");
                Errored = true;
                break;
            case AniDbResponseCode.UnknownCommand:
                Logger.LogError("Uknown command, {Command}, {RequestText}", Command, RequestText);
                UdpProcessor.Pause($"Unknown AniDB command, investigate {Command}");
                Errored = true;
                break;
            case AniDbResponseCode.IllegalInputOrAccessDenied:
                Logger.LogError("Illegal input or access is denied, {Command}, {RequestText}", Command, RequestText);
                UdpProcessor.Pause($"Illegal AniDB input, investigate {Command}");
                Errored = true;
                break;
            default:
                if (!Enum.IsDefined(typeof(AniDbResponseCode), ResponseCode))
                {
                    Logger.LogError("Response Code {ResponseCode} not found in enumeration: Code string: {codeString}", ResponseCode,
                        ResponseCodeString);
                    UdpProcessor.Pause($"Unknown response code: {ResponseCode}");
                    Errored = true;
                }
                break;
        }
        return false;
    }
}
