using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    public interface IWordPressMigrationQueue
    {
        ValueTask QueueAsync(
            WordPressMigrationJob job,
            CancellationToken cancellationToken = default);

        ValueTask<WordPressMigrationJob> DequeueAsync(
            CancellationToken cancellationToken);
    }
}
