using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class MsapTermsRepository(ApplicationDbContext db): Repository<MsapTerms>(db), IMsapTermsRepository
    {
        private readonly ApplicationDbContext _db = db;

        public override async Task<IEnumerable<MsapTerms>> GetAllAsync(Expression<Func<MsapTerms, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MsapTerms> query = dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(MsapTerms model, CancellationToken cancellationToken = default)
        {
            var existingTerms = await _db.MsapTerms
                .FirstOrDefaultAsync(x => x.TermsCode == model.TermsCode, cancellationToken)
                                   ?? throw new InvalidOperationException($"Terms with code '{model.TermsCode}' not found.");

            existingTerms.TermsCode = model.TermsCode;
            existingTerms.NumberOfDays = model.NumberOfDays;
            existingTerms.NumberOfMonths = model.NumberOfMonths;

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

        public async Task<List<SelectListItem>> GetTermsListAsyncByCode(CancellationToken cancellationToken = default)
        {
            return await _db.MsapTerms
                .OrderBy(x => x.TermsCode)
                .Select(x => new SelectListItem
                {
                    Value = x.TermsCode,
                    Text = x.TermsCode
                })
                .ToListAsync(cancellationToken);
        }
    }
}