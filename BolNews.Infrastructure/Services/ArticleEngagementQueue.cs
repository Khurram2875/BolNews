using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;
using BolNews.Application.Interfaces;

namespace BolNews.Infrastructure.Services
{
    public sealed class ArticleEngagementQueue : IArticleEngagementQueue
    {
        private readonly Channel<ArticleEngagementItem> _channel;

        public ArticleEngagementQueue()
        {
            _channel = Channel.CreateBounded<ArticleEngagementItem>(
                new BoundedChannelOptions(100_000)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false
                });
        }

        public bool TryEnqueue(int articleId, bool incrementViewCount)
        {
            return _channel.Writer.TryWrite(
                new ArticleEngagementItem(
                    articleId,
                    incrementViewCount));
        }

        public IAsyncEnumerable<ArticleEngagementItem> ReadAllAsync(
            CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }

    public sealed record ArticleEngagementItem(
        int ArticleId,
        bool IncrementViewCount);
}
