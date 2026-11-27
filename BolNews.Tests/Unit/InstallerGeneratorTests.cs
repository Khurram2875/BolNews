using CmsInstaller;
using FluentAssertions;
using Xunit;

namespace BolNews.Tests.Unit;

public class InstallerGeneratorTests
{
    private static readonly SiteNames Names = new("acme_news");

    [Theory]
    [InlineData("acme_news", true)]
    [InlineData("a", false)]
    [InlineData("Acme", false)]
    [InlineData("1news", false)]
    [InlineData("mysql", false)]
    [InlineData("this_name_is_far_too_long_for_users", false)]
    [InlineData("news-site", false)]
    public void DatabaseNameValidation(string name, bool valid) =>
        (SiteNames.ValidateDatabaseName(name) is null).Should().Be(valid);

    [Fact]
    public void NamesNeverContainProductName()
    {
        var all = string.Join('|', Names.AppUser, Names.MigratorUser, Names.EnvFile, Names.ServicePath, Names.VhostPath, Names.KeyDir, Names.LogPath);
        all.ToLowerInvariant().Should().NotContain("bolnews");
        Names.ServiceName.Should().Be("acme_news.service");
    }

    [Fact]
    public void EnvFileRoundTripsSpecialCharacters()
    {
        var value = "pa\"ss\\word;with#chars=and spaces";
        var text = Generators.BuildEnvFile(Names, new[] { new EnvEntry("A__B", value, "c"), new EnvEntry("Missing__Key", null) });
        var parsed = Generators.ParseEnvFile(text);
        parsed["A__B"].Should().Be(value);
        parsed.Should().NotContainKey("Missing__Key");
        text.Should().Contain("# Missing__Key=\"\"");
    }

    [Fact]
    public void EnvQuoteRemovesLineBreaks() => Generators.EnvQuote("a\nb\r").Should().Be("\"a b \"");

    [Fact]
    public void ProvisionSqlLimitsUsersToTheirDatabase()
    {
        var sql = Generators.ProvisionSql("acme_news", "acme_news_app", "pw1", "acme_news_migrator", "pw2", new[] { "localhost", "127.0.0.1" });
        sql.Should().Contain(s => s.StartsWith("CREATE DATABASE IF NOT EXISTS `acme_news`"));
        sql.Where(s => s.StartsWith("GRANT")).Should().OnlyContain(s => s.Contains("ON `acme_news`.*"));
        sql.Should().NotContain(s => s.Contains("*.*") || s.Contains("GRANT OPTION"));
        sql.Count(s => s.StartsWith("CREATE USER")).Should().Be(4);
    }

    [Fact]
    public void EscapeSqlHandlesQuotes() => Generators.EscapeSql("a'b\\c").Should().Be("a''b\\\\c");

    [Fact]
    public void PasswordsAreLongAndSafe()
    {
        var p = Generators.Password();
        p.Length.Should().Be(32);
        p.All(char.IsLetterOrDigit).Should().BeTrue();
        Generators.Password().Should().NotBe(p);
    }

    [Fact]
    public void ApacheSiteHasStaticExclusionsAndOnlyAddsHttpsWhenAsked()
    {
        var http = Generators.BuildApacheSite(Names, "news.example.com", new[] { "www.example.com" }, "/var/www/app", 5001, HttpsMode.None, null, null);
        http.Should().Contain("ServerName news.example.com").And.Contain("ServerAlias www.example.com")
            .And.Contain("ProxyPass /uploads !").And.Contain("ProxyPass / http://127.0.0.1:5001/")
            .And.Contain("DocumentRoot /var/www/app/wwwroot").And.Contain("installer-site: acme_news")
            .And.NotContain("*:443").And.NotContain("Indexes");

        var https = Generators.BuildApacheSite(Names, "news.example.com", Array.Empty<string>(), "/var/www/app", 5001, HttpsMode.OwnCertificate, "/c.crt", "/k.key");
        https.Should().Contain("<VirtualHost *:443>").And.Contain("SSLCertificateFile /c.crt").And.Contain("X-Forwarded-Proto \"https\"");
        https.Should().NotContain("ServerAlias");
    }

    [Fact]
    public void SystemdUnitUsesSiteNamesAndEnvironmentFile()
    {
        var unit = Generators.BuildSystemdUnit(Names, "/var/www/app", "/var/www/app/Site.Web", 5001, "www-data", true, false);
        unit.Should().Contain("EnvironmentFile=/etc/acme_news/acme_news.env").And.Contain("ExecStart=/var/www/app/Site.Web")
            .And.Contain("ASPNETCORE_URLS=http://127.0.0.1:5001").And.Contain("SyslogIdentifier=acme_news").And.Contain("User=www-data");
        unit.ToLowerInvariant().Should().NotContain("bolnews");
    }

    [Fact]
    public void ConnectionStringSupportsSocketAndTcp()
    {
        Generators.ConnectionString("127.0.0.1", 3306, "db", "u", "p", true).Should().Contain("Server=127.0.0.1;Port=3306;");
        Generators.ConnectionString("/run/mysqld/mysqld.sock", 3306, "db", "u", "p", true).Should().Contain("ConnectionProtocol=UnixSocket").And.NotContain("Port=");
    }
}
