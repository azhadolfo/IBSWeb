using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository
{
    public class SupplierRepository : Repository<Supplier>, ISupplierRepository
    {
        private ApplicationDbContext _db;

        public SupplierRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            Supplier? lastSupplier = await _db
                .Suppliers
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

        public async Task<bool> IsSupplierExistAsync(string supplierName, CancellationToken cancellationToken = default)
        {
            return await _db.Suppliers
                .AnyAsync(s => s.SupplierName == supplierName, cancellationToken);
        }

        public async Task<bool> IsTinNoExistAsync(string tin, CancellationToken cancellationToken = default)
        {
            return await _db.Suppliers
                .AnyAsync(s => s.SupplierTin == tin, cancellationToken);
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

        public async Task UpdateAsync(Supplier model, CancellationToken cancellationToken = default)
        {
            Supplier existingSupplier = await _db.Suppliers
                .FindAsync(model.SupplierId, cancellationToken) ?? throw new InvalidOperationException($"Supplier with id '{model.SupplierId}' not found.");

            existingSupplier.SupplierName = model.SupplierName;
            existingSupplier.SupplierAddress = model.SupplierAddress;
            existingSupplier.SupplierTin = model.SupplierTin;
            existingSupplier.SupplierTerms = model.SupplierTerms;
            existingSupplier.VatType = model.VatType;
            existingSupplier.TaxType = model.TaxType;

            if (_db.ChangeTracker.HasChanges())
            {
                existingSupplier.EditedBy = model.EditedBy;
                existingSupplier.EditedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }
    }
}
