using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Http;

namespace IBS.DataAccess.Repository.MasterFile.IRepository
{
    public interface ISupplierRepository : IRepository<FilprideSupplier>
    {
        Task<bool> IsTinNoExistAsync(string tin, string branch, string company, CancellationToken cancellationToken = default);

        Task<bool> IsSupplierExistAsync(string supplierName, string company, CancellationToken cancellationToken = default);

        Task<string> GenerateCodeAsync(string company, CancellationToken cancellationToken = default);

        Task<string> SaveProofOfRegistration(IFormFile file, string localPath, CancellationToken cancellationToken = default);

        Task UpdateAsync(FilprideSupplier model, CancellationToken cancellationToken = default);
    }
}