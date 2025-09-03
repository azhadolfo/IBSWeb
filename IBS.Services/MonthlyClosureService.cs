using Google.Apis.Drive.v3.Data;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride;
using IBS.Models.Filpride.Books;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace IBS.Services
{

    public interface IMonthlyClosureService
    {
        Task Execute(DateOnly monthDate, string company, string user, CancellationToken cancellationToken = default);
    }

    public class MonthlyClosureService : IMonthlyClosureService
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly ILogger<MonthlyClosureService> _logger;

        private readonly IUnitOfWork _unitOfWork;

        public MonthlyClosureService(ApplicationDbContext dbContext,
            ILogger<MonthlyClosureService> logger, IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(DateOnly monthDate, string company, string user, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var isMonthAlreadyLocked = await _unitOfWork.IsPeriodPostedAsync(monthDate, cancellationToken);

                if (isMonthAlreadyLocked)
                {
                    throw new InvalidOperationException($"{monthDate:MMMM yyyy} is already locked.");
                }

                var hasUnliftedDrs = await _dbContext.FilprideDeliveryReceipts
                    .AnyAsync(x => x.Date.Month == monthDate.Month
                                   && x.Date.Year == monthDate.Year
                                   && x.VoidedBy == null
                                   && x.CanceledBy == null
                                   && !x.HasReceivingReport, cancellationToken);

                if (hasUnliftedDrs)
                {
                    throw new InvalidOperationException($"There are still unlifted DRs for {monthDate:MMMM yyyy}. " +
                                                        $"Closing for this month cannot proceed.");
                }

                await AutoReversalForCvWithoutDcrDate(monthDate, cancellationToken);
                await ComputeNibit(monthDate, cancellationToken);
                await RecordNotUpdatedSales(monthDate, cancellationToken);
                await RecordNotUpdatedPurchases(monthDate, cancellationToken);

                var postedPeriod = new PostedPeriod
                {
                    Company = company,
                    Month = monthDate.Month,
                    Year = monthDate.Year,
                    IsPosted = true,
                    PostedBy = user,
                    PostedOn = DateTimeHelper.GetCurrentPhilippineTime()
                };

                await _dbContext.PostedPeriods.AddAsync(postedPeriod, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task InTransit(DateTime previousMonth)
        {
            var hadInTransit = await _dbContext.FilprideDeliveryReceipts
                .AnyAsync(dr =>
                    dr.Date.Month == previousMonth.Month &&
                    dr.Date.Year == previousMonth.Year &&
                    dr.Status == nameof(DRStatus.PendingDelivery));

            if (!hadInTransit)
            {
                return;
            }

            try
            {
                var users = await _dbContext.ApplicationUsers
                    .Where(u => u.Department == SD.Department_Logistics ||
                                u.Department == SD.Department_ManagementAccounting)
                    .Select(u => u.Id)
                    .ToListAsync();


                var link = "<a href='/Filpride/Report/DispatchReport' target='_blank'>Dispatch Report</a>";
                var message = $"Is the in-transit report final? Kindly generate the {link} and " +
                              $"answer this question to enable the creation of DR for the month of " +
                              $"{DateTimeHelper.GetCurrentPhilippineTime():MMM yyyy}. \n" +
                              $"CC: Management Accounting";

                await _unitOfWork.Notifications.AddNotificationToMultipleUsersAsync(users, message);

                var lockCreationOfDr = await _dbContext.AppSettings
                    .FirstOrDefaultAsync(a => a.SettingKey == AppSettingKey.LockTheCreationOfDr);

                if (lockCreationOfDr == null)
                {
                    lockCreationOfDr = new AppSetting
                    {
                        SettingKey = AppSettingKey.LockTheCreationOfDr,
                        Value = "true"
                    };

                    await _dbContext.AppSettings.AddAsync(lockCreationOfDr);
                }
                else
                {
                    lockCreationOfDr.Value = "true";
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing in-transit notifications.");
                throw;
            }
        }

        private async Task AutoReversalForCvWithoutDcrDate(DateOnly previousMonth, CancellationToken cancellationToken)
        {
            try
            {
                var endOfPreviousMonth = previousMonth.AddDays(-1);

                var disbursementsWithoutDcrDate = await _dbContext.FilprideCheckVoucherHeaders
                    .Where(cv =>
                        cv.Date.Month == previousMonth.Month &&
                        cv.Date.Year == previousMonth.Year &&
                        cv.CvType != nameof(CVType.Invoicing) &&
                        cv.PostedBy != null &&
                        cv.DcrDate == null
                    )
                    .ToListAsync(cancellationToken);

                if (disbursementsWithoutDcrDate.Count == 0)
                {
                    return;
                }

                foreach (var cv in disbursementsWithoutDcrDate)
                {
                    var ledgers = new List<FilprideGeneralLedgerBook>();
                    var journalBooks = new List<FilprideJournalBook>();

                    var details = await _dbContext.FilprideCheckVoucherDetails
                        .Where(cvd => cvd.CheckVoucherHeaderId == cv.CheckVoucherHeaderId)
                        .ToListAsync(cancellationToken);

                    foreach (var cvDetails in details)
                    {
                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = endOfPreviousMonth,
                            Reference = cv.CheckVoucherHeaderNo!,
                            Description = cv.Particulars!,
                            AccountNo = cvDetails.AccountNo,
                            AccountTitle = cvDetails.AccountName,
                            Debit = cvDetails.Credit,
                            Credit = cvDetails.Debit,
                            Company = cv.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = previousMonth,
                            Reference = cv.CheckVoucherHeaderNo!,
                            Description = cv.Particulars!,
                            AccountNo = cvDetails.AccountNo,
                            AccountTitle = cvDetails.AccountName,
                            Debit = cvDetails.Debit,
                            Credit = cvDetails.Credit,
                            Company = cv.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        journalBooks.Add(new FilprideJournalBook
                        {
                            Date = endOfPreviousMonth,
                            Reference = cv.CheckVoucherHeaderNo!,
                            Description = cv.Particulars!,
                            AccountTitle = $"{cvDetails.AccountNo} {cvDetails.AccountName}",
                            Debit = cvDetails.Credit,
                            Credit = cvDetails.Debit,
                            Company = cv.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        journalBooks.Add(new FilprideJournalBook
                        {
                            Date = endOfPreviousMonth,
                            Reference = cv.CheckVoucherHeaderNo!,
                            Description = cv.Particulars!,
                            AccountTitle = $"{cvDetails.AccountNo} {cvDetails.AccountName}",
                            Debit = cvDetails.Debit,
                            Credit = cvDetails.Credit,
                            Company = cv.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        if (!_unitOfWork.FilprideCheckVoucher.IsJournalEntriesBalanced(ledgers))
                        {
                            throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                        }

                    }

                    await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
                    await _dbContext.FilprideJournalBooks.AddRangeAsync(journalBooks, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the auto reversal for CV.");
                throw;
            }
        }

        private async Task ComputeNibit(DateOnly previousMonth, CancellationToken cancellationToken)
        {
            try
            {
                var generalLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac!.ParentAccount) // Level 2
                    .ThenInclude(ac => ac!.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date.Month == previousMonth.Month &&
                        gl.Date.Year == previousMonth.Year &&
                        gl.AccountId != null &&  // TODO Uncomment this if the GL is fixed
                        gl.Company == "Filpride") // TODO Make this dynamic later on
                    .ToListAsync(cancellationToken);

                if (!generalLedgers.Any())
                {
                    return;
                }

                if (generalLedgers.Sum(g => g.Debit) != generalLedgers.Sum(g => g.Debit))
                {
                    throw new InvalidOperationException($"GL balance mismatch. " +
                                                        $"Debit:{generalLedgers.Sum(g => g.Debit):N4}, " +
                                                        $"Credit: {generalLedgers.Sum(g => g.Credit):N4}");
                }

                var groupByLevelOne = generalLedgers
                    .OrderBy(gl => gl.Account.AccountNumber)
                    .Where(gl => gl.Account.FinancialStatementType == nameof(FinancialStatementType.PnL))
                    .GroupBy(gl =>
                    {
                        // Traverse the account hierarchy to find the top-level parent account
                        var currentAccount = gl.Account;
                        while (currentAccount.ParentAccount != null)
                        {
                            currentAccount = currentAccount.ParentAccount;
                        }
                        // Return the top-level parent account (mother account)
                        return new { currentAccount.AccountNumber, currentAccount.AccountName };
                    });

                decimal nibit = 0;
                foreach (var account in groupByLevelOne)
                {
                    if (nibit == 0)
                    {
                        nibit += account.Sum(a => a.Account.NormalBalance == nameof(NormalBalance.Debit) ?
                            a.Debit - a.Credit :
                            a.Credit - a.Debit);

                        continue;
                    }

                    nibit -= account.Sum(a => a.Account.NormalBalance == nameof(NormalBalance.Debit) ?
                        a.Debit - a.Credit :
                        a.Credit - a.Debit);
                }

                var nibitForThePeriod = new FilprideMonthlyNibit
                {
                    Month = previousMonth.Month,
                    Year = previousMonth.Year,
                    Company = "Filpride", // TODO Make this dynamic soon
                    NetIncome = nibit,
                    PriorPeriodAdjustment = generalLedgers
                        .Where(g => g.AccountTitle.Contains("Prior Period"))
                        .Sum(g => g.Debit - g.Credit),
                };

                var beginning = await _dbContext.FilprideMonthlyNibits
                    .OrderByDescending(m => m.Year)
                    .ThenByDescending(m => m.Month)
                    .FirstOrDefaultAsync(cancellationToken);

                if (beginning != null)
                {
                    nibitForThePeriod.BeginningBalance = beginning.EndingBalance;
                }

                nibitForThePeriod.EndingBalance = nibitForThePeriod.BeginningBalance + nibitForThePeriod.NetIncome + nibitForThePeriod.PriorPeriodAdjustment;

                await _dbContext.FilprideMonthlyNibits.AddAsync(nibitForThePeriod, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while computing the nibit for the month.");
                throw;
            }
        }

        private async Task RecordNotUpdatedSales(DateOnly previousMonth, CancellationToken cancellationToken)
        {
            try
            {
                var cosNotUpdatedPrice = await _dbContext.FilprideCustomerOrderSlips
                    .Include(x => x.DeliveryReceipts)
                    .Where(x => x.Date.Month == previousMonth.Month
                                      && x.Date.Year == previousMonth.Year
                                      && x.OldPrice == 0
                                      && x.DeliveredQuantity > 0)
                    .ToListAsync(cancellationToken);

                if (cosNotUpdatedPrice.Count == 0)
                {
                    return;
                }

                var lockedRecordQueues = new List<FilprideSalesLockedRecordsQueue>();
                var lockedDate = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime());

                foreach (var cos in cosNotUpdatedPrice)
                {
                    foreach (var dr in cos.DeliveryReceipts!)
                    {
                        lockedRecordQueues.Add(new FilprideSalesLockedRecordsQueue
                        {
                            LockedDate = lockedDate,
                            DeliveryReceiptId = dr.DeliveryReceiptId,
                            Quantity = dr.Quantity,
                            Price = cos.DeliveredPrice
                        });
                    }
                }

                await _dbContext.FilprideSalesLockedRecordsQueues.AddRangeAsync(lockedRecordQueues, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while recording the not updated sales for the month.");
                throw;
            }
        }

        private async Task RecordNotUpdatedPurchases(DateOnly previousMonth, CancellationToken cancellationToken)
        {
            try
            {
                var poNotUpdatedPrice = await _dbContext.FilpridePurchaseOrders
                    .Include(x => x.ReceivingReports)
                    .Where(x => x.Date.Month == previousMonth.Month
                                && x.Date.Year == previousMonth.Year
                                && x.UnTriggeredQuantity != 0
                                && x.QuantityReceived > 0)
                    .ToListAsync(cancellationToken);

                if (poNotUpdatedPrice.Count == 0)
                {
                    return;
                }

                var lockedRecordQueues = new List<FilpridePurchaseLockedRecordsQueue>();
                var lockedDate = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime());

                foreach (var po in poNotUpdatedPrice)
                {
                    foreach (var rr in po.ReceivingReports!)
                    {
                        lockedRecordQueues.Add(new FilpridePurchaseLockedRecordsQueue
                        {
                            LockedDate = lockedDate,
                            ReceivingReportId = rr.ReceivingReportId,
                            Quantity = rr.QuantityReceived,
                            Price =  po.Price
                        });
                    }
                }

                await _dbContext.FilpridePurchaseLockedRecordsQueues.AddRangeAsync(lockedRecordQueues, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while recording the not updated sales for the month.");
                throw;
            }
        }
    }
}
