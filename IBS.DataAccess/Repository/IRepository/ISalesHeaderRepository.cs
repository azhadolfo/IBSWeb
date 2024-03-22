using IBS.Models;
using IBS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface ISalesHeaderRepository : IRepository<SalesHeader>
    {
        Task ComputeSalesPerCashier(DateOnly yesterday, bool HasPoSales, CancellationToken cancellationToken = default);

        Task PostAsync(string id, CancellationToken cancellationToken = default);

        Task UpdateAsync(SalesVM model, double[] closing, double[] opening, CancellationToken cancellationToken = default);
    }
}