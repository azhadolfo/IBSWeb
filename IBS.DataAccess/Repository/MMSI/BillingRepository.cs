using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using IBS.Models.MMSI.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class BillingRepository : Repository<MMSIBilling>, IBillingRepository
    {
        public readonly ApplicationDbContext _dbContext;

        public BillingRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public override async Task<IEnumerable<MMSIBilling>> GetAllAsync(Expression<Func<MMSIBilling, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MMSIBilling> query = dbSet
                .Include(a => a.Terminal)
                .Include(a => a.Vessel)
                .Include(a => a.Customer)
                .Include(a => a.Port);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<MMSIBilling?> GetAsync(Expression<Func<MMSIBilling, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MMSIBilling> query = dbSet
                .Include(b => b.Terminal)
                .ThenInclude(t => t!.Port)
                .Include(b => b.Vessel)
                .Include(b => b.Customer)
                .Include(b => b.Principal);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<string>?> GetToBillDispatchTicketListAsync(string billingId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.MMSIDispatchTickets.Where(t => t.BillingId == billingId)
                .Select(d => d.DispatchTicketId.ToString()).ToListAsync(cancellationToken);
        }

        public async Task<List<string>?> GetUniqueTugboatsListAsync(string billingId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.MMSIDispatchTickets
                .Where(dt => dt.BillingId == billingId)
                .Select(dt => dt.Tugboat!.TugboatName.ToString())
                .Distinct() // Ensures unique values
                .ToListAsync(cancellationToken);
        }

        public async Task<List<MMSIDispatchTicket>?> GetPaidDispatchTicketsAsync(string billingId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.MMSIDispatchTickets
                .Where(dt => dt.BillingId == billingId)
                .Include(a => a.Service)
                .Include(a => a.Terminal).ThenInclude(t => t!.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .OrderBy(dt => dt.DateLeft).ThenBy(dt => dt.TimeLeft)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSITerminalsByPortId(int portId, CancellationToken cancellationToken)
        {
            var terminals = await _dbContext
                .MMSITerminals
                .Where(t => t.PortId == portId)
                .OrderBy(t => t.TerminalName)
                .ToListAsync(cancellationToken);

            var terminalsList = terminals.Select(t => new SelectListItem
            {
                Value = t.TerminalId.ToString(),
                Text = t.TerminalName
            }).ToList();

            return terminalsList;
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

        public async Task<List<SelectListItem>> GetMMSICustomersWithBillablesSelectList(int currentCustomerId, string type, CancellationToken cancellationToken = default)
        {
            var dispatchToBeBilled = await _dbContext.MMSIDispatchTickets
                .Where(t => t.Status == "For Billing" || (currentCustomerId != 0 && t.CustomerId == currentCustomerId))
                .Include(t => t.Customer)
                .ToListAsync(cancellationToken);

            var listOfCustomerWithBillableTickets = dispatchToBeBilled
                .Select(t => t.Customer!.CustomerId)
                .Distinct()
                .ToList();

            return await _dbContext.FilprideCustomers
                .Where(c => c.IsMMSI == true && listOfCustomerWithBillableTickets.Contains(c.CustomerId) &&
                            (string.IsNullOrEmpty(type) || c.Type == type))
                .OrderBy(s => s.CustomerName)
                .Select(s => new SelectListItem
                {
                    Value = s.CustomerId.ToString(),
                    Text = s.CustomerName
                }).ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSIUnbilledTicketsById(string type, CancellationToken cancellationToken = default)
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

        public async Task<List<SelectListItem>?> GetMMSIUnbilledTicketsByCustomer(int? customerId, CancellationToken cancellationToken)
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
                Text = b.DispatchNumber
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

        public async Task<string> GenerateBillingNumber(CancellationToken cancellationToken = default)
        {
            var lastRecord = await _dbContext.MMSIBillings
                .Where(b => b.IsUndocumented == true && b.MMSIBillingNumber != null)
                .OrderByDescending(b => b.MMSIBillingNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastRecord == null)
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

        public MMSIBilling ProcessAddress(MMSIBilling model, CancellationToken cancellationToken = default)
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

            foreach (string word in words!)
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

            model.AddressLine1 = firstString;
            model.AddressLine2 = secondString;
            model.AddressLine3 = thirdString;
            model.AddressLine4 = fourthString;

            return model;
        }
    }
}
