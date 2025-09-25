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
    public class CustomerOrderSlipExpiration : IJob
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly ILogger<CustomerOrderSlipExpiration> _logger;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        public CustomerOrderSlipExpiration(ApplicationDbContext dbContext, ILogger<CustomerOrderSlipExpiration> logger,
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

                    break; //remove this after the demo
                }

                // Save the changes to the database
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
