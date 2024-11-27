using IBS.DataAccess.Data;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ExpireUnusedCustomerOrderSlipsService : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;

        public ExpireUnusedCustomerOrderSlipsService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //_timer = new Timer(ExpireUnusedCos, null, TimeSpan.Zero, TimeSpan.FromDays(1));
            return Task.CompletedTask;
        }

        private async void ExpireUnusedCos(object state)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // Replace with your actual DbContext

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var expiredCosList = await dbContext.FilprideCustomerOrderSlips
                    .Where(cos => cos.ExpirationDate != null && cos.ExpirationDate < today && cos.Status != nameof(CosStatus.Expired) && cos.DeliveredQuantity == 0)
                    .ToListAsync();

                if (expiredCosList.Count != 0)
                {
                    foreach (var cos in expiredCosList)
                    {
                        cos.Status = nameof(CosStatus.Expired);
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
