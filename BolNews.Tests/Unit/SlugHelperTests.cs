using BolNews.Application.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace BolNews.Tests.Unit
{
    /// <summary>
    /// Tests for SlugHelper.GenerateSlug.
    /// These are pure-function tests — no database or mocks needed.
    /// </summary>
    public class SlugHelperTests
    {
        [Fact]
        public void GenerateSlug_BasicTitle_ReturnsLowercaseHyphenated()
        {
            var slug = SlugHelper.GenerateSlug("Breaking News Today");

            slug.Should().Be("breaking-news-today");
        }

        [Fact]
        public void GenerateSlug_UpperCaseInput_ReturnsAllLowercase()
        {
            var slug = SlugHelper.GenerateSlug("PAKISTAN ELECTIONS 2024");

            slug.Should().Be("pakistan-elections-2024");
        }

        [Fact]
        public void GenerateSlug_SpecialCharacters_AreStripped()
        {
            var slug = SlugHelper.GenerateSlug("Top 10 Stocks: What's Hot & What's Not!");

            slug.Should().NotContain(":")
                         .And.NotContain("'")
                         .And.NotContain("&")
                         .And.NotContain("!");
        }

        [Fact]
        public void GenerateSlug_MultipleSpaces_CollapsedToSingleHyphen()
        {
            var slug = SlugHelper.GenerateSlug("Imran   Khan   Latest   News");

            slug.Should().Be("imran-khan-latest-news");
            slug.Should().NotContain("--");
        }

        [Fact]
        public void GenerateSlug_LeadingAndTrailingSpaces_AreTrimmed()
        {
            var slug = SlugHelper.GenerateSlug("   Karachi Weather   ");

            slug.Should().Be("karachi-weather");
            slug.Should().NotStartWith("-").And.NotEndWith("-");
        }

        [Fact]
        public void GenerateSlug_NumbersInTitle_ArePreserved()
        {
            var slug = SlugHelper.GenerateSlug("PSX 100 Index Falls 500 Points");

            slug.Should().Contain("100").And.Contain("500");
        }

        [Fact]
        public void GenerateSlug_AlreadySlugFormatted_ReturnsSameValue()
        {
            var input = "already-a-slug";
            var slug  = SlugHelper.GenerateSlug(input);

            slug.Should().Be("already-a-slug");
        }

        [Fact]
        public void GenerateSlug_UrduRomanised_StripsUnsupportedChars()
        {
            // Urdu unicode chars should be stripped, latin parts preserved
            var slug = SlugHelper.GenerateSlug("Lahore احتجاج 2024");

            slug.Should().Contain("lahore").And.Contain("2024");
            slug.Should().MatchRegex("^[a-z0-9-]+$");
        }
    }
}
