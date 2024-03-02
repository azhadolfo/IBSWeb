using IBS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork
    {
        ICustomerRepository Customer { get; }

        ICategoryRepository Category { get; }

        IProductRepository Product { get; }

        ICompanyRepository Company { get; }

        ISalesHeaderRepository SalesHeader { get; }

        ISalesDetailRepository SalesDetail { get; }

        IStationRepository Station { get; }

        IGeneralLedgerRepository GeneralLedger { get; }

        IFuelPurchaseRepository FuelPurchase { get; }

        ILubePurchaseHeaderRepository LubePurchaseHeader { get; }

        ILubePurchaseDetailRepository LubePurchaseDetail { get; }

        ISupplierRepository Supplier { get; }

        Task SaveAsync(CancellationToken cancellationToken = default);

    }
}