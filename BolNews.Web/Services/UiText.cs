using System.Globalization;

namespace BolNews.Web.Services;

/// <summary>
/// Fixed public-page labels ("Latest", "Vote now", ...) in English or Urdu. The language comes from the site profile
/// (see the culture middleware in Program.cs). Unknown keys fall back to the English key itself, so a missing
/// translation never breaks a page.
/// Usage in Razor:  @T["Latest"]
/// </summary>
public sealed class UiText
{
    private static readonly Dictionary<string, string> Urdu = new(StringComparer.Ordinal)
    {
        ["Home"] = "ہوم", ["News"] = "خبریں", ["Latest"] = "تازہ ترین", ["Latest News"] = "تازہ ترین خبریں",
        ["Latest updates"] = "تازہ ترین اپ ڈیٹس", ["Trending"] = "ٹرینڈنگ", ["Smart Trending"] = "ٹرینڈنگ",
        ["Top Stories"] = "اہم خبریں", ["Popular Stories"] = "مقبول خبریں", ["Popular Right Now"] = "اس وقت مقبول",
        ["Featured"] = "نمایاں", ["Headlines"] = "سرخیاں", ["More"] = "مزید", ["Shows"] = "پروگرام", ["Videos"] = "ویڈیوز",
        ["Watch Live"] = "لائیو دیکھیں", ["LIVE"] = "لائیو", ["Live Updates"] = "لائیو اپ ڈیٹس", ["LIVE UPDATES"] = "لائیو اپ ڈیٹس",
        ["Timeline"] = "ٹائم لائن", ["Previous"] = "پچھلا", ["Next"] = "اگلا", ["Cancel"] = "منسوخ کریں",
        ["Vote now"] = "ووٹ دیں", ["Submit vote"] = "ووٹ جمع کریں", ["Final results"] = "حتمی نتائج",
        ["PKR"] = "روپے", ["PETROL"] = "پیٹرول", ["DIESEL"] = "ڈیزل",
        ["Related articles"] = "متعلقہ خبریں", ["Saved articles"] = "محفوظ شدہ خبریں", ["Clear saved articles"] = "محفوظ خبریں صاف کریں",
        ["Search Results"] = "تلاش کے نتائج", ["Search"] = "تلاش", ["Tags"] = "ٹیگز", ["Tag"] = "ٹیگ",
        ["No articles yet"] = "ابھی کوئی خبر نہیں", ["Page not found"] = "صفحہ نہیں ملا", ["Something went wrong"] = "کچھ غلط ہو گیا",
        ["Return to home"] = "ہوم پر واپس جائیں", ["Back to Home"] = "ہوم پر واپس جائیں", ["Access denied"] = "رسائی ممنوع", ["Access Denied"] = "رسائی ممنوع",
        ["Menu"] = "مینو", ["Advertisement"] = "اشتہار", ["Social links"] = "سوشل لنکس", ["Save article"] = "خبر محفوظ کریں",
        ["Listen to article"] = "خبر سنیں", ["Share on Facebook"] = "فیس بک پر شیئر کریں", ["Share on X"] = "ایکس پر شیئر کریں",
        ["Share on WhatsApp"] = "واٹس ایپ پر شیئر کریں", ["Live events"] = "لائیو ایونٹس", ["Category stories"] = "اس زمرے کی خبریں",
        ["More news"] = "مزید خبریں", ["Related categories"] = "متعلقہ زمرے", ["Video pagination"] = "ویڈیو صفحات",
        ["Saved articles pages"] = "محفوظ خبروں کے صفحات", ["Recommended articles pages"] = "تجویز کردہ خبروں کے صفحات",
        ["Saved and recommended articles"] = "محفوظ اور تجویز کردہ خبریں", ["Top stories"] = "اہم خبریں", ["Video Thumbnail"] = "ویڈیو تھمب نیل",
        ["Copy link and open Instagram"] = "لنک کاپی کریں اور انسٹاگرام کھولیں",
        ["Poll"] = "رائے شماری", ["votes"] = "ووٹ", ["Thank you for voting"] = "ووٹ دینے کا شکریہ",
        ["Updated"] = "اپ ڈیٹ", ["Published"] = "شائع", ["By"] = "از", ["Read more"] = "مزید پڑھیں", ["Read full story"] = "پوری خبر پڑھیں",
        ["Follow us"] = "ہمیں فالو کریں", ["Privacy Policy"] = "پرائیویسی پالیسی", ["About Us"] = "ہمارے بارے میں", ["Contact"] = "رابطہ",
        ["Pakistan"] = "پاکستان", ["World"] = "دنیا", ["Sports"] = "کھیل", ["Entertainment"] = "تفریح", ["Business"] = "کاروبار"
    };

    public bool IsUrdu =>
        string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "ur", StringComparison.OrdinalIgnoreCase);

    public string this[string key] => IsUrdu && Urdu.TryGetValue(key, out var value) ? value : key;
}
