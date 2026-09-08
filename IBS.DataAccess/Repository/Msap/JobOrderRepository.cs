using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models;
using IBS.Models.Msap;
using IBS.Utility.Msap;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class JobOrderRepository(ApplicationDbContext db): Repository<JobOrder>(db), IJobOrderRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task<IEnumerable<JobOrder>> GetAllJobOrdersWithDetailsAsync(CancellationToken cancellationToken)
        {
            return await _db.MsapJobOrders
                .Include(j => j.Customer)
                .Include(j => j.Vessel)
                .Include(j => j.Port)
                .Include(j => j.Terminal)
                .Include(j => j.DispatchTickets.Where(dt => dt.Status != SD.DispatchTicketStatus.Deleted && dt.Status != SD.ServiceRequestStatus.ServiceRequestDeleted))
                .OrderByDescending(j => j.JobOrderNumber)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<JobOrder>> GetJobOrdersWithDetailsAsync(DateTime start, DateTime end, CancellationToken cancellationToken)
        {
            return await _db.MsapJobOrders
                .Include(j => j.Customer)
                .Include(j => j.Vessel)
                .Include(j => j.Port)
                .Include(j => j.Terminal)
                .Include(j => j.DispatchTickets.Where(dt => dt.Status != SD.DispatchTicketStatus.Deleted && dt.Status != SD.ServiceRequestStatus.ServiceRequestDeleted))
                .Where(j => j.PlannedStartTime <= end && j.PlannedEndTime >= start)
                .ToListAsync(cancellationToken);
        }

        public async Task<JobOrder?> GetJobOrderWithDetailsAsync(int id, CancellationToken cancellationToken)
        {
            return await _db.MsapJobOrders
                .Include(j => j.Customer)
                .Include(j => j.Vessel)
                .Include(j => j.Port)
                .Include(j => j.Terminal)
                .Include(j => j.DispatchTickets.Where(dt => dt.Status != SD.DispatchTicketStatus.Deleted && dt.Status != SD.ServiceRequestStatus.ServiceRequestDeleted))
                    .ThenInclude(dt => dt.Service)
                .Include(j => j.DispatchTickets.Where(dt => dt.Status != SD.DispatchTicketStatus.Deleted && dt.Status != SD.ServiceRequestStatus.ServiceRequestDeleted))
                    .ThenInclude(dt => dt.Terminal)
                    .ThenInclude(t => t.Port)
                .Include(j => j.DispatchTickets.Where(dt => dt.Status != SD.DispatchTicketStatus.Deleted && dt.Status != SD.ServiceRequestStatus.ServiceRequestDeleted))
                    .ThenInclude(dt => dt.Tugboat)
                .Include(j => j.DispatchTickets.Where(dt => dt.Status != SD.DispatchTicketStatus.Deleted && dt.Status != SD.ServiceRequestStatus.ServiceRequestDeleted))
                    .ThenInclude(dt => dt.TugMaster)
                .FirstOrDefaultAsync(j => j.JobOrderId == id, cancellationToken);
        }

        public async Task<string> GenerateJobOrderNumber(CancellationToken cancellationToken)
        {
            var year = DateTime.Now.Year;
            var lastRecord = await _db.MsapJobOrders
                .Where(j => j.JobOrderNumber.StartsWith($"JO-{year}"))
                .OrderByDescending(j => j.JobOrderNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastRecord == null)
            {
                return $"JO-{year}-0001";
            }

            var parts = lastRecord.JobOrderNumber.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[2], out int lastNumber))
            {
                return $"JO-{year}-{(lastNumber + 1):D4}";
            }

            return $"JO-{year}-0001";
        }
        public async Task<List<JobOrder>> SearchBillableJobOrdersAsync(string term, int customerId, int limit, CancellationToken cancellationToken)
        {
            var query = dbSet.AsNoTracking()
                .Where(j => j.CustomerId == customerId &&
                            j.DispatchTickets.Any(dt => dt.Status == SD.DispatchTicketStatus.ForBilling && dt.BillingId == null) &&
                            j.DispatchTickets.All(dt => dt.Status == SD.DispatchTicketStatus.ForBilling ||
                                                        dt.Status == SD.DispatchTicketStatus.Billed ||
                                                        dt.Status == SD.DispatchTicketStatus.Deleted ||
                                                        dt.Status == SD.ServiceRequestStatus.ServiceRequestDeleted));

            if (!string.IsNullOrWhiteSpace(term))
            {
                var s = term.ToLower();
                query = query.Where(j => j.JobOrderNumber.ToLower().Contains(s));
            }

            return await query
                .OrderByDescending(j => j.Date)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IEnumerable<JobOrder> Data, int RecordsFiltered, int TotalRecords)> GetPagedJobOrdersAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            IQueryable<JobOrder> query = dbSet
                .Include(j => j.Customer)
                .Include(j => j.Vessel)
                .Include(j => j.Port)
                .Include(j => j.Terminal)
                .Include(j => j.DispatchTickets.Where(dt => dt.Status != SD.DispatchTicketStatus.Deleted && dt.Status != SD.ServiceRequestStatus.ServiceRequestDeleted));

            if (!string.IsNullOrEmpty(parameters.Search.Value))
            {
                var s = parameters.Search.Value.ToLower();
                query = query.Where(dt =>
                    dt.JobOrderNumber.ToLower().Contains(s) ||
                    dt.Customer.CustomerName.ToLower().Contains(s) ||
                    dt.Status.ToLower().Contains(s)
                );
            }

            // Column-specific search
            if (parameters.Columns != null)
            {
                foreach (var column in parameters.Columns)
                {
                    if (column.Search?.Value is { Length: > 0 } searchValue)
                    {
                        if (column.Data == "status")
                        {
                            query = query.Where(b => b.Status.ToLower() == searchValue);
                        }
                        else if (column.Data == "date" || column.Data == "Date")
                        {
                            if (DateOnly.TryParse(searchValue, out var parsedDate))
                            {
                                query = query.Where(b => b.Date == parsedDate);
                            }
                        }
                    }
                }
            }

            var totalRecords = await dbSet.CountAsync(cancellationToken);
            var recordsFiltered = await query.CountAsync(cancellationToken);

            if (parameters.Order?.Count > 0 && parameters.Columns != null)
            {
                var order = parameters.Order[0];
                var column = parameters.Columns[order.Column];

                query = order.Dir == "desc"
                    ? query.OrderByDescending(e => EF.Property<object>(e, char.ToUpper(column.Data[0]) + column.Data.Substring(1)))
                    : query.OrderBy(e => EF.Property<object>(e, char.ToUpper(column.Data[0]) + column.Data.Substring(1)));
            }
            else
            {
                query = query.OrderByDescending(j => j.Date);
            }

            var data = await query
                .Skip(parameters.Start)
                .Take(parameters.Length)
                .ToListAsync(cancellationToken);

            return (data, recordsFiltered, totalRecords);
        }
    }
}


