using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ReceivingReportRepository : Repository<FilprideReceivingReport>, IReceivingReportRepository
    {
        private ApplicationDbContext _db;

        public ReceivingReportRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<DateOnly> CalculateDueDateAsync(string terms, DateOnly transactionDate, CancellationToken cancellationToken = default)
        {
            DateOnly dueDate;

            switch (terms)
            {
                case SD.Terms_7d:
                    return dueDate = transactionDate.AddDays(7);

                case SD.Terms_15d:
                    return dueDate = transactionDate.AddDays(15);

                case SD.Terms_30d:
                    return dueDate = transactionDate.AddDays(30);

                case SD.Terms_M15:
                    return dueDate = transactionDate.AddMonths(1).AddDays(15 - transactionDate.Day);

                case SD.Terms_M30:
                    if (transactionDate.Month == 1)
                    {
                        dueDate = new DateOnly(transactionDate.Year, transactionDate.Month, 1).AddMonths(2).AddDays(-1);
                    }
                    else
                    {
                        dueDate = new DateOnly(transactionDate.Year, transactionDate.Month, 1).AddMonths(2).AddDays(-1);

                        if (dueDate.Day == 31)
                        {
                            dueDate = dueDate.AddDays(-1);
                        }
                    }
                    return dueDate;

                default:
                    return dueDate = transactionDate;
            }
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            FilpridePurchaseOrder? lastPo = await _db
                .FilpridePurchaseOrders
                .OrderBy(c => c.PurchaseOrderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastPo != null)
            {
                string lastSeries = lastPo.PurchaseOrderNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "RR0000000001";
            }
        }
    }
}