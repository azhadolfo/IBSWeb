using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Http;

namespace IBS.DataAccess.Repository.MasterFile.IRepository
{
    public interface ISupplierRepository : IRepository<Supplier>
    {
        Task<bool> IsTinNoExistAsync(string tin, CancellationToken cancellationToken = default);

        Task<bool> IsSupplierExistAsync(string supplierName, CancellationToken cancellationToken = default);

        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task<string> SaveProofOfRegistration(IFormFile file, string localPath, CancellationToken cancellationToken = default);

        Task UpdateAsync(Supplier model, CancellationToken cancellationToken = default);

    }
}
