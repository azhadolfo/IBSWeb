using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Models.Mobility.MasterFile;
using IBS.Utility;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class SupplierRepository : Repository<MobilitySupplier>, ISupplierRepository
    {
        private ApplicationDbContext _db;

        public SupplierRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string stationCodeClaims, CancellationToken cancellationToken = default)
        {
            MobilitySupplier? lastSupplier = await _db
                .MobilitySuppliers
                .Where(s => s.StationCode == stationCodeClaims)
                .OrderBy(s => s.SupplierId)
                .LastOrDefaultAsync(cancellationToken);

            if (lastSupplier != null)
            {
                string lastCode = lastSupplier.SupplierCode!;
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

        public async Task UpdateAsync(MobilitySupplier model, CancellationToken cancellationToken = default)
        {
            MobilitySupplier existingSupplier = await _db.MobilitySuppliers
                .FindAsync(model.SupplierId, cancellationToken) ?? throw new InvalidOperationException($"Supplier with id '{model.SupplierId}' not found.");

            existingSupplier.TradeName = model.TradeName;
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
            existingSupplier.ReasonOfExemption = model.ReasonOfExemption;
            existingSupplier.Validity = model.Validity;
            existingSupplier.ValidityDate = model.ValidityDate;

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

                FilprideAuditTrail auditTrailBook = new(existingSupplier.CreatedBy!, $"Edited supplier {existingSupplier.SupplierCode}", "Supplier", nameof(Mobility));
                await _db.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        public async Task<List<SelectListItem>> GetMobilityTradeSupplierListAsyncById(string stationCodeClaims, CancellationToken cancellationToken = default)
        {
            return await _db.MobilitySuppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive && s.StationCode == stationCodeClaims && s.Category == "Trade")
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetmobilityTradeSupplierListAsyncById(string stationCode, CancellationToken cancellationToken = default)
        {
            return await _db.MobilitySuppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive && s.Category == "Trade" && s.StationCode == stationCode)
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }
    }
}
