using IBS.DataAccess.Repository.IRepository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IBS.DataAccess.Repository
{
    public class ImportService : IHostedService, IDisposable
    {
        private readonly ILogger<ImportService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;

        public ImportService(ILogger<ImportService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sales import service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromDays(1)); // Adjust the interval as needed

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            try
            {
                await unitOfWork.SalesHeader.ImportSales();

                _logger.LogInformation("Sales import completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sales import.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sales import service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }

}
