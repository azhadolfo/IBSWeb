using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
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
                var today = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime());
                var previousMonthDate = today.AddMonths(-1);

                await GetTheUnliftedDrs(previousMonthDate);
                await ProcessAmortization(today);

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task GetTheUnliftedDrs(DateOnly previousMonthDate)
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
                _logger.LogError(ex, "Error while getting the unlifted DRs for {Date}", previousMonthDate);
                throw;
            }
        }

        private async Task ProcessAmortization(DateOnly dateToday)
        {
            try
            {
                var amortizationSetting = await _dbContext.JvAmortizationSettings
                .Include(x => x.JvHeader)
                    .ThenInclude(x => x.Details)
                .Where(x =>
                (x.NextRunDate == null || x.NextRunDate <= dateToday) &&
                x.IsActive &&
                x.JvHeader.PostedBy != null)
                .ToListAsync();

                if (amortizationSetting.Count == 0)
                {
                    return;
                }

                var newJournalVouchers = new List<FilprideJournalVoucherHeader>();

                foreach (var amortization in amortizationSetting)
                {
                    var sourceJv = amortization.JvHeader;

                    if (sourceJv?.Details == null || sourceJv.Details.Count == 0)
                    {
                        throw new InvalidOperationException($"The source journal voucher for amortization with ID {amortization.JvId} is missing or has no details.");
                    }

                    var generatedCode = await _unitOfWork
                    .FilprideJournalVoucher
                    .GenerateCodeAsync(sourceJv.Company, sourceJv.Type);

                    var newHeader = new FilprideJournalVoucherHeader
                    {
                        Type = sourceJv.Type,
                        JournalVoucherHeaderNo = generatedCode,
                        Date = dateToday,
                        References = sourceJv.References,
                        CVId = sourceJv.CVId,
                        Particulars = sourceJv.Particulars,
                        CRNo = sourceJv.CRNo,
                        JVReason = sourceJv.JVReason,
                        CreatedBy = "SYSTEM",
                        Company = sourceJv.Company,
                        JvType = nameof(JvType.Amortization),
                        Status = nameof(JvStatus.Pending),
                        Details = new List<FilprideJournalVoucherDetail>()
                    };

                    foreach (var detail in sourceJv.Details)
                    {
                        var newDetail = new FilprideJournalVoucherDetail
                        {
                            AccountNo = detail.AccountNo,
                            AccountName = detail.AccountName,
                            TransactionNo = detail.TransactionNo,
                            Debit = detail.Debit,
                            Credit = detail.Credit,
                            SubAccountType = detail.SubAccountType,
                            SubAccountId = detail.SubAccountId,
                            SubAccountName = detail.SubAccountName
                        };
                        newHeader.Details.Add(newDetail);
                    }

                    newJournalVouchers.Add(newHeader);
                    amortization.NextRunDate = dateToday.AddMonths(1);
                    amortization.LastRunDate = dateToday;
                    amortization.OccurrenceRemaining--;
                    amortization.IsActive = amortization.OccurrenceRemaining > 0;
                }

                await _dbContext.FilprideJournalVoucherHeaders.AddRangeAsync(newJournalVouchers);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing amortization for {Date}", dateToday);
                throw;
            }
        }
    }
}
