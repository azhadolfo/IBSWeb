using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility.MasterFile;
using IBS.Utility;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class PickUpPointRepository : Repository<MobilityPickUpPoint>, IPickUpPointRepository
    {
        private ApplicationDbContext _db;

        public PickUpPointRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<List<SelectListItem>> GetMobilityTradeSupplierListAsyncById(string stationCode, CancellationToken cancellationToken = default)
        {
            return await _db.MobilitySuppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive && s.StationCode == stationCode && s.Category == "Trade")
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetDistinctPickupPointList(CancellationToken cancellationToken = default)
        {
            return await _db.MobilityPickUpPoints
                .GroupBy(p => p.Depot)
                .OrderBy(g => g.Key)
                .Select(g => new SelectListItem
                {
                    Value = g.First().PickUpPointId.ToString(),
                    Text = g.Key // g.Key is the Depot name
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetPickUpPointListBasedOnSupplier(int supplierId, CancellationToken cancellationToken = default)
        {
            return await _db.MobilityPickUpPoints
                .OrderBy(p => p.Depot)
                .Where(p => p.SupplierId == supplierId)
                .Select(po => new SelectListItem
                {
                    Value = po.PickUpPointId.ToString(),
                    Text = po.Depot
                })
                .ToListAsync(cancellationToken);
        }
    }
}
