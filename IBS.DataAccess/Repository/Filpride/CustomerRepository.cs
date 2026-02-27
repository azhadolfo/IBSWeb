using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Enums;
using IBS.Models.Filpride.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
                .OrderByDescending(c => c.CustomerId)
                .FirstOrDefaultAsync(c => c.CustomerType == customerType, cancellationToken);

            if (lastCustomer == null)
            {
                return customerType switch
                {
                    nameof(CustomerType.Retail) => "RET0001",
                    nameof(CustomerType.Industrial) => "IND0001",
                    nameof(CustomerType.Reseller) => "RES0001",
                    _ => "GOV0001"
                };
            }

            var lastCode = lastCustomer.CustomerCode!;
            var numericPart = lastCode.Substring(3);

            // Parse the numeric part and increment it by one
            var incrementedNumber = int.Parse(numericPart) + 1;

            // Format the incremented number with leading zeros and concatenate with the letter part
            return lastCode.Substring(0, 3) + incrementedNumber.ToString("D4");
        }

        public async Task<bool> IsTinNoExistAsync(string tin, string company, CancellationToken cancellationToken = default)
        {
            if (tin == "000-000-000-00000")
                return false;

            return await _db.FilprideCustomers
                .AnyAsync(c =>
                    c.Company == company &&
                    c.CustomerTin == tin,
                    cancellationToken);
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
            existingCustomer.CommissionRate = model.CommissionRate;
            existingCustomer.CommissioneeId = model.CommissioneeId;

            if (_db.ChangeTracker.HasChanges())
            {
                existingCustomer.EditedBy = model.EditedBy;
                existingCustomer.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
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

        public override async Task<FilprideCustomer?> GetAsync(Expression<Func<FilprideCustomer, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                    .Include(c => c.Commissionee)
                    .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideCustomer>> GetAllAsync(Expression<Func<FilprideCustomer, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideCustomer> query = dbSet
                .Include(dr => dr.Commissionee);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
