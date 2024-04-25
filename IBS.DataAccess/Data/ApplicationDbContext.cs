using IBS.Models;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        #region--Sales Entity
        public DbSet<Fuel> Fuels { get; set; }
        public DbSet<Lube> Lubes { get; set; }
        public DbSet<SafeDrop> SafeDrops { get; set; }
        public DbSet<SalesHeader> SalesHeaders { get; set; }
        public DbSet<SalesDetail> SalesDetails { get; set; }
        public DbSet<PoSalesRaw> PoSalesRaw { get; set; }
        public DbSet<POSales> POSales { get; set; }
        public DbSet<CsvFile> CsvFiles { get; set; }
        #endregion

        #region--Purchase Entity
        public DbSet<FuelPurchase> FuelPurchase { get; set; }
        public DbSet<FuelDelivery> FuelDeliveries { get; set; }
        public DbSet<LubeDelivery> LubeDeliveries { get; set; }
        public DbSet<LubePurchaseHeader> LubePurchaseHeaders { get; set; }
        public DbSet<LubePurchaseDetail> LubePurchaseDetails { get; set; }
        #endregion

        #region --Inventory Entity
        public DbSet<Inventory> Inventories { get; set; }
        #endregion

        #region --Book Entity

        public DbSet<GeneralLedger> GeneralLedgers { get; set; }

        #endregion

        #region --Master File Entity

        public DbSet<Company> Companies { get; set; }
        public DbSet<ChartOfAccount> ChartOfAccounts { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Station> Stations { get; set; }

        #endregion --Master File Entities

        #region--Views Entity

        public DbSet<FuelSalesView> FuelSalesViews { get; set; }
        public DbSet<GeneralLedgerView> GeneralLedgerViews { get; set; }
        public DbSet<CoaSummaryReportView> CoaSummaryReportViews { get; set; }

        #endregion

        #region--Fluent API Implementation
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            #region-- Master File

            // Customer
            builder.Entity<Customer>(c =>
            {
                c.HasIndex(c => c.CustomerCode).IsUnique();
                c.HasIndex(c => c.CustomerName);
            });

            // Company
            builder.Entity<Company>(c =>
            {
                c.HasIndex(c => c.CompanyCode).IsUnique();
                c.HasIndex(c => c.CompanyName).IsUnique();
            });

            // Product
            builder.Entity<Product>(p =>
            {
                p.HasIndex(p => p.ProductCode).IsUnique();
                p.HasIndex(p => p.ProductName).IsUnique();
            });

            // Station
            builder.Entity<Station>(s =>
            {
                s.HasIndex(s => s.PosCode).IsUnique();
                s.HasIndex(s => s.StationCode).IsUnique();
                s.HasIndex(s => s.StationName).IsUnique();
            });

            // Supplier
            builder.Entity<Supplier>(s =>
            {
                s.HasIndex(s => s.SupplierCode).IsUnique();
                s.HasIndex(s => s.SupplierName).IsUnique();
            });

            #endregion

            #region-- Sales

            // Fuel
            builder.Entity<Fuel>(f =>
            {
                f.HasIndex(f => f.xONAME);
                f.HasIndex(f => f.INV_DATE);
                f.HasIndex(f => f.xSITECODE);
                f.HasIndex(f => f.IsProcessed);
                f.HasIndex(f => f.Particulars);
                f.HasIndex(f => f.Shift);
                f.HasIndex(f => f.ItemCode);
                f.HasIndex(f => f.xPUMP);
            });

            // Lube
            builder.Entity<Lube>(l =>
            {
                l.HasIndex(l => l.Cashier);
                l.HasIndex(l => l.INV_DATE);
            });

            // SafeDrop
            builder.Entity<SafeDrop>(s =>
            {
                s.HasIndex(s => s.xONAME);
                s.HasIndex(s => s.INV_DATE);
            });

            // SalesHeader
            builder.Entity<SalesHeader>(s =>
            {
                s.HasIndex(s => s.SalesNo).IsUnique();
                s.HasIndex(s => s.Cashier);
                s.HasIndex(s => s.Shift);
                s.HasIndex(s => s.StationCode);
                s.HasIndex(s => s.StationPosCode);
                s.HasIndex(s => s.Date);
            });

            // SalesDetail
            builder.Entity<SalesDetail>(s => s
                .HasIndex(s => s.SalesNo));

            builder.Entity<SalesDetail>()
                .HasOne(s => s.SalesHeader)
                .WithMany()
                .HasForeignKey(s => s.SalesHeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            // POSales
            builder.Entity<PoSalesRaw>(po =>
            {
                po.HasIndex(po => po.shiftrecid);
                po.HasIndex(po => po.stncode);
                po.HasIndex(po => po.tripticket);
            });

            builder.Entity<POSales>(po => po
                .HasIndex(po => po.POSalesNo)
                .IsUnique());

            #endregion

            #region--Purchase

            // FuelDelivery
            builder.Entity<FuelDelivery>(f =>
            {
                f.HasIndex(f => f.shiftrecid);
                f.HasIndex(f => f.stncode);
            });

            // FuelPurchase
            builder.Entity<FuelPurchase>(f =>
            {
                f.HasIndex(f => f.FuelPurchaseNo).IsUnique();
                f.HasIndex(f => f.StationCode);
                f.HasIndex(f => f.ProductCode);
            });

            // LubeDelivery
            builder.Entity<LubeDelivery>(l =>
            {
                l.HasIndex(l => l.shiftrecid);
                l.HasIndex(l => l.stncode);
                l.HasIndex(l => l.dtllink);
            });

            // LubePurchaseHeader
            builder.Entity<LubePurchaseHeader>(lh => lh
                .HasIndex(lh => lh.LubePurchaseHeaderNo)
                .IsUnique());

            builder.Entity<LubePurchaseHeader>(lh =>
            {
                lh.HasIndex(lh => lh.LubePurchaseHeaderNo).IsUnique();
                lh.HasIndex(lh => lh.StationCode);
            });

            // LubePurchaseDetail
            builder.Entity<LubePurchaseDetail>()
                .HasOne(ld => ld.LubePurchaseHeader)
                .WithMany()
                .HasForeignKey(ld => ld.LubePurchaseHeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LubePurchaseDetail>(ld =>
            {
                ld.HasIndex(ld => ld.LubePurchaseHeaderNo);
                ld.HasIndex(lh => lh.ProductCode);
            });

            #endregion

            #region--Chart Of Account
            builder.Entity<ChartOfAccount>(coa =>
            {
                coa.HasIndex(coa => coa.AccountNumber).IsUnique();
                coa.HasIndex(coa => coa.AccountName);
            });
            #endregion

            #region--General Ledger
            builder.Entity<GeneralLedger>(g =>
                {
                    g.HasIndex(g => g.TransactionDate);
                    g.HasIndex(g => g.Reference);
                    g.HasIndex(g => g.AccountNumber);
                    g.HasIndex(g => g.AccountTitle);
                    g.HasIndex(g => g.ProductCode);
                    g.HasIndex(g => g.JournalReference);
                    g.HasIndex(g => g.StationCode);
                    g.HasIndex(g => g.SupplierCode);
                    g.HasIndex(g => g.CustomerCode);
                });
            #endregion

            #region--Inventory
            builder.Entity<Inventory>(i =>
               {
                   i.HasIndex(i => i.ProductCode);
                   i.HasIndex(i => i.StationCode);
                   i.HasIndex(i => i.TransactionNo);
               });
            #endregion

            #region--Views

            builder.Entity<FuelSalesView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("fuel_sales_view");
            });

            builder.Entity<GeneralLedgerView>(entity =>
            {
                entity.ToView("general_ledger_view");
            });

            builder.Entity<CoaSummaryReportView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("coa_summary_report_view");
            });

            #endregion
        }
        #endregion
    }
}