using Microsoft.AspNetCore.DataProtection;

namespace BolNews.Web.Services.Tts;

/// <summary>Encrypts provider API keys at rest with ASP.NET Core Data Protection. A key that cannot be decrypted (for example after the server's key ring was reset) reads as missing and must be re-entered.</summary>
public sealed class TtsKeyProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("BolNews.PublisherTts.ApiKey.v1");

    public string Protect(string apiKey) => _protector.Protect(apiKey.Trim());

    public string? Unprotect(string? protectedKey)
    {
        if (string.IsNullOrWhiteSpace(protectedKey)) return null;
        try { return _protector.Unprotect(protectedKey); }
        catch (System.Security.Cryptography.CryptographicException) { return null; }
    }
}
