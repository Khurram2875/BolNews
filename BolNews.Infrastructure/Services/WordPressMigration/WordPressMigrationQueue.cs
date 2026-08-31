using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    public class WordPressMigrationQueue
    : IWordPressMigrationQueue
    {
        private readonly Channel<WordPressMigrationJob> _queue;

        public WordPressMigrationQueue()
        {
            _queue =
                Channel.CreateUnbounded<WordPressMigrationJob>(
                    new UnboundedChannelOptions
                    {
                        SingleReader = true,
                        SingleWriter = false
                    });
        }

        public async ValueTask QueueAsync(
            WordPressMigrationJob job,
            CancellationToken cancellationToken = default)
        {
            await _queue.Writer.WriteAsync(
                job,
                cancellationToken);
        }

        public async ValueTask<WordPressMigrationJob> DequeueAsync(
            CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(
                cancellationToken);
        }
    }
}
