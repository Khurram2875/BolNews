using System.Text;

namespace CmsInstaller;

public sealed class InputClosedException : Exception
{
    public InputClosedException() : base("The keyboard input was closed before the installer finished.") { }
}

public sealed class UserQuitException : Exception
{
    public UserQuitException() : base("Installation stopped by the user.") { }
}

/// <summary>Console prompts. Every prompt re-asks on a typo instead of failing, so a mistake never ends the installer.</summary>
public static class Ui
{
    /// <summary>Set by the Ctrl+C handler; prompts stop politely when it is true.</summary>
    public static volatile bool Cancelled;

    private static void Write(string text, ConsoleColor? color)
    {
        if (color.HasValue && !Console.IsOutputRedirected) Console.ForegroundColor = color.Value;
        Console.WriteLine(text);
        if (color.HasValue && !Console.IsOutputRedirected) Console.ResetColor();
    }

    public static void Line(string text = "") => Console.WriteLine(text);
    public static void Info(string text) => Write("  " + text, null);
    public static void Good(string text) => Write("  [ok] " + text, ConsoleColor.Green);
    public static void Warn(string text) => Write("  [!] " + text, ConsoleColor.Yellow);
    public static void Fail(string text) => Write("  [x] " + text, ConsoleColor.Red);
    public static void Hint(string text) => Write("      " + text, ConsoleColor.DarkGray);

    public static void Title(string text)
    {
        Line();
        Write(new string('=', Math.Min(72, Math.Max(text.Length + 4, 40))), ConsoleColor.Cyan);
        Write("  " + text, ConsoleColor.Cyan);
        Write(new string('=', Math.Min(72, Math.Max(text.Length + 4, 40))), ConsoleColor.Cyan);
    }

    public static void Section(string text)
    {
        Line();
        Write(text, ConsoleColor.Cyan);
        Write(new string('-', Math.Min(72, text.Length)), ConsoleColor.DarkGray);
    }

    private static string? ReadLineChecked()
    {
        var line = Console.ReadLine();
        if (Cancelled) throw new OperationCanceledException();
        if (line is null) throw new InputClosedException();
        return line;
    }

    public static string Ask(string label, string? defaultValue = null, Func<string, string?>? validate = null, string? help = null)
    {
        if (help is not null) Hint(help);
        while (true)
        {
            Console.Write(defaultValue is null ? $"  {label}: " : $"  {label} [{defaultValue}]: ");
            var value = ReadLineChecked()!.Trim();
            if (value.Length == 0 && defaultValue is not null) value = defaultValue;
            if (value.Length == 0) { Warn("A value is required."); continue; }
            var problem = validate?.Invoke(value);
            if (problem is null) return value;
            Warn(problem);
        }
    }

    /// <summary>Optional answer: an empty line returns an empty string.</summary>
    public static string AskOptional(string label, string? help = null)
    {
        if (help is not null) Hint(help);
        Console.Write($"  {label}: ");
        return ReadLineChecked()!.Trim();
    }

    public static string AskSecret(string label, bool allowEmpty = false, int minLength = 0, string? help = null)
    {
        if (help is not null) Hint(help);
        while (true)
        {
            Console.Write($"  {label}: ");
            var value = ReadSecret();
            if (value.Length == 0 && allowEmpty) return value;
            if (value.Length == 0) { Warn("A value is required."); continue; }
            if (value.Length < minLength) { Warn($"Please use at least {minLength} characters."); continue; }
            return value;
        }
    }

    private static string ReadSecret()
    {
        if (Console.IsInputRedirected) return ReadLineChecked()!.Trim();
        var chars = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (Cancelled) throw new OperationCanceledException();
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (chars.Length > 0) { chars.Length--; Console.Write("\b \b"); }
                continue;
            }
            if (!char.IsControl(key.KeyChar)) { chars.Append(key.KeyChar); Console.Write('*'); }
        }
        Console.WriteLine();
        return chars.ToString().Trim();
    }

    public static bool Confirm(string label, bool defaultYes)
    {
        while (true)
        {
            Console.Write($"  {label} [{(defaultYes ? "Y/n" : "y/N")}]: ");
            var value = ReadLineChecked()!.Trim().ToLowerInvariant();
            if (value.Length == 0) return defaultYes;
            if (value is "y" or "yes") return true;
            if (value is "n" or "no") return false;
            Warn("Please answer y or n.");
        }
    }

    /// <summary>Numbered menu. Returns the zero-based index of the choice.</summary>
    public static int Choose(string label, IReadOnlyList<string> options, int defaultIndex = 0)
    {
        Line($"  {label}");
        for (var i = 0; i < options.Count; i++) Line($"    {i + 1}) {options[i]}");
        while (true)
        {
            Console.Write($"  Enter a number [{defaultIndex + 1}]: ");
            var value = ReadLineChecked()!.Trim();
            if (value.Length == 0) return defaultIndex;
            if (int.TryParse(value, out var n) && n >= 1 && n <= options.Count) return n - 1;
            Warn($"Please enter a number from 1 to {options.Count}.");
        }
    }

    public static void PressEnter(string text = "Press Enter to close this window...")
    {
        if (Console.IsInputRedirected) return;
        Console.Write("  " + text);
        try { Console.ReadLine(); } catch { /* nothing to wait for */ }
    }
}
