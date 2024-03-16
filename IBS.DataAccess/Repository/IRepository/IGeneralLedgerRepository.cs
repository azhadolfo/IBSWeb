using IBS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IGeneralLedgerRepository : IRepository<GeneralLedger>
    {
        byte[] ExportToExcel(IEnumerable<GeneralLedger> ledgers, DateOnly dateTo, DateOnly dateFrom, object accountNo, object accountName, string productCode);
    }
}
