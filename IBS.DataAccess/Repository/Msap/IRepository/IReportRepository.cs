using IBS.Models.Msap;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IMsapReportRepository
    {
        Task<List<DispatchTicket>> GetDispatchReportData(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default, bool filterByBillingDate = false);
    }
}


