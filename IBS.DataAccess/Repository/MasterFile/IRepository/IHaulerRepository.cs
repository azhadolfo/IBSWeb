using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MasterFile;

namespace IBS.DataAccess.Repository.MasterFile.IRepository
{
    public interface IHaulerRepository : IRepository<Hauler>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task<bool> IsHaulerNameExistAsync(string haulerName, CancellationToken cancellationToken);

        Task UpdateAsync(Hauler model, CancellationToken cancellationToken = default);
    }
}