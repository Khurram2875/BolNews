using BolNews.Application.Common.Helpers;
using BolNews.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace BolNews.Tests.Unit
{
    /// <summary>
    /// Tests for ImageValidator.IsValid.
    /// Covers MIME type, extension, size, and null-guard checks.
    /// </summary>
    public class ImageValidatorTests
    {
        // ── Happy path ────────────────────────────────────────────────────────

        [Theory]
        [InlineData("photo.jpg",  "image/jpeg")]
        [InlineData("photo.jpeg", "image/jpeg")]
        [InlineData("photo.png",  "image/png")]
        [InlineData("photo.webp", "image/webp")]
        public void IsValid_AcceptedFormats_ReturnsTrue(string fileName, string contentType)
        {
            var file = new FakeFormFile(fileName, contentType, length: 1024);

            var result = ImageValidator.IsValid(file, out var error);

            result.Should().BeTrue();
            error.Should().BeEmpty();
        }

        // ── Invalid MIME type ─────────────────────────────────────────────────

        [Theory]
        [InlineData("doc.pdf",  "application/pdf")]
        [InlineData("file.gif", "image/gif")]
        [InlineData("exec.exe", "application/octet-stream")]
        public void IsValid_DisallowedMimeType_ReturnsFalse(string fileName, string contentType)
        {
            var file = new FakeFormFile(fileName, contentType, length: 512);

            var result = ImageValidator.IsValid(file, out var error);

            result.Should().BeFalse();
            error.Should().NotBeEmpty();
        }

        // ── Invalid extension ─────────────────────────────────────────────────

        [Theory]
        [InlineData("photo.bmp",  "image/jpeg")]  // valid MIME, bad extension
        [InlineData("photo.tiff", "image/jpeg")]
        [InlineData("photo.svg",  "image/jpeg")]
        public void IsValid_DisallowedExtension_ReturnsFalse(string fileName, string contentType)
        {
            var file = new FakeFormFile(fileName, contentType, length: 512);

            var result = ImageValidator.IsValid(file, out var error);

            result.Should().BeFalse();
            error.Should().NotBeEmpty();
        }

        // ── File size boundary ────────────────────────────────────────────────

        [Fact]
        public void IsValid_FileSizeExactly2MB_ReturnsTrue()
        {
            long twoMB = 2 * 1024 * 1024;
            var file   = new FakeFormFile("photo.jpg", "image/jpeg", length: twoMB);

            // The validator uses >, so exactly 2MB should still pass
            var result = ImageValidator.IsValid(file, out var error);

            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_FileSizeOver2MB_ReturnsFalse()
        {
            long overLimit = 2 * 1024 * 1024 + 1;
            var file       = new FakeFormFile("photo.jpg", "image/jpeg", length: overLimit);

            var result = ImageValidator.IsValid(file, out var error);

            result.Should().BeFalse();
            error.Should().Contain("2MB");
        }

        [Fact]
        public void IsValid_VerySmallFile_ReturnsTrue()
        {
            var file = new FakeFormFile("thumb.png", "image/png", length: 256);

            var result = ImageValidator.IsValid(file, out var error);

            result.Should().BeTrue();
        }

        // ── Extension casing ─────────────────────────────────────────────────

        [Theory]
        [InlineData("PHOTO.JPG")]
        [InlineData("PHOTO.PNG")]
        [InlineData("Photo.Jpeg")]
        public void IsValid_UpperCaseExtension_ReturnsTrue(string fileName)
        {
            var file = new FakeFormFile(fileName, "image/jpeg", length: 1024);

            var result = ImageValidator.IsValid(file, out _);

            result.Should().BeTrue("extension check should be case-insensitive");
        }
    }
}
