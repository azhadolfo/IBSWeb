using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class MsapRepository : IMsapRepository
    {
        public readonly ApplicationDbContext _dbContext;

        public MsapRepository (ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<SelectListItem>> GetMMSIActivitiesServicesById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> activitiesServices = await _dbContext.MMSIActivitiesServices
                .OrderBy(s => s.ActivityServiceNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.ActivityServiceId.ToString(),
                    Text = s.ActivityServiceNumber + " " + s.ActivityServiceName
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
            List<SelectListItem> customers = await _dbContext.MMSICustomers.OrderBy(s => s.CustomerName).Select(s => new SelectListItem
            {
                Value = s.MMSICustomerId.ToString(),
                Text = s.CustomerName
            }).ToListAsync(cancellationToken);

            return customers;
        }

        public async Task<List<SelectListItem>> GetMMSITerminalsById(int portId, CancellationToken cancellationToken = default)
        {
            if (portId != null)
            {
                List<SelectListItem> terminals = await _dbContext.MMSITerminals
                    .Where(t => t.PortId == portId)
                    .OrderBy(s => s.TerminalNumber).Select(s => new SelectListItem
                    {
                        Value = s.TerminalId.ToString(),
                        Text = s.TerminalNumber + " " + s.TerminalName
                    }).ToListAsync(cancellationToken);

                return terminals;
            }
            else return null;
        }

        public async Task<List<SelectListItem>> GetMMSIUnbilledTicketsById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> dispatchTicketList = await _dbContext.MMSIDispatchTickets
                .Where(dt => dt.Status == "For Billing")
                .OrderBy(dt => dt.DispatchNumber).Select(s => new SelectListItem
                {
                    Value = s.DispatchTicketId.ToString(),
                    Text = s.DispatchNumber
                }).ToListAsync(cancellationToken);

            return dispatchTicketList;
        }

        public async Task<List<SelectListItem>> GetMMSIBilledTicketsById(int id, CancellationToken cancellationToken = default)
        {
            List<SelectListItem> dispatchTicketList = await _dbContext.MMSIDispatchTickets
                .Where(dt => dt.BillingId == id.ToString())
                .OrderBy(dt => dt.DispatchNumber).Select(s => new SelectListItem
                {
                    Value = s.DispatchTicketId.ToString(),
                    Text = s.DispatchNumber
                }).ToListAsync(cancellationToken);

            return dispatchTicketList;
        }

        public async Task<List<SelectListItem>> GetMMSIUncollectedBillingsById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> billingsList = await _dbContext.MMSIBillings
                .Where(dt => dt.Status == "For Collection")
                .OrderBy(dt => dt.MMSIBillingNumber).Select(s => new SelectListItem
                {
                    Value = s.MMSIBillingId.ToString(),
                    Text = $"{s.MMSIBillingNumber} - {s.Customer.CustomerName}, {s.Date}"
                }).ToListAsync(cancellationToken);

            return billingsList;
        }

        public async Task<List<SelectListItem>> GetMMSICollectedBillsById(int collectionId, CancellationToken cancellationToken = default)
        {
            List<SelectListItem> billingsList = await _dbContext.MMSIBillings
                .Where(dt => dt.MMSICollectionId == collectionId)
                .OrderBy(dt => dt.MMSIBillingNumber).Select(b => new SelectListItem
                {
                    Value = b.MMSIBillingId.ToString(),
                    Text = $"{b.MMSIBillingNumber} - {b.Customer.CustomerName}, {b.Date}"
                }).ToListAsync(cancellationToken);

            return billingsList;
        }

        public async Task<List<SelectListItem>> GetMMSIBillingsByCustomer (int? customerId, CancellationToken cancellationToken)
        {
            var billings = await _dbContext
                .MMSIBillings
                .Where(t => t.CustomerId == customerId && t.Status == "For Collection")
                .OrderBy(t => t.MMSIBillingNumber)
                .ToListAsync(cancellationToken);

            var billingsList = billings.Select(b => new SelectListItem
            {
                Value = b.MMSIBillingId.ToString(),
                Text = $"{b.MMSIBillingNumber} - {b.Customer.CustomerName}, {b.Date}"
            }).ToList();

            return billingsList;
        }

        public async Task<List<SelectListItem>> GetMMSICompanyOwnerSelectListById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> companyOwnerList = await _dbContext.MMSICompanyOwners
                .OrderBy(dt => dt.CompanyOwnerNumber).Select(s => new SelectListItem
                {
                    Value = s.MMSICompanyOwnerId.ToString(),
                    Text = $"{s.CompanyOwnerNumber} {s.CompanyOwnerName}"
                }).ToListAsync(cancellationToken);

            return companyOwnerList;
        }

        public async Task<MMSIDispatchTicket> GetDispatchTicketLists(MMSIDispatchTicket model, CancellationToken cancellationToken = default)
        {
            model.ActivitiesServices = await GetMMSIActivitiesServicesById(cancellationToken);
            model.Ports = await GetMMSIPortsById(cancellationToken);
            model.Terminals = await GetMMSITerminalsById(model, cancellationToken);
            model.Tugboats = await GetMMSITugboatsById(cancellationToken);
            model.TugMasters = await GetMMSITugMastersById(cancellationToken);
            model.Vessels = await GetMMSIVesselsById(cancellationToken);
            model.Customers = await GetMMSICustomersById(cancellationToken);

            return model;
        }

        public async Task<MMSIBilling> GetBillingLists(MMSIBilling model, CancellationToken cancellationToken = default)
        {
            model.Ports = await GetMMSIPortsById(cancellationToken);
            model.Terminals = await GetMMSIAllTerminalsById(cancellationToken);
            model.Vessels = await GetMMSIVesselsById(cancellationToken);
            model.Customers = await GetMMSICustomersById(cancellationToken);

            return model;
        }

        public async Task<string> GenerateBillingNumber (CancellationToken cancellationToken = default)
        {
            var lastRecord = await _dbContext.MMSIBillings
                .Where(b => b.IsUndocumented == true && b.MMSIBillingNumber != null)
                .OrderByDescending(b => b.MMSIBillingNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if(lastRecord == null)
            {
                return "BL00000001";
            }
            else
            {
                var lastSeries = lastRecord.MMSIBillingNumber.Substring(3);
                int parsed = int.Parse(lastSeries) + 1;
                return "BL" + (parsed.ToString("D8"));
            }
        }

        public async Task<string> GenerateCollectionNumber (CancellationToken cancellationToken = default)
        {
            var lastRecord = await _dbContext.MMSICollections
                .Where(b => b.IsUndocumented == true && b.CollectionNumber != null)
                .OrderByDescending(b => b.CollectionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if(lastRecord == null)
            {
                return "CL00000001";
            }
            else
            {
                var lastSeries = lastRecord.CollectionNumber.Substring(3);
                int parsed = int.Parse(lastSeries) + 1;
                return "CL" + (parsed.ToString("D8"));
            }
        }

        public async Task<MMSIBilling> ProcessAddress(MMSIBilling model, CancellationToken cancellationToken = default)
        {
            // Splitting the address for the view
            string[]? words = null;
            if (model.PrincipalId != null)
            {
                words = model.Principal?.Address?.Split(' ');
            }
            else
            {
                words = model.Customer?.CustomerAddress?.Split(' ');
            }
            List<string> resultStrings = new List<string>();
            string currentString = "";

            foreach (string word in words)
            {
                if (currentString.Length + word.Length + (currentString.Length > 0 ? 1 : 0) > 40)
                {
                    if (currentString.Length > 0)
                    {
                        resultStrings.Add(currentString.Trim());
                    }
                    currentString = word;
                }
                else
                {
                    currentString += (currentString.Length > 0 ? " " : "") + word;
                }
            }
            if (currentString.Length > 0)
            {
                resultStrings.Add(currentString.Trim());
            }
            string firstString = resultStrings.Count > 0 ? resultStrings[0] : "";
            string secondString = resultStrings.Count > 1 ? resultStrings[1] : "";
            string thirdString = resultStrings.Count > 2 ? resultStrings[2] : "";
            string fourthString = resultStrings.Count > 3 ? resultStrings[3] : "";

            var results = resultStrings;
            model.AddressLine1 = firstString;
            model.AddressLine2 = secondString;
            model.AddressLine3 = thirdString;
            model.AddressLine4 = fourthString;

            return model;
        }
    }
}
