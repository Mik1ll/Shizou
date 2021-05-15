using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;

namespace Shizou.AniDbApi
{
    public abstract class AniDbUdpRequest
    {
        private readonly UdpClient _client;
        private readonly UdpRateLimiter _rateLimiter;
        private readonly string _sessionKey;
        protected readonly ILogger<AniDbUdpRequest> Logger;

        protected AniDbUdpRequest(UdpClient client, ILogger<AniDbUdpRequest> logger, UdpRateLimiter rateLimiter, string sessionKey)
        {
            Logger = logger;
            _rateLimiter = rateLimiter;
            _client = client;
            _sessionKey = sessionKey;
        }

        public abstract string CommandText { get; }
        public abstract List<(string name, string value)> Params { get; }

        public string? ResponseText { get; protected set; }
        public bool Errored { get; protected set; }
        public string? ErrorText { get; protected set; }
        public AniDbResponseCode? ResponseCode { get; protected set; }
        public string? ResponseCodeString { get; protected set; }
        public Encoding Encoding { get; } = Encoding.UTF8;

        public async Task SendRequest()
        {
            var requestBuilder = new StringBuilder(CommandText + " ");
            if (!new List<string> {"PING", "ENCRYPT", "AUTH", "VERSION"}.Contains(CommandText))
                Params.Add(("s", _sessionKey));
            // TODO: Check if AUTH allows passwords with special characters
            foreach (var (name, param) in Params)
                requestBuilder.Append($"{name}={Regex.Replace(HttpUtility.HtmlEncode(param), @"\r?\n|\r", "<br />")}&");
            // Removes the extra & at end of parameters
            requestBuilder.Length--;
            var requestText = requestBuilder.ToString();
            Logger.LogInformation("Sending AniDb UDP text: {requestText}", requestText);
            var dgramBytes = Encoding.GetBytes(requestText);
            await _rateLimiter.EnsureRate();
            try
            {
                await _client.SendAsync(dgramBytes, dgramBytes.Length);
            }
            catch (Exception ex)
            {
                Errored = true;
                ErrorText = ex.Message;
                Logger.LogError(ex, "Error sending data: {exceptionMsg}", ex.Message);
                return;
            }
            try
            {
                var receivedBytes = (await _client.ReceiveAsync()).Buffer;
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
                if (ResponseCode == AniDbResponseCode.InternalServerError || ResponseCode > AniDbResponseCode.ServerBusy && (int)ResponseCode < 700)
                {
                    Errored = true;
                    ErrorText = "Critical server error";
                    Logger.LogCritical("AniDB Server CRITICAL ERROR {errorCode} : {errorText}", ResponseCode, ResponseCodeString);
                    return;
                }
                if (!Enum.IsDefined(typeof(AniDbResponseCode), ResponseCode))
                    throw new KeyNotFoundException($"Response Code {ResponseCode} not found in enumeration");
            }
            catch (Exception ex)
            {
                Errored = true;
                ErrorText = ex.Message;
                Logger.LogError(ex, "Error receiving data: {exceptionMsg}", ex.Message);
            }
            // TODO: handle server 6xx errors
        }
    }
}
