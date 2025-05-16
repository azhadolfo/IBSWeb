using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class BillingRepository : Repository<MMSIBilling>, IBillingRepository
    {
        public readonly ApplicationDbContext _dbContext;

        public BillingRepository (ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
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

        public async Task<List<SelectListItem>> GetMMSIVesselsById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> vessels = await _dbContext.MMSIVessels.OrderBy(s => s.VesselName).Select(s => new SelectListItem
            {
                Value = s.VesselId.ToString(),
                Text = s.VesselName
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

        public async Task<List<SelectListItem>> GetMMSICustomersWithBillablesById(CancellationToken cancellationToken = default)
        {
            var dispatchToBeBilled = await _dbContext.MMSIDispatchTickets
                .Where(t => t.Status == "For Billing")
                .Include(t => t.Customer)
                .ToListAsync(cancellationToken);

            var listOfCustomerWithBillableTickets = dispatchToBeBilled
                .Select(t => t.Customer.CustomerId)
                .Distinct()
                .ToList();

            return await _dbContext.FilprideCustomers
                .Where(c => c.IsMMSI == true && listOfCustomerWithBillableTickets.Contains(c.CustomerId))
                .OrderBy(s => s.CustomerName)
                .Select(s => new SelectListItem
            {
                Value = s.CustomerId.ToString(),
                Text = s.CustomerName
            }).ToListAsync(cancellationToken);
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

        public async Task<List<SelectListItem>?> GetMMSIUnbilledTicketsByCustomer (int? customerId, CancellationToken cancellationToken)
        {
            var tickets = await _dbContext
                .MMSIDispatchTickets
                .Where(b => b.CustomerId == customerId && b.Status == "For Billing")
                .Include(b => b.Customer)
                .OrderBy(b => b.DispatchNumber)
                .ToListAsync(cancellationToken);

            var ticketsList = tickets.Select(b => new SelectListItem
            {
                Value = b.DispatchTicketId.ToString(),
                Text = $"{b.DispatchNumber} - {b.Customer.CustomerName}, {b.Date}"
            }).ToList();

            return ticketsList;
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

        public async Task<CreateBillingViewModel> GetBillingLists(CreateBillingViewModel model, CancellationToken cancellationToken = default)
        {
            model.Ports = await GetMMSIPortsById(cancellationToken);
            model.Terminals = await GetMMSIAllTerminalsById(cancellationToken);
            model.Vessels = await GetMMSIVesselsById(cancellationToken);
            model.Customers = await GetMMSICustomersWithBillablesById(cancellationToken);

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
