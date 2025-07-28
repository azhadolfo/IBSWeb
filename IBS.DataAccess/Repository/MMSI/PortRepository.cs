using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class PortRepository : Repository<MMSIPort>, IPortRepository
    {
        private readonly ApplicationDbContext _db;

        public PortRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSIPortsSelectList(CancellationToken cancellationToken = default)
        {
            var ports = await _db.MMSIPorts
                .OrderBy(s => s.PortName)
                .Select(s => new SelectListItem
                {
                    Value = s.PortId.ToString(),
                    Text = s.PortName
                }).ToListAsync(cancellationToken);

            return ports;
        }
    }
}
