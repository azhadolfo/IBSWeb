using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Utility.Enums;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class JournalVoucherRepository : Repository<MobilityJournalVoucherHeader>, IJournalVoucherRepository
    {
        private readonly ApplicationDbContext _db;

        public JournalVoucherRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string stationCode, string? type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(stationCode, cancellationToken);
            }

            return await GenerateCodeForUnDocumented(stationCode, cancellationToken);
        }

        private async Task<string> GenerateCodeForDocumented(string stationCode, CancellationToken cancellationToken = default)
        {
            var lastJv = await _db
                .MobilityJournalVoucherHeaders
                .Where(c => c.StationCode == stationCode && c.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.JournalVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastJv == null)
            {
                return "JV0000000001";
            }

            var lastSeries = lastJv.JournalVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
        }

        private async Task<string> GenerateCodeForUnDocumented(string stationCode, CancellationToken cancellationToken = default)
        {
            var lastJv = await _db
                .MobilityJournalVoucherHeaders
                .Where(c => c.StationCode == stationCode && c.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.JournalVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastJv == null)
            {
                return "JVU000000001";
            }

            var lastSeries = lastJv.JournalVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");

        }

        public override async Task<MobilityJournalVoucherHeader?> GetAsync(Expression<Func<MobilityJournalVoucherHeader, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(cv => cv.CheckVoucherHeader)
                .ThenInclude(supplier => supplier!.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<MobilityJournalVoucherHeader>> GetAllAsync(Expression<Func<MobilityJournalVoucherHeader, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MobilityJournalVoucherHeader> query = dbSet
                .Include(cv => cv.CheckVoucherHeader)
                .ThenInclude(supplier => supplier!.Supplier);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
