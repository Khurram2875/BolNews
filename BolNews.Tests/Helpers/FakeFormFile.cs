using Microsoft.AspNetCore.Http;

namespace BolNews.Tests.Helpers
{
    /// <summary>
    /// Minimal IFormFile implementation for unit testing ImageValidator
    /// without needing a real HTTP request or disk file.
    /// </summary>
    public sealed class FakeFormFile : IFormFile
    {
        private readonly byte[] _content;

        public FakeFormFile(string fileName, string contentType, long length = 0)
        {
            FileName    = fileName;
            ContentType = contentType;
            _content    = new byte[length > 0 ? length : 1];
            Length      = length > 0 ? length : 1;
        }

        public string ContentType                { get; }
        public string ContentDisposition         => $"form-data; name=\"file\"; filename=\"{FileName}\"";
        public IHeaderDictionary Headers         => new HeaderDictionary();
        public long Length                       { get; }
        public string Name                       => "file";
        public string FileName                   { get; }

        public Stream OpenReadStream()           => new MemoryStream(_content);
        public void CopyTo(Stream target)        => target.Write(_content, 0, _content.Length);
        public Task CopyToAsync(Stream target, CancellationToken ct = default)
            => target.WriteAsync(_content, 0, _content.Length, ct);
    }
}
