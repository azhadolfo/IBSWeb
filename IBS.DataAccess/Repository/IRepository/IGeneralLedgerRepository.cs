using IBS.Models;
using IBS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IGeneralLedgerRepository : IRepository<GeneralLedger>
    {
        byte[] ExportToExcel(IEnumerable<GeneralLedgerView> ledgers, DateOnly dateTo, DateOnly dateFrom, object accountNo, object accountName, string productCode);

        Task<IEnumerable<GeneralLedgerView>> GetLedgerViewByTransaction(DateOnly dateFrom, DateOnly dateTo, string stationCode, CancellationToken cancellationToken = default);

        Task<IEnumerable<GeneralLedgerView>> GetLedgerViewByJournal(DateOnly dateFrom, DateOnly dateTo, string stationCode, string journal, CancellationToken cancellationToken = default);

        Task<IEnumerable<GeneralLedgerView>> GetLedgerViewByAccountNo(DateOnly dateFrom, DateOnly dateTo, string stationCode, string accountNo, string productCode, CancellationToken cancellationToken = default);
    }
}
