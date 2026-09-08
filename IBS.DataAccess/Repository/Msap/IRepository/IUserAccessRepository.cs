using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap.Enums;
using IBS.Models.Msap.MasterFile;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IUserAccessRepository : IRepository<UserAccess>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<string>> GetUserIdsWithAccessAsync(ProcedureEnum procedure, CancellationToken cancellationToken = default);
    }
}


