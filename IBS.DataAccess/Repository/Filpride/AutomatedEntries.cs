using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Utility;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IBS.DataAccess.Repository.Filpride
{
    public class AutomatedEntries : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer;
        private CancellationTokenSource _cancellationTokenSource;

        public AutomatedEntries(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromDays(1)); // Change interval as needed
            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            try
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    return;
                if (DateTime.UtcNow.Day == DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month))
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var _unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                        var cvList = await _dbContext.FilprideCheckVoucherHeaders
                                .Where(cv => cv.StartDate <= DateOnly.FromDateTime(DateTime.UtcNow) && (cv.LastCreatedDate < DateTime.UtcNow || cv.LastCreatedDate == null) && !cv.IsComplete && cv.PostedBy != null)
                                .ToListAsync(_cancellationTokenSource.Token);

                        if (cvList.Count != 0)
                        {
                            foreach (var cv in cvList)
                            {
                                var accountEntries = await _dbContext.FilprideCheckVoucherDetails
                                    .Where(cvd => cvd.TransactionNo == cv.CheckVoucherHeaderNo && (cvd.AccountNo.StartsWith("10201") || cvd.AccountNo.StartsWith("10105")))
                                    .ToListAsync(_cancellationTokenSource.Token);

                                foreach (var accountEntry in accountEntries)
                                {
                                    if (accountEntry.AccountNo.StartsWith("10201"))
                                    {
                                        cv.NumberOfMonthsCreated++;
                                        cv.LastCreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

                                        if (cv.NumberOfMonths == cv.NumberOfMonthsCreated)
                                        {
                                            cv.IsComplete = true;
                                        }

                                        var header = new FilprideJournalVoucherHeader
                                        {
                                            JournalVoucherHeaderNo = await _unitOfWork.FilprideJournalVoucher.GenerateCodeAsync(cv.Company, _cancellationTokenSource.Token),
                                            CVId = cv.CheckVoucherHeaderId,
                                            JVReason = "Depreciation",
                                            Particulars = $"Depreciation of : CV Particulars {cv.Particulars} for the month of {DateTime.UtcNow:MMMM yyyy}.",
                                            Date = DateOnly.FromDateTime(DateTime.UtcNow),
                                            Company = cv.Company,
                                            CreatedBy = cv.CreatedBy,
                                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                                        };

                                        await _dbContext.AddAsync(header);
                                        await _dbContext.SaveChangesAsync();
                                        var details = new List<FilprideJournalVoucherDetail>();

                                        details.Add(new FilprideJournalVoucherDetail
                                        {
                                            AccountNo = "5020107",
                                            AccountName = "Depreciation Expense",
                                            TransactionNo = header.JournalVoucherHeaderNo,
                                            Debit = cv.AmountPerMonth,
                                            Credit = 0,
                                            JournalVoucherHeaderId = header.JournalVoucherHeaderId
                                        });

                                        details.Add(new FilprideJournalVoucherDetail
                                        {
                                            AccountNo = accountEntry.AccountName.Contains("Building") ? "1020104" : "1020105",
                                            AccountName = accountEntry.AccountName.Contains("Building") ? "Accummulated Depreciation - Building and improvements" : "Accummulated Depreciation - Equipment",
                                            TransactionNo = header.JournalVoucherHeaderNo,
                                            Debit = 0,
                                            Credit = cv.AmountPerMonth,
                                            JournalVoucherHeaderId = header.JournalVoucherHeaderId
                                        });

                                        await _dbContext.AddRangeAsync(details);
                                        await _dbContext.SaveChangesAsync();
                                    }
                                    else if (accountEntry.AccountNo.StartsWith("10105"))
                                    {
                                        //Prepaid
                                        cv.NumberOfMonthsCreated++;
                                        cv.LastCreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

                                        if (cv.NumberOfMonths == cv.NumberOfMonthsCreated)
                                        {
                                            cv.IsComplete = true;
                                        }

                                        var header = new FilprideJournalVoucherHeader
                                        {
                                            JournalVoucherHeaderNo = await _unitOfWork.FilprideJournalVoucher.GenerateCodeAsync(cv.Company, _cancellationTokenSource.Token),
                                            CVId = cv.CheckVoucherHeaderId,
                                            JVReason = "Prepaid",
                                            Particulars = $"Prepaid: CV Particulars {cv.Particulars} for the month of {DateTime.UtcNow:MMMM yyyy}.",
                                            Date = DateOnly.FromDateTime(DateTime.UtcNow),
                                            CreatedBy = cv.CreatedBy,
                                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                                        };

                                        await _dbContext.AddAsync(header);
                                        await _dbContext.SaveChangesAsync();

                                        var details = new List<FilprideJournalVoucherDetail>();

                                        details.Add(new FilprideJournalVoucherDetail
                                        {
                                            AccountNo = "5020115",
                                            AccountName = "Rental Expense",
                                            TransactionNo = header.JournalVoucherHeaderNo,
                                            Debit = cv.AmountPerMonth,
                                            Credit = 0
                                        });

                                        details.Add(new FilprideJournalVoucherDetail
                                        {
                                            AccountNo = "1010501",
                                            AccountName = "Prepaid Expenses - Rental",
                                            TransactionNo = header.JournalVoucherHeaderNo,
                                            Debit = 0,
                                            Credit = cv.AmountPerMonth
                                        });

                                        await _dbContext.AddRangeAsync(details);
                                        await _dbContext.SaveChangesAsync();
                                    }
                                    else
                                    {
                                        //Accrued
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
