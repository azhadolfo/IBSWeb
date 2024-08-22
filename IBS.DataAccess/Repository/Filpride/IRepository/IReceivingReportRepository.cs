using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IReceivingReportRepository : IRepository<FilprideReceivingReport>
    {
        Task<string> GenerateCodeAsync(string company, CancellationToken cancellationToken = default);

        Task UpdatePOAsync(int id, decimal quantityReceived, CancellationToken cancellationToken = default);

        Task<int> RemoveQuantityReceived(int id, decimal quantityReceived, CancellationToken cancellationToken = default);

        Task<DateOnly> ComputeDueDateAsync(int poId, DateOnly rrDate, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetReceivingReportListAsync(string[] rrNos, string company, CancellationToken cancellationToken = default);
    }
}