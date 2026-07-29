using IBS.DTOs;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class LockedPeriodAdjustmentRepository : Repository<LockedPeriodAdjustment>, ILockedPeriodAdjustmentRepository
    {
        private readonly ApplicationDbContext _db;

        public LockedPeriodAdjustmentRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddIfPeriodPostedAsync(
            LockedPeriodAdjustmentRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.AdjustmentValue == 0m)
            {
                return;
            }

            var isPeriodPosted = await _db.PostedPeriods
                .AnyAsync(m =>
                    m.Module == request.Module.ToString() &&
                    m.IsPosted &&
                    m.Year == request.TransactionDate.Year &&
                    m.Month == request.TransactionDate.Month,
                    cancellationToken);

            if (!isPeriodPosted)
            {
                return;
            }

            await dbSet.AddAsync(new LockedPeriodAdjustment
            {
                Period = new DateOnly(request.TransactionDate.Year, request.TransactionDate.Month, 1),
                EntityType = request.EntityType,
                EntityTypeNo = request.EntityNo,
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                SupplierId = request.SupplierId,
                SupplierName = request.SupplierName,
                AdjustmentType = request.AdjustmentType,
                OldValue = request.OldValue,
                NewValue = request.NewValue,
                AdjustmentValue = request.AdjustmentValue,
                AffectedQuantity = request.AffectedQuantity,
                Reason = request.Reason,
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
            }, cancellationToken);
        }
    }
}
