using System.Text;

namespace CmsInstaller;

/// <summary>File helpers that create secret files with restrictive permissions from the first byte and honour dry-run mode.</summary>
public static class FileWriter
{
    public static void EnsureDirectory(string path, string mode = "755")
    {
        if (Shell.DryRun) { Ui.Hint($"[dry run] would create folder {path}"); return; }
        Directory.CreateDirectory(path);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, ParseMode(mode));
    }

    public static async Task WriteSecretAsync(string path, string content, string directory, CancellationToken ct)
    {
        if (Shell.DryRun)
        {
            Ui.Hint($"[dry run] would write {path} (mode 600, owner root). Secrets are hidden here:");
            foreach (var line in content.Split('\n')) Ui.Hint("    " + MaskSecrets(line));
            return;
        }
        EnsureDirectory(directory, "750");
        var temp = path + ".tmp";
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, Share = FileShare.None };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        await using (var stream = new FileStream(temp, options))
        await using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            await writer.WriteAsync(content.Replace("\r\n", "\n"));
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(temp, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.Move(temp, path, overwrite: true);
    }

    public static void WriteText(string path, string content, string mode)
    {
        if (Shell.DryRun)
        {
            Ui.Hint($"[dry run] would write {path}:");
            foreach (var line in content.Split('\n')) Ui.Hint("    " + line);
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content.Replace("\r\n", "\n"), new UTF8Encoding(false));
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, ParseMode(mode));
    }

    public static void Copy(string from, string to)
    {
        if (Shell.DryRun) { Ui.Hint($"[dry run] would back up {from} to {to}"); return; }
        File.Copy(from, to, overwrite: true);
    }

    public static void Delete(string path)
    {
        if (Shell.DryRun) return;
        try { File.Delete(path); } catch (IOException) { }
    }

    public static string MaskSecrets(string line)
    {
        var eq = line.IndexOf('=');
        if (eq < 0 || line.StartsWith('#')) return line;
        var key = line[..eq];
        return key + "=\"********\"";
    }

    private static UnixFileMode ParseMode(string octal) => (UnixFileMode)Convert.ToInt32(octal, 8);
}
