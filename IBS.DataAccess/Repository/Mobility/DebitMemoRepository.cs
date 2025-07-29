using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Utility.Enums;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class DebitMemoRepository : Repository<MobilityDebitMemo>, IDebitMemoRepository
    {
        private readonly ApplicationDbContext _db;

        public DebitMemoRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string stationCode, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(stationCode, cancellationToken);
            }

            return await GenerateCodeForUnDocumented(stationCode, cancellationToken);
        }

        private async Task<string> GenerateCodeForDocumented(string stationCode, CancellationToken cancellationToken = default)
        {
            var lastDm = await _db
                .MobilityDebitMemos
                .Where(cm => cm.StationCode == stationCode && cm.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.DebitMemoNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastDm == null)
            {
                return "DM0000000001";
            }

            var lastSeries = lastDm.DebitMemoNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");

        }

        private async Task<string> GenerateCodeForUnDocumented(string stationCode, CancellationToken cancellationToken = default)
        {
            var lastDm = await _db
                .MobilityDebitMemos
                .Where(cm => cm.StationCode == stationCode && cm.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.DebitMemoNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastDm == null)
            {
                return "DMU000000001";
            }

            var lastSeries = lastDm.DebitMemoNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");

        }

        public override async Task<MobilityDebitMemo?> GetAsync(Expression<Func<MobilityDebitMemo, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<MobilityDebitMemo>> GetAllAsync(Expression<Func<MobilityDebitMemo, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MobilityDebitMemo> query = dbSet
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Service);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
