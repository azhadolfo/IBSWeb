using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Models.Mobility.MasterFile;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class CustomerRepository : Repository<MobilityCustomer>, ICustomerRepository
    {
        private ApplicationDbContext _db;

        public CustomerRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task UpdateAsync(MobilityCustomer model, CancellationToken cancellationToken = default)
        {
            MobilityCustomer existingCustomer = await _db.MobilityCustomers
                .FindAsync(model.CustomerId, cancellationToken) ?? throw new InvalidOperationException($"Customer with id '{model.CustomerId}' not found.");

            existingCustomer.CustomerName = model.CustomerName;
            existingCustomer.CustomerCodeName = model.CustomerCodeName;
            existingCustomer.StationCode = model.StationCode;
            existingCustomer.QuantityLimit = model.QuantityLimit;
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
            existingCustomer.IsCheckDetailsRequired = model.IsCheckDetailsRequired;


            if (_db.ChangeTracker.HasChanges())
            {
                existingCustomer.EditedBy = model.EditedBy;
                existingCustomer.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                FilprideAuditTrail auditTrailBook = new(existingCustomer.CreatedBy!, $"Edited customer {existingCustomer.CustomerCode}", "Customer", nameof(Mobility));
                await _db.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        public async Task<string> GenerateCodeAsync(string customerType, string stationCode, CancellationToken cancellationToken = default)
        {
            MobilityCustomer? lastCustomer = await _db
                .MobilityCustomers
                .Where(c => c.CustomerType == customerType && c.StationCode == stationCode)
                .OrderBy(c => c.CustomerId)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCustomer != null)
            {
                string lastCode = lastCustomer.CustomerCode!;
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
            else if (customerType == nameof(CustomerType.Reseller))
            {
                return "RES0001";
            }
            else
            {
                return "GOV0001";
            }
        }
    }
}
