using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class DispatchTicketRepository : Repository<MMSIDispatchTicket>, IDispatchTicketRepository
    {
        private readonly ApplicationDbContext _db;

        public DispatchTicketRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public override async Task<IEnumerable<MMSIDispatchTicket>> GetAllAsync(Expression<Func<MMSIDispatchTicket, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MMSIDispatchTicket> query = dbSet
                .Include(a => a.Service)
                .Include(a => a.Terminal).ThenInclude(t => t!.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<MMSIDispatchTicket?> GetAsync(Expression<Func<MMSIDispatchTicket, bool>> filter, CancellationToken cancellationToken = default)
        {
            var model =  await dbSet.Where(filter)
                .Include(a => a.Service)
                .Include(a => a.Terminal).ThenInclude(t => t!.Port)
                .Include(a => a.Tugboat).ThenInclude(t => t!.TugboatOwner)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .FirstOrDefaultAsync(cancellationToken);

            if (model!.CustomerId != 0 && model.CustomerId != null)
            {
                model.Customer = await _db.FilprideCustomers
                    .FirstOrDefaultAsync(x => x.CustomerId == model.CustomerId, cancellationToken);
            }

            return model;
        }

        public async Task<List<SelectListItem>> GetMMSIActivitiesServicesById(CancellationToken cancellationToken = default)
        {
            var activitiesServices = await _db.MMSIServices
                .OrderBy(s => s.ServiceNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = s.ServiceNumber + " " + s.ServiceName
                }).ToListAsync(cancellationToken);

            return activitiesServices;
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

        private async Task<List<SelectListItem>> GetMMSITerminalsById(MMSIDispatchTicket model, CancellationToken cancellationToken = default)
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

        public async Task<List<SelectListItem>> GetMMSICustomersById(CancellationToken cancellationToken = default)
        {
            return await _db.FilprideCustomers
                .Where(c => c.IsMMSI == true)
                .OrderBy(s => s.CustomerName)
                .Select(s => new SelectListItem
                {
                    Value = s.CustomerId.ToString(),
                    Text = s.CustomerName
                }).ToListAsync(cancellationToken);
        }

        public async Task<MMSIDispatchTicket> GetDispatchTicketLists(MMSIDispatchTicket model, CancellationToken cancellationToken = default)
        {
            model.Services = await GetMMSIActivitiesServicesById(cancellationToken);
            model.Ports = await GetMMSIPortsById(cancellationToken);
            model.Tugboats = await GetMMSITugboatsById(cancellationToken);
            model.TugMasters = await GetMMSITugMastersById(cancellationToken);
            model.Vessels = await GetMMSIVesselsById(cancellationToken);
            model.Terminals = await GetMMSITerminalsById(model, cancellationToken);

            return model;
        }
    }
}
