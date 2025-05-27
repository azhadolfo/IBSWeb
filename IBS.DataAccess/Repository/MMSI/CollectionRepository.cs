using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class CollectionRepository : Repository<MMSICollection>, ICollectionRepository
    {
        public readonly ApplicationDbContext _dbContext;

        public CollectionRepository (ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<SelectListItem>> GetMMSIActivitiesServicesById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> activitiesServices = await _dbContext.MMSIServices
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
            List<SelectListItem> ports = await _dbContext.MMSIPorts
                .OrderBy(s => s.PortNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.PortId.ToString(),
                    Text = s.PortNumber+ " " + s.PortName
                }).ToListAsync(cancellationToken);

            return ports;
        }

        public async Task<List<SelectListItem>> GetMMSITerminalsById(MMSIDispatchTicket model, CancellationToken cancellationToken = default)
        {
            List<SelectListItem> terminals = new List<SelectListItem>();

            if (model.Terminal?.Port?.PortId != null)
            {
                terminals = await _dbContext.MMSITerminals
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
                terminals = await _dbContext.MMSITerminals
                .OrderBy(s => s.TerminalNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.TerminalId.ToString(),
                    Text = s.TerminalNumber + " " + s.TerminalName
                }).ToListAsync(cancellationToken);
            }

            return terminals;
        }

        public async Task<List<SelectListItem>> GetMMSIAllTerminalsById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> terminals = await _dbContext.MMSITerminals
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
            List<SelectListItem> tugBoats = await _dbContext.MMSITugboats.OrderBy(s => s.TugboatNumber).Select(s => new SelectListItem
            {
                Value = s.TugboatId.ToString(),
                Text = s.TugboatNumber + " " + s.TugboatName
            }).ToListAsync(cancellationToken);

            return tugBoats;
        }

        public async Task<List<SelectListItem>> GetMMSITugMastersById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> tugMasters = await _dbContext.MMSITugMasters.OrderBy(s => s.TugMasterNumber).Select(s => new SelectListItem
            {
                Value = s.TugMasterId.ToString(),
                Text = s.TugMasterNumber + " " + s.TugMasterName
            }).ToListAsync(cancellationToken);

            return tugMasters;
        }

        public async Task<List<SelectListItem>> GetMMSIVesselsById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> vessels = await _dbContext.MMSIVessels.OrderBy(s => s.VesselNumber).Select(s => new SelectListItem
            {
                Value = s.VesselId.ToString(),
                Text = s.VesselNumber + " " + s.VesselName + " " + s.VesselType
            }).ToListAsync(cancellationToken);

            return vessels;
        }

        public async Task<List<SelectListItem>> GetMMSICustomersById(CancellationToken cancellationToken = default)
        {
            return await _dbContext.FilprideCustomers
                .Where(c => c.IsMMSI == true)
                .OrderBy(s => s.CustomerName)
                .Select(s => new SelectListItem
            {
                Value = s.CustomerId.ToString(),
                Text = s.CustomerName
            }).ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSICustomersWithCollectiblesSelectList(int collectionId, CancellationToken cancellationToken = default)
        {
            var billingsToBeCollected = await _dbContext.MMSIBillings
                .Where(t => t.Status == "For Collection" || (collectionId == 0 || t.CollectionId == collectionId))
                .Include(t => t.Customer)
                .ToListAsync(cancellationToken);

            var listOfCustomerWithCollectibleBillings = billingsToBeCollected
                .Select(t => t.Customer!.CustomerId)
                .Distinct()
                .ToList();

            return await _dbContext.FilprideCustomers
                .Where(c => c.IsMMSI == true && listOfCustomerWithCollectibleBillings.Contains(c.CustomerId))
                .OrderBy(s => s.CustomerName)
                .Select(s => new SelectListItem
                {
                    Value = s.CustomerId.ToString(),
                    Text = s.CustomerName
                }).ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSIUncollectedBillingsById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> billingsList = await _dbContext.MMSIBillings
                .Where(dt => dt.Status == "For Collection")
                .OrderBy(dt => dt.MMSIBillingNumber).Select(s => new SelectListItem
                {
                    Value = s.MMSIBillingId.ToString(),
                    Text = $"{s.MMSIBillingNumber} - {s.Customer!.CustomerName}, {s.Date}"
                }).ToListAsync(cancellationToken);

            return billingsList;
        }

        public async Task<List<SelectListItem>> GetMMSICollectedBillsById(int collectionId, CancellationToken cancellationToken = default)
        {
            List<SelectListItem> billingsList = await _dbContext.MMSIBillings
                .Where(dt => dt.CollectionId == collectionId)
                .OrderBy(dt => dt.MMSIBillingNumber).Select(b => new SelectListItem
                {
                    Value = b.MMSIBillingId.ToString(),
                    Text = $"{b.MMSIBillingNumber}"
                }).ToListAsync(cancellationToken);

            return billingsList;
        }

        public async Task<List<SelectListItem>?> GetMMSIUncollectedBillingsByCustomer (int? customerId, CancellationToken cancellationToken)
        {
            var billings = await _dbContext
                .MMSIBillings
                .Where(b => b.CustomerId == customerId && b.Status == "For Collection")
                .Include(b => b.Customer)
                .OrderBy(b => b.MMSIBillingNumber)
                .ToListAsync(cancellationToken);

            var billingsList = billings.Select(b => new SelectListItem
            {
                Value = b.MMSIBillingId.ToString(),
                Text = $"{b.MMSIBillingNumber}"
            }).ToList();

            return billingsList;
        }

        public async Task<string> GenerateCollectionNumber (CancellationToken cancellationToken = default)
        {
            var lastRecord = await _dbContext.MMSICollections
                .Where(b => b.IsUndocumented == true && b.MMSICollectionNumber != null)
                .OrderByDescending(b => b.MMSICollectionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if(lastRecord == null)
            {
                return "CL00000001";
            }
            else
            {
                var lastSeries = lastRecord.MMSICollectionNumber.Substring(3);
                int parsed = int.Parse(lastSeries) + 1;
                return "CL" + (parsed.ToString("D8"));
            }
        }
    }
}
