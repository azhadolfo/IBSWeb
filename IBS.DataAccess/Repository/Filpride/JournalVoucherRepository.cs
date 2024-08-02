using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class JournalVoucherRepository : Repository<FilprideJournalVoucherHeader>, IJournalVoucherRepository
    {
        private ApplicationDbContext _db;

        public JournalVoucherRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            FilprideJournalVoucherHeader? lastJv = await _db
                .FilprideJournalVoucherHeaders
                .OrderBy(c => c.JournalVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastJv != null)
            {
                string lastSeries = lastJv.JournalVoucherHeaderNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "JV0000000001";
            }
        }
    }
}