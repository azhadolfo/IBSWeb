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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChartOfAccount>(coa =>
            {
                coa.HasIndex(c => c.AccountNumber).IsUnique();
                coa.HasIndex(c => c.AccountName);
            });
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Fuel> Fuels { get; set; }
        public DbSet<Lube> Lubes { get; set; }
        public DbSet<SafeDrop> SafeDrops { get; set; }

        #region --Master File Entities

        public DbSet<Company> Companies { get; set; }
        public DbSet<ChartOfAccount> ChartOfAccounts { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Station> Stations { get; set; }

        #endregion --Master File Entities
    }
}