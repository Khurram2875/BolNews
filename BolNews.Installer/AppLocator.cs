using System.Text.Json;

namespace CmsInstaller;

public sealed record AppInfo(string Directory, string AssemblyName, bool SelfContained)
{
    public string DllPath => Path.Combine(Directory, AssemblyName + ".dll");
    public string ExePath => Path.Combine(Directory, AssemblyName);
}

/// <summary>Finds the published website (the folder that holds the application dll, its runtimeconfig and wwwroot).</summary>
public static class AppLocator
{
    /// <summary>Returns the website in <paramref name="directory"/>, or null when it does not look like a published website.</summary>
    public static AppInfo? Inspect(string directory)
    {
        try
        {
            if (!Directory.Exists(directory) || !Directory.Exists(Path.Combine(directory, "wwwroot"))) return null;
            var configs = Directory.GetFiles(directory, "*.runtimeconfig.json");
            AppInfo? best = null;
            foreach (var config in configs)
            {
                var name = Path.GetFileName(config)[..^".runtimeconfig.json".Length];
                if (!File.Exists(Path.Combine(directory, name + ".dll"))) continue;
                if (!File.Exists(Path.Combine(directory, name + ".deps.json"))) continue;
                bool selfContained;
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(config));
                    selfContained = doc.RootElement.TryGetProperty("runtimeOptions", out var options) && options.TryGetProperty("includedFrameworks", out _);
                }
                catch (JsonException) { continue; }
                var info = new AppInfo(Path.GetFullPath(directory), name, selfContained);
                // Prefer the assembly that ends in ".Web" (the website) over helper tools published beside it.
                if (best is null || name.EndsWith(".Web", StringComparison.OrdinalIgnoreCase)) best = info;
            }
            return best;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return null; }
    }

    public static List<AppInfo> Scan(IEnumerable<string> extraDirectories)
    {
        var roots = new List<string>(extraDirectories);
        foreach (var parent in new[] { "/var/www", "/opt", "/srv", "/home" })
        {
            try { if (Directory.Exists(parent)) roots.AddRange(Directory.GetDirectories(parent)); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
        roots.Add(Environment.CurrentDirectory);
        var found = new List<AppInfo>();
        foreach (var root in roots.Select(r => Path.GetFullPath(r)).Distinct())
        {
            var info = Inspect(root);
            if (info is not null && !found.Any(f => f.Directory == info.Directory)) found.Add(info);
        }
        return found;
    }
}
