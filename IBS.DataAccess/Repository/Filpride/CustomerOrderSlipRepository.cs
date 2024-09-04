using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
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
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po.Product);

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
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po.Product)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(CustomerOrderSlipViewModel viewModel, CancellationToken cancellationToken = default)
        {
            var existingRecord = await GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

            existingRecord.Date = viewModel.Date;
            existingRecord.DeliveryDateAndTime = viewModel.DeliveryDateAndTime;
            existingRecord.CustomerId = viewModel.CustomerId;
            existingRecord.CustomerPoNo = viewModel.CustomerPoNo;
            existingRecord.Quantity = viewModel.Quantity;
            existingRecord.DeliveredPrice = viewModel.DeliveredPrice;
            existingRecord.Vat = viewModel.Vat;
            existingRecord.TotalAmount = viewModel.TotalAmount;
            existingRecord.Remarks = viewModel.Remarks;

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

        public async Task<List<SelectListItem>> GetCosListAsync(CancellationToken cancellationToken = default)
        {
            return await _db.FilprideCustomerOrderSlips
                .OrderBy(cos => cos.CustomerOrderSlipId)
                .Where(cos => cos.ApprovedBy != null)
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
                .Where(cos => cos.ApprovedBy != null && cos.CustomerId == customerId && !cos.IsDelivered)
                .Select(cos => new SelectListItem
                {
                    Value = cos.CustomerOrderSlipId.ToString(),
                    Text = cos.CustomerOrderSlipNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task PostAsync(FilprideCustomerOrderSlip customerOrderSlip, CancellationToken cancellationToken = default)
        {
            //PENDING process the method here
            customerOrderSlip.ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7));
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}