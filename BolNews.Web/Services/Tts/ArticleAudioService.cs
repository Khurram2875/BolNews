using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Web.Services.Tts;

public sealed record ArticleAudioResult(string Url, int Characters);

/// <summary>
/// Creates, stores and reuses provider audio for published articles. The file name contains a hash of the spoken text and
/// the voice settings, so editing an article (or changing the voice) makes the next listen generate fresh audio and the
/// old file is deleted. Returns null when no provider is configured so the page falls back to browser voices.
/// </summary>
public sealed class ArticleAudioService(
    AppDbContext db,
    SiteProfileService profiles,
    TtsKeyProtector protector,
    TtsUsageTracker usage,
    IEnumerable<ITtsProvider> providers,
    IWebHostEnvironment env,
    ILogger<ArticleAudioService> logger)
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> Locks = new();

    public ITtsProvider? Provider(string id) => providers.FirstOrDefault(p => p.Id == id);

    public async Task<ArticleAudioResult?> GetOrCreateAsync(int articleId, CancellationToken ct)
    {
        var profile = await profiles.GetAsync(ct);
        var tts = profile.Tts;
        if (!tts.IsConfigured || Provider(tts.Provider) is not { } provider) return null;

        var article = await db.Articles.AsNoTracking()
            .Where(a => a.Id == articleId && a.IsPublished && a.WorkflowStatus == ArticleWorkflowStatus.Published)
            .Select(a => new { a.Title, a.Summary, a.Content })
            .FirstOrDefaultAsync(ct);
        if (article is null) return null;

        var language = profile.LanguageCode == "ur" ? "ur" : "en";
        var text = ArticleSpeechText.Build(article.Title, article.Summary, article.Content, language == "ur");
        if (string.IsNullOrWhiteSpace(text)) return null;

        var voice = tts.Voice ?? provider.DefaultVoice(language) ?? "";
        var hash = Hash($"{provider.Id}|{voice}|{tts.Model ?? provider.DefaultModel}|{tts.Region}|{language}|{text}");
        var directory = Path.Combine(env.WebRootPath, "uploads", "audio", articleId.ToString());
        var file = Path.Combine(directory, hash + ".mp3");
        var url = $"/uploads/audio/{articleId}/{hash}.mp3";
        if (File.Exists(file)) return new ArticleAudioResult(url, text.Length);

        var gate = Locks.GetOrAdd(articleId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            if (File.Exists(file)) return new ArticleAudioResult(url, text.Length);

            var apiKey = protector.Unprotect(tts.ApiKeyProtected)
                ?? throw new TtsException("The saved API key could not be read. Enter it again in Publisher Settings.");
            usage.EnsureWithinLimit(tts.MonthlyCharacterLimit, text.Length);

            var bytes = await SynthesizeAsync(provider, text, tts, apiKey, language, ct);

            Directory.CreateDirectory(directory);
            var temp = file + ".tmp";
            await File.WriteAllBytesAsync(temp, bytes, ct);
            File.Move(temp, file, overwrite: true);
            usage.Add(text.Length);

            // Earlier versions of this article's audio are no longer valid.
            foreach (var old in Directory.GetFiles(directory, "*.mp3").Where(f => !string.Equals(f, file, StringComparison.OrdinalIgnoreCase)))
            {
                try { File.Delete(old); } catch (IOException) { }
            }
            return new ArticleAudioResult(url, text.Length);
        }
        catch (TtsException ex)
        {
            logger.LogWarning("Text-to-speech failed for article {ArticleId}: {Message}", articleId, ex.Message);
            throw;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Splits long text on sentence ends, calls the provider per piece and joins the MP3 data.</summary>
    public static async Task<byte[]> SynthesizeAsync(ITtsProvider provider, string text, TtsSettings tts, string apiKey, string language, CancellationToken ct)
    {
        using var output = new MemoryStream();
        foreach (var chunk in Chunk(text, provider.MaxChunkChars))
        {
            var bytes = await provider.SynthesizeAsync(chunk, tts, apiKey, language, ct);
            output.Write(bytes, 0, bytes.Length);
        }
        return output.ToArray();
    }

    public static IEnumerable<string> Chunk(string text, int max)
    {
        var current = new StringBuilder();
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var piece in line.Length <= max ? new[] { line } : SplitLong(line, max))
            {
                if (current.Length > 0 && current.Length + 1 + piece.Length > max)
                {
                    yield return current.ToString();
                    current.Clear();
                }
                if (current.Length > 0) current.Append('\n');
                current.Append(piece);
            }
        }
        if (current.Length > 0) yield return current.ToString();
    }

    private static IEnumerable<string> SplitLong(string line, int max)
    {
        var current = new StringBuilder();
        foreach (var word in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.Length > 0 && current.Length + 1 + word.Length > max)
            {
                yield return current.ToString();
                current.Clear();
            }
            if (current.Length > 0) current.Append(' ');
            current.Append(word.Length > max ? word[..max] : word);
        }
        if (current.Length > 0) yield return current.ToString();
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24].ToLowerInvariant();
}
