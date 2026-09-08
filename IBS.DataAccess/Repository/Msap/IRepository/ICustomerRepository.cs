using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap.MasterFile;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IMsapCustomerRepository : IRepository<MsapCustomer>
    {
        Task<bool> IsTinNoExistAsync(string tin, string company, CancellationToken cancellationToken = default);

        Task<string> GenerateCodeAsync(string customerType, CancellationToken cancellationToken = default);

        Task UpdateAsync(MsapCustomer model, CancellationToken cancellationToken = default);

        Task<List<MsapCustomer>> SearchCustomersAsync(string term, int limit, CancellationToken cancellationToken);

        Task<List<object>> SearchCustomersDtoAsync(string term, int limit, CancellationToken cancellationToken);
    }
}