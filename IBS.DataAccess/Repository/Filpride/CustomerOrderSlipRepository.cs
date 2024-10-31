using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class CustomerOrderSlipRepository : Repository<FilprideCustomerOrderSlip>, ICustomerOrderSlipRepository
    {
        private ApplicationDbContext _db;

        public CustomerOrderSlipRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            FilprideCustomerOrderSlip? lastCos = await _db
                .FilprideCustomerOrderSlips
                .OrderBy(c => c.CustomerOrderSlipNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCos != null)
            {
                string lastSeries = lastCos.CustomerOrderSlipNo;
                string numericPart = lastSeries.Substring(3);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "COS0000000001";
            }
        }

        public override async Task<IEnumerable<FilprideCustomerOrderSlip>> GetAllAsync(Expression<Func<FilprideCustomerOrderSlip, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideCustomerOrderSlip> query = dbSet
                .Include(cos => cos.Customer)
                .Include(cos => cos.Product)
                .Include(cos => cos.Supplier)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po.Product)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po.Supplier);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<FilprideCustomerOrderSlip> GetAsync(Expression<Func<FilprideCustomerOrderSlip, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(cos => cos.Customer)
                .Include(cos => cos.Product)
                .Include(cos => cos.Supplier)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po.Product)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(CustomerOrderSlipViewModel viewModel, CancellationToken cancellationToken = default)
        {
            var existingRecord = await GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

            existingRecord.Date = viewModel.Date;
            existingRecord.CustomerId = viewModel.CustomerId;
            existingRecord.CustomerPoNo = viewModel.CustomerPoNo;
            existingRecord.Quantity = viewModel.Quantity;
            existingRecord.DeliveredPrice = viewModel.DeliveredPrice;
            existingRecord.TotalAmount = viewModel.TotalAmount;
            existingRecord.AccountSpecialist = viewModel.AccountSpecialist;
            existingRecord.Remarks = viewModel.Remarks;
            existingRecord.HasCommission = viewModel.HasCommission;
            existingRecord.CommissioneeId = viewModel.CommissioneeId;
            existingRecord.CommissionRate = viewModel.CommissionerRate;
            existingRecord.ProductId = viewModel.ProductId;
            existingRecord.OldCosNo = viewModel.OtcCosNo;

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

        public async Task<List<SelectListItem>> GetCosListNotDeliveredAsync(CancellationToken cancellationToken = default)
        {
            return await _db.FilprideCustomerOrderSlips
                .OrderBy(cos => cos.CustomerOrderSlipId)
                .Where(cos => cos.Status == nameof(CosStatus.Approved))
                .Select(cos => new SelectListItem
                {
                    Value = cos.CustomerOrderSlipId.ToString(),
                    Text = cos.CustomerOrderSlipNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCosListPerCustomerNotDeliveredAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideCustomerOrderSlips
                .OrderBy(cos => cos.CustomerOrderSlipId)
                .Where(cos => cos.Status == nameof(CosStatus.Approved) && cos.CustomerId == customerId)
                .Select(cos => new SelectListItem
                {
                    Value = cos.CustomerOrderSlipId.ToString(),
                    Text = cos.CustomerOrderSlipNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCosListPerCustomerAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideCustomerOrderSlips
                .OrderBy(cos => cos.CustomerOrderSlipId)
                .Where(cos => cos.Status == nameof(CosStatus.Approved) || cos.Status == nameof(CosStatus.Completed) && cos.CustomerId == customerId)
                .Select(cos => new SelectListItem
                {
                    Value = cos.CustomerOrderSlipId.ToString(),
                    Text = cos.CustomerOrderSlipNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task OperationManagerApproved(FilprideCustomerOrderSlip customerOrderSlip, decimal grossMargin, CancellationToken cancellationToken = default)
        {
            customerOrderSlip.ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7));

            customerOrderSlip.DeliveredPrice = UpdateCosPrice(grossMargin, customerOrderSlip);

            customerOrderSlip.TotalAmount = customerOrderSlip.Quantity * customerOrderSlip.DeliveredPrice;

            customerOrderSlip.Status = nameof(CosStatus.ForApprovalOfFM);

            await _db.SaveChangesAsync(cancellationToken);
        }

        private decimal UpdateCosPrice(decimal grossMargin, FilprideCustomerOrderSlip existingRecord)
        {
            decimal netOfVatProductCost = 0;

            if (!existingRecord.HasMultiplePO)
            {
                netOfVatProductCost = existingRecord.PurchaseOrder.Price / 1.12m;
            }
            else
            {
                var appointedSupplier = _db.FilprideCOSAppointedSuppliers
                        .Where(a => a.CustomerOrderSlipId == existingRecord.CustomerOrderSlipId)
                        .ToList();

                decimal totalPoAmount = 0;

                foreach (var item in appointedSupplier)
                {
                    var po = _db.FilpridePurchaseOrders.Find(item.PurchaseOrderId);
                    totalPoAmount += item.Quantity * ComputeNetOfVat(po.Price);
                }

                netOfVatProductCost = totalPoAmount / appointedSupplier.Sum(a => a.Quantity);
            }

            var netOfVatCosPrice = existingRecord.DeliveredPrice / 1.12m;
            var netOfVatFreightCharge = existingRecord.Freight / 1.12m;
            var existingGrossMargin = netOfVatCosPrice - netOfVatProductCost - netOfVatFreightCharge - existingRecord.CommissionRate;

            if (Math.Round((decimal)existingGrossMargin, 4) != grossMargin)
            {
                decimal newNetOfVatCosPrice = grossMargin + (decimal)(existingRecord.CommissionRate + netOfVatFreightCharge + netOfVatProductCost);
                return Math.Round((ComputeVatAmount(newNetOfVatCosPrice) + newNetOfVatCosPrice), 4);
            }

            return existingRecord.DeliveredPrice;
        }

        public async Task<decimal> GetCustomerCreditBalance(int customerId, CancellationToken cancellationToken = default)
        {
            //Beginning Balance to be discussed

            var drForTheMonth = await _db.FilprideDeliveryReceipts
                .Where(dr => dr.CustomerId == customerId && dr.Date.Month == DateTime.Now.Month && dr.Date.Year == DateTime.Now.Year)
                .SumAsync(dr => dr.TotalAmount, cancellationToken);

            var outstandingCos = await _db.FilprideCustomerOrderSlips
                .Where(cos => cos.ExpirationDate >= DateOnly.FromDateTime(DateTime.Now) && cos.Status == nameof(CosStatus.Approved))
                .SumAsync(cos => cos.TotalAmount, cancellationToken);

            var availableCreditLimit = await _db.FilprideCustomers
                .Where(c => c.CustomerId == customerId)
                .Select(c => c.CreditLimitAsOfToday)
                .FirstOrDefaultAsync(cancellationToken);

            return availableCreditLimit - (drForTheMonth + outstandingCos);
        }

        public async Task FinanceApproved(FilprideCustomerOrderSlip customerOrderSlip, CancellationToken cancellationToken = default)
        {
            customerOrderSlip.Status = nameof(CosStatus.Approved);

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}