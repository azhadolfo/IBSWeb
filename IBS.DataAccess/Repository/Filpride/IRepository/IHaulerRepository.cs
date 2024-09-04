using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IHaulerRepository : IRepository<FilprideHauler>
    {
        Task<string> GenerateCodeAsync(string company, CancellationToken cancellationToken = default);

        Task<bool> IsHaulerNameExistAsync(string haulerName, string company, CancellationToken cancellationToken);

        Task UpdateAsync(FilprideHauler model, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetHaulerListAsync(string company, CancellationToken cancellationToken = default);
    }
}