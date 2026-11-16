using System.Net.Http.Json;
using BolNews.Domain.Entities;

namespace BolNews.Web.Services.Tts;

public sealed class ElevenLabsTtsProvider(IHttpClientFactory factory) : ITtsProvider
{
    public string Id => TtsSettings.ElevenLabs;
    public string Label => "ElevenLabs";
    public int MaxChunkChars => 2500;
    public bool NeedsRegion => false;
    // Eleven v4 is the model ElevenLabs lists as supporting Urdu; English works on every model.
    public string? DefaultModel => "eleven_v4";
    public string? DefaultVoice(string languageCode) => null;
    public string VoiceHelp => "Voice ID from your ElevenLabs Voice Library or My Voices (a long code, not the voice name).";
    public string KeyHelp => "API key from elevenlabs.io (Developers → API keys). It needs the Text to Speech permission.";

    public async Task<byte[]> SynthesizeAsync(string text, TtsSettings settings, string apiKey, string languageCode, CancellationToken ct)
    {
        var voice = Uri.EscapeDataString(settings.Voice ?? throw new TtsException("ElevenLabs needs a voice ID."));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.elevenlabs.io/v1/text-to-speech/{voice}?output_format=mp3_44100_128")
        {
            Content = JsonContent.Create(new { text, model_id = settings.Model ?? DefaultModel })
        };
        request.Headers.Add("xi-api-key", apiKey);
        request.Headers.Accept.ParseAdd("audio/mpeg");
        using var response = await TtsHttp.SendAsync(factory, request, Label, ct);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
