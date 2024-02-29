﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
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
        public IFuelPurchaseRepository FuelDelivery { get; private set; }
        public ILubePurchaseRepository LubeDelivery { get; private set; }

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
            FuelDelivery = new FuelPurchaseRepository(_db);
            LubeDelivery = new LubePurchaseRepository(_db);
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}