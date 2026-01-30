using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface ITerminalRepository : IRepository<MMSITerminal>
    {
        /// <summary>
/// Persist pending changes in the repository to the underlying data store.
/// </summary>
/// <param name="cancellationToken">Token to cancel the save operation.</param>
Task SaveAsync(CancellationToken cancellationToken);

        /// <summary>
/// Retrieve selectable items for MMSI terminals, optionally filtered by port.
/// </summary>
/// <param name="portId">Optional port identifier to filter terminals; pass <c>null</c> to include terminals from all ports.</param>
/// <returns>A <see cref="List{SelectListItem}"/> containing items for matching MMSI terminals, or <c>null</c> if no terminals are available.</returns>
Task<List<SelectListItem>?> GetMMSITerminalsSelectList(int? portId, CancellationToken cancellationToken = default);
    }
}