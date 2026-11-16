using System.Text.Json;
using System.Text.Json.Serialization;

namespace BolNews.Domain.Entities;

/// <summary>
/// Optional server-side text-to-speech for article read-aloud. When no provider is chosen (or the key is missing)
/// the article page falls back to the visitor's browser voices. The API key is only ever stored encrypted
/// (<see cref="ApiKeyProtected"/>); <see cref="NewApiKey"/> exists just to carry a freshly typed key from the form.
/// </summary>
public class TtsSettings
{
    public const string ElevenLabs = "elevenlabs";
    public const string Azure = "azure";
    public const string Google = "google";
    public static readonly string[] Providers = { ElevenLabs, Azure, Google };

    /// <summary>Empty = browser voices only.</summary>
    public string Provider { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? Voice { get; set; }
    public string? Model { get; set; }
    /// <summary>Maximum characters sent to the provider per calendar month (UTC). 0 = no limit.</summary>
    public int MonthlyCharacterLimit { get; set; }
    public string? ApiKeyProtected { get; set; }

    [JsonIgnore] public string? NewApiKey { get; set; }
    [JsonIgnore] public bool ClearApiKey { get; set; }

    [JsonIgnore] public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKeyProtected);

    /// <summary>True when the article page should offer provider audio instead of browser voices.</summary>
    [JsonIgnore]
    public bool IsConfigured =>
        Providers.Contains(Provider) && HasApiKey &&
        (Provider != Azure || !string.IsNullOrWhiteSpace(Region)) &&
        (Provider != ElevenLabs || !string.IsNullOrWhiteSpace(Voice));

    public string ToJson() => JsonSerializer.Serialize(this);

    public static TtsSettings FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new TtsSettings();
        try { return Normalize(JsonSerializer.Deserialize<TtsSettings>(json)); }
        catch (JsonException) { return new TtsSettings(); }
    }

    public static TtsSettings Normalize(TtsSettings? value)
    {
        var s = value ?? new TtsSettings();
        s.Provider = (s.Provider ?? "").Trim().ToLowerInvariant();
        if (!Providers.Contains(s.Provider)) s.Provider = string.Empty;
        s.Region = string.IsNullOrWhiteSpace(s.Region) ? null : s.Region.Trim().ToLowerInvariant();
        s.Voice = string.IsNullOrWhiteSpace(s.Voice) ? null : s.Voice.Trim();
        s.Model = string.IsNullOrWhiteSpace(s.Model) ? null : s.Model.Trim();
        s.MonthlyCharacterLimit = Math.Clamp(s.MonthlyCharacterLimit, 0, 100_000_000);
        return s;
    }
}
