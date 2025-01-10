using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Utility;
using IBS.Utility.Helpers;

namespace IBS.DataAccess.Repository.Mobility
{
    public class ReceivingReportRepository : Repository<MobilityReceivingReport>, IReceivingReportRepository
    {
        private ApplicationDbContext _db;

        public ReceivingReportRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string stationCode, CancellationToken cancellationToken = default)
        {
            MobilityReceivingReport? lastRr = await _db
                .MobilityReceivingReports
                .OrderBy(c => c.ReceivingReportNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastRr != null)
            {
                string lastSeries = lastRr.ReceivingReportNo.Substring(lastRr.ReceivingReportNo.IndexOf('-') + 3);
                int incrementedNumber = int.Parse(lastSeries) + 1;

                return $"{stationCode}-RR{incrementedNumber:D5}";
            }
            else
            {
                return $"{stationCode}-RR00001"; //S07-RR00001
            }
        }

        public override async Task<IEnumerable<MobilityReceivingReport>> GetAllAsync(Expression<Func<MobilityReceivingReport, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MobilityReceivingReport> query = dbSet
                .Include(rr => rr.FilprideDeliveryReceipt)
                .ThenInclude(dr => dr.CustomerOrderSlip)
                .ThenInclude(cos => cos.PurchaseOrder)
                .ThenInclude(po => po.Product);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<MobilityReceivingReport> GetAsync(Expression<Func<MobilityReceivingReport, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(po => po.FilprideDeliveryReceipt)
                .ThenInclude(dr => dr.CustomerOrderSlip)
                .ThenInclude(cos => cos.PurchaseOrder)
                .ThenInclude(po => po.Product)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task PostAsync(MobilityReceivingReport receivingReport, CancellationToken cancellationToken = default)
        {
            //PENDING process the method here

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(ReceivingReportViewModel viewModel, CancellationToken cancellationToken)
        {
            var existingRecord = await _db.MobilityReceivingReports
                .FindAsync(viewModel.ReceivingReportId, cancellationToken);

            existingRecord.Date = viewModel.Date;
            existingRecord.Driver = viewModel.Driver;
            existingRecord.PlateNo = viewModel.PlateNo;
            existingRecord.Remarks = viewModel.Remarks;
            existingRecord.DeliveryReceiptId = viewModel.DeliveryReceiptId;
            existingRecord.ReceivedQuantity = viewModel.ReceivedQuantity;

            if (_db.ChangeTracker.HasChanges())
            {
                existingRecord.EditedBy = viewModel.CurrentUser;
                existingRecord.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }
    }
}