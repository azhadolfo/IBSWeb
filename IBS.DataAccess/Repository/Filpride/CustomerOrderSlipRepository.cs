using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class CustomerOrderSlipRepository : Repository<FilprideCustomerOrderSlip>, ICustomerOrderSlipRepository
    {
        private ApplicationDbContext _db;

        public CustomerOrderSlipRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string companyClaims, CancellationToken cancellationToken = default)
        {
            FilprideCustomerOrderSlip? lastCos = await _db
                .FilprideCustomerOrderSlips
                .Where(c => c.Company == companyClaims)
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
                .Include(cos => cos.Hauler)
                .Include(cos => cos.Product)
                .Include(cos => cos.Supplier)
                .Include(cos => cos.PickUpPoint)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po!.Product)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po!.Supplier);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<FilprideCustomerOrderSlip?> GetAsync(Expression<Func<FilprideCustomerOrderSlip, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(cos => cos.Customer)
                .Include(cos => cos.Hauler)
                .Include(cos => cos.Product)
                .Include(cos => cos.Supplier)
                .Include(cos => cos.PickUpPoint)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po!.Product)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(CustomerOrderSlipViewModel viewModel, CancellationToken cancellationToken = default)
        {
            var existingRecord = await GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId,
                cancellationToken) ?? throw new NullReferenceException("CustomerOrderSlip not found");

            var customer = await _db.FilprideCustomers
                .FirstOrDefaultAsync(x => x.CustomerId ==  viewModel.CustomerOrderSlipId, cancellationToken)
                ?? throw new ArgumentException("Customer not found");

            existingRecord.Date = viewModel.Date;
            existingRecord.CustomerId = viewModel.CustomerId;
            existingRecord.CustomerAddress = viewModel.CustomerAddress!;
            existingRecord.CustomerTin = viewModel.TinNo!;
            existingRecord.CustomerPoNo = viewModel.CustomerPoNo;
            existingRecord.Quantity = viewModel.Quantity;
            existingRecord.BalanceQuantity = existingRecord.Quantity;
            existingRecord.DeliveredPrice = viewModel.DeliveredPrice;
            existingRecord.TotalAmount = viewModel.TotalAmount;
            existingRecord.AccountSpecialist = viewModel.AccountSpecialist;
            existingRecord.Remarks = viewModel.Remarks;
            existingRecord.HasCommission = viewModel.HasCommission;
            existingRecord.CommissioneeId = existingRecord.HasCommission ? viewModel.CommissioneeId : null;
            existingRecord.CommissionRate = existingRecord.HasCommission ? viewModel.CommissionRate : 0;
            existingRecord.ProductId = viewModel.ProductId;
            existingRecord.OldCosNo = viewModel.OtcCosNo;
            existingRecord.Branch = viewModel.SelectedBranch;
            existingRecord.Terms = viewModel.Terms;
            existingRecord.CustomerType = viewModel.CustomerType!;
            existingRecord.OldPrice = !customer.RequiresPriceAdjustment ? viewModel.DeliveredPrice : 0;

            if (existingRecord.Branch != null)
            {
                var branch = await _db.FilprideCustomerBranches
                    .Where(b => b.BranchName == existingRecord.Branch)
                    .FirstOrDefaultAsync(cancellationToken);

                existingRecord.CustomerAddress = branch!.BranchAddress;
                existingRecord.CustomerTin = branch.BranchTin;
            }

            if (_db.ChangeTracker.HasChanges())
            {
                existingRecord.EditedBy = viewModel.CurrentUser;
                existingRecord.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                FilprideAuditTrail auditTrailBook = new(existingRecord.EditedBy!, $"Edit customer order slip# {existingRecord.CustomerOrderSlipNo}", "Customer Order Slip", existingRecord.Company);
                await _db.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        public async Task<List<SelectListItem>> GetCosListNotDeliveredAsync(string companyClaims, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideCustomerOrderSlips
                .OrderBy(cos => cos.CustomerOrderSlipId)
                .Where(cos =>
                    cos.Company == companyClaims &&
                    (!cos.IsDelivered && cos.Status == nameof(CosStatus.Completed)) || cos.Status == nameof(CosStatus.ForDR))
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
                .Where(cos => ((!cos.IsDelivered &&
                                cos.Status == nameof(CosStatus.Completed)) ||
                                cos.Status == nameof(CosStatus.ForDR) ||
                                cos.Status == nameof(CosStatus.Closed)) &&
                                cos.CustomerId == customerId)
                .Select(cos => new SelectListItem
                {
                    Value = cos.CustomerOrderSlipId.ToString(),
                    Text = cos.CustomerOrderSlipNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCosListPerCustomerAsync(int customerId, CancellationToken cancellationToken = default)
        {
            var test = await _db.FilprideCustomerOrderSlips
                .OrderBy(cos => cos.CustomerOrderSlipId)
                .Where(cos => (cos.Status == nameof(CosStatus.ForDR) ||
                               cos.Status == nameof(CosStatus.Completed) ||
                               cos.Status == nameof(CosStatus.Closed)) &&
                               cos.CustomerId == customerId)
                .Select(cos => new SelectListItem
                {
                    Value = cos.CustomerOrderSlipId.ToString(),
                    Text = cos.CustomerOrderSlipNo
                })
                .ToListAsync(cancellationToken);

            return test;
        }

        public async Task OperationManagerApproved(FilprideCustomerOrderSlip customerOrderSlip, CancellationToken cancellationToken = default)
        {
            customerOrderSlip.ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

            customerOrderSlip.TotalAmount = customerOrderSlip.Quantity * customerOrderSlip.DeliveredPrice;

            customerOrderSlip.Status = nameof(CosStatus.ForApprovalOfFM);

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<decimal> GetCustomerCreditBalance(int customerId, CancellationToken cancellationToken = default)
        {
            //Beginning Balance to be discussed

            var drForTheMonth = await _db.FilprideDeliveryReceipts
                .Where(dr => dr.CustomerId == customerId && dr.Date.Month == DateTime.UtcNow.Month && dr.Date.Year == DateTime.UtcNow.Year)
                .SumAsync(dr => dr.TotalAmount, cancellationToken);

            var outstandingCos = await _db.FilprideCustomerOrderSlips
                .Where(cos => cos.CustomerId == customerId && cos.ExpirationDate >= DateOnly.FromDateTime(DateTime.UtcNow) && cos.Status == nameof(CosStatus.ForDR))
                .SumAsync(cos => cos.TotalAmount, cancellationToken);

            var availableCreditLimit = await _db.FilprideCustomers
                .Where(c => c.CustomerId == customerId)
                .Select(c => c.CreditLimitAsOfToday)
                .FirstOrDefaultAsync(cancellationToken);

            return availableCreditLimit - (drForTheMonth + outstandingCos);
        }

        public async Task FinanceApproved(FilprideCustomerOrderSlip customerOrderSlip, CancellationToken cancellationToken = default)
        {
            customerOrderSlip.Status = nameof(CosStatus.ForDR);

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
