using System.Net;
using System.Text;
using BolNews.Domain.Entities;

namespace BolNews.Web.Services.Tts;

public sealed class AzureTtsProvider(IHttpClientFactory factory) : ITtsProvider
{
    public string Id => TtsSettings.Azure;
    public string Label => "Microsoft Azure Speech";
    public int MaxChunkChars => 3000;
    public bool NeedsRegion => true;
    public string? DefaultModel => null;
    public string? DefaultVoice(string languageCode) => languageCode == "ur" ? "ur-PK-UzmaNeural" : "en-US-JennyNeural";
    public string VoiceHelp => "Neural voice name, for example ur-PK-UzmaNeural or ur-PK-AsadNeural (Urdu), en-US-JennyNeural (English). Empty = default for the site language.";
    public string KeyHelp => "Key 1 or Key 2 of an Azure Speech resource, plus its region (for example southeastasia or uaenorth).";

    public async Task<byte[]> SynthesizeAsync(string text, TtsSettings settings, string apiKey, string languageCode, CancellationToken ct)
    {
        var region = settings.Region ?? throw new TtsException("Azure Speech needs a region.");
        if (!region.All(c => char.IsLetterOrDigit(c))) throw new TtsException("The Azure region must be letters and digits only, for example southeastasia.");
        var voice = settings.Voice ?? DefaultVoice(languageCode)!;
        var xmlLang = voice.Length >= 5 && voice[2] == '-' ? voice[..5] : (languageCode == "ur" ? "ur-PK" : "en-US");
        var ssml = $"<speak version='1.0' xml:lang='{WebUtility.HtmlEncode(xmlLang)}'><voice name='{WebUtility.HtmlEncode(voice)}'>{WebUtility.HtmlEncode(text)}</voice></speak>";

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{region}.tts.speech.microsoft.com/cognitiveservices/v1")
        {
            Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml")
        };
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        request.Headers.Add("X-Microsoft-OutputFormat", "audio-24khz-96kbitrate-mono-mp3");
        request.Headers.UserAgent.ParseAdd("BolNewsCms");
        using var response = await TtsHttp.SendAsync(factory, request, Label, ct);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
