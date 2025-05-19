using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class MMSIReportRepository : IMMSIReportRepository
    {
        public readonly ApplicationDbContext _dbContext;

        public MMSIReportRepository (ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<MMSIDispatchTicket>> GetSalesReport (DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var dispatchTickets = await _dbContext.MMSIDispatchTickets
                .Where(dt => dt.Date >= dateFrom
                             && dt.Date <= dateTo
                             && dt.Status != "For Posting"
                             && dt.Status != "Cancelled"
                             && dt.Status != "Disapproved")
                .Include(dt => dt.Customer)
                .Include(dt => dt.Vessel)
                .Include(dt => dt.Tugboat)
                .Include(dt => dt.Terminal)
                .ThenInclude(t => t.Port)
                .Include(dt => dt.Service)
                .OrderBy(dt => dt.Date)
                .ToListAsync(cancellationToken);

            foreach (var dispatchTicket in dispatchTickets)
            {
                dispatchTicket.Billing = await _dbContext.MMSIBillings
                    .Where(b => b.MMSIBillingId == int.Parse(dispatchTicket.BillingId))
                    .Include(b => b.Customer)
                    .Include(b => b.Principal)
                    .FirstOrDefaultAsync(cancellationToken);

                dispatchTicket.Billing.Collection = await _dbContext.MMSICollections
                    .Where(c => c.MMSICollectionId == dispatchTicket.Billing.CollectionId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return dispatchTickets;
        }
    }
}
