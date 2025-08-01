using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class CustomerRepository : Repository<FilprideCustomer>, ICustomerRepository
    {
        private readonly ApplicationDbContext _db;

        public CustomerRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string customerType, CancellationToken cancellationToken = default)
        {
            var lastCustomer = await _db
                .FilprideCustomers
                .Where(c => c.CustomerType == customerType)
                .OrderBy(c => c.CustomerId)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCustomer != null)
            {
                var lastCode = lastCustomer.CustomerCode!;
                var numericPart = lastCode.Substring(3);

                // Parse the numeric part and increment it by one
                var incrementedNumber = int.Parse(numericPart) + 1;

                // Format the incremented number with leading zeros and concatenate with the letter part
                return lastCode.Substring(0, 3) + incrementedNumber.ToString("D4");
            }

            return customerType switch
            {
                nameof(CustomerType.Retail) => "RET0001",
                nameof(CustomerType.Industrial) => "IND0001",
                nameof(CustomerType.Reseller) => "RES0001",
                _ => "GOV0001"
            };
        }

        public async Task<bool> IsTinNoExistAsync(string tin, string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideCustomers
                .AnyAsync(c => c.Company == company && c.CustomerTin == tin, cancellationToken);
        }

        public async Task UpdateAsync(FilprideCustomer model, CancellationToken cancellationToken = default)
        {
            var existingCustomer = await _db.FilprideCustomers
                .FirstOrDefaultAsync(x => x.CustomerId == model.CustomerId, cancellationToken)
                                   ?? throw new InvalidOperationException($"Customer with id '{model.CustomerId}' not found.");

            existingCustomer.CustomerName = model.CustomerName;
            existingCustomer.CustomerAddress = model.CustomerAddress;
            existingCustomer.CustomerTin = model.CustomerTin;
            existingCustomer.BusinessStyle = model.BusinessStyle;
            existingCustomer.CustomerTerms = model.CustomerTerms;
            existingCustomer.CustomerType = model.CustomerType;
            existingCustomer.WithHoldingVat = model.WithHoldingVat;
            existingCustomer.WithHoldingTax = model.WithHoldingTax;
            existingCustomer.ClusterCode = model.ClusterCode;
            existingCustomer.CreditLimit = model.CreditLimit;
            existingCustomer.CreditLimitAsOfToday = model.CreditLimitAsOfToday;
            existingCustomer.ZipCode = model.ZipCode;
            existingCustomer.RetentionRate = model.RetentionRate;
            existingCustomer.IsFilpride = model.IsFilpride;
            existingCustomer.IsMobility = model.IsMobility;
            existingCustomer.IsBienes = model.IsBienes;
            existingCustomer.IsMMSI = model.IsMMSI;
            existingCustomer.VatType = model.VatType;
            existingCustomer.Type = model.Type;
            existingCustomer.RequiresPriceAdjustment = model.RequiresPriceAdjustment;
            existingCustomer.StationCode = model.StationCode;

            if (_db.ChangeTracker.HasChanges())
            {
                existingCustomer.EditedBy = model.EditedBy;
                existingCustomer.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                FilprideAuditTrail auditTrailBook = new(existingCustomer.CreatedBy!, $"Edited customer {existingCustomer.CustomerCode}", "Customer", existingCustomer.Company);
                await _db.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        public async Task<List<SelectListItem>> GetCustomerBranchesSelectListAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideCustomerBranches
                .OrderBy(c => c.BranchName)
                .Where(c => c.CustomerId == customerId)
                .Select(b => new SelectListItem
                {
                    Value = b.BranchName,
                    Text = b.BranchName
                })
                .ToListAsync(cancellationToken);
        }
    }
}
