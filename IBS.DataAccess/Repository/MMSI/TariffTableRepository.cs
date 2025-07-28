using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class TariffTableRepository : Repository<MMSITariffRate>, ITariffTableRepository
    {
        private readonly ApplicationDbContext _db;

        public TariffTableRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public override async Task<MMSITariffRate?> GetAsync(Expression<Func<MMSITariffRate, bool>> filter, CancellationToken cancellationToken = default)
        {
            var model =  await dbSet
                .Include(t => t.Terminal).ThenInclude(t => t!.Port)
                .Where(filter)
                .OrderByDescending(t => t.AsOfDate)
                .FirstOrDefaultAsync(cancellationToken);

            return model;
        }

        public override async Task<IEnumerable<MMSITariffRate>> GetAllAsync(Expression<Func<MMSITariffRate, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MMSITariffRate> query = dbSet
                .Include(t => t.Customer)
                .Include(t => t.Terminal).ThenInclude(t => t!.Port)
                .Include(t => t.Service);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSIPortsById(CancellationToken cancellationToken = default)
        {
            var ports = await _db.MMSIPorts
                .OrderBy(s => s.PortNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.PortId.ToString(),
                    Text = s.PortNumber + " " + s.PortName
                }).ToListAsync(cancellationToken);

            return ports;
        }

        public async Task<List<SelectListItem>> GetMMSITerminalsById(MMSIDispatchTicket model, CancellationToken cancellationToken = default)
        {
            List<SelectListItem> terminals;

            if (model.Terminal?.Port?.PortId != null)
            {
                terminals = await _db.MMSITerminals
                .Where(t => t.PortId == model.Terminal.Port.PortId)
                .OrderBy(s => s.TerminalNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.TerminalId.ToString(),
                    Text = s.TerminalNumber + " " + s.TerminalName
                }).ToListAsync(cancellationToken);
            }
            else
            {
                terminals = await _db.MMSITerminals
                .OrderBy(s => s.TerminalNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.TerminalId.ToString(),
                    Text = s.TerminalNumber + " " + s.TerminalName
                }).ToListAsync(cancellationToken);
            }

            return terminals;
        }

        public async Task<List<SelectListItem>> GetMMSITerminalsById(int portId, CancellationToken cancellationToken = default)
        {
            var terminals = await _db.MMSITerminals
                .Where(t => t.PortId == portId)
                .OrderBy(s => s.TerminalNumber).Select(s => new SelectListItem
                {
                    Value = s.TerminalId.ToString(),
                    Text = s.TerminalNumber + " " + s.TerminalName
                }).ToListAsync(cancellationToken);

            return terminals;
        }

        public async Task<List<SelectListItem>> GetMMSIAllTerminalsById(CancellationToken cancellationToken = default)
        {
            var terminals = await _db.MMSITerminals
                .OrderBy(s => s.TerminalNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.TerminalId.ToString(),
                    Text = s.TerminalNumber + " " + s.TerminalName,
                }).ToListAsync(cancellationToken);

            return terminals;
        }

        public async Task<List<SelectListItem>> GetMMSITugboatsById(CancellationToken cancellationToken = default)
        {
            var tugBoats = await _db.MMSITugboats.OrderBy(s => s.TugboatNumber).Select(s => new SelectListItem
            {
                Value = s.TugboatId.ToString(),
                Text = s.TugboatNumber + " " + s.TugboatName
            }).ToListAsync(cancellationToken);

            return tugBoats;
        }

        public async Task<List<SelectListItem>> GetMMSITugMastersById(CancellationToken cancellationToken = default)
        {
            var tugMasters = await _db.MMSITugMasters.OrderBy(s => s.TugMasterNumber).Select(s => new SelectListItem
            {
                Value = s.TugMasterId.ToString(),
                Text = s.TugMasterNumber + " " + s.TugMasterName
            }).ToListAsync(cancellationToken);

            return tugMasters;
        }

        public async Task<List<SelectListItem>> GetMMSIVesselsById(CancellationToken cancellationToken = default)
        {
            var vessels = await _db.MMSIVessels.OrderBy(s => s.VesselNumber).Select(s => new SelectListItem
            {
                Value = s.VesselId.ToString(),
                Text = s.VesselNumber + " " + s.VesselName + " " + s.VesselType
            }).ToListAsync(cancellationToken);

            return vessels;
        }
    }
}
