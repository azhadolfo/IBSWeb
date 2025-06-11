using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IMMSIReportRepository
    {
        Task<List<MMSIDispatchTicket>> GetSalesReport(DateOnly DateFrom, DateOnly DateTo, CancellationToken cancellationToken = default);
    }
}
