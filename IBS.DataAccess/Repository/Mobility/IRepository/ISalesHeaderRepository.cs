using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface ISalesHeaderRepository : IRepository<MobilitySalesHeader>, ILogReportRepository
    {
        Task PostAsync(string id, string postedBy, string stationCode, CancellationToken cancellationToken = default);

        Task UpdateAsync(MobilitySalesHeader model, CancellationToken cancellationToken = default);

        IEnumerable<dynamic> GetSalesHeaderJoin(IEnumerable<MobilitySalesHeader> salesHeaders, CancellationToken cancellationToken = default);

        Task ComputeSalesPerCashier(bool hasPoSales, CancellationToken cancellationToken = default);

        Task<(int fuelCount, bool hasPoSales)> ProcessFuelGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default);

        Task<(int lubeCount, bool hasPoSales)> ProcessLubeGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default);

        Task<int> ProcessSafeDropGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetPostedDsrList(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetUnpostedDsrList(string stationCode, CancellationToken cancellationToken = default);

        Task ProcessCustomerInvoicing(CustomerInvoicingViewModel viewModel, CancellationToken cancellationToken);

        Task<int> ProcessFmsFuelSalesGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default);

        Task<int> ProcessFmsLubeSalesGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default);

        Task ProcessFmsCalibrationGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default);

        Task ProcessFmsCashierShiftGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default);


    }
}
