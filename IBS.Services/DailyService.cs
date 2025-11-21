using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace IBS.Services
{
    public class DailyService : IJob
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly ILogger<DailyService> _logger;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        public DailyService(ApplicationDbContext dbContext, ILogger<DailyService> logger,
            UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _logger = logger;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var today = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime());

                await CosExpiration(today);

                await transaction.CommitAsync();

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, ex.Message);
            }
        }

        private async Task CosExpiration(DateOnly today)
        {
            var cosList = await _dbContext.FilprideCustomerOrderSlips
                .Where(cos => cos.ExpirationDate <= today
                              && cos.Status != nameof(CosStatus.Completed)
                              && cos.Status != nameof(CosStatus.Expired)
                              && cos.Status != nameof(CosStatus.Closed)
                              && cos.Status != nameof(CosStatus.Disapproved))
                .ToListAsync();

            if (cosList.Count == 0)
            {
                return;
            }

            foreach (var cos in cosList)
            {
                // Record the current status before updating
                var previousStatus = cos.Status;

                // Update the status to Expired
                cos.Status = nameof(CosStatus.Expired);

                // Append the previous status and timestamp to the remarks
                cos.Remarks = $"Previous status: [{previousStatus}] updated to Expired on {today}. {cos.Remarks}";
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task LockPlacement(DateTime today)
        {
            var placements = await _dbContext.BienesPlacements
                .Where(p => p.LockedDate <= today && !p.IsLocked)
                .ToListAsync();

            if (placements.Count == 0)
            {
                return;
            }

            foreach (var placement in placements)
            {
                placement.IsLocked = true;
                placement.Status = nameof(PlacementStatus.Locked);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
