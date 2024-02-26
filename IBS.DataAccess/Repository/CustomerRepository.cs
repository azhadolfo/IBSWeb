using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        private ApplicationDbContext _db;

        public CustomerRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string customerType, CancellationToken cancellationToken = default)
        {
            Customer? lastCustomer = await _db
                .Customers
                .Where(c => c.CustomerType == customerType)
                .OrderBy(c => c.CustomerId)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCustomer != null)
            {
                // Extract the last customer's code
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                string lastCode = lastCustomer.CustomerCode;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                // Extract the numeric part of the code
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                string numericPart = lastCode.Substring(3); // Assuming code format starts with a letter followed by numeric part
#pragma warning restore CS8602 // Dereference of a possibly null reference.

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

        public async Task<bool> IsTinNoExistAsync(string tin, CancellationToken cancellationToken = default)
        {
            return await _db.Customers
                .AnyAsync(c => c.CustomerTin == tin, cancellationToken);
        }

        public async Task UpdateAsync(Customer model, CancellationToken cancellationToken = default)
        {
            Customer? existingCustomer = await _db.Customers
                .FindAsync(model.CustomerId, cancellationToken);

            existingCustomer!.CustomerName = model.CustomerName;
            existingCustomer!.CustomerAddress = model.CustomerAddress;
            existingCustomer!.CustomerTin = model.CustomerTin;
            existingCustomer!.BusinessStyle = model.BusinessStyle;
            existingCustomer!.CustomerTerms = model.CustomerTerms;
            existingCustomer!.CustomerType = model.CustomerType;
            existingCustomer!.WithHoldingVat = model.WithHoldingVat;
            existingCustomer!.WithHoldingTax = model.WithHoldingTax;

            if (_db.ChangeTracker.HasChanges())
            {
                existingCustomer!.EditedBy = "Ako";
                existingCustomer!.EditedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("No data changes!");
            }
        }
    }
}