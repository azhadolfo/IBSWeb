using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ICustomerOrderSlipRepository : IRepository<FilprideCustomerOrderSlip>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);
    }
}