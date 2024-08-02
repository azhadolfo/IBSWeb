using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IPurchaseOrderRepo : IRepository<PurchaseOrder>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);
    }
}