using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
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
                var today = DateTimeHelper.GetCurrentPhilippineTime();
                var previousMonth = today.AddMonths(-1);
                await InTransit(previousMonth);
                await CheckTheUntriggeredPurchaseOrders(previousMonth);
                await AutoReversalForCvWithoutDcrDate(previousMonth);
                _logger.LogInformation($"MonthlyClosureService is running at: {DateTimeHelper.GetCurrentPhilippineTime()}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
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

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var logisticUsers = await _dbContext.ApplicationUsers
                    .Where(u => u.Department == SD.Department_Logistics)
                    .ToListAsync();

                var managementAccountingUsers = await _dbContext.ApplicationUsers
                    .Where(u => u.Department == SD.Department_ManagementAccounting)
                    .ToListAsync();

                var link = "<a href='/Filpride/Report/DispatchReport' target='_blank'>Dispatch Report</a>";
                var message = $"Is the in-transit report final? Kindly generate the {link} and " +
                              $"answer this question to enable the creation of DR for the month of " +
                              $"{DateTime.UtcNow:MMM yyyy}.";
                var ccMessage = "CC: Management Accounting";

                // Send notifications to Logistics
                foreach (var user in logisticUsers)
                {
                    await _unitOfWork.Notifications.AddNotificationAsync(user.Id, $"{message} {ccMessage}", true);
                }

                // Send notifications to Management Accounting
                foreach (var user in managementAccountingUsers)
                {
                    await _unitOfWork.Notifications.AddNotificationAsync(user.Id, $"{message} {ccMessage}", true);
                }

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
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing in-transit notifications.");
                await transaction.RollbackAsync();
            }
        }

        private async Task CheckTheUntriggeredPurchaseOrders(DateTime previousMonth)
        {

            var purchaseOrders = await _unitOfWork.FilpridePurchaseOrder
                .GetUntriggeredPurchaseOrderNumbersAsync();

            if (!purchaseOrders.Any())
            {
                return;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var tnsUsers = await _dbContext.ApplicationUsers
                    .Where(u => u.Department == SD.Department_TradeAndSupply)
                    .ToListAsync();

                var managementAccountingUsers = await _dbContext.ApplicationUsers
                    .Where(u => u.Department == SD.Department_ManagementAccounting)
                    .ToListAsync();

                var purchaseOrderNosList = string.Join(", ", purchaseOrders);
                var message = $"Kindly trigger the following purchase orders: {purchaseOrderNosList}. " +
                              $"To enable the creation of purchase order for the month of " +
                              $"{DateTime.UtcNow:MMM yyyy}.";
                var ccMessage = "CC: Management Accounting";

                // Send notifications to Logistics
                foreach (var user in tnsUsers)
                {
                    await _unitOfWork.Notifications.AddNotificationAsync(user.Id, $"{message} {ccMessage}");
                }

                // Send notifications to Management Accounting
                foreach (var user in managementAccountingUsers)
                {
                    await _unitOfWork.Notifications.AddNotificationAsync(user.Id, $"{message} {ccMessage}");
                }

                var lockCreationOfPo = await _dbContext.AppSettings
                    .FirstOrDefaultAsync(a => a.SettingKey == AppSettingKey.LockTheCreationOfPo);

                if (lockCreationOfPo == null)
                {
                    lockCreationOfPo = new AppSetting
                    {
                        SettingKey = AppSettingKey.LockTheCreationOfPo,
                        Value = "true"
                    };

                    await _dbContext.AppSettings.AddAsync(lockCreationOfPo);
                }
                else
                {
                    lockCreationOfPo.Value = "true";
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing un-triggered purchase orders notifications.");
                await transaction.RollbackAsync();
            }

        }

        private async Task AutoReversalForCvWithoutDcrDate(DateTime previousMonth)
        {

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var today = DateTimeHelper.GetCurrentPhilippineTime();

                // Start of the current month
                var startOfMonth = new DateTime(today.Year, today.Month, 1);

                var endOfPreviousMonth = startOfMonth.AddDays(-1);

                var disbursementsWithoutDcrDate = await _dbContext.FilprideCheckVoucherHeaders
                    .Where(cv =>
                        cv.Date.Month == previousMonth.AddMonths(-3).Month &&
                        cv.Date.Year == previousMonth.AddMonths(-3).Year &&
                        cv.CvType != nameof(CVType.Invoicing) &&
                        cv.PostedBy != null &&
                        cv.DcrDate == null
                    )
                    .ToListAsync();

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
                        .ToListAsync();

                    foreach (var cvDetails in details)
                    {
                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = DateOnly.FromDateTime(endOfPreviousMonth),
                            Reference = cv.CheckVoucherHeaderNo,
                            Description = cv.Particulars,
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
                            Date = DateOnly.FromDateTime(startOfMonth),
                            Reference = cv.CheckVoucherHeaderNo,
                            Description = cv.Particulars,
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
                            Date = DateOnly.FromDateTime(endOfPreviousMonth),
                            Reference = cv.CheckVoucherHeaderNo,
                            Description = cv.Particulars,
                            AccountTitle = $"{cvDetails.AccountNo} {cvDetails.AccountName}",
                            Debit = cvDetails.Credit,
                            Credit = cvDetails.Debit,
                            Company = cv.Company,
                            CreatedBy = "SYSTEM GENERATED",
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        });

                        journalBooks.Add(new FilprideJournalBook
                        {
                            Date = DateOnly.FromDateTime(endOfPreviousMonth),
                            Reference = cv.CheckVoucherHeaderNo,
                            Description = cv.Particulars,
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

                    await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers);
                    await _dbContext.FilprideJournalBooks.AddRangeAsync(journalBooks);
                    await _dbContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the auto reversal for CV.");
                await transaction.RollbackAsync();
            }
        }
    }
}
