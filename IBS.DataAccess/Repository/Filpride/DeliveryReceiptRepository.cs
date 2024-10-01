﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class DeliveryReceiptRepository : Repository<FilprideDeliveryReceipt>, IDeliveryReceiptRepository
    {
        private ApplicationDbContext _db;

        public DeliveryReceiptRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            FilprideDeliveryReceipt? lastDr = await _db
                .FilprideDeliveryReceipts
                .OrderBy(c => c.DeliveryReceiptNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastDr != null)
            {
                string lastSeries = lastDr.DeliveryReceiptNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "DR0000000001";
            }
        }

        public override async Task<IEnumerable<FilprideDeliveryReceipt>> GetAllAsync(Expression<Func<FilprideDeliveryReceipt, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideDeliveryReceipt> query = dbSet
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PurchaseOrder).ThenInclude(po => po.Product)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PurchaseOrder).ThenInclude(po => po.Supplier)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.Hauler)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PickUpPoint)
                .Include(dr => dr.Customer);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<FilprideDeliveryReceipt> GetAsync(Expression<Func<FilprideDeliveryReceipt, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PurchaseOrder).ThenInclude(po => po.Product)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PurchaseOrder).ThenInclude(po => po.Supplier)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.Hauler)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos.PickUpPoint)
                .Include(dr => dr.Customer)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken = default)
        {
            var existingRecord = await GetAsync(dr => dr.DeliveryReceiptId == viewModel.DeliverReceiptId, cancellationToken);

            existingRecord.Date = viewModel.Date;
            existingRecord.EstimatedTimeOfArrival = viewModel.ETA;
            existingRecord.CustomerOrderSlipId = viewModel.CustomerOrderSlipId;
            existingRecord.CustomerId = viewModel.CustomerId;
            existingRecord.Remarks = viewModel.Remarks;
            existingRecord.Quantity = viewModel.Volume;

            if (existingRecord.TotalAmount != viewModel.TotalAmount)
            {
                existingRecord.TotalAmount = viewModel.TotalAmount;
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

        public async Task<List<SelectListItem>> GetDeliveryReceiptListAsync(CancellationToken cancellationToken = default)
        {
            return await _db.FilprideDeliveryReceipts
                .OrderBy(dr => dr.DeliveryReceiptId)
                .Where(dr => dr.PostedBy != null && dr.DeliveredDate != null)
                .Select(dr => new SelectListItem
                {
                    Value = dr.DeliveryReceiptId.ToString(),
                    Text = dr.DeliveryReceiptNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetDeliveryReceiptListByCos(int cosId, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideDeliveryReceipts
                    .OrderBy(dr => dr.DeliveryReceiptId)
                    .Where(dr => dr.PostedBy != null && dr.CustomerOrderSlipId == cosId && dr.DeliveredDate != null)
                    .Select(dr => new SelectListItem
                    {
                        Value = dr.DeliveryReceiptId.ToString(),
                        Text = dr.DeliveryReceiptNo
                    })
                    .ToListAsync(cancellationToken);
        }

        public async Task PostAsync(FilprideDeliveryReceipt deliveryReceipt, CancellationToken cancellationToken = default)
        {
            #region--Update COS

            await UpdateCosRemainingVolumeAsync(deliveryReceipt.CustomerOrderSlipId, deliveryReceipt.Quantity, cancellationToken);

            #endregion

            #region General Ledger Book Recording

            var ledgers = new List<FilprideGeneralLedgerBook>();

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = deliveryReceipt.Date,
                Reference = deliveryReceipt.DeliveryReceiptNo,
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.CustomerOrderSlip.Hauler.SupplierName}",
                AccountNo = deliveryReceipt.CustomerOrderSlip.Terms == SD.Terms_Cod ? "1010201" : "1010101",
                AccountTitle = deliveryReceipt.CustomerOrderSlip.Terms == SD.Terms_Cod ? "Cash in Bank" : "AR-Trade Receivable",
                Debit = deliveryReceipt.TotalAmount,
                Credit = 0,
                Company = deliveryReceipt.Company,
                CreatedBy = deliveryReceipt.CreatedBy,
                CreatedDate = deliveryReceipt.CreatedDate
            });

            var (salesAcctNo, salesAcctTitle) = GetSalesAccountTitle(deliveryReceipt.CustomerOrderSlip.PurchaseOrder.Product.ProductName);

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = deliveryReceipt.Date,
                Reference = deliveryReceipt.DeliveryReceiptNo,
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.CustomerOrderSlip.Hauler.SupplierName}",
                AccountNo = salesAcctNo,
                AccountTitle = salesAcctTitle,
                Debit = 0,
                Credit = ComputeNetOfVat(deliveryReceipt.TotalAmount),
                Company = deliveryReceipt.Company,
                CreatedBy = deliveryReceipt.CreatedBy,
                CreatedDate = deliveryReceipt.CreatedDate
            });

            ledgers.Add(new FilprideGeneralLedgerBook
            {
                Date = deliveryReceipt.Date,
                Reference = deliveryReceipt.DeliveryReceiptNo,
                Description = $"{deliveryReceipt.CustomerOrderSlip.DeliveryOption} by {deliveryReceipt.CustomerOrderSlip.Hauler.SupplierName}",
                AccountNo = "2010301",
                AccountTitle = "Vat Output",
                Debit = 0,
                Credit = ComputeVatAmount(ComputeNetOfVat(deliveryReceipt.TotalAmount)),
                Company = deliveryReceipt.Company,
                CreatedBy = deliveryReceipt.CreatedBy,
                CreatedDate = deliveryReceipt.CreatedDate
            });

            if (!IsJournalEntriesBalanced(ledgers))
            {
                throw new ArgumentException("Debit and Credit is not equal, check your entries.");
            }

            await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

            #endregion

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task UpdateCosRemainingVolumeAsync(int cosId, decimal drVolume, CancellationToken cancellationToken)
        {
            var cos = await _db.FilprideCustomerOrderSlips
                .FirstOrDefaultAsync(po => po.CustomerOrderSlipId == cosId, cancellationToken) ?? throw new InvalidOperationException("No record found.");

            if (cos != null)
            {
                cos.DeliveredQuantity = drVolume;
                cos.BalanceQuantity -= cos.DeliveredQuantity;

                if (cos.BalanceQuantity <= 0)
                {
                    cos.IsDelivered = true;
                }
            }
        }
    }
}