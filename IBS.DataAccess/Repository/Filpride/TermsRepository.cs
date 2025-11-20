using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class TermsRepository : Repository<FilprideTerms>, ITermsRepository
    {
        private readonly ApplicationDbContext _db;

        public TermsRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public override async Task<IEnumerable<FilprideTerms>> GetAllAsync(Expression<Func<FilprideTerms, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideTerms> query = dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(FilprideTerms model, CancellationToken cancellationToken = default)
        {
            var existingTerms = await _db.FilprideTerms
                .FirstOrDefaultAsync(x => x.TermsCode == model.TermsCode, cancellationToken)
                                   ?? throw new InvalidOperationException($"Terms with code '{model.TermsCode}' not found.");

            existingTerms.TermsCode = model.TermsCode;
            existingTerms.NumberOfDays = model.NumberOfDays;
            existingTerms.NumberOfMonths = model.NumberOfMonths;
            existingTerms.Company = model.Company;

            if (_db.ChangeTracker.HasChanges())
            {
                existingTerms.EditedBy = model.EditedBy;
                existingTerms.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        public async Task<List<SelectListItem>> GetFilprideTermsListAsyncByCode(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideTerms
                .OrderBy(x => x.TermsCode)
                .Where(x => x.Company == company)
                .Select(x => new SelectListItem
                {
                    Value = x.TermsCode,
                    Text = x.TermsCode
                })
                .ToListAsync(cancellationToken);
        }
    }
}
