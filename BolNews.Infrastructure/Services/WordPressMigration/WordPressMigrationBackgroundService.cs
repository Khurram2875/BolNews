using BolNews.Application.Interfaces;
using BolNews.Infrastructure.Services.WordPressMigration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    public class WordPressMigrationBackgroundService
     : BackgroundService
    {
        private readonly IWordPressMigrationQueue _queue;
        private readonly WordPressMigrationState _state;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WordPressMigrationBackgroundService> _logger;

        public WordPressMigrationBackgroundService(
            IWordPressMigrationQueue queue,
            WordPressMigrationState state,
            IServiceScopeFactory scopeFactory,
            ILogger<WordPressMigrationBackgroundService> logger)
        {
            _queue = queue;
            _state = state;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "WordPress migration background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var job =
                        await _queue.DequeueAsync(
                            stoppingToken);

                    await ProcessJobAsync(
                        job,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected WordPress migration background error.");
                }
            }
        }

        private async Task ProcessJobAsync(
            WordPressMigrationJob job,
            CancellationToken stoppingToken)
        {
            try
            {
                using var scope =
                    _scopeFactory.CreateScope();

                using var linkedCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        stoppingToken,
                        _state.Token);

                var cancellationToken =
                    linkedCancellation.Token;

                switch (job.Operation)
                {
                    case WordPressMigrationOperation.Import:

                        await ProcessImportAsync(
                            scope,
                            job,
                            cancellationToken);

                        break;

                    case WordPressMigrationOperation.RepairFeaturedImages:

                        await ProcessFeaturedImagesAsync(
                            scope,
                            job,
                            cancellationToken);

                        break;

                    case WordPressMigrationOperation.NormalizeContent:

                        await ProcessNormalizeContentAsync(
                            scope,
                            job,
                            cancellationToken);

                        break;

                    case WordPressMigrationOperation.ProcessInlineMedia:

                        await ProcessInlineMediaAsync(
                            scope,
                            job,
                            cancellationToken);

                        break;

                    default:

                        throw new InvalidOperationException(
                            $"Unsupported migration operation: {job.Operation}");
                }
            }
            catch (OperationCanceledException)
            {
                _state.MarkAborted(
                    _state.Total,
                    _state.Imported,
                    _state.Repaired,
                    _state.Skipped,
                    _state.Failed);

                _logger.LogWarning(
                    "WordPress migration operation aborted. Operation={Operation}",
                    job.Operation);
            }
            catch (Exception ex)
            {
                _state.MarkFailed(
                    _state.Total,
                    _state.Imported,
                    _state.Repaired,
                    _state.Skipped,
                    _state.Failed);

                _logger.LogError(
                    ex,
                    "WordPress migration operation failed. Operation={Operation}",
                    job.Operation);
            }
        }

        private async Task ProcessImportAsync(
            IServiceScope scope,
            WordPressMigrationJob job,
            CancellationToken cancellationToken)
        {
            var importService =
                scope.ServiceProvider
                    .GetRequiredService<
                        IWordPressArticleImportService>();

            var result =
                await importService.ImportAsync(
                    job.FromDate,
                    job.ToDate,
                    job.CurrentUserId!,
                    job.Take,
                    cancellationToken);

            _state.Complete(
                result.Total,
                result.Imported,
                0,
                result.Skipped,
                result.Failed);
        }

        private async Task ProcessFeaturedImagesAsync(
            IServiceScope scope,
            WordPressMigrationJob job,
            CancellationToken cancellationToken)
        {
            var repairService =
                scope.ServiceProvider
                    .GetRequiredService<
                        WordPressMigrationRepairService>();

            var result =
                await repairService.RepairFeaturedImagesAsync(
                    job.FromDate,
                    job.ToDate,
                    cancellationToken);

            _state.Complete(
                result.Total,
                0,
                result.Repaired,
                result.Skipped,
                result.Failed);
        }

        private async Task ProcessNormalizeContentAsync(
            IServiceScope scope,
            WordPressMigrationJob job,
            CancellationToken cancellationToken)
        {
            var repairService =
                scope.ServiceProvider
                    .GetRequiredService<
                        WordPressMigrationRepairService>();

            var result =
                await repairService.NormalizeContentAsync(
                    job.FromDate,
                    job.ToDate,
                    cancellationToken);

            _state.Complete(
                result.Total,
                0,
                result.Repaired,
                result.Skipped,
                result.Failed);
        }

        private async Task ProcessInlineMediaAsync(
            IServiceScope scope,
            WordPressMigrationJob job,
            CancellationToken cancellationToken)
        {
            var repairService =
                scope.ServiceProvider
                    .GetRequiredService<
                        WordPressMigrationRepairService>();

            var result =
                await repairService.ProcessInlineMediaAsync(
                    job.FromDate,
                    job.ToDate,
                    cancellationToken);

            _state.Complete(
                result.Total,
                0,
                result.Repaired,
                result.Skipped,
                result.Failed);
        }
    }
}
