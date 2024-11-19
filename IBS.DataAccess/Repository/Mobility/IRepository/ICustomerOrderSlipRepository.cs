using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility.ViewModels;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface ICustomerOrderSlipRepository : IRepository<MobilityCustomerOrderSlip>
    {
        Task UpdateCustomerCreditLimitAsync(int customerId, decimal quantity, decimal oldQuantity = default, CancellationToken cancellationToken = default);
    }
}