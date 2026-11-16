using BolNews.Domain.Entities;

namespace BolNews.Web.Services.Tts;

/// <summary>A failure the publisher can act on (bad key, wrong voice, quota). Messages never contain the API key.</summary>
public sealed class TtsException(string message, Exception? inner = null) : Exception(message, inner);

public interface ITtsProvider
{
    string Id { get; }
    string Label { get; }
    /// <summary>Largest text (in characters) sent to the provider in one request; longer articles are sent in pieces and joined.</summary>
    int MaxChunkChars { get; }
    bool NeedsRegion { get; }
    string? DefaultModel { get; }
    /// <summary>Voice used when the publisher leaves the field empty; null when a voice must be chosen.</summary>
    string? DefaultVoice(string languageCode);
    string VoiceHelp { get; }
    string KeyHelp { get; }

    /// <summary>Returns MP3 bytes for the text.</summary>
    Task<byte[]> SynthesizeAsync(string text, TtsSettings settings, string apiKey, string languageCode, CancellationToken ct);
}
