using IBS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        IInventoryRepository Inventory { get; }

        IChartOfAccountRepository ChartOfAccount { get; }

        IPOSalesRepository PurchaseOrder { get; }

        Task SaveAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCustomersAsync();

        Task<List<SelectListItem>> GetProductsAsyncByCode();

        Task<List<SelectListItem>> GetProductsAsyncById();

        Task<List<SelectListItem>> GetStationAsyncByCode();

        Task<List<SelectListItem>> GetStationAsyncById();

        Task<List<SelectListItem>> GetChartOfAccountAsyncByNo();

        Task<List<SelectListItem>> GetChartOfAccountAsyncById();

    }
}