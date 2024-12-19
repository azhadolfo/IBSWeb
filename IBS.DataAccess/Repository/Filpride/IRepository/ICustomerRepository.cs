using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MasterFile.IRepository
{
    public interface ICustomerRepository : IRepository<FilprideCustomer>
    {
        Task<bool> IsTinNoExistAsync(string tin, string company, CancellationToken cancellationToken = default);

        Task<string> GenerateCodeAsync(string customerType, string company, CancellationToken cancellationToken = default);

        Task UpdateAsync(FilprideCustomer model, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCustomerBranchesSelectListAsync(int customerId, CancellationToken cancellationToken = default);
    }
}
