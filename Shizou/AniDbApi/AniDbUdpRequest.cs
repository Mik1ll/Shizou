using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;

namespace Shizou.AniDbApi
{
    public abstract class AniDbUdpRequest
    {
        private readonly AniDbUdp _udpApi;
        protected readonly ILogger<AniDbUdpRequest> Logger;

        protected AniDbUdpRequest(ILogger<AniDbUdpRequest> logger, AniDbUdp udpApi)
        {
            Logger = logger;
            _udpApi = udpApi;
        }

        public abstract string Command { get; }
        public abstract List<(string name, string value)> Params { get; }

        public string? ResponseText { get; protected set; }
        public bool Errored { get; protected set; }
        public AniDbResponseCode? ResponseCode { get; protected set; }
        public string? ResponseCodeString { get; protected set; }
        public Encoding Encoding { get; } = Encoding.UTF8;

        public abstract Task Process();

        public async Task SendRequest()
        {
            var requestBuilder = new StringBuilder(Command + " ");
            if (!new List<string> {"PING", "ENCRYPT", "AUTH", "VERSION"}.Contains(Command))
                if (!string.IsNullOrWhiteSpace(_udpApi.SessionKey))
                {
                    Params.Add(("s", _udpApi.SessionKey));
                }
                else
                {
                    Errored = true;
                    ResponseCode = AniDbResponseCode.InvalidSession;
                    return;
                }
            // TODO: Check if AUTH allows passwords with special characters
            foreach (var (name, param) in Params)
                requestBuilder.Append($"{name}={Regex.Replace(HttpUtility.HtmlEncode(param), @"\r?\n|\r", "<br />")}&");
            // Removes the extra & at end of parameters
            requestBuilder.Length--;
            var requestText = requestBuilder.ToString();
            Logger.LogInformation("Sending AniDb UDP text: {requestText}", requestText);
            var dgramBytes = Encoding.GetBytes(requestText);
            await _udpApi.RateLimiter.EnsureRate();
            try
            {
                await _udpApi.UdpClient.SendAsync(dgramBytes, dgramBytes.Length);
            }
            catch (Exception ex)
            {
                Errored = true;
                Logger.LogError(ex, "Error sending data: {exceptionMsg}", ex.Message);
                return;
            }
            try
            {
                var receivedBytes = (await _udpApi.UdpClient.ReceiveAsync()).Buffer;
                Stream memStream = new MemoryStream(receivedBytes);
                if (receivedBytes.Length > 2 && receivedBytes[0] == 0 && receivedBytes[1] == 0)
                    memStream = new DeflateStream(memStream, CompressionMode.Decompress);
                using var reader = new StreamReader(memStream, Encoding);
                var codeLine = reader.ReadLine();
                ResponseText = reader.ReadToEnd();
                if (codeLine is null || codeLine.Length <= 2)
                    throw new InvalidOperationException("AniDB response is empty");
                ResponseCode = (AniDbResponseCode)int.Parse(codeLine[..3]);
                if (codeLine.Length >= 5)
                    ResponseCodeString = codeLine[4..];
                HandleSharedErrors();
            }
            catch (Exception ex)
            {
                Errored = true;
                Logger.LogError(ex, "Error receiving data: {exceptionMsg}", ex.Message);
            }
        }

        private void HandleSharedErrors()
        {
            if (Errored)
                switch (ResponseCode)
                {
                    // No response
                    case null:
                        break;
                    case AniDbResponseCode.ServerBusy:
                        break;
                    case AniDbResponseCode.Banned:
                        _udpApi.Banned = true;
                        _udpApi.BanReason = ResponseText;
                        Logger.LogWarning("Banned: {banReason}, waiting {hours}hr {minutes}min ({unbanTime})", _udpApi.BanReason, _udpApi.BanPeriod.Hours,
                            _udpApi.BanPeriod.Minutes, DateTime.Now + _udpApi.BanPeriod);
                        break;
                    case AniDbResponseCode.InvalidSession:
                        Logger.LogWarning("Invalid session, reauth");
                        _udpApi.LoggedIn = false;
                        break;
                    case AniDbResponseCode.LoginFirst:
                        Logger.LogWarning("Not logged in, reauth");
                        _udpApi.LoggedIn = false;
                        break;
                    case AniDbResponseCode.AccessDenied:
                        Logger.LogError("Access denied");
                        break;
                    case AniDbResponseCode.InternalServerError or (> AniDbResponseCode.ServerBusy and < (AniDbResponseCode)700):
                        Logger.LogCritical("AniDB Server CRITICAL ERROR {errorCode} : {errorCodeStr}", ResponseCode, ResponseCodeString);
                        break;
                    case AniDbResponseCode.UnknownCommand:
                        Logger.LogError("Unknown command");
                        // TODO: decide what to do here
                        break;
                    case AniDbResponseCode.IllegalInputOrAccessDenied:
                        Logger.LogError("Illegal input or access is denied");
                        // TODO: decide what to do here
                        break;
                    default:
                        if (!Enum.IsDefined(typeof(AniDbResponseCode), ResponseCode))
                            Logger.LogError("Response Code {ResponseCode} not found in enumeration: Code string: {codeString}", ResponseCode,
                                ResponseCodeString);
                        break;
                }
        }
    }
}
