namespace BolNews.Domain.Entities;

/// <summary>
/// Per-installation newsroom identity and public-facing customization.
/// Each commercial customer deployment owns its own database/profile.
/// </summary>
public class SiteProfile
{
    public int Id { get; set; }
    public string SiteName { get; set; } = "Bol News";
    public string ShortName { get; set; } = "BOL News";
    public string Tagline { get; set; } = "Latest News, Politics, Sports and Entertainment";
    public string HeaderLogoUrl { get; set; } = "/images/logo-black.png";
    public string FooterLogoUrl { get; set; } = "/images/logo-black.png";
    public string FaviconUrl { get; set; } = "/bol_network.ico";
    public string PrimaryColor { get; set; } = "#d30b12";
    public string AccentColor { get; set; } = "#d30b12";
    public string ThemeName { get; set; } = "default";
    public string LiveStreamUrl { get; set; } = "/live";
    public string? LiveVideoEmbedUrl { get; set; } = "https://www.youtube.com/embed/9N6SWn1BQpA?rel=0&modestbranding=1";
    public string? LiveThumbnailUrl { get; set; } = "https://img.youtube.com/vi/9N6SWn1BQpA/maxresdefault.jpg";
    public string CopyrightText { get; set; } = "BOL Media Network. All rights reserved.";
    public string DefaultMetaTitle { get; set; } = "Bol News - Latest News, Politics, Sports and Entertainment";
    public string DefaultMetaDescription { get; set; } = "Latest news from Bol News";
    public string? CustomCss { get; set; }
    public string? AdScriptUrl { get; set; } = "https://avads.live/s/av-bolnews.js";
    public string? WhatsAppUrl { get; set; } = "https://whatsapp.com/channel/0029Vb7ZANWGJP8NYx9M4g0I";
    public string? FacebookUrl { get; set; } = "https://www.facebook.com/BOLNETWORK/";
    public string? XUrl { get; set; } = "https://twitter.com/BOLNETWORK";
    public string? YouTubeUrl { get; set; } = "https://www.youtube.com/@BOLNewsofficial";
    public string? InstagramUrl { get; set; } = "https://www.instagram.com/bolnetwork/";
    public string? TikTokUrl { get; set; } = "https://www.tiktok.com/@bolnews";
    public string? UrduEditionUrl { get; set; } = "https://urdu.bolnews.com";
    public string OtherEditionLabel { get; set; } = "Urdu";
    public string? FooterColumnOneLinks { get; set; } = "Latest News|/category/latest-news\nHealth|/category/health\nShowbiz|/category/showbiz\nTrending|/category/trending\nViral|/category/viral";
    public string? FooterColumnTwoLinks { get; set; } = "About Us|/about-us\nContact Us|/contact-us\nAdvertise With Us|/advertise\nUrdu|https://urdu.bolnews.com\nLive|/live";
    public string? FooterColumnThreeLinks { get; set; } = "Branded Content|/branded-content\nPrivacy Policy|/privacy-policy\nTerms Of Service|/terms-of-service\nEditorial Policy|/editorial-policy\nBlog|/blogs";
    public string? AboutContent { get; set; }
    public string? ContactContent { get; set; }
    public string? AdvertiseContent { get; set; }
    public string? BlogsContent { get; set; }
    public string? BrandedContent { get; set; }
    public string? EditorialPolicyContent { get; set; }
    public string? PrivacyPolicyContent { get; set; }
    public string? TermsOfServiceContent { get; set; }
    public DateTime UpdatedAt { get; set; }
}
