using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace IBS.Services
{
    public class StartOfTheMonthService : IJob
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<StartOfTheMonthService> _logger;

        private readonly ApplicationDbContext _dbContext;

        public StartOfTheMonthService(IUnitOfWork unitOfWork,
            ILogger<StartOfTheMonthService> logger, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var today = DateTimeHelper.GetCurrentPhilippineTime();
                var previousMonthDate =  today.AddMonths(-1);

                // This method will capture the unlifted DR, send the notification to TNS if found any.
                await GetTheUnliftedDrs(previousMonthDate);

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task GetTheUnliftedDrs(DateTime previousMonthDate)
        {
            try
            {
                var hasUnliftedDrs = await _dbContext.FilprideDeliveryReceipts
                    .AnyAsync(x => x.Date.Month == previousMonthDate.Month
                                   && x.Date.Year == previousMonthDate.Year
                                   && !x.HasReceivingReport);

                if (hasUnliftedDrs)
                {
                    var users = await _dbContext.ApplicationUsers
                        .Where(u => u.Department == SD.Department_TradeAndSupply
                                    || u.Department == SD.Department_ManagementAccounting)
                        .Select(u => u.Id)
                        .ToListAsync();

                    var message = $"There are still unlifted reports for {previousMonthDate:MMM yyyy}. " +
                                  $"Please ensure the lifting dates for the remaining DRs are recorded to avoid issues during the month-end closing. " +
                                  $"CC: Management Accounting";

                    await _unitOfWork.Notifications.AddNotificationToMultipleUsersAsync(users, message);

                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while getting the unlifted DRs.", ex);
            }
        }
    }
}
