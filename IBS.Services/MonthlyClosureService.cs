using Google.Apis.Drive.v3.Data;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride;
using IBS.Models.Filpride.Books;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
            ILogger<MonthlyClosureService> logger,
            IUnitOfWork unitOfWork)
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

                if (!isMonthAlreadyLocked)
                {
                    throw new InvalidOperationException($"{monthDate:MMMM yyyy} is not locked.");
                }

                var hasUnliftedDrs = await _dbContext.FilprideDeliveryReceipts
                    .AnyAsync(x => x.Company == company &&
                                   x.Date.Month == monthDate.Month &&
                                   x.Date.Year == monthDate.Year &&
                                   x.VoidedBy == null &&
                                   x.CanceledBy == null &&
                                   !x.HasReceivingReport, cancellationToken);

                if (hasUnliftedDrs)
                {
                    throw new InvalidOperationException($"There are still unlifted DRs for {monthDate:MMMM yyyy}. " +
                                                        $"Closing for this month cannot proceed.");
                }

                /// TODO await AutoReversalForCvWithoutDcrDate(monthDate, company, cancellationToken);
                await ComputeNibit(monthDate, company, cancellationToken);
                await RecordNotUpdatedSales(monthDate, company, cancellationToken);
                await RecordNotUpdatedPurchases(monthDate, company, cancellationToken);
                await RecordGlPeriodBalance(monthDate, company, cancellationToken);

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

        private async Task AutoReversalForCvWithoutDcrDate(DateOnly periodMonth, string company, CancellationToken cancellationToken)
        {
            try
            {
                var endOfPreviousMonth = periodMonth.AddDays(-1);

                var disbursementsWithoutDcrDate = await _dbContext.FilprideCheckVoucherHeaders
                    .Where(cv =>
                        cv.Company == company &&
                        cv.Date.Month == periodMonth.Month &&
                        cv.Date.Year == periodMonth.Year &&
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
                    var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                    var ledgers = new List<FilprideGeneralLedgerBook>();
                    var journalBooks = new List<FilprideJournalBook>();

                    var details = await _dbContext.FilprideCheckVoucherDetails
                        .Where(cvd => cvd.CheckVoucherHeaderId == cv.CheckVoucherHeaderId)
                        .ToListAsync(cancellationToken);

                    foreach (var detail in details)
                    {
                        var account = accountTitlesDto.Find(c => c.AccountNumber == detail.AccountNo)
                                      ?? throw new ArgumentException($"Account title '{detail.AccountNo}' not found.");

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = endOfPreviousMonth,
                            Reference = cv.CheckVoucherHeaderNo!,
                            Description = cv.Particulars!,
                            AccountId = account.AccountId,
                            AccountNo = account.AccountNumber,
                            AccountTitle = account.AccountName,
                            Debit = detail.Credit,
                            Credit = detail.Debit,
                            Company = cv.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            SubAccountId = detail.SubAccountId,
                            SubAccountType = detail.SubAccountType,
                            SubAccountName = detail.SubAccountName,
                            ModuleType = nameof(ModuleType.Disbursement)
                        });

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = periodMonth,
                            Reference = cv.CheckVoucherHeaderNo!,
                            Description = cv.Particulars!,
                            AccountId = account.AccountId,
                            AccountNo = account.AccountNumber,
                            AccountTitle = account.AccountName,
                            Debit = detail.Debit,
                            Credit = detail.Credit,
                            Company = cv.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            SubAccountId = detail.SubAccountId,
                            SubAccountType = detail.SubAccountType,
                            SubAccountName = detail.SubAccountName,
                            ModuleType = nameof(ModuleType.Disbursement)
                        });

                        journalBooks.Add(new FilprideJournalBook
                        {
                            Date = endOfPreviousMonth,
                            Reference = cv.CheckVoucherHeaderNo!,
                            Description = cv.Particulars!,
                            AccountTitle = $"{account.AccountNumber} {account.AccountName}",
                            Debit = detail.Credit,
                            Credit = detail.Debit,
                            Company = cv.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        journalBooks.Add(new FilprideJournalBook
                        {
                            Date = endOfPreviousMonth,
                            Reference = cv.CheckVoucherHeaderNo!,
                            Description = cv.Particulars!,
                            AccountTitle = $"{account.AccountNumber} {account.AccountName}",
                            Debit = detail.Debit,
                            Credit = detail.Credit,
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

        private async Task ComputeNibit(DateOnly periodMonth, string company, CancellationToken cancellationToken)
        {
            try
            {
                var generalLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account)
                    .ThenInclude(filprideChartOfAccount => filprideChartOfAccount.ParentAccount) // Level 4
                    .Where(gl =>
                        gl.Date.Month == periodMonth.Month &&
                        gl.Date.Year == periodMonth.Year &&
                        gl.Company == company)
                    .ToListAsync(cancellationToken);

                if (!generalLedgers.Any())
                {
                    return;
                }

                if (!_unitOfWork.FilprideCheckVoucher.IsJournalEntriesBalanced(generalLedgers))
                {
                    throw new InvalidOperationException($"GL balance mismatch. " +
                                                        $"Debit:{generalLedgers.Sum(g => g.Debit):N2}, " +
                                                        $"Credit: {generalLedgers.Sum(g => g.Credit):N2}");
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
                    var accountBalance = account.Sum(a =>
                        a.Account.NormalBalance == nameof(NormalBalance.Debit)
                            ? a.Debit - a.Credit
                            : a.Credit - a.Debit);

                    if (account.Key.AccountNumber!.StartsWith("4") || account.Key.AccountNumber!.StartsWith("601"))
                    {
                        nibit += accountBalance;
                    }
                    else
                    {
                        nibit -= accountBalance;
                    }
                }

                var nibitForThePeriod = new FilprideMonthlyNibit
                {
                    Month = periodMonth.Month,
                    Year = periodMonth.Year,
                    Company = company,
                    NetIncome = nibit,
                    PriorPeriodAdjustment = generalLedgers
                        .Where(g => g.AccountTitle.Contains("Prior Period"))
                        .Sum(g => g.Debit - g.Credit),
                };

                var beginning = await _dbContext.FilprideMonthlyNibits
                    .OrderByDescending(m => m.Year)
                    .ThenByDescending(m => m.Month)
                    .FirstOrDefaultAsync(m => m.Company == company, cancellationToken);

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

        private async Task RecordNotUpdatedSales(DateOnly periodMonth, string company, CancellationToken cancellationToken)
        {
            try
            {
                var cosNotUpdatedPrice = await _dbContext.FilprideCustomerOrderSlips
                    .Include(x => x.DeliveryReceipts)
                    .Where(x =>
                        x.Company == company &&
                        x.Date.Month == periodMonth.Month &&
                        x.Date.Year == periodMonth.Year &&
                        x.OldPrice == 0 &&
                        x.DeliveredQuantity > 0)
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

        private async Task RecordNotUpdatedPurchases(DateOnly periodMonth, string company, CancellationToken cancellationToken)
        {
            try
            {
                var poNotUpdatedPrice = await _dbContext.FilpridePurchaseOrders
                    .Include(x => x.ReceivingReports)
                    .Where(x =>
                        x.Company == company &&
                        x.Date.Month == periodMonth.Month &&
                        x.Date.Year == periodMonth.Year &&
                        x.UnTriggeredQuantity != 0 &&
                        x.QuantityReceived > 0)
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

        private async Task RecordGlPeriodBalance(DateOnly periodMonth, string company, CancellationToken cancellationToken)
        {
            try
            {
                var periodEnd = periodMonth.AddMonths(1).AddDays(-1);

                // Get all accounts from COA for this company
                var allAccounts = await _dbContext.FilprideChartOfAccounts
                    .OrderBy(x => x.AccountNumber)
                    .ToListAsync(cancellationToken);

                if (allAccounts.Count == 0)
                {
                    _logger.LogWarning("No accounts found in COA");
                    return;
                }

                var glEntries = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(x => x.Account)
                    .Where(x =>
                        x.Company == company &&
                        x.Date.Month == periodMonth.Month &&
                        x.Date.Year == periodMonth.Year)
                    .ToListAsync(cancellationToken);

                var glGroupedByAccount = glEntries
                    .GroupBy(x => x.AccountId)
                    .Select(g => new
                    {
                        AccountId = g.Key,
                        g.First().Account,
                        TotalDebit = g.Sum(x => x.Debit),
                        TotalCredit = g.Sum(x => x.Credit)
                    })
                    .ToDictionary(x => x.AccountId);

                var glGroupedBySubAccount = glEntries
                    .Where(x => x.SubAccountId.HasValue && x.SubAccountType.HasValue)
                    .GroupBy(x => new
                    {
                        x.AccountId,
                        x.SubAccountId,
                        x.SubAccountType,
                        x.SubAccountName
                    })
                    .Select(g => new
                    {
                        g.Key.AccountId,
                        g.Key.SubAccountId,
                        g.Key.SubAccountType,
                        g.Key.SubAccountName,
                        Account = g.First().Account,
                        TotalDebit = g.Sum(x => x.Debit),
                        TotalCredit = g.Sum(x => x.Credit)
                    })
                    .ToList();

                var accountIds = allAccounts.Select(x => x.AccountId).ToList();
                var closedAt = DateTimeHelper.GetCurrentPhilippineTime();

                // Get beginning balances for all accounts
                var beginningBalancesDict = await _dbContext.FilprideGlPeriodBalances
                    .Where(x => accountIds.Contains(x.AccountId) && x.PeriodEndDate < periodEnd)
                    .GroupBy(x => x.AccountId)
                    .Select(g => new
                    {
                        AccountId = g.Key,
                        EndingBalance = g.OrderByDescending(x => x.PeriodEndDate)
                                         .Select(x => x.EndingBalance)
                                         .FirstOrDefault()
                    })
                    .ToDictionaryAsync(x => x.AccountId, x => x.EndingBalance, cancellationToken);

                var glBalances = new List<FilprideGLPeriodBalance>();

                // Process all accounts from COA
                foreach (var account in allAccounts)
                {
                    var beginningBalance = beginningBalancesDict.GetValueOrDefault(account.AccountId, 0m);

                    // Get totals from GL entries (zero if no transactions)
                    var accountTransactions = glGroupedByAccount.GetValueOrDefault(account.AccountId);
                    var totalDebit = accountTransactions?.TotalDebit ?? 0m;
                    var totalCredit = accountTransactions?.TotalCredit ?? 0m;

                    var totalBalance = account.NormalBalance == nameof(NormalBalance.Debit)
                        ? totalDebit - totalCredit
                        : totalCredit - totalDebit;

                    var endingBalance = beginningBalance + totalBalance;

                    glBalances.Add(new FilprideGLPeriodBalance
                    {
                        AccountId = account.AccountId,
                        PeriodStartDate = periodMonth,
                        PeriodEndDate = periodEnd,
                        FiscalYear = periodMonth.Year,
                        FiscalPeriod = periodMonth.Month,
                        BeginningBalance = beginningBalance,
                        DebitTotal = totalDebit,
                        CreditTotal = totalCredit,
                        EndingBalance = endingBalance,
                        IsClosed = true,
                        ClosedAt = closedAt,
                        Company = company
                    });
                }

                var subAccountBalances = new List<FilprideGLSubAccountBalance>();

                if (glGroupedBySubAccount.Any())
                {
                    var subAccountBeginningBalances = await _dbContext.FilprideGlSubAccountBalances
                        .Where(x => accountIds.Contains(x.AccountId) && x.PeriodEndDate < periodEnd)
                        .ToListAsync(cancellationToken);

                    var subAccountBalancesDict = subAccountBeginningBalances
                        .GroupBy(x => new { x.AccountId, x.SubAccountId, x.SubAccountType })
                        .Select(g => new
                        {
                            Key = g.Key,
                            EndingBalance = g.OrderByDescending(x => x.PeriodEndDate)
                                             .Select(x => x.EndingBalance)
                                             .FirstOrDefault()
                        })
                        .ToDictionary(x => x.Key, x => x.EndingBalance);

                    foreach (var subAccount in glGroupedBySubAccount)
                    {
                        var key = new
                        {
                            subAccount.AccountId,
                            SubAccountId = subAccount.SubAccountId!.Value,
                            SubAccountType = subAccount.SubAccountType!.Value
                        };

                        var beginningBalance = subAccountBalancesDict.GetValueOrDefault(key, 0m);

                        var totalBalance = subAccount.Account.NormalBalance == nameof(NormalBalance.Debit)
                            ? subAccount.TotalDebit - subAccount.TotalCredit
                            : subAccount.TotalCredit - subAccount.TotalDebit;

                        var endingBalance = beginningBalance + totalBalance;

                        subAccountBalances.Add(new FilprideGLSubAccountBalance
                        {
                            AccountId = subAccount.AccountId,
                            SubAccountType = subAccount.SubAccountType.Value,
                            SubAccountId = subAccount.SubAccountId.Value,
                            SubAccountName = subAccount.SubAccountName!,
                            PeriodStartDate = periodMonth,
                            PeriodEndDate = periodEnd,
                            FiscalYear = periodMonth.Year,
                            FiscalPeriod = periodMonth.Month,
                            BeginningBalance = beginningBalance,
                            DebitTotal = subAccount.TotalDebit,
                            CreditTotal = subAccount.TotalCredit,
                            EndingBalance = endingBalance,
                            IsClosed = true,
                            Company = company
                        });
                    }
                }

                if (glBalances.Any())
                {
                    await _dbContext.FilprideGlPeriodBalances.AddRangeAsync(glBalances, cancellationToken);
                }

                if (subAccountBalances.Any())
                {
                    await _dbContext.FilprideGlSubAccountBalances.AddRangeAsync(subAccountBalances, cancellationToken);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Recorded GL balances for period {PeriodMonth} and company {Company}: {AccountCount} accounts ({ActiveCount} with transactions), {SubAccountCount} sub-accounts.",
                    periodMonth.ToString("yyyy-MM-dd"), company, glBalances.Count, glGroupedByAccount.Count, subAccountBalances.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "An error occurred while recording the GL balance for period {PeriodMonth} and company {Company}.",
                    periodMonth.ToString("yyyy-MM-dd"), company);
                throw;
            }
        }
    }
}
