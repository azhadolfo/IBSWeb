using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MasterFile
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private readonly ApplicationDbContext _db;

        public CompanyRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            var lastCompany = await _db
                .Companies
                .OrderBy(c => c.CompanyId)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCompany == null)
            {
                return "C01";
            }

            var lastCode = lastCompany.CompanyCode!;
            var numericPart = lastCode.Substring(1);

            // Parse the numeric part and increment it by one
            var incrementedNumber = int.Parse(numericPart) + 1;

            // Format the incremented number with leading zeros and concatenate with the letter part
            return $"{lastCode[0]}{incrementedNumber:D2}"; //e.g C02

        }

        public async Task<bool> IsCompanyExistAsync(string companyName, CancellationToken cancellationToken = default)
        {
            return await _db.Companies
                .AnyAsync(c => c.CompanyName == companyName, cancellationToken);
        }

        public async Task<bool> IsTinNoExistAsync(string tinNo, CancellationToken cancellationToken = default)
        {
            return await _db.Companies
                .AnyAsync(c => c.CompanyTin == tinNo, cancellationToken);
        }

        public async Task UpdateAsync(Company model, CancellationToken cancellationToken = default)
        {
            var existingCompany = await _db.Companies
                .FirstOrDefaultAsync(x => x.CompanyId == model.CompanyId, cancellationToken) ?? throw new InvalidOperationException($"Company with id '{model.CompanyId}' not found.");

            existingCompany.CompanyName = model.CompanyName;
            existingCompany.CompanyAddress = model.CompanyAddress;
            existingCompany.CompanyTin = model.CompanyTin;
            existingCompany.BusinessStyle = model.BusinessStyle;

            if (_db.ChangeTracker.HasChanges())
            {
                existingCompany.EditedBy = model.EditedBy;
                existingCompany.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }
    }
}
