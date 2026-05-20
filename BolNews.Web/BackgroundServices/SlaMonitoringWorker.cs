using BolNews.Application.Interfaces;

namespace BolNews.Web.BackgroundServices
{
    public class SlaMonitoringWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public SlaMonitoringWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope =
                    _serviceProvider.CreateScope();

                var escalationService =
                    scope.ServiceProvider
                        .GetRequiredService<ISlaEscalationService>();

                await escalationService.ProcessAsync();

                await Task.Delay(
                    TimeSpan.FromMinutes(15),
                    stoppingToken);
            }
        }
    }
}
