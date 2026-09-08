using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.Msap;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class MsapReportRepository(ApplicationDbContext db): IMsapReportRepository
    {
        public async Task<List<DispatchTicket>> GetDispatchReportData(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default, bool filterByBillingDate = false)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be earlier than Date To.");
            }

            var query = db.MsapDispatchTickets.AsQueryable();

            if (filterByBillingDate)
            {
                query = query.Where(dt => dt.BillingId != null
                    && db.MsapBillings.Any(b => b.MsapBillingId == dt.BillingId && b.Date >= dateFrom && b.Date <= dateTo));
            }
            else
            {
                query = query.Where(dt => dt.Date >= dateFrom && dt.Date <= dateTo);
            }

            var tickets = filterByBillingDate
                ? await query
                    .Include(dt => dt.Customer)
                    .Include(dt => dt.Vessel)
                    .Include(dt => dt.Tugboat)
                    .ThenInclude(t => t.TugboatOwner)
                    .Include(dt => dt.TugMaster)
                    .Include(dt => dt.Terminal)
                    .ThenInclude(t => t.Port)
                    .Include(dt => dt.Service)
                    .OrderBy(dt => dt.Billing!.Date)
                    .ThenBy(dt => dt.DispatchNumber)
                    .ToListAsync(cancellationToken)
                : await query
                    .Include(dt => dt.Customer)
                    .Include(dt => dt.Vessel)
                    .Include(dt => dt.Tugboat)
                    .ThenInclude(t => t.TugboatOwner)
                    .Include(dt => dt.TugMaster)
                    .Include(dt => dt.Terminal)
                    .ThenInclude(t => t.Port)
                    .Include(dt => dt.Service)
                    .OrderBy(dt => dt.Date)
                    .ThenBy(dt => dt.DispatchNumber)
                    .ToListAsync(cancellationToken);

            var billingIds = tickets.Where(t => t.BillingId.HasValue).Select(t => t.BillingId!.Value).Distinct().ToList();
            if (billingIds.Count != 0)
            {
                var billings = await db.MsapBillings
                    .Where(b => billingIds.Contains(b.MsapBillingId))
                    .Include(b => b.Customer)
                    .Include(b => b.Principal)
                    .ToListAsync(cancellationToken);

                var collectionIds = billings.Where(b => b.CollectionId.HasValue).Select(b => b.CollectionId!.Value).Distinct().ToList();
                var collections = collectionIds.Count != 0
                    ? await db.MsapCollections.Where(c => collectionIds.Contains(c.MsapCollectionId)).ToListAsync(cancellationToken)
                    : [];

                foreach (var ticket in tickets.Where(t => t.BillingId.HasValue))
                {
                    ticket.Billing = billings.FirstOrDefault(b => b.MsapBillingId == ticket.BillingId);
                    if (ticket.Billing?.CollectionId != null)
                    {
                        ticket.Billing.Collection = collections.FirstOrDefault(c => c.MsapCollectionId == ticket.Billing.CollectionId);
                    }
                }
            }

            return tickets;
        }
    }
}


