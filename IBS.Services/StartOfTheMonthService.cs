using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace IBS.Services
{
    [DisallowConcurrentExecution]
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
                await SendNotificationToManagementAccounting(previousMonthDate);
                await SendNotificationToCNC(previousMonthDate);
                await ReverseTheJvEntries();

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

                var groupedAmortizations = amortizationSetting
                    .GroupBy(a => new { a.JvHeader.Company, a.JvHeader.Type })
                    .ToList();

                foreach (var group in groupedAmortizations)
                {
                    var baseCode = await _unitOfWork.FilprideJournalVoucher
                        .GenerateCodeAsync(group.Key.Company, group.Key.Type);

                    var offset = 0;
                    foreach (var amortization in group)
                    {
                        var sourceJv = amortization.JvHeader;

                        if (sourceJv?.Details == null || sourceJv.Details.Count == 0)
                        {
                            throw new InvalidOperationException(
                                $"The source journal voucher for amortization with ID {amortization.JvId} is missing or has no details.");
                        }

                        var generatedCode = IncrementCode(baseCode, offset++);

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
                            CreatedBy = "SYSTEM GENERATED",
                            Company = sourceJv.Company,
                            JvType = nameof(JvType.Amortization),
                            Status = nameof(JvStatus.Pending),
                            Details = sourceJv.Details.Select(detail => new FilprideJournalVoucherDetail
                            {
                                AccountNo = detail.AccountNo,
                                AccountName = detail.AccountName,
                                TransactionNo = detail.TransactionNo,
                                Debit = detail.Debit,
                                Credit = detail.Credit,
                                SubAccountType = detail.SubAccountType,
                                SubAccountId = detail.SubAccountId,
                                SubAccountName = detail.SubAccountName
                            }).ToList()
                        };

                        newJournalVouchers.Add(newHeader);
                        amortization.LastRunDate = dateToday;
                        if (amortization.OccurrenceRemaining > 0)
                        {
                            amortization.OccurrenceRemaining--;
                        }
                        amortization.IsActive = amortization.OccurrenceRemaining > 0;
                        amortization.NextRunDate = amortization.IsActive ? dateToday.AddMonths(1) : null;
                    }
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

        private static string IncrementCode(string baseCode, int offset)
        {
            if (baseCode.StartsWith("JVU"))
            {
                var numericPart = baseCode.Substring(3);
                var incremented = long.Parse(numericPart) + offset;
                return "JVU" + incremented.ToString("D9");
            }
            else
            {
                var numericPart = baseCode.Substring(2);
                var incremented = long.Parse(numericPart) + offset;
                return "JV" + incremented.ToString("D10");
            }
        }

        private async Task SendNotificationToManagementAccounting(DateOnly previousMonth)
        {
            var users = await _dbContext.ApplicationUsers
                .Where(u => u.IsActive && u.Department == SD.Department_ManagementAccounting)
                .Select(u => u.Id)
                .ToListAsync();

            var message = $"Kindly generate the journal voucher list for {previousMonth:MMM yyyy}.";

            await _unitOfWork.Notifications.AddNotificationToMultipleUsersAsync(users, message);
        }

        private async Task SendNotificationToCNC(DateOnly previousMonth)
        {
            var users = await _dbContext.ApplicationUsers
                .Where(u => u.IsActive && u.Department == SD.Department_CreditAndCollection)
                .Select(u => u.Id)
                .ToListAsync();

            var message = $"Please ensure the transaction fee is created before the system closes the books for {previousMonth:MMM yyyy}.";

            await _unitOfWork.Notifications.AddNotificationToMultipleUsersAsync(users, message);
        }

        private async Task ReverseTheJvEntries()
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var currentDateTime = DateTimeHelper.GetCurrentPhilippineTime();
                var currentDate = DateOnly.FromDateTime(currentDateTime);

                var journalVouchers = await _unitOfWork.FilprideJournalVoucher
                    .GetAllAsync(x => x.AutoReverseNextMonth &&
                        !x.IsAutoReverseNextMonthProcessed);

                var generalLedgerList = await _dbContext.FilprideGeneralLedgerBooks
                    .Where(x => journalVouchers.Select(x => x.JournalVoucherHeaderNo).Contains(x.Reference))
                    .ToListAsync();

                var generalLedger = new List<FilprideGeneralLedgerBook>();

                foreach (var journalVoucher in generalLedgerList)
                {
                    var reversalEntry = new FilprideGeneralLedgerBook
                    {
                        Date = currentDate,
                        Reference = journalVoucher.Reference,
                        AccountNo = journalVoucher.AccountNo,
                        AccountTitle = journalVoucher.AccountTitle,
                        Description = "Reversal of entries due to accrual",
                        Debit = journalVoucher.Credit,
                        Credit = journalVoucher.Debit,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = currentDateTime,
                        IsPosted = true,
                        Company = journalVoucher.Company,
                        AccountId = journalVoucher.AccountId,
                        SubAccountType = journalVoucher.SubAccountType,
                        SubAccountId = journalVoucher.SubAccountId,
                        SubAccountName = journalVoucher.SubAccountName,
                        ModuleType = journalVoucher.ModuleType,
                    };

                    generalLedger.Add(reversalEntry);
                }
                foreach (var journalVoucher in journalVouchers)
                {
                    journalVoucher.IsAutoReverseNextMonthProcessed = true;
                }

                await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(generalLedger);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to do reverse journal voucher entries. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, "SYSTEM GENERATED");
                await transaction.RollbackAsync();
            }
        }
    }
}
