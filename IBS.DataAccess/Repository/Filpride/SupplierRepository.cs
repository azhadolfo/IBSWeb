using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class SupplierRepository : Repository<FilprideSupplier>, ISupplierRepository
    {
        private ApplicationDbContext _db;

        public SupplierRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            FilprideSupplier? lastSupplier = await _db
                .FilprideSuppliers
                .OrderBy(s => s.SupplierId)
                .LastOrDefaultAsync(cancellationToken);

            if (lastSupplier != null)
            {
                string lastCode = lastSupplier.SupplierCode;
                string numericPart = lastCode.Substring(1);

                // Parse the numeric part and increment it by one
                int incrementedNumber = int.Parse(numericPart) + 1;

                // Format the incremented number with leading zeros and concatenate with the letter part
                return $"{lastCode[0]}{incrementedNumber:D6}"; //e.g S000002
            }
            else
            {
                return "S000001";
            }
        }

        public async Task<bool> IsSupplierExistAsync(string supplierName, string category, string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideSuppliers
                .AnyAsync(s => s.Company == company && s.SupplierName == supplierName && s.Category == category, cancellationToken);
        }

        public async Task<bool> IsTinNoExistAsync(string tin, string branch, string category, string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideSuppliers
                .AnyAsync(s => s.Company == company && s.SupplierTin == tin && s.Branch == branch && s.Category == category, cancellationToken);
        }

        public async Task<string> SaveProofOfRegistration(IFormFile file, string localPath, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }

            string fileName = Path.GetFileName(file.FileName);
            string fileSavePath = Path.Combine(localPath, fileName);

            await using (FileStream stream = new(fileSavePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            return fileSavePath;
        }

        public async Task UpdateAsync(FilprideSupplier model, CancellationToken cancellationToken = default)
        {
            FilprideSupplier existingSupplier = await _db.FilprideSuppliers
                .FindAsync(model.SupplierId, cancellationToken) ?? throw new InvalidOperationException($"Supplier with id '{model.SupplierId}' not found.");

            existingSupplier.Category = model.Category;
            existingSupplier.SupplierName = model.SupplierName;
            existingSupplier.SupplierAddress = model.SupplierAddress;
            existingSupplier.SupplierTin = model.SupplierTin;
            existingSupplier.Branch = model.Branch;
            existingSupplier.SupplierTerms = model.SupplierTerms;
            existingSupplier.VatType = model.VatType;
            existingSupplier.TaxType = model.TaxType;
            existingSupplier.DefaultExpenseNumber = model.DefaultExpenseNumber;
            existingSupplier.WithholdingTaxPercent = model.WithholdingTaxPercent;
            existingSupplier.ZipCode = model.ZipCode;
            existingSupplier.IsFilpride = model.IsFilpride;
            existingSupplier.IsMobility = model.IsMobility;
            existingSupplier.IsBienes = model.IsBienes;

            if (model.ProofOfRegistrationFilePath != null && existingSupplier.ProofOfRegistrationFilePath != model.ProofOfRegistrationFilePath)
            {
                existingSupplier.ProofOfRegistrationFilePath = model.ProofOfRegistrationFilePath;
            }

            if (model.ProofOfExemptionFilePath != null && existingSupplier.ProofOfExemptionFilePath != model.ProofOfExemptionFilePath)
            {
                existingSupplier.ProofOfExemptionFilePath = model.ProofOfExemptionFilePath;
            }

            if (_db.ChangeTracker.HasChanges())
            {
                existingSupplier.EditedBy = model.EditedBy;
                existingSupplier.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                FilprideAuditTrail auditTrailBook = new(existingSupplier.CreatedBy, $"Edited supplier {existingSupplier.SupplierCode}", "Supplier", "", existingSupplier.Company);
                await _db.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        public async Task<List<SelectListItem>> GetFilprideTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideSuppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive && s.Category == "Trade" && (company == nameof(Filpride) ? s.IsFilpride : s.IsMobility))
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }
    }
}
