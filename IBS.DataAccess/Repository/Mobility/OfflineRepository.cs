using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class OfflineRepository : Repository<MobilityOffline>, IOfflineRepository
    {
        private ApplicationDbContext _db;

        public OfflineRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<MobilityOffline> GetOffline(int offlineId, CancellationToken cancellationToken = default)
        {
            return await _db.MobilityOfflines
                .FirstOrDefaultAsync(o => o.OfflineId == offlineId, cancellationToken);
        }

        public async Task<List<SelectListItem>> GetOfflineListAsync(string stationCode, CancellationToken cancellationToken = default)
        {
            return await _db.MobilityOfflines
                .OrderBy(o => o.OfflineId)
                .Where(o => !o.IsResolve && (stationCode == "ALL" || o.StationCode == stationCode))
                .Select(o => new SelectListItem
                {
                    Value = o.OfflineId.ToString(),
                    Text = $"#{o.SeriesNo} {o.FirstDsrNo} - {o.SecondDsrNo}"
                })
                .ToListAsync(cancellationToken);
        }

        public async Task InsertEntry(AdjustReportViewModel model, CancellationToken cancellationToken = default)
        {
            var offlineRecord = await _db.MobilityOfflines
                .FindAsync(new object[] { model.SelectedOfflineId }, cancellationToken);

            var salesHeader = await _db.MobilitySalesHeaders
                .FirstOrDefaultAsync(s => s.SalesNo == model.AffectedDSRNo && s.StationCode == offlineRecord.StationCode, cancellationToken);

            var salesDetail = await _db.MobilitySalesDetails
                .Where(s => s.SalesHeaderId == salesHeader.SalesHeaderId)
                .ToListAsync(cancellationToken);

            var detailToUpdate = salesDetail
                .Find(s => s.Particular == $"{offlineRecord.Product} (P{offlineRecord.Pump})");

            if (model.AffectedDSRNo == offlineRecord.FirstDsrNo)
            {
                detailToUpdate.Closing = model.FirstDsrClosingAfter;
                offlineRecord.Balance -= model.FirstDsrClosingAfter - model.FirstDsrClosingBefore;
            }
            else
            {
                detailToUpdate.Opening = model.SecondDsrOpeningAfter;
                offlineRecord.Balance -= model.SecondDsrOpeningBefore - model.SecondDsrOpeningAfter;
            }

            detailToUpdate.Liters = detailToUpdate.Closing - detailToUpdate.Opening;
            detailToUpdate.Value = detailToUpdate.Liters * detailToUpdate.Price;

            salesHeader.FuelSalesTotalAmount = salesDetail.Sum(s => s.Value);
            salesHeader.TotalSales = salesHeader.FuelSalesTotalAmount + salesHeader.LubesTotalAmount;
            salesHeader.GainOrLoss = salesHeader.SafeDropTotalAmount - salesHeader.TotalSales;

            offlineRecord.NewClosing = model.SecondDsrClosingAfter;
            offlineRecord.LastUpdatedBy = "System"; // Change to a more descriptive value
            offlineRecord.LastUpdatedDate = DateTime.Now;

            if (offlineRecord.Balance <= 0)
            {
                offlineRecord.IsResolve = true;

                var problematicDsr = await _db.MobilitySalesHeaders
                    .Where(s => s.StationCode == offlineRecord.StationCode && (s.SalesNo == offlineRecord.FirstDsrNo || s.SalesNo == offlineRecord.SecondDsrNo))
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