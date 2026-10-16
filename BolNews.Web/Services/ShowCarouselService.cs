using System.Text.Json;

namespace BolNews.Web.Services;

public sealed class ShowCarouselService(IWebHostEnvironment environment)
{
    private static readonly string[] Extensions = [".webp", ".jpg", ".jpeg", ".png"];
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string Folder => Path.Combine(environment.WebRootPath, "images", "shows");
    private string Manifest => Path.Combine(Folder, "carousel.json");

    public async Task<List<string>> GetImagesAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var files = GetAvailableFiles();
            if (!File.Exists(Manifest)) return files;
            await using var stream = File.OpenRead(Manifest);
            var chosen = await JsonSerializer.DeserializeAsync<List<string>>(stream) ?? [];
            return chosen.Where(IsSafeName).Where(files.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
        finally { _gate.Release(); }
    }

    public List<string> GetAvailableFiles()
    {
        Directory.CreateDirectory(Folder);
        return Directory.GetFiles(Folder).Select(Path.GetFileName).Where(x => x != null && IsSafeName(x))
            .Select(x => x!).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task SaveOrderAsync(IEnumerable<string> names)
    {
        await _gate.WaitAsync();
        try
        {
            Directory.CreateDirectory(Folder);
            var available = GetAvailableFiles().ToHashSet(StringComparer.OrdinalIgnoreCase);
            var safe = names.Where(IsSafeName).Where(available.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            await WriteManifestAsync(safe);
        }
        finally { _gate.Release(); }
    }

    public async Task<string> SaveUploadAsync(IFormFile upload)
    {
        var extension = Path.GetExtension(upload.FileName).ToLowerInvariant();
        if (!Extensions.Contains(extension)) throw new InvalidDataException("Upload a WEBP, JPG, JPEG, or PNG image.");
        if (upload.Length == 0 || upload.Length > 15 * 1024 * 1024) throw new InvalidDataException("Image must be between 1 byte and 15 MB.");
        Directory.CreateDirectory(Folder);
        var name = $"{Guid.NewGuid():N}{extension}";
        await using var output = File.Create(Path.Combine(Folder, name));
        await upload.CopyToAsync(output);
        return name;
    }

    public async Task<bool> DeleteAsync(string name)
    {
        if (!IsSafeName(name)) return false;
        await _gate.WaitAsync();
        try
        {
            var path = Path.Combine(Folder, name);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            if (File.Exists(Manifest))
            {
                try
                {
                    await using var input = File.OpenRead(Manifest);
                    var current = await JsonSerializer.DeserializeAsync<List<string>>(input) ?? [];
                    var remaining = current.Where(x => !string.Equals(x, name, StringComparison.OrdinalIgnoreCase)).ToList();
                    await WriteManifestAsync(remaining);
                }
                catch (IOException) { /* Missing images are filtered out when the carousel is read. */ }
                catch (UnauthorizedAccessException) { /* Missing images are filtered out when the carousel is read. */ }
            }
            return true;
        }
        finally { _gate.Release(); }
    }

    private async Task WriteManifestAsync(List<string> names)
    {
        // Some hosts allow modifying a file but deny deleting/replacing directory entries.
        // Truncate and rewrite in place instead of File.Move(overwrite: true).
        await using var output = new FileStream(Manifest, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(output, names);
    }

    private static bool IsSafeName(string name) => !string.IsNullOrWhiteSpace(name) &&
        string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal) && Extensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase);
}
