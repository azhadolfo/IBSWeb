﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
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
                .Select(o => new SelectListItem
                {
                    Value = o.OfflineId.ToString(),
                    Text = $"Offline#{o.SeriesNo}"
                })
                .ToListAsync(cancellationToken);
        }

        public async Task InserEntry(ManualEntryViewModel model, CancellationToken cancellationToken = default)
        {
            var offlineRecord = await _db.Offlines
                .FindAsync(model.SelectedOfflineId, cancellationToken);

            var salesHeader = await _db.SalesHeaders
                .FirstOrDefaultAsync(s => s.SalesNo == model.AffectedDSRNo);

            var salesDetail = await _db.SalesDetails
                .Where(s => s.SalesHeaderId == salesHeader.SalesHeaderId)
                .ToListAsync(cancellationToken);

            if (model.AffectedDSRNo == offlineRecord.ClosingDSRNo)
            {
                var detailToUpdate = salesDetail.FirstOrDefault(s => s.Particular == $"{offlineRecord.Product} (P{offlineRecord.Pump})");

                detailToUpdate.Closing = offlineRecord.Opening;
                detailToUpdate.Liters = detailToUpdate.Closing - detailToUpdate.Opening;
                detailToUpdate.Value = (decimal)detailToUpdate.Liters * detailToUpdate.Price;

                salesHeader.FuelSalesTotalAmount = salesDetail.Sum(s => s.Value);
                salesHeader.TotalSales = salesHeader.FuelSalesTotalAmount + salesHeader.LubesTotalAmount;
                salesHeader.GainOrLoss = salesHeader.SafeDropTotalAmount - salesHeader.TotalSales;

                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {

            }

            offlineRecord.NewClosing = model.Closing;
            offlineRecord.Balance -= model.Closing - model.Opening;
            offlineRecord.LastUpdatedBy = "Ako";
            offlineRecord.LastUpdatedDate = DateTime.Now;
            offlineRecord.IsResolve = offlineRecord.Balance <= 0 ? true : false;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
