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
        public DbSet<Fuel> Fuels { get; set; }
        public DbSet<Lube> Lubes { get; set; }
        public DbSet<SafeDrop> SafeDrops { get; set; }
        public DbSet<SalesHeader> SalesHeaders { get; set; }
        public DbSet<SalesDetail> SalesDetails { get; set; }

        #region --Master File Entities

        public DbSet<Company> Companies { get; set; }
        public DbSet<ChartOfAccount> ChartOfAccounts { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Station> Stations { get; set; }

        #endregion --Master File Entities

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ChartOfAccount>(coa =>
            {
                coa.HasIndex(coa => coa.AccountNumber).IsUnique();
                coa.HasIndex(coa => coa.AccountName);
            });

            builder.Entity<Customer>(c =>
            {
                c.HasIndex(c => c.CustomerId).IsUnique();
                c.HasIndex(c => c.CustomerCode).IsUnique();
                c.HasIndex(c => c.CustomerName);
            });

            builder.Entity<Company>(c =>
            {
                c.HasIndex(c => c.CompanyId).IsUnique();
                c.HasIndex(c => c.CompanyCode).IsUnique();
                c.HasIndex(c => c.CompanyName).IsUnique();
            });

            builder.Entity<Product>(p =>
            {
                p.HasIndex(p => p.ProductId).IsUnique();
                p.HasIndex(p => p.ProductCode).IsUnique();
                p.HasIndex(p => p.ProductName).IsUnique();
            });

            builder.Entity<Fuel>(f =>
            {
                f.HasIndex(f => f.xONAME);
            });

            builder.Entity<Lube>(l =>
            {
                l.HasIndex(l => l.Cashier);
            });

            builder.Entity<SafeDrop>(s =>
            {
                s.HasIndex(s => s.xONAME);
            });
        }
    }
}