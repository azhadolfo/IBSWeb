using IBS.Models;
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

        public DbSet<Category> Categories { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        #region--Sales Entity
        public DbSet<Fuel> Fuels { get; set; }
        public DbSet<Lube> Lubes { get; set; }
        public DbSet<SafeDrop> SafeDrops { get; set; }
        public DbSet<SalesHeader> SalesHeaders { get; set; }
        public DbSet<SalesDetail> SalesDetails { get; set; }
        public DbSet<PoSalesRaw> PoSalesRaw { get; set; }
        public DbSet<POSales> POSales { get; set; }
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
            builder.Entity<FuelPurchase>(f => f
                .HasIndex(f => f.FuelPurchaseNo)
                .IsUnique());

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

            // LubePurchaseDetail
            builder.Entity<LubePurchaseDetail>()
                .HasOne(ld => ld.LubePurchaseHeader)
                .WithMany()
                .HasForeignKey(ld => ld.LubePurchaseHeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LubePurchaseDetail>(ld => ld
                .HasIndex(ld => ld.LubePurchaseHeaderNo));

            #endregion

            // ChartOfAccount
            builder.Entity<ChartOfAccount>(coa =>
            {
                coa.HasIndex(coa => coa.AccountNumber).IsUnique();
                coa.HasIndex(coa => coa.AccountName);
            });

            // GeneralLedger
            builder.Entity<GeneralLedger>(g =>
            {
                g.HasIndex(g => g.TransactionDate);
                g.HasIndex(g => g.Reference);
                g.HasIndex(g => g.AccountNumber);
                g.HasIndex(g => g.AccountTitle);
                g.HasIndex(g => g.ProductCode);
                g.HasIndex(g => g.JournalReference);
            });

            // Inventory
            builder.Entity<Inventory>(i =>
            {
                i.HasIndex(i => i.ProductCode);
                i.HasIndex(i => i.StationCode);
                i.HasIndex(i => i.TransactionNo);
            });
        }
        #endregion
    }
}