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
        Task ComputeSalesPerCashier(DateOnly yesterday, CancellationToken cancellationToken = default);

        Task PostAsync(int id, CancellationToken cancellationToken = default);

        Task UpdateAsync(SalesHeader model, CancellationToken cancellationToken = default);
    }
}