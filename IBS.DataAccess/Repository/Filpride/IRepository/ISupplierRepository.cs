using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ISupplierRepository : IRepository<FilprideSupplier>
    {
        Task<bool> IsTinNoExistAsync(string tin, string branch, string category, string company, CancellationToken cancellationToken = default);

        Task<bool> IsSupplierExistAsync(string supplierName, string category, string company, CancellationToken cancellationToken = default);

        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task<string> SaveProofOfRegistration(IFormFile file, string localPath, CancellationToken cancellationToken = default);

        Task UpdateAsync(FilprideSupplier model, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetFilprideTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default);
    }
}
