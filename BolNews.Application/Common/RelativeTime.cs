using System.Globalization;

namespace BolNews.Application.Common;

/// <summary>"5 minutes ago" in the language of the current request culture (English or Urdu).</summary>
public static class RelativeTime
{
    public static string Format(DateTime? publishedUtc)
    {
        if (!publishedUtc.HasValue) return "";
        var span = DateTime.UtcNow - publishedUtc.Value;
        var urdu = string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "ur", StringComparison.OrdinalIgnoreCase);

        if (span.TotalSeconds < 60) return Build(Math.Max(1, (int)span.TotalSeconds), "second", "seconds", "سیکنڈ", "سیکنڈ", urdu);
        if (span.TotalMinutes < 60) return Build((int)span.TotalMinutes, "minute", "minutes", "منٹ", "منٹ", urdu);
        if (span.TotalHours < 24) return Build((int)span.TotalHours, "hour", "hours", "گھنٹہ", "گھنٹے", urdu);
        if (span.TotalDays < 7) return Build((int)span.TotalDays, "day", "days", "دن", "دن", urdu);
        if (span.TotalDays < 30) return Build((int)(span.TotalDays / 7), "week", "weeks", "ہفتہ", "ہفتے", urdu);
        if (span.TotalDays < 365) return Build((int)(span.TotalDays / 30), "month", "months", "مہینہ", "مہینے", urdu);
        return Build((int)(span.TotalDays / 365), "year", "years", "سال", "سال", urdu);
    }

    private static string Build(int count, string en1, string enN, string ur1, string urN, bool urdu) =>
        urdu ? $"{count} {(count == 1 ? ur1 : urN)} پہلے" : $"{count} {(count == 1 ? en1 : enN)} ago";
}
