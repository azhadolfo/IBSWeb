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

        public async Task<List<SelectListItem>> GetFilprideTradeSupplierListAsyncById(string stationCode, CancellationToken cancellationToken = default)
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
    }
}
