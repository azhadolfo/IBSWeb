using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;

namespace IBS.Services.Msap
{
    public interface ITerminalService
    {
        Task<IEnumerable<Terminal>> GetAllAsync(CancellationToken cancellationToken);
        Task<Terminal?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<Terminal> PopulateSelectListsAsync(Terminal? model, CancellationToken cancellationToken);
        Task<ServiceResult<int>> CreateAsync(Terminal model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> UpdateAsync(Terminal model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken);
        Task<IEnumerable<object>> GetTerminalsByPortAsync(int portId, CancellationToken cancellationToken);
    }
}
