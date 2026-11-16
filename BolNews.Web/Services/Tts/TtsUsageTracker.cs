using System.Text.Json;

namespace BolNews.Web.Services.Tts;

/// <summary>Counts characters sent to the provider per UTC month in a small file next to the audio, so a monthly limit can stop surprise bills. Single-server design.</summary>
public sealed class TtsUsageTracker(IWebHostEnvironment env)
{
    private static readonly object Gate = new();
    private sealed record Usage(string Month, long Characters);

    private string FilePath => Path.Combine(env.WebRootPath, "uploads", "audio", "_usage.json");
    private static string CurrentMonth => DateTime.UtcNow.ToString("yyyy-MM");

    private Usage Read()
    {
        try
        {
            if (File.Exists(FilePath) && JsonSerializer.Deserialize<Usage>(File.ReadAllText(FilePath)) is { } u && u.Month == CurrentMonth) return u;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { }
        return new Usage(CurrentMonth, 0);
    }

    public long UsedThisMonth() { lock (Gate) return Read().Characters; }

    /// <summary>Throws when this request would pass the limit (limit 0 = unlimited).</summary>
    public void EnsureWithinLimit(int limit, int characters)
    {
        if (limit <= 0) return;
        lock (Gate)
        {
            if (Read().Characters + characters > limit)
                throw new TtsException("The monthly text-to-speech character limit has been reached.");
        }
    }

    public void Add(int characters)
    {
        lock (Gate)
        {
            var u = Read();
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(u with { Characters = u.Characters + characters }));
        }
    }
}
