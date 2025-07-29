using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.Models.Mobility;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Utility.Enums;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class ServiceInvoiceRepository : Repository<MobilityServiceInvoice>, IServiceInvoiceRepository
    {
        private readonly ApplicationDbContext _db;

        public ServiceInvoiceRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(company, cancellationToken);
            }

            return await GenerateCodeForUnDocumented(company, cancellationToken);
        }


        private async Task<string> GenerateCodeForDocumented(string stationCode, CancellationToken cancellationToken)
        {
            var lastSv = await _db
                .MobilityServiceInvoices
                .Where(c => c.StationCode == stationCode && c.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.ServiceInvoiceNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastSv == null)
            {
                return "SV0000000001";
            }

            var lastSeries = lastSv.ServiceInvoiceNo;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");

        }

        private async Task<string> GenerateCodeForUnDocumented(string stationCode, CancellationToken cancellationToken)
        {
            var lastSv = await _db
                .MobilityServiceInvoices
                .Where(c => c.StationCode == stationCode && c.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.ServiceInvoiceNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastSv == null)
            {
                return "SVU000000001";
            }

            var lastSeries = lastSv.ServiceInvoiceNo;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
        }

        public override async Task<MobilityServiceInvoice?> GetAsync(Expression<Func<MobilityServiceInvoice, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(s => s.Customer)
                .Include(s => s.Service)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<MobilityServiceInvoice>> GetAllAsync(Expression<Func<MobilityServiceInvoice, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MobilityServiceInvoice> query = dbSet
                .Include(s => s.Customer)
                .Include(s => s.Service);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
