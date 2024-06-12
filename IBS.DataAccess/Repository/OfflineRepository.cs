﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository
{
    public class OfflineRepository : Repository<Offline>, IOfflineRepository
    {
        private ApplicationDbContext _db;

        public OfflineRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<Offline> GetOffline(int offlineId, CancellationToken cancellationToken = default)
        {
            return await _db.Offlines
                .FirstOrDefaultAsync(o => o.OfflineId == offlineId, cancellationToken);
        }

        public async Task<List<SelectListItem>> GetOfflineListAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Offlines
                .OrderBy(o => o.OfflineId)
                .Where(o => !o.IsResolve)
                .Select(o => new SelectListItem
                {
                    Value = o.OfflineId.ToString(),
                    Text = $"Offline#{o.SeriesNo}"
                })
                .ToListAsync(cancellationToken);
        }

        public async Task InsertEntry(AdjustReportViewModel model, CancellationToken cancellationToken = default)
        {
            var offlineRecord = await _db.Offlines
                .FindAsync(new object[] { model.SelectedOfflineId }, cancellationToken);

            var salesHeader = await _db.SalesHeaders
                .FirstOrDefaultAsync(s => s.SalesNo == model.AffectedDSRNo, cancellationToken);

            var salesDetail = await _db.SalesDetails
                .Where(s => s.SalesHeaderId == salesHeader.SalesHeaderId)
                .ToListAsync(cancellationToken);

            var detailToUpdate = salesDetail
                .Find(s => s.Particular == $"{offlineRecord.Product} (P{offlineRecord.Pump})");

            if (model.AffectedDSRNo == offlineRecord.ClosingDSRNo)
            {
                detailToUpdate.Closing = offlineRecord.Opening;
            }
            else
            {
                detailToUpdate.Opening = offlineRecord.Closing;
            }

            detailToUpdate.Liters = detailToUpdate.Closing - detailToUpdate.Opening;
            detailToUpdate.Value = detailToUpdate.Liters * detailToUpdate.Price;

            salesHeader.FuelSalesTotalAmount = salesDetail.Sum(s => s.Value);
            salesHeader.TotalSales = salesHeader.FuelSalesTotalAmount + salesHeader.LubesTotalAmount;
            salesHeader.GainOrLoss = salesHeader.SafeDropTotalAmount - salesHeader.TotalSales;

            offlineRecord.NewClosing = model.Closing;
            offlineRecord.Balance -= (model.Opening - model.Closing);
            offlineRecord.LastUpdatedBy = "System"; // Change to a more descriptive value
            offlineRecord.LastUpdatedDate = DateTime.UtcNow;

            if (offlineRecord.Balance <= 0)
            {
                offlineRecord.IsResolve = true;

                var problematicDsr = await _db.SalesHeaders
                    .Where(o => o.SalesNo == offlineRecord.ClosingDSRNo || o.SalesNo == offlineRecord.OpeningDSRNo)
                    .ToListAsync(cancellationToken);

                foreach (var offline in problematicDsr)
                {
                    offline.IsTransactionNormal = true;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

    }
}
