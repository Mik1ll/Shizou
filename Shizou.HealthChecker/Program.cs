var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, _, _, _) => message.RequestUri is { IsLoopback: true },
};
var httpClient = new HttpClient(handler);
httpClient.DefaultRequestHeaders.ConnectionClose = true;

if (args.Length > 0 && Uri.TryCreate(args[0], UriKind.Absolute, out var uri))
{
    var resp = await httpClient.GetAsync(uri);
    Console.Write(await resp.Content.ReadAsStringAsync());
    return resp.IsSuccessStatusCode ? 0 : 1;
}

throw new ArgumentNullException(nameof(args), "A valid URI must be given as first argument");
