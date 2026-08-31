using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    public sealed class WordPressMediaSource
    {
        private readonly WordPressMediaOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WordPressMediaSource> _logger;

        public WordPressMediaSource(
            IOptions<WordPressMediaOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<WordPressMediaSource> logger)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<Stream> OpenAsync(
            string imageUrl,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException(
                    "Image URL/path is required.",
                    nameof(imageUrl));

            if (!string.IsNullOrWhiteSpace(_options.LocalRoot))
            {
                var localPath = ResolveLocalPath(imageUrl);

                if (localPath != null && File.Exists(localPath))
                {
                    _logger.LogInformation(
                        "WORDPRESS_MEDIA source=LOCAL path={Path}",
                        localPath);

                    return new FileStream(
                        localPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        bufferSize: 64 * 1024,
                        useAsync: true);
                }

                _logger.LogWarning(
                    "WORDPRESS_MEDIA local file not found. " +
                    "URL={Url} LocalRoot={LocalRoot}",
                    imageUrl,
                    _options.LocalRoot);
            }

            if (!_options.AllowRemoteFallback)
            {
                throw new FileNotFoundException(
                    $"WordPress media file was not found locally and " +
                    $"remote fallback is disabled: {imageUrl}");
            }

            _logger.LogWarning(
                "WORDPRESS_MEDIA source=REMOTE_FALLBACK URL={Url}",
                imageUrl);

            var client =
                _httpClientFactory.CreateClient("WordPressMedia");

            var response = await client.GetAsync(
                imageUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync(
                cancellationToken);
        }

        private string? ResolveLocalPath(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(_options.LocalRoot))
                return null;

            string path;

            if (Uri.TryCreate(
                    imageUrl,
                    UriKind.Absolute,
                    out var uri))
            {
                path = uri.AbsolutePath;
            }
            else
            {
                path = imageUrl;
            }

            // We only want the portion AFTER /wp-content/
            const string marker = "/wp-content/";

            var markerIndex = path.IndexOf(
                marker,
                StringComparison.OrdinalIgnoreCase);

            if (markerIndex >= 0)
            {
                path = path[(markerIndex + marker.Length)..];
            }
            else
            {
                // Already a relative WordPress path such as:
                // /uploads/2026/05/image.jpg

                if (!path.StartsWith(
                        "/uploads/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                path = path.TrimStart('/');
            }

            path = Uri.UnescapeDataString(path);

            path = path.Replace(
                '/',
                Path.DirectorySeparatorChar);

            path = path.TrimStart(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

            var root = Path.GetFullPath(
                _options.LocalRoot);

            var fullPath = Path.GetFullPath(
                Path.Combine(root, path));

            // Prevent escaping LocalRoot through ../
            var rootWithSeparator =
                root.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(
                    rootWithSeparator,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Resolved WordPress media path is outside " +
                    $"the configured LocalRoot: {imageUrl}");
            }

            return fullPath;
        }
    }
}
