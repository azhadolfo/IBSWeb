using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.Msap;
using IBS.Models.Msap.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class ServiceRequestRepository(ApplicationDbContext db)
        : Repository<DispatchTicket>(db), IServiceRequestRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<ServiceRequestViewModel> GetDispatchTicketSelectLists(ServiceRequestViewModel model, CancellationToken cancellationToken = default)
        {
            model.Services = await GetMsapActivitiesServicesById(cancellationToken);
            model.Ports = await GetMsapPortsById(cancellationToken);
            model.Tugboats = await GetMsapTugboatsById(cancellationToken);
            model.TugMasters = await GetMsapTugMastersById(cancellationToken);
            model.Vessels = await GetMsapVesselsById(cancellationToken);
            model.Terminals = await GetMsapTerminalsById(model, cancellationToken);

            return model;
        }

        private async Task<List<SelectListItem>> GetMsapActivitiesServicesById(CancellationToken cancellationToken = default)
        {
            var activitiesServices = await _db.MsapServices
                .OrderBy(s => s.ServiceName)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = s.ServiceName
                }).ToListAsync(cancellationToken);

            return activitiesServices;
        }

        private async Task<List<SelectListItem>> GetMsapPortsById(CancellationToken cancellationToken = default)
        {
            var ports = await _db.MsapPorts
                .OrderBy(s => s.PortName)
                .Select(s => new SelectListItem
                {
                    Value = s.PortId.ToString(),
                    Text = s.PortName
                }).ToListAsync(cancellationToken);

            return ports;
        }

        private async Task<List<SelectListItem>> GetMsapTerminalsById(ServiceRequestViewModel model, CancellationToken cancellationToken = default)
        {
            List<SelectListItem> terminals;

            if (model.Terminal?.Port?.PortId != null)
            {
                terminals = await _db.MsapTerminals
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
                terminals = await _db.MsapTerminals
                    .Where(t => t.PortId == model.PortId)
                    .OrderBy(s => s.TerminalName)
                    .Select(s => new SelectListItem
                    {
                        Value = s.TerminalId.ToString(),
                        Text = s.TerminalName
                    }).ToListAsync(cancellationToken);
            }

            return terminals;
        }

        private async Task<List<SelectListItem>> GetMsapTugboatsById(CancellationToken cancellationToken = default)
        {
            var tugBoats = await _db.MsapTugboats
                .OrderBy(s => s.TugboatName)
                .Select(s => new SelectListItem
                {
                    Value = s.TugboatId.ToString(),
                    Text = s.TugboatName
                }).ToListAsync(cancellationToken);

            return tugBoats;
        }

        private async Task<List<SelectListItem>> GetMsapTugMastersById(CancellationToken cancellationToken = default)
        {
            var tugMasters = await _db.MsapTugMasters
                .OrderBy(s => s.TugMasterName)
                .Select(s => new SelectListItem
                {
                    Value = s.TugMasterId.ToString(),
                    Text = s.TugMasterName
                }).ToListAsync(cancellationToken);

            return tugMasters;
        }

        private async Task<List<SelectListItem>> GetMsapVesselsById(CancellationToken cancellationToken = default)
        {
            var vessels = await _db.MsapVessels
                .OrderBy(s => s.VesselName)
                .Select(s => new SelectListItem
                {
                    Value = s.VesselId.ToString(),
                    Text = s.VesselName
                }).ToListAsync(cancellationToken);

            return vessels;
        }
    }
}


