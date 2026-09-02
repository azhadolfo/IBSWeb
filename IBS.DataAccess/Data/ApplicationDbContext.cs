using IBS.Models;
using IBS.Models.Bienes;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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

        public DbSet<AppSetting> AppSettings { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<UserNotification> UserNotifications { get; set; }

        public DbSet<HubConnection> HubConnections { get; set; }

        public DbSet<PostedPeriod> PostedPeriods { get; set; }

        public DbSet<DepartmentAccess> DepartmentAccesses { get; set; }

        public DbSet<LockedPeriodAdjustment> LockedPeriodAdjustments { get; set; }

        #region--FILPRIDE

        public DbSet<FilprideCustomerOrderSlip> FilprideCustomerOrderSlips { get; set; }

        public DbSet<FilprideDeliveryReceipt> FilprideDeliveryReceipts { get; set; }

        public DbSet<FilprideDeliveryReceiptDetail> FilprideDeliveryReceiptDetails { get; set; }

        public DbSet<FilprideFreight> FilprideFreights { get; set; }

        public DbSet<FilprideAuthorityToLoad> FilprideAuthorityToLoads { get; set; }

        public DbSet<FilprideCOSAppointedSupplier> FilprideCOSAppointedSuppliers { get; set; }

        public DbSet<FilpridePOActualPrice> FilpridePOActualPrices { get; set; }

        public DbSet<FilprideCustomerBranch> FilprideCustomerBranches { get; set; }

        public DbSet<FilprideBookAtlDetail> FilprideBookAtlDetails { get; set; }

        public DbSet<FilprideMonthlyNibit> FilprideMonthlyNibits { get; set; }

        public DbSet<FilprideGLPeriodBalance> FilprideGlPeriodBalances { get; set; }

        public DbSet<FilprideGLSubAccountBalance> FilprideGlSubAccountBalances { get; set; }

        public DbSet<FilprideProvisionalReceipt> FilprideProvisionalReceipts { get; set; }

        #region--Master File

        public DbSet<FilprideCustomer> FilprideCustomers { get; set; }

        public DbSet<FilprideSupplier> FilprideSuppliers { get; set; }

        public DbSet<FilpridePickUpPoint> FilpridePickUpPoints { get; set; }

        public DbSet<FilprideTerms> FilprideTerms { get; set; }

        #endregion

        #endregion

        #region --BIENES

        public DbSet<BienesPlacement> BienesPlacements { get; set; }

        #endregion

        #region --Master File Entity

        public DbSet<Company> Companies { get; set; }

        public DbSet<FilprideChartOfAccount> FilprideChartOfAccounts { get; set; }
        public DbSet<Product> Products { get; set; }

        #endregion --Master File Entities

        #region AAS Migration

        #region Accounts Receivable
        public DbSet<FilprideBankAccount> FilprideBankAccounts { get; set; }
        public DbSet<FilprideService> FilprideServices { get; set; }
        public DbSet<FilprideCollectionReceipt> FilprideCollectionReceipts { get; set; }
        public DbSet<FilprideCreditMemo> FilprideCreditMemos { get; set; }
        public DbSet<FilprideDebitMemo> FilprideDebitMemos { get; set; }
        public DbSet<FilprideSalesInvoice> FilprideSalesInvoices { get; set; }
        public DbSet<FilprideServiceInvoice> FilprideServiceInvoices { get; set; }
        public DbSet<FilprideOffsettings> FilprideOffsettings { get; set; }
        public DbSet<FilprideCollectionReceiptDetail> FilprideCollectionReceiptDetails { get; set; }
        #endregion

        #region Accounts Payable

        public DbSet<FilprideCheckVoucherHeader> FilprideCheckVoucherHeaders { get; set; }
        public DbSet<FilprideCheckVoucherDetail> FilprideCheckVoucherDetails { get; set; }
        public DbSet<FilprideJournalVoucherHeader> FilprideJournalVoucherHeaders { get; set; }
        public DbSet<FilprideJournalVoucherDetail> FilprideJournalVoucherDetails { get; set; }
        public DbSet<FilpridePurchaseOrder> FilpridePurchaseOrders { get; set; }
        public DbSet<FilprideReceivingReport> FilprideReceivingReports { get; set; }

        public DbSet<FilprideMultipleCheckVoucherPayment> FilprideMultipleCheckVoucherPayments { get; set; }

        public DbSet<FilprideCVTradePayment> FilprideCVTradePayments { get; set; }

        public DbSet<JvAmortizationSetting> JvAmortizationSettings { get; set; }

        #endregion

        #region Books

        public DbSet<FilprideGeneralLedgerBook> FilprideGeneralLedgerBooks { get; set; }
        public DbSet<FilprideInventory> FilprideInventories { get; set; }
        public DbSet<FilprideAuditTrail> FilprideAuditTrails { get; set; }

        #endregion

        #endregion

        #region--Fluent API Implementation

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            #region-- Master File

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

            // BankAccount
            builder.Entity<FilprideBankAccount>()
                .Property(x => x.IsActive)
                .HasDefaultValue(true);

            #endregion

            #region--Chart Of Account
            builder.Entity<FilprideChartOfAccount>(coa =>
            {
                coa.HasIndex(coa => coa.AccountNumber).IsUnique();
                coa.HasIndex(coa => coa.AccountName);
                coa.HasQueryFilter(coa => !coa.IsHidden);
            });
            #endregion

            #region--Filpride

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

                cos.HasOne(cos => cos.Commissionee)
                    .WithMany()
                    .HasForeignKey(cos => cos.CommissioneeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FilprideDeliveryReceipt>(dr =>
            {
                dr.HasIndex(dr => dr.DeliveryReceiptNo).IsUnique();

                dr.HasIndex(dr => dr.Date);

                dr.HasOne(dr => dr.CustomerOrderSlip)
                    .WithMany(cos => cos.DeliveryReceipts)
                    .HasForeignKey(dr => dr.CustomerOrderSlipId)
                    .OnDelete(DeleteBehavior.Restrict);

                dr.HasOne(dr => dr.Commissionee)
                    .WithMany()
                    .HasForeignKey(dr => dr.CommissioneeId)
                    .OnDelete(DeleteBehavior.Restrict);

                dr.HasOne(dr => dr.Customer)
                    .WithMany()
                    .HasForeignKey(dr => dr.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                dr.HasOne(dr => dr.Hauler)
                    .WithMany()
                    .HasForeignKey(dr => dr.HaulerId)
                    .OnDelete(DeleteBehavior.Restrict);

                dr.HasOne(dr => dr.AuthorityToLoad)
                    .WithMany()
                    .HasForeignKey(dr => dr.AuthorityToLoadId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FilprideDeliveryReceiptDetail>(d =>
            {
                d.HasOne(d => d.DeliveryReceipt)
                    .WithMany(dr => dr.Details)
                    .HasForeignKey(d => d.DeliveryReceiptId)
                    .OnDelete(DeleteBehavior.Cascade);

                d.HasOne(d => d.CustomerOrderSlip)
                    .WithMany()
                    .HasForeignKey(d => d.CustomerOrderSlipId)
                    .OnDelete(DeleteBehavior.Restrict);

                d.HasOne(d => d.PurchaseOrder)
                    .WithMany()
                    .HasForeignKey(d => d.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                d.HasOne(d => d.AuthorityToLoad)
                    .WithMany()
                    .HasForeignKey(d => d.AuthorityToLoadId)
                    .OnDelete(DeleteBehavior.Restrict);

                d.HasOne(d => d.Product)
                    .WithMany()
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FilprideCOSAppointedSupplier>(a =>
            {
                a.HasOne(a => a.PurchaseOrder)
                    .WithMany()
                    .HasForeignKey(a => a.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                a.HasOne(a => a.CustomerOrderSlip)
                    .WithMany(cos => cos.AppointedSuppliers)
                    .HasForeignKey(a => a.CustomerOrderSlipId)
                    .OnDelete(DeleteBehavior.Restrict);

                a.HasOne(a => a.Supplier)
                    .WithMany()
                    .HasForeignKey(a => a.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FilpridePOActualPrice>(p =>
            {
                p.HasOne(p => p.PurchaseOrder)
                    .WithMany(po => po.ActualPrices)
                    .HasForeignKey(p => p.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FilprideCustomerBranch>(b =>
            {
                b.HasOne(b => b.Customer)
                    .WithMany(c => c.Branches)
                    .HasForeignKey(b => b.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FilprideBookAtlDetail>(b =>
            {
                b.HasOne(b => b.Header)
                    .WithMany(b => b.Details)
                    .HasForeignKey(b => b.AuthorityToLoadId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(b => b.CustomerOrderSlip)
                    .WithMany()
                    .HasForeignKey(b => b.CustomerOrderSlipId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(b => b.AppointedSupplier)
                    .WithMany()
                    .HasForeignKey(b => b.AppointedId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(b => b.Supplier)
                    .WithMany()
                    .HasForeignKey(b => b.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FilprideAuthorityToLoad>(b =>
            {
                b.HasOne(b => b.Supplier)
                    .WithMany()
                    .HasForeignKey(b => b.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(b => b.AuthorityToLoadNo).IsUnique();
            });

            builder.Entity<FilprideMonthlyNibit>(n =>
            {
                n.HasQueryFilter(x => x.IsValid);
                n.HasIndex(n => n.Month);
                n.HasIndex(n => n.Year);
            });

            #region-- Master File

            // FilprideCustomer
            builder.Entity<FilprideCustomer>(c =>
            {
                c.HasIndex(c => c.CustomerCode);
                c.HasIndex(c => c.CustomerName);
            });

            // FilprideSupplier
            builder.Entity<FilprideSupplier>(s =>
            {
                s.HasIndex(s => s.SupplierCode);
                s.HasIndex(s => s.SupplierName);
            });

            // FilpridePickUpPoint
            builder.Entity<FilpridePickUpPoint>(p =>
            {
                p.HasOne(p => p.Supplier)
                    .WithMany()
                    .HasForeignKey(p => p.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FilprideGLPeriodBalance>(b =>
            {
                b.HasQueryFilter(x => x.IsValid);

                b.HasOne(a => a.Account)
                    .WithMany(c => c.Balances)
                    .HasForeignKey(a => a.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FilprideGLSubAccountBalance>(b =>
            {
                b.HasQueryFilter(x => x.IsValid);

                b.HasOne(a => a.Account)
                    .WithMany(c => c.SubAccountBalances)
                    .HasForeignKey(a => a.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            #region -- Accounts Receivable --

            #region -- Sales Invoice --

            builder.Entity<FilprideSalesInvoice>(si =>
            {
                si.HasOne(si => si.Product)
                .WithMany()
                .HasForeignKey(si => si.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

                si.HasOne(si => si.Customer)
                .WithMany()
                .HasForeignKey(si => si.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

                si.HasOne(si => si.PurchaseOrder)
                .WithMany()
                .HasForeignKey(si => si.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

                si.HasIndex(si => si.SalesInvoiceNo).IsUnique();
            });

            #endregion -- Sales Invoice --

            #region -- Service Invoice --

            builder.Entity<FilprideServiceInvoice>(sv =>
            {
                sv.HasOne(sv => sv.Customer)
                .WithMany()
                .HasForeignKey(sv => sv.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

                sv.HasOne(sv => sv.Service)
                .WithMany()
                .HasForeignKey(sv => sv.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

                sv.HasOne(sv => sv.DeliveryReceipt)
                    .WithMany()
                    .HasForeignKey(sv => sv.DeliveryReceiptId)
                    .OnDelete(DeleteBehavior.Restrict);

                sv.HasIndex(sv => sv.ServiceInvoiceNo).IsUnique();
            });

            #endregion -- Service Invoice --

            #region -- Collection Receipt --

            builder.Entity<FilprideCollectionReceipt>(cr =>
            {
                cr.HasOne(cr => cr.SalesInvoice)
                .WithMany()
                .HasForeignKey(cr => cr.SalesInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

                cr.HasOne(cr => cr.ServiceInvoice)
                .WithMany()
                .HasForeignKey(cr => cr.ServiceInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

                cr.HasOne(cr => cr.Customer)
                .WithMany()
                .HasForeignKey(cr => cr.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

                cr.HasOne(cr => cr.BankAccount)
                    .WithMany()
                    .HasForeignKey(cr => cr.BankId)
                    .OnDelete(DeleteBehavior.Restrict);

                cr.HasIndex(cr => cr.CollectionReceiptNo).IsUnique();
            });

            builder.Entity<FilprideCollectionReceiptDetail>(crd =>
            {
                crd.HasOne(d => d.FilprideCollectionReceipt)
                    .WithMany(d => d.ReceiptDetails)
                    .HasForeignKey(d => d.CollectionReceiptId)
                    .OnDelete(DeleteBehavior.Restrict);

                crd.HasIndex(d => d.InvoiceNo);

                crd.HasIndex(d => d.CollectionReceiptNo);
            });

            builder.Entity<FilprideProvisionalReceipt>(pr =>
            {
                pr.HasOne(p => p.Supplier)
                    .WithMany()
                    .HasForeignKey(p => p.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);

                pr.HasOne(p => p.BankAccount)
                    .WithMany()
                    .HasForeignKey(p => p.BankId)
                    .OnDelete(DeleteBehavior.Restrict);

                pr.HasIndex(d => d.SeriesNumber).IsUnique();
            });

            #endregion -- Collection Receipt --

            #region -- Debit Memo --

            builder.Entity<FilprideDebitMemo>(dm =>
            {
                dm.HasOne(dm => dm.SalesInvoice)
                .WithMany()
                .HasForeignKey(dm => dm.SalesInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

                dm.HasOne(dm => dm.ServiceInvoice)
                .WithMany()
                .HasForeignKey(dm => dm.ServiceInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

                dm.HasIndex(dm => dm.DebitMemoNo).IsUnique();
            });

            #endregion -- Debit Memo --

            #region -- Credit Memo --

            builder.Entity<FilprideCreditMemo>(cm =>
            {
                cm.HasOne(cm => cm.SalesInvoice)
                .WithMany()
                .HasForeignKey(cm => cm.SalesInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

                cm.HasOne(cm => cm.ServiceInvoice)
                .WithMany()
                .HasForeignKey(cm => cm.ServiceInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

                cm.HasIndex(cm => cm.CreditMemoNo).IsUnique();
            });

            #endregion -- Credit Memo --

            #endregion -- Accounts Receivable --

            #region -- Accounts Payable --

            #region -- Purchase Order --

            builder.Entity<FilpridePurchaseOrder>(po =>
            {
                po.HasOne(po => po.Supplier)
                .WithMany()
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

                po.HasOne(po => po.Product)
                .WithMany()
                .HasForeignKey(po => po.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

                po.HasOne(po => po.Customer)
                .WithMany()
                .HasForeignKey(po => po.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

                po.HasOne(po => po.PickUpPoint)
                    .WithMany()
                    .HasForeignKey(po => po.PickUpPointId)
                    .OnDelete(DeleteBehavior.Restrict);

                po.HasIndex(po => po.PurchaseOrderNo).IsUnique();
            });

            #endregion -- Purchase Order --

            #region -- Receving Report --

            builder.Entity<FilprideReceivingReport>(rr =>
            {
                rr.HasOne(rr => rr.PurchaseOrder)
                .WithMany(po => po.ReceivingReports)
                .HasForeignKey(rr => rr.POId)
                .OnDelete(DeleteBehavior.Restrict);

                rr.HasOne(rr => rr.DeliveryReceipt)
                .WithMany()
                .HasForeignKey(rr => rr.DeliveryReceiptId)
                .OnDelete(DeleteBehavior.Restrict);

                rr.HasIndex(rr => rr.ReceivingReportNo).IsUnique();
            });

            #endregion -- Receving Report --

            #region -- Check Voucher Header --

            builder.Entity<FilprideCheckVoucherHeader>(cv =>
            {
                cv.HasOne(cv => cv.Supplier)
                .WithMany()
                .HasForeignKey(cv => cv.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

                cv.HasOne(cv => cv.BankAccount)
                .WithMany()
                .HasForeignKey(cv => cv.BankId)
                .OnDelete(DeleteBehavior.Restrict);

                cv.HasIndex(cv => cv.CheckVoucherHeaderNo).IsUnique();
            });

            #endregion -- Check Voucher --

            #region -- Check Voucher Details --

            builder.Entity<FilprideCheckVoucherDetail>(cv =>
            {
                cv.HasOne(cv => cv.CheckVoucherHeader)
                    .WithMany(cv => cv.Details)
                    .HasForeignKey(cv => cv.CheckVoucherHeaderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Check Voucher Details --

            #region -- Check Voucher Trade Payment --

            builder.Entity<FilprideCVTradePayment>(cv =>
            {
                cv.HasOne(cv => cv.CV)
                    .WithMany()
                    .HasForeignKey(cv => cv.CheckVoucherId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Check Voucher Trade Payment --

            #region -- Journal Voucher Header --

            builder.Entity<FilprideJournalVoucherHeader>(jv =>
            {
                jv.HasOne(jv => jv.CheckVoucherHeader)
                .WithMany()
                .HasForeignKey(jv => jv.CVId)
                .OnDelete(DeleteBehavior.Restrict);

                jv.HasIndex(jv => jv.JournalVoucherHeaderNo).IsUnique();
            });

            #endregion -- Check Voucher --

            #region -- Journal Voucher Details --

            builder.Entity<FilprideJournalVoucherDetail>(jv =>
            {
                jv.HasOne(jv => jv.JournalVoucherHeader)
                    .WithMany(jv => jv.Details)
                    .HasForeignKey(jv => jv.JournalVoucherHeaderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            #region

            builder.Entity<JvAmortizationSetting>(jv =>
            {
                jv.HasOne(jv => jv.JvHeader)
                    .WithMany()
                    .HasForeignKey(jv => jv.JvId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            #endregion

            #region -- Multiple Check Voucher Payment --

            builder.Entity<FilprideMultipleCheckVoucherPayment>(mcvp =>
            {
                mcvp.HasOne(mcvp => mcvp.CheckVoucherHeaderPayment)
                    .WithMany()
                    .HasForeignKey(mcvp => mcvp.CheckVoucherHeaderPaymentId)
                    .OnDelete(DeleteBehavior.Restrict);

                mcvp.HasOne(mcvp => mcvp.CheckVoucherHeaderInvoice)
                    .WithMany()
                    .HasForeignKey(mcvp => mcvp.CheckVoucherHeaderInvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Multiple Check Voucher Payment --

            #endregion -- Accounts Payable --

            #region-- Books --

            builder.Entity<FilprideGeneralLedgerBook>(gl =>
            {
                gl.HasOne(gl => gl.Account)
                    .WithMany()
                    .HasForeignKey(gl => gl.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            #endregion

            #region--AppSettings

            builder.Entity<AppSetting>(a =>
            {
                a.HasIndex(a => a.SettingKey).IsUnique();
            });

            #endregion

            #region --Bienes

            builder.Entity<BienesPlacement>(placement =>
            {
                placement.HasOne(p => p.BankAccount)
                    .WithMany()
                    .HasForeignKey(p => p.BankId)
                    .OnDelete(DeleteBehavior.Restrict);

                placement.HasOne(p => p.Company)
                    .WithMany()
                    .HasForeignKey(p => p.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                placement.HasIndex(p => p.ControlNumber);
            });

            #endregion
        }

        #endregion
    }
}
