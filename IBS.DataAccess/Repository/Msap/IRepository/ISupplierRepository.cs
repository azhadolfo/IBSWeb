using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IMsapSupplierRepository : IRepository<MsapSupplier>
    {
        Task<bool> IsTinNoExistAsync(string tin, string branch, string category, string company, CancellationToken cancellationToken = default);

        Task<bool> IsSupplierExistAsync(string supplierName, string category, string company, CancellationToken cancellationToken = default);

        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task<string> SaveProofOfRegistration(IFormFile file, string localPath, CancellationToken cancellationToken = default);

        Task UpdateAsync(MsapSupplier model, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default);
    }
}