using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class GeneralLedgerRepository : Repository<FilprideGeneralLedgerBook>, IGeneralLedgerRepository
    {
        private readonly ApplicationDbContext _db;

        public GeneralLedgerRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task ReverseEntries(string? reference, CancellationToken cancellationToken = default)
        {
            var existingGlEntries = await _db.FilprideGeneralLedgerBooks
                .Where(gl => gl.Reference == reference)
                .ToListAsync(cancellationToken);

            if (existingGlEntries.Count == 0)
            {
                return;
            }

            var reversingDate = DateTimeHelper.GetCurrentPhilippineTime();

            var reversingEntries = existingGlEntries.Select(gl => new FilprideGeneralLedgerBook
            {
                Date = DateOnly.FromDateTime(reversingDate),
                Reference = gl.Reference,
                Description = $"Reversal of '{gl.Description}'",
                AccountNo = gl.AccountNo,
                AccountTitle = gl.AccountTitle,
                Debit = gl.Credit,   // Swap
                Credit = gl.Debit,   // Swap
                CreatedBy = gl.CreatedBy,
                CreatedDate = reversingDate,
                IsPosted = gl.IsPosted,
                AccountId = gl.AccountId,
                ModuleType = gl.ModuleType,
                SubAccountId = gl.SubAccountId,
                SubAccountName = gl.SubAccountName,
                SubAccountType = gl.SubAccountType,
            }).ToList();

            await _db.FilprideGeneralLedgerBooks.AddRangeAsync(reversingEntries, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
