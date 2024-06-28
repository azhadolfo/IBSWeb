using IBS.Models;
using IBS.Models.Filpride;
using IBS.Models.MasterFile;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSnakeCaseNamingConvention();
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        public DbSet<LogMessage> LogMessages { get; set; }

        #region--MOBILITY

        #region--Sales Entity
        public DbSet<MobilityFuel> MobilityFuels { get; set; }
        public DbSet<MobilityLube> MobilityLubes { get; set; }
        public DbSet<MobilitySafeDrop> MobilitySafeDrops { get; set; }
        public DbSet<MobilitySalesHeader> MobilitySalesHeaders { get; set; }
        public DbSet<MobilitySalesDetail> MobilitySalesDetails { get; set; }
        public DbSet<MobilityPoSalesRaw> MobilityPoSalesRaw { get; set; }
        public DbSet<POSales> MobilityPOSales { get; set; }
        public DbSet<CsvFile> CsvFiles { get; set; }
        public DbSet<MobilityOffline> MobilityOfflines { get; set; }
        #endregion

        #region--Purchase Entity
        public DbSet<MobilityFuelPurchase> MobilityFuelPurchase { get; set; }
        public DbSet<MobilityFuelDelivery> MobilityFuelDeliveries { get; set; }
        public DbSet<LubeDelivery> MobilityLubeDeliveries { get; set; }
        public DbSet<MobilityLubePurchaseHeader> MobilityLubePurchaseHeaders { get; set; }
        public DbSet<MobilityLubePurchaseDetail> MobilityLubePurchaseDetails { get; set; }
        #endregion

        #endregion

        #region--FILPRIDE

        public DbSet<FilpridePurchaseOrder> FilpridePurchaseOrders { get; set; }

        public DbSet<FilprideReceivingReport> FilprideReceivingReports { get; set; }

        public DbSet<FilprideCustomerOrderSlip> FilprideCustomerOrderSlips { get; set; }

        public DbSet<FilprideDeliveryReceipt> FilprideDeliveryReceipts { get; set; }

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
        public DbSet<Hauler> Haulers { get; set; }

        #endregion --Master File Entities

        #region--Views Entity

        public DbSet<FuelSalesView> FuelSalesViews { get; set; }
        public DbSet<GeneralLedgerView> GeneralLedgerViews { get; set; }

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

            builder.Entity<Hauler>(h =>
            {
                h.HasIndex(h => h.HaulerCode).IsUnique();
                h.HasIndex(h => h.HaulerName).IsUnique();
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

            #endregion

            #region--Mobility

            #region-- Sales

            // Fuel
            builder.Entity<MobilityFuel>(f =>
            {
                f.HasIndex(f => f.xONAME);
                f.HasIndex(f => f.INV_DATE);
                f.HasIndex(f => f.xSITECODE);
                f.HasIndex(f => f.Particulars);
                f.HasIndex(f => f.Shift);
                f.HasIndex(f => f.ItemCode);
                f.HasIndex(f => f.xPUMP);
            });

            // Lube
            builder.Entity<MobilityLube>(l =>
            {
                l.HasIndex(l => l.Cashier);
                l.HasIndex(l => l.INV_DATE);
            });

            // SafeDrop
            builder.Entity<MobilitySafeDrop>(s =>
            {
                s.HasIndex(s => s.xONAME);
                s.HasIndex(s => s.INV_DATE);
            });

            // MobilitySalesHeader
            builder.Entity<MobilitySalesHeader>(s =>
            {
                s.HasIndex(s => s.SalesNo);
                s.HasIndex(s => s.Cashier);
                s.HasIndex(s => s.Shift);
                s.HasIndex(s => s.StationCode);
                s.HasIndex(s => s.Date);
            });

            // MobilitySalesDetail
            builder.Entity<MobilitySalesDetail>(s =>
            {
                s.HasIndex(s => s.SalesNo);
                s.HasIndex(s => s.StationCode);
            });

            builder.Entity<MobilitySalesDetail>()
                .HasOne(s => s.SalesHeader)
                .WithMany()
                .HasForeignKey(s => s.SalesHeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            // MobilityPOSales
            builder.Entity<MobilityPoSalesRaw>(po =>
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
            builder.Entity<MobilityFuelDelivery>(f =>
            {
                f.HasIndex(f => f.shiftrecid);
                f.HasIndex(f => f.stncode);
            });

            // MobilityFuelPurchase
            builder.Entity<MobilityFuelPurchase>(f =>
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

            // MobilityLubePurchaseHeader
            builder.Entity<MobilityLubePurchaseHeader>(lh => lh
                .HasIndex(lh => lh.LubePurchaseHeaderNo)
                .IsUnique());

            builder.Entity<MobilityLubePurchaseHeader>(lh =>
            {
                lh.HasIndex(lh => lh.LubePurchaseHeaderNo).IsUnique();
                lh.HasIndex(lh => lh.StationCode);
            });

            // MobilityLubePurchaseDetail
            builder.Entity<MobilityLubePurchaseDetail>()
                .HasOne(ld => ld.LubePurchaseHeader)
                .WithMany()
                .HasForeignKey(ld => ld.LubePurchaseHeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MobilityLubePurchaseDetail>(ld =>
            {
                ld.HasIndex(ld => ld.LubePurchaseHeaderNo);
                ld.HasIndex(lh => lh.ProductCode);
            });

            #endregion

            #endregion

            #region--Filpride

            builder.Entity<FilpridePurchaseOrder>(po =>
            {
                po.HasIndex(po => po.PurchaseOrderNo).IsUnique();
                po.HasIndex(po => po.Date);

                po.HasOne(po => po.Supplier)
                .WithMany()
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

                po.HasOne(po => po.Product)
                .WithMany()
                .HasForeignKey(po => po.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FilprideReceivingReport>(rr =>
            {
                rr.HasIndex(rr => rr.ReceivingReportNo).IsUnique();
                rr.HasIndex(rr => rr.Date);

                rr.HasOne(rr => rr.PurchaseOrder)
                .WithMany()
                .HasForeignKey(rr => rr.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

                rr.HasOne(rr => rr.Customer)
                .WithMany()
                .HasForeignKey(rr => rr.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FilprideCustomerOrderSlip>(cos =>
            {
                cos.HasIndex(cos => cos.CustomerOrderSlipNo).IsUnique();
                cos.HasIndex(cos => cos.Date);

                cos.HasOne(cos => cos.PurchaseOrder)
                .WithMany()
                .HasForeignKey(cos => cos.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

                cos.HasOne(cos => cos.Customer)
                .WithMany()
                .HasForeignKey(cos => cos.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FilprideDeliveryReceipt>(dr =>
            {
                dr.HasIndex(dr => dr.DeliveryReceiptNo).IsUnique();
                dr.HasIndex(dr => dr.InvoiceNo).IsUnique();
                dr.HasIndex(dr => dr.Date);

                dr.HasOne(dr => dr.CustomerOrderSlip)
                .WithMany()
                .HasForeignKey(dr => dr.CustomerOrderSlipId)
                .OnDelete(DeleteBehavior.Restrict);

                dr.HasOne(dr => dr.Hauler)
                .WithMany()
                .HasForeignKey(dr => dr.HaulerId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion
        }

        #endregion
    }
}