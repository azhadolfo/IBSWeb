using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Utility.Enums;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class CreditMemoRepository : Repository<MobilityCreditMemo>, ICreditMemoRepository
    {
        private readonly ApplicationDbContext _db;

        public CreditMemoRepository(ApplicationDbContext db) : base(db)
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

        private async Task<string> GenerateCodeForDocumented(string stationCode, CancellationToken cancellationToken = default)
        {
            var lastCm = await _db
                .MobilityCreditMemos
                .Where(cm => cm.StationCode == stationCode && cm.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.CreditMemoNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCm == null)
            {
                return "CM0000000001";
            }

            var lastSeries = lastCm.CreditMemoNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");

        }

        private async Task<string> GenerateCodeForUnDocumented(string stationCode, CancellationToken cancellationToken = default)
        {
            var lastCm = await _db
                .MobilityCreditMemos
                .Where(cm => cm.StationCode == stationCode && cm.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.CreditMemoNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCm == null)
            {
                return "CMU000000001";
            }

            var lastSeries = lastCm.CreditMemoNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");

        }

        public override async Task<MobilityCreditMemo?> GetAsync(Expression<Func<MobilityCreditMemo, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<MobilityCreditMemo>> GetAllAsync(Expression<Func<MobilityCreditMemo, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MobilityCreditMemo> query = dbSet
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
