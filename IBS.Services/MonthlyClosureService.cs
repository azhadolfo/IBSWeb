using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace IBS.Services
{
    public class MonthlyClosureService : IJob
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly ILogger<MonthlyClosureService> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        public MonthlyClosureService(ApplicationDbContext dbContext, ILogger<MonthlyClosureService> logger,
            UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _logger = logger;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await InTransit();

                _logger.LogInformation(
                    $"MonthlyClosureService is running at: {DateTimeHelper.GetCurrentPhilippineTime()}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private async Task InTransit()
        {
            var previousMonth = DateTime.Now.AddMonths(-1);
            var hadInTransit = await _dbContext.FilprideDeliveryReceipts
                .AnyAsync(dr =>
                    dr.Date.Month == previousMonth.Month &&
                    dr.Date.Year == previousMonth.Year &&
                    dr.Status == nameof(DRStatus.Pending));

            if (hadInTransit)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    var logisticUser = await _dbContext.ApplicationUsers
                        .Where(u => u.Department == SD.Department_Logistics.ToString())
                        .ToListAsync();

                    foreach (var user in logisticUser)
                    {
                        var message = "Is the in-transit report final?";
                        await _unitOfWork.Notifications.AddNotificationAsync(user.Id, message);
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing in-transit notifications.");
                    await transaction.RollbackAsync();
                }
            }
        }
    }
}
