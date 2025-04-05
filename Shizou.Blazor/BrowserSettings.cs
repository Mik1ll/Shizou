using System.Text.Json;
using Microsoft.JSInterop;

namespace Shizou.Blazor;

public class BrowserSettings
{
    private const string SettingsKey = "shizou.settings";

    public string ExternalPlayerScheme { get; set; } = "shizou:";

    public bool ExternalPlayerInstalled { get; set; }

    public static async Task<BrowserSettings> GetSettingsAsync(IJSRuntime jsRuntime)
    {
        var elem = await jsRuntime.InvokeAsync<JsonElement>("window.localStorage.getItem", SettingsKey);
        try
        {
            if (elem.ValueKind is JsonValueKind.String && elem.GetString() is { } value)
                return JsonSerializer.Deserialize<BrowserSettings>(value) ?? new BrowserSettings();
        }
        catch (JsonException)
        {
        }

        return new BrowserSettings();
    }

    public async Task SaveSettingsAsync(IJSRuntime jsRuntime)
    {
        await jsRuntime.InvokeVoidAsync("window.localStorage.setItem", SettingsKey, JsonSerializer.Serialize(this));
    }
}
