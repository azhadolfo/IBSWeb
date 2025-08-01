using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class MMSIReportRepository : IMMSIReportRepository
    {
        private readonly ApplicationDbContext _db;

        public MMSIReportRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<MMSIDispatchTicket>> GetSalesReport(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var dispatchTickets = await _db.MMSIDispatchTickets
                .Where(dt => dt.Date >= dateFrom
                             && dt.Date <= dateTo
                             && dt.Status != "For Posting"
                             && dt.Status != "Cancelled"
                             && dt.Status != "Disapproved")
                .Include(dt => dt.Customer)
                .Include(dt => dt.Vessel)
                .Include(dt => dt.Tugboat)
                .Include(dt => dt.Terminal)
                .ThenInclude(t => t!.Port)
                .Include(dt => dt.Service)
                .OrderBy(dt => dt.Date)
                .ToListAsync(cancellationToken);

            foreach (var dispatchTicket in dispatchTickets)
            {
                if (dispatchTicket.BillingId != null)
                {
                    dispatchTicket.Billing = await _db.MMSIBillings
                        .Where(b => b.MMSIBillingId == int.Parse(dispatchTicket.BillingId))
                        .Include(b => b.Customer)
                        .Include(b => b.Principal)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (dispatchTicket.Billing?.CollectionId != null)
                    {
                        dispatchTicket.Billing.Collection = await _db.MMSICollections
                            .Where(c => c.MMSICollectionId == dispatchTicket.Billing.CollectionId)
                            .FirstOrDefaultAsync(cancellationToken);
                    }
                }
            }

            return dispatchTickets;
        }
    }
}
