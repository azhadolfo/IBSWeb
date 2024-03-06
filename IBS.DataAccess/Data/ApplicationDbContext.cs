﻿using IBS.Models;
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

            // ChartOfAccount
            builder.Entity<ChartOfAccount>(coa =>
            {
                coa.HasIndex(coa => coa.AccountNumber).IsUnique();
                coa.HasIndex(coa => coa.AccountName);
            });

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
            builder.Entity<SalesDetail>(s =>
            {
                s.HasIndex(s => s.SalesNo);
            });

            builder.Entity<SalesDetail>()
                .HasOne(s => s.SalesHeader)
                .WithMany()
                .HasForeignKey(s => s.SalesHeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            // LubePurchaseDetail
            builder.Entity<LubePurchaseDetail>()
                .HasOne(l => l.LubeDeliveryHeader)
                .WithMany()
                .HasForeignKey(l => l.LubeDeliveryHeaderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
        #endregion
    }
}