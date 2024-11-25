using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.Filpride.MasterFile;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class CustomerRepository : Repository<FilprideCustomer>, ICustomerRepository
    {
        private ApplicationDbContext _db;

        public CustomerRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string customerType, string company, CancellationToken cancellationToken = default)
        {
            FilprideCustomer? lastCustomer = await _db
                .FilprideCustomers
                .Where(c => c.Company == company && c.CustomerType == customerType)
                .OrderBy(c => c.CustomerId)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCustomer != null)
            {
                string lastCode = lastCustomer.CustomerCode;
                string numericPart = lastCode.Substring(3);

                // Parse the numeric part and increment it by one
                int incrementedNumber = int.Parse(numericPart) + 1;

                // Format the incremented number with leading zeros and concatenate with the letter part
                return lastCode.Substring(0, 3) + incrementedNumber.ToString("D4");
            }

            if (customerType == nameof(CustomerType.Retail))
            {
                return "RET0001";
            }
            else if (customerType == nameof(CustomerType.Industrial))
            {
                return "IND0001";
            }
            else
            {
                return "GOV0001";
            }
        }

        public async Task<bool> IsTinNoExistAsync(string tin, string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideCustomers
                .AnyAsync(c => c.Company == company && c.CustomerTin == tin, cancellationToken);
        }

        public async Task UpdateAsync(FilprideCustomer model, CancellationToken cancellationToken = default)
        {
            FilprideCustomer existingCustomer = await _db.FilprideCustomers
                .FindAsync(model.CustomerId, cancellationToken) ?? throw new InvalidOperationException($"Customer with id '{model.CustomerId}' not found.");

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
    }
}