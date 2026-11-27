using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace CmsInstaller;

public sealed record ShellResult(int ExitCode, string Output)
{
    public bool Ok => ExitCode == 0;
}

/// <summary>Runs external commands (apt, systemctl, a2enmod, mariadb...). Arguments are passed as a list, never through a shell.</summary>
public static class Shell
{
    public static bool DryRun { get; set; }
    private static readonly object LogLock = new();
    private static string? _logPath;

    public static void SetLog(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            _logPath = path;
        }
        catch { _logPath = null; }
    }

    public static void Log(string line)
    {
        if (_logPath is null || DryRun) return;
        try { lock (LogLock) File.AppendAllText(_logPath, $"{DateTime.UtcNow:u} {line}{Environment.NewLine}"); }
        catch { /* logging must never stop the installer */ }
    }

    /// <summary>Finds a program on PATH plus the sbin folders that root's PATH sometimes lacks.</summary>
    public static string? Which(string name)
    {
        var dirs = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(':', StringSplitOptions.RemoveEmptyEntries)
            .Concat(new[] { "/usr/local/sbin", "/usr/sbin", "/sbin", "/usr/local/bin", "/usr/bin", "/bin" });
        foreach (var dir in dirs)
        {
            var candidate = Path.Combine(dir, name);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    public static async Task<ShellResult> RunAsync(string file, IEnumerable<string> args, string? stdin = null,
        IDictionary<string, string>? env = null, int timeoutSeconds = 600, CancellationToken ct = default, bool changesSystem = true)
    {
        var argList = args.ToList();
        var display = file + " " + string.Join(' ', argList);
        if (DryRun && changesSystem)
        {
            Ui.Hint("[dry run] would run: " + display);
            return new ShellResult(0, string.Empty);
        }

        Log("RUN " + display);
        var info = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = stdin is not null,
            UseShellExecute = false, CreateNoWindow = true
        };
        foreach (var a in argList) info.ArgumentList.Add(a);
        info.Environment["DEBIAN_FRONTEND"] = "noninteractive";
        info.Environment["LC_ALL"] = "C";
        if (env is not null) foreach (var pair in env) info.Environment[pair.Key] = pair.Value;

        Process process;
        try { process = Process.Start(info) ?? throw new InvalidOperationException("The command could not be started."); }
        catch (Win32Exception ex)
        {
            Log("  could not start: " + ex.Message);
            return new ShellResult(127, $"{file}: {ex.Message}");
        }

        using (process)
        {
            var output = new StringBuilder();
            void OnData(object? _, DataReceivedEventArgs e) { if (e.Data is not null) lock (output) output.AppendLine(e.Data); }
            process.OutputDataReceived += OnData;
            process.ErrorDataReceived += OnData;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            if (stdin is not null)
            {
                await process.StandardInput.WriteAsync(stdin);
                process.StandardInput.Close();
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
                if (ct.IsCancellationRequested) throw;
                Log("  timed out");
                return new ShellResult(124, "The command took too long and was stopped.");
            }
            process.WaitForExit(); // flush the async output readers
            string text; lock (output) text = output.ToString().Trim();
            Log($"  exit {process.ExitCode}" + (text.Length > 0 ? ": " + Tail(text, 1500) : string.Empty));
            return new ShellResult(process.ExitCode, text);
        }
    }

    public static string Tail(string text, int max) => text.Length <= max ? text : "..." + text[^max..];
}
