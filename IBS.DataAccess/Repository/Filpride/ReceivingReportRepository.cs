using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ReceivingReportRepository : Repository<FilprideReceivingReport>, IReceivingReportRepository
    {
        private ApplicationDbContext _db;

        public ReceivingReportRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public DateOnly CalculateDueDate(string terms, DateOnly transactionDate, CancellationToken cancellationToken = default)
        {
            DateOnly dueDate;

            switch (terms)
            {
                case SD.Terms_7d:
                    return dueDate = transactionDate.AddDays(7);

                case SD.Terms_15d:
                    return dueDate = transactionDate.AddDays(15);

                case SD.Terms_30d:
                    return dueDate = transactionDate.AddDays(30);

                case SD.Terms_M15:
                    return dueDate = transactionDate.AddMonths(1).AddDays(15 - transactionDate.Day);

                case SD.Terms_M30:
                    if (transactionDate.Month == 1)
                    {
                        dueDate = new DateOnly(transactionDate.Year, transactionDate.Month, 1).AddMonths(2).AddDays(-1);
                    }
                    else
                    {
                        dueDate = new DateOnly(transactionDate.Year, transactionDate.Month, 1).AddMonths(2).AddDays(-1);

                        if (dueDate.Day == 31)
                        {
                            dueDate = dueDate.AddDays(-1);
                        }
                    }
                    return dueDate;

                default:
                    return dueDate = transactionDate;
            }
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            FilprideReceivingReport? lastRr = await _db
                .FilprideReceivingReports
                .OrderBy(c => c.ReceivingReportNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastRr != null)
            {
                string lastSeries = lastRr.ReceivingReportNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "RR0000000001";
            }
        }

        public override async Task<IEnumerable<FilprideReceivingReport>> GetAllAsync(Expression<Func<FilprideReceivingReport, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideReceivingReport> query = dbSet
                .Include(rr => rr.Customer)
                .Include(rr => rr.DeliveryReceipt).ThenInclude(dr => dr.Hauler)
                .Include(rr => rr.DeliveryReceipt).ThenInclude(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PurchaseOrder).ThenInclude(po => po.Product)
                .Include(rr => rr.DeliveryReceipt).ThenInclude(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PurchaseOrder).ThenInclude(po => po.Supplier);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<FilprideReceivingReport> GetAsync(Expression<Func<FilprideReceivingReport, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(rr => rr.Customer)
                .Include(rr => rr.DeliveryReceipt).ThenInclude(dr => dr.Hauler)
                .Include(rr => rr.DeliveryReceipt).ThenInclude(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PurchaseOrder).ThenInclude(po => po.Product)
                .Include(rr => rr.DeliveryReceipt).ThenInclude(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PurchaseOrder).ThenInclude(po => po.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task PostAsync(FilprideReceivingReport receivingReport, CancellationToken cancellationToken = default)
        {
            //PENDING journal entries of rr
            #region--General Ledger Recording

            #endregion

            #region--Update PO Served
            await UpdatePoServedAsync(receivingReport.DeliveryReceipt.CustomerOrderSlip.PurchaseOrderId, receivingReport.QuantityDelivered, cancellationToken);
            #endregion

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(ReceivingReportViewModel viewModel, CancellationToken cancellationToken = default)
        {
            var existingRecord = await GetAsync(rr => rr.ReceivingReportId == viewModel.ReceivingReportId, cancellationToken);

            existingRecord.Date = viewModel.Date;
            existingRecord.DeliveryReceiptId = viewModel.DeliveryReceiptId;
            existingRecord.CustomerId = viewModel.CustomerId;
            existingRecord.SupplierSiNo = viewModel.SupplierSiNo;
            existingRecord.SupplierSiDate = viewModel.SupplierSiDate;
            existingRecord.SupplierDrNo = viewModel.SupplierDrNo;
            existingRecord.SupplierDrDate = viewModel.SupplierDrDate;
            existingRecord.WithdrawalCertificate = viewModel.WithdrawalCertificate;
            existingRecord.OtherReference = viewModel.OtherReference;
            existingRecord.TotalFreight = viewModel.TotalFreight;
            existingRecord.Remarks = viewModel.Remarks;

            bool quantityChanged = existingRecord.QuantityDelivered != viewModel.QuantityDelivered ||
                           existingRecord.QuantityReceived != viewModel.QuantityReceived;

            if (quantityChanged)
            {
                existingRecord.QuantityReceived = viewModel.QuantityReceived;
                existingRecord.QuantityDelivered = viewModel.QuantityDelivered;
                existingRecord.TotalAmount = viewModel.QuantityDelivered * viewModel.QuantityReceived;
                existingRecord.GainOrLoss = viewModel.QuantityReceived - viewModel.QuantityDelivered;

                if (existingRecord.DeliveryReceipt.CustomerOrderSlip.PurchaseOrder.Supplier.VatType == SD.VatType_Vatable)
                {
                    existingRecord.NetOfVatAmount = ComputeNetOfVat(existingRecord.TotalAmount);
                    existingRecord.VatAmount = ComputeVatAmount(existingRecord.TotalAmount);
                }
                else
                {
                    existingRecord.NetOfVatAmount = existingRecord.TotalAmount;
                    existingRecord.VatAmount = 0;
                }

                if (existingRecord.DeliveryReceipt.CustomerOrderSlip.PurchaseOrder.Supplier.TaxType == SD.TaxType_WithTax)
                {
                    existingRecord.NetOfTaxAmount = existingRecord.NetOfVatAmount * 0.01m;
                }
                else
                {
                    existingRecord.NetOfTaxAmount = existingRecord.NetOfVatAmount;
                }
            }

            if (_db.ChangeTracker.HasChanges())
            {
                existingRecord.EditedBy = viewModel.CurrentUser;
                existingRecord.EditedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        private async Task UpdatePoServedAsync(int id, decimal quantityDelivered, CancellationToken cancellationToken = default)
        {
            var purchaseOrder = await _db.FilpridePurchaseOrders
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken) ?? throw new InvalidOperationException("No record found.");

            purchaseOrder.QuantityReceived += quantityDelivered;

            if (purchaseOrder.QuantityReceived == purchaseOrder.Quantity)
            {
                purchaseOrder.IsReceived = true;
                purchaseOrder.ReceivedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else if (purchaseOrder.QuantityReceived > purchaseOrder.Quantity)
            {
                throw new InvalidOperationException("The entered Quantity Delivered exceeds the Purchase Order Quantity.");
            }
        }
    }
}