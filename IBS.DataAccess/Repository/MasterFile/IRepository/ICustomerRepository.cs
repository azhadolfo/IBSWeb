using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MasterFile;

namespace IBS.DataAccess.Repository.MasterFile.IRepository
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<bool> IsTinNoExistAsync(string tin, CancellationToken cancellationToken = default);

        Task<string> GenerateCodeAsync(string customerType, CancellationToken cancellationToken = default);

        Task UpdateAsync(Customer model, CancellationToken cancellationToken = default);
    }
}