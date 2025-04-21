using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Utility;
using IBS.Utility.Helpers;
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
                .Where(o => !o.IsResolve && o.StationCode == stationCode)
                .Select(o => new SelectListItem
                {
                    Value = o.OfflineId.ToString(),
                    Text = $"#{o.SeriesNo} {o.FirstDsrNo} - {o.SecondDsrNo}"
                })
                .ToListAsync(cancellationToken);
        }

        public async Task InsertEntry(AdjustReportViewModel model, CancellationToken cancellationToken = default)
        {
            var offlineList = await _db.MobilityOfflines
                .ToListAsync(cancellationToken);

            var selectedOffline = offlineList.Find(o => o.OfflineId == model.SelectedOfflineId);

            var salesHeader = await _db.MobilitySalesHeaders
                .FirstOrDefaultAsync(s => s.SalesNo == model.AffectedDSRNo && s.StationCode == selectedOffline.StationCode, cancellationToken);

            var salesDetail = await _db.MobilitySalesDetails
                .Where(s => s.SalesHeaderId == salesHeader.SalesHeaderId)
                .ToListAsync(cancellationToken);

            var detailToUpdate = salesDetail
                .Find(s => s.Particular == $"{selectedOffline.Product} (P{selectedOffline.Pump})");

            if (model.AffectedDSRNo == selectedOffline.FirstDsrNo)
            {
                detailToUpdate.Closing = model.FirstDsrClosingAfter;
                selectedOffline.Balance -= model.FirstDsrClosingAfter - model.FirstDsrClosingBefore;
            }
            else
            {
                detailToUpdate.Opening = model.SecondDsrOpeningAfter;
                selectedOffline.Balance -= model.SecondDsrOpeningBefore - model.SecondDsrOpeningAfter;
            }

            detailToUpdate.Liters = detailToUpdate.Closing - detailToUpdate.Opening;
            detailToUpdate.Value = detailToUpdate.Liters * detailToUpdate.Price;

            salesHeader.FuelSalesTotalAmount = salesDetail.Sum(s => s.Value);
            salesHeader.TotalSales = salesHeader.FuelSalesTotalAmount + salesHeader.LubesTotalAmount;
            salesHeader.GainOrLoss = salesHeader.SafeDropTotalAmount - salesHeader.TotalSales;

            selectedOffline.NewClosing = model.SecondDsrClosingAfter;
            selectedOffline.LastUpdatedBy = "System"; // Change to a more descriptive value
            selectedOffline.LastUpdatedDate = DateTimeHelper.GetCurrentPhilippineTime();

            if (selectedOffline.Balance <= 0)
            {
                selectedOffline.IsResolve = true;

                var firstDsrHasPendingOffline = offlineList.Any(o => !o.IsResolve && (o.FirstDsrNo == selectedOffline.FirstDsrNo || o.SecondDsrNo == selectedOffline.FirstDsrNo));

                if (!firstDsrHasPendingOffline)
                {
                    var problematicDsr = await _db.MobilitySalesHeaders
                   .FirstOrDefaultAsync(s => s.StationCode == selectedOffline.StationCode && s.SalesNo == selectedOffline.FirstDsrNo, cancellationToken);

                    problematicDsr.IsTransactionNormal = true;
                }

                var secondDsrHasPendingOffline = offlineList.Any(o => !o.IsResolve && (o.FirstDsrNo == selectedOffline.SecondDsrNo || o.SecondDsrNo == selectedOffline.SecondDsrNo));

                if (!secondDsrHasPendingOffline)
                {
                    var problematicDsr = await _db.MobilitySalesHeaders
                   .FirstOrDefaultAsync(s => s.StationCode == selectedOffline.StationCode && s.SalesNo == selectedOffline.SecondDsrNo, cancellationToken);

                    problematicDsr.IsTransactionNormal = true;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
