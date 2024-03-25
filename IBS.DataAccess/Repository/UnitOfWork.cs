﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _db;
        public ICustomerRepository Customer { get; private set; }
        public ICategoryRepository Category { get; private set; }
        public IProductRepository Product { get; private set; }
        public ICompanyRepository Company { get; private set; }
        public ISalesHeaderRepository SalesHeader { get; private set; }
        public ISalesDetailRepository SalesDetail { get; private set; }
        public IStationRepository Station { get; private set; }
        public IGeneralLedgerRepository GeneralLedger { get; private set; }
        public IFuelPurchaseRepository FuelPurchase { get; private set; }
        public ILubePurchaseHeaderRepository LubePurchaseHeader { get; private set; }
        public ILubePurchaseDetailRepository LubePurchaseDetail { get; private set; }
        public ISupplierRepository Supplier { get; private set; }
        public IInventoryRepository Inventory { get; private set; }
        public IChartOfAccountRepository ChartOfAccount { get; private set; }
        public IPOSalesRepository PurchaseOrder { get; private set; }

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            Customer = new CustomerRepository(_db);
            Category = new CategoryRepository(_db);
            Product = new ProductRepository(_db);
            Company = new CompanyRepository(_db);
            SalesHeader = new SalesHeaderRepository(_db);
            SalesDetail = new SalesDetailRepository(_db);
            Station = new StationRepository(_db);
            GeneralLedger = new GeneralLedgerRepository(_db);
            FuelPurchase = new FuelPurchaseRepository(_db);
            LubePurchaseHeader = new LubePurchaseHeaderRepository(_db);
            LubePurchaseDetail = new LubePurchaseDetailRepository(_db);
            Supplier = new SupplierRepository(_db);
            Inventory = new InventoryRepository(_db);
            ChartOfAccount = new ChartOfAccountRepository(_db);
            PurchaseOrder = new POSalesRepository(_db);
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCustomersAsync()
        {
            return await _db.Customers
                .OrderBy(c => c.CustomerId)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName
                })
                .ToListAsync();
        }

        public async Task<List<SelectListItem>> GetProductsAsyncByCode()
        {
            return await _db.Products
                .OrderBy(p => p.ProductId)
                .Select(p => new SelectListItem
                {
                    Value = p.ProductCode,
                    Text = p.ProductCode + " " + p.ProductName
                })
                .ToListAsync();
        }

        public async Task<List<SelectListItem>> GetProductsAsyncById()
        {
            return await _db.Products
                .OrderBy(p => p.ProductId)
                .Select(p => new SelectListItem
                {
                    Value = p.ProductId.ToString(),
                    Text = p.ProductCode + " " + p.ProductName
                })
                .ToListAsync();
        }

        public async Task<List<SelectListItem>> GetStationAsyncByCode()
        {
            return await _db.Stations
                .OrderBy(s => s.StationId)
                .Select(s => new SelectListItem
                {
                    Value = s.StationCode,
                    Text = s.StationCode + " " + s.StationName
                })
                .ToListAsync();
        }

        public async Task<List<SelectListItem>> GetStationAsyncById()
        {
            return await _db.Stations
                .OrderBy(s => s.StationId)
                .Select(s => new SelectListItem
                {
                    Value = s.StationId.ToString(),
                    Text = s.StationCode + " " + s.StationName
                })
                .ToListAsync();
        }

        public async Task<List<SelectListItem>> GetChartOfAccountAsyncById()
        {
            return await _db.ChartOfAccounts
                .OrderBy(c => c.AccountId)
                .Where(c => c.Level == 3)
                .Select(c => new SelectListItem
                {
                    Value = c.AccountId.ToString(),
                    Text = c.AccountNumber + " " + c.AccountName
                })
                .ToListAsync();
        }

        public async Task<List<SelectListItem>> GetChartOfAccountAsyncByNo()
        {
            return await _db.ChartOfAccounts
                .OrderBy(c => c.AccountNumber)
                .Where(c => c.Level == 3)
                .Select(c => new SelectListItem
                {
                    Value = c.AccountNumber,
                    Text = c.AccountNumber + " " + c.AccountName
                })
                .ToListAsync();
        }

    }
}