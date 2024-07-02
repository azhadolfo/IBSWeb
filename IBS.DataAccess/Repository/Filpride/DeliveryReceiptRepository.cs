using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.ViewModels;
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
                .Include(dr => dr.Customer)
                .Include(dr => dr.Hauler);

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
                .Include(dr => dr.Customer)
                .Include(dr => dr.Hauler)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken = default)
        {
            var existingRecord = await GetAsync(dr => dr.DeliveryReceiptId == viewModel.DeliverReceiptId, cancellationToken);

            existingRecord.Date = viewModel.Date;
            existingRecord.CustomerOrderSlipId = viewModel.CustomerOrderSlipId;
            existingRecord.HaulerId = viewModel.HaulerId;
            existingRecord.CustomerId = viewModel.CustomerId;
            existingRecord.Freight = viewModel.Freight;
            existingRecord.LoadPort = viewModel.LoadPort;
            existingRecord.AuthorityToLoadNo = viewModel.AuthorityToLoadNo;
            existingRecord.Remarks = viewModel.Remarks;
            existingRecord.Quantity = viewModel.Volume;

            if (existingRecord.TotalAmount != viewModel.TotalAmount)
            {
                existingRecord.TotalAmount = viewModel.TotalAmount;
                existingRecord.NetOfVatAmount = ComputeNetOfVat(existingRecord.TotalAmount);
                existingRecord.VatAmount = ComputeVatAmount(existingRecord.TotalAmount);
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
                .OrderBy(po => po.DeliveryReceiptId)
                .Where(po => po.PostedBy != null)
                .Select(po => new SelectListItem
                {
                    Value = po.DeliveryReceiptId.ToString(),
                    Text = po.DeliveryReceiptNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetDeliveryReceiptListByCustomerAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideDeliveryReceipts
                    .OrderBy(cos => cos.DeliveryReceiptId)
                    .Where(cos => cos.PostedBy != null && cos.CustomerId == customerId)
                    .Select(cos => new SelectListItem
                    {
                        Value = cos.DeliveryReceiptId.ToString(),
                        Text = cos.DeliveryReceiptNo
                    })
                    .ToListAsync(cancellationToken);
        }

        public async Task PostAsync(FilprideDeliveryReceipt deliveryReceipt, CancellationToken cancellationToken = default)
        {
            #region--Update COS

            await UpdateCosRemainingVolumeAsync(deliveryReceipt.CustomerOrderSlipId, deliveryReceipt.Quantity, cancellationToken);

            #endregion

            //PENDING process the method here

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

                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}