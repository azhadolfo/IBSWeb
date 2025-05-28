using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class ServiceRequestRepository : Repository<MMSIDispatchTicket>, IServiceRequestRepository
    {
        public readonly ApplicationDbContext _dbContext;

        public ServiceRequestRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<SelectListItem>> GetMMSIActivitiesServicesById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> activitiesServices = await _dbContext.MMSIServices
                .OrderBy(s => s.ServiceName)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = s.ServiceName
                }).ToListAsync(cancellationToken);

            return activitiesServices;
        }

        public async Task<List<SelectListItem>> GetMMSIPortsById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> ports = await _dbContext.MMSIPorts
                .OrderBy(s => s.PortName)
                .Select(s => new SelectListItem
                {
                    Value = s.PortId.ToString(),
                    Text = s.PortName
                }).ToListAsync(cancellationToken);

            return ports;
        }

        public async Task<List<SelectListItem>> GetMMSITerminalsById(ServiceRequestViewModel model, CancellationToken cancellationToken = default)
        {
            List<SelectListItem> terminals = new List<SelectListItem>();

            if (model.Terminal?.Port?.PortId != null)
            {
                terminals = await _dbContext.MMSITerminals
                .Where(t => t.PortId == model.Terminal.Port.PortId)
                .OrderBy(s => s.TerminalName)
                .Select(s => new SelectListItem
                {
                    Value = s.TerminalId.ToString(),
                    Text = s.TerminalName
                }).ToListAsync(cancellationToken);
            }
            else
            {
                terminals = await _dbContext.MMSITerminals
                .OrderBy(s => s.TerminalName)
                .Select(s => new SelectListItem
                {
                    Value = s.TerminalId.ToString(),
                    Text = s.TerminalName
                }).ToListAsync(cancellationToken);
            }

            return terminals;
        }

        public async Task<List<SelectListItem>> GetMMSITugboatsById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> tugBoats = await _dbContext.MMSITugboats
                .OrderBy(s => s.TugboatName)
                .Select(s => new SelectListItem
                {
                    Value = s.TugboatId.ToString(),
                    Text = s.TugboatName
                }).ToListAsync(cancellationToken);

            return tugBoats;
        }

        public async Task<List<SelectListItem>> GetMMSITugMastersById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> tugMasters = await _dbContext.MMSITugMasters
                .OrderBy(s => s.TugMasterName)
                .Select(s => new SelectListItem
                {
                    Value = s.TugMasterId.ToString(),
                    Text = s.TugMasterName
                }).ToListAsync(cancellationToken);

            return tugMasters;
        }

        public async Task<List<SelectListItem>> GetMMSIVesselsById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> vessels = await _dbContext.MMSIVessels
                .OrderBy(s => s.VesselName)
                .Select(s => new SelectListItem
                {
                    Value = s.VesselId.ToString(),
                    Text = $"{s.VesselName} ({s.VesselType})"
                }).ToListAsync(cancellationToken);

            return vessels;
        }

        public async Task<ServiceRequestViewModel> GetDispatchTicketLists(ServiceRequestViewModel model, CancellationToken cancellationToken = default)
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
