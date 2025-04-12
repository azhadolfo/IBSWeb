using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace IBS.Services
{
    public class LockPlacementService : IJob
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly ILogger<LockPlacementService> _logger;

        public LockPlacementService(ApplicationDbContext dbContext,
            ILogger<LockPlacementService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var today = DateTimeHelper.GetCurrentPhilippineTime();

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
                await transaction.CommitAsync();

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
