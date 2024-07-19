using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IGeneralLedgerRepository : IRepository<MobilityGeneralLedger>
    {
        byte[] ExportToExcel(IEnumerable<GeneralLedgerView> ledgers, DateOnly dateTo, DateOnly dateFrom, object accountNo, object accountName, string productCode);

        Task<IEnumerable<GeneralLedgerView>> GetLedgerViewByTransaction(DateOnly dateFrom, DateOnly dateTo, string stationCode, CancellationToken cancellationToken = default);

        Task<IEnumerable<GeneralLedgerView>> GetLedgerViewByJournal(DateOnly dateFrom, DateOnly dateTo, string stationCode, string journal, CancellationToken cancellationToken = default);

        Task<IEnumerable<GeneralLedgerView>> GetLedgerViewByAccountNo(DateOnly dateFrom, DateOnly dateTo, string stationCode, string accountNo, string productCode, CancellationToken cancellationToken = default);
    }
}