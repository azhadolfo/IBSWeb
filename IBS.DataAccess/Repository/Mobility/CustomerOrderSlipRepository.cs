using IBS.DataAccess.Data;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class CustomerOrderSlipRepository : Repository<MobilityCustomerOrderSlip>, IRepository.ICustomerOrderSlipRepository
    {
        private ApplicationDbContext _db;

        public CustomerOrderSlipRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task UpdateCustomerCreditLimitAsync(int customerId, decimal quantity, decimal oldQuantity = default, CancellationToken cancellationToken = default)
        {
            var customer = await _db.MobilityCustomers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken)
                ?? throw new ArgumentException("Customer not found!", nameof(customerId));

            if (oldQuantity != default)
            {
                customer.QuantityLimit += oldQuantity;
            }

            if (quantity > customer.QuantityLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), $"Quantity exceeds the available limit ({customer.QuantityLimit:N2}) of the customer.");
            }

            customer.QuantityLimit -= quantity;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
