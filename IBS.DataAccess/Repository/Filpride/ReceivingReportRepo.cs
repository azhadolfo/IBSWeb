using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ReceivingReportRepo : Repository<ReceivingReport>, IReceivingReportRepo
    {
        private ApplicationDbContext _db;

        public ReceivingReportRepo(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<DateOnly> ComputeDueDateAsync(int poId, DateOnly rrDate, CancellationToken cancellationToken = default)
        {
            var po = await _db
                .PurchaseOrders
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == poId, cancellationToken);

            if (po != null)
            {
                DateOnly dueDate;

                switch (po.Terms)
                {
                    case "7D":
                        return dueDate = rrDate.AddDays(7);

                    case "15D":
                        return dueDate = rrDate.AddDays(15);

                    case "30D":
                        return dueDate = rrDate.AddDays(30);

                    case "M15":
                        return dueDate = rrDate.AddMonths(1).AddDays(15 - rrDate.Day);

                    case "M30":
                        if (rrDate.Month == 1)
                        {
                            dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);
                        }
                        else
                        {
                            dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);

                            if (dueDate.Day == 31)
                            {
                                dueDate = dueDate.AddDays(-1);
                            }
                        }
                        return dueDate;

                    default:
                        return dueDate = rrDate;
                }
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            ReceivingReport? lastRr = await _db
                .ReceivingReports
                .OrderBy(c => c.ReceivingReportNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastRr != null)
            {
                string lastSeries = lastRr.ReceivingReportNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "RR0000000001";
            }
        }

        public async Task<int> RemoveQuantityReceived(int id, decimal quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _db.PurchaseOrders
                    .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (po != null)
            {
                po.QuantityReceived -= quantityReceived;

                if (po.IsReceived == true)
                {
                    po.IsReceived = false;
                    po.ReceivedDate = DateTime.MaxValue;
                }
                if (po.QuantityReceived > po.Quantity)
                {
                    throw new ArgumentException("Input is exceed to remaining quantity received");
                }

                return await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }

        public async Task<int> UpdatePOAsync(int id, decimal quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _db.PurchaseOrders
                    .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (po != null)
            {
                po.QuantityReceived += quantityReceived;

                if (po.QuantityReceived == po.Quantity)
                {
                    po.IsReceived = true;
                    po.ReceivedDate = DateTime.Now;
                }
                if (po.QuantityReceived > po.Quantity)
                {
                    throw new ArgumentException("Input is exceed to remaining quantity received");
                }

                return await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }
    }
}