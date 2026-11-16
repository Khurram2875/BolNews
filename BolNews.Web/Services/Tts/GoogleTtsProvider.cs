using System.Net.Http.Json;
using System.Text.Json;
using BolNews.Domain.Entities;

namespace BolNews.Web.Services.Tts;

public sealed class GoogleTtsProvider(IHttpClientFactory factory) : ITtsProvider
{
    public string Id => TtsSettings.Google;
    public string Label => "Google Cloud Text-to-Speech";
    public int MaxChunkChars => 1500; // the API limit is 5000 bytes; Urdu uses two bytes per character
    public bool NeedsRegion => false;
    public string? DefaultModel => null;
    public string? DefaultVoice(string languageCode) => languageCode == "ur" ? "ur-IN-Standard-A" : "en-US-Neural2-C";
    public string VoiceHelp => "Voice name, for example ur-IN-Standard-A or ur-IN-Wavenet-A (Urdu), en-US-Neural2-C (English). Empty = default for the site language.";
    public string KeyHelp => "API key from a Google Cloud project with the Cloud Text-to-Speech API enabled.";

    public async Task<byte[]> SynthesizeAsync(string text, TtsSettings settings, string apiKey, string languageCode, CancellationToken ct)
    {
        var voice = settings.Voice ?? DefaultVoice(languageCode)!;
        var parts = voice.Split('-');
        var voiceLang = parts.Length >= 2 ? parts[0] + "-" + parts[1] : (languageCode == "ur" ? "ur-IN" : "en-US");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://texttospeech.googleapis.com/v1/text:synthesize")
        {
            Content = JsonContent.Create(new
            {
                input = new { text },
                voice = new { languageCode = voiceLang, name = voice },
                audioConfig = new { audioEncoding = "MP3" }
            })
        };
        request.Headers.Add("X-Goog-Api-Key", apiKey);
        using var response = await TtsHttp.SendAsync(factory, request, Label, ct);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        if (!json.RootElement.TryGetProperty("audioContent", out var audio) || audio.GetString() is not { Length: > 0 } base64)
            throw new TtsException("Google returned no audio.");
        return Convert.FromBase64String(base64);
    }
}
