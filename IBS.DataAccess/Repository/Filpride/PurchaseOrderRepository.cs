﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class PurchaseOrderRepository : Repository<FilpridePurchaseOrder>, IPurchaseOrderRepository
    {
        private ApplicationDbContext _db;

        public PurchaseOrderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string company, CancellationToken cancellationToken = default)
        {
            FilpridePurchaseOrder? lastPo = await _db
                .FilpridePurchaseOrders
                .Where(c => c.Company == company && !c.PurchaseOrderNo.StartsWith("POBEG"))
                .OrderBy(c => c.PurchaseOrderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastPo != null)
            {
                string lastSeries = lastPo.PurchaseOrderNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "PO0000000001";
            }
        }

        public override async Task<FilpridePurchaseOrder> GetAsync(Expression<Func<FilpridePurchaseOrder, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(p => p.Supplier)
                .Include(p => p.Product)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilpridePurchaseOrder>> GetAllAsync(Expression<Func<FilpridePurchaseOrder, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilpridePurchaseOrder> query = dbSet
                .Include(p => p.Supplier)
                .Include(p => p.Product);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetPurchaseOrderListAsyncByCode(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(p => p.Company == company && !p.IsReceived)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderNo,
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetPurchaseOrderListAsyncById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePurchaseOrders
                .Where(p => p.Company == company && !p.IsReceived)
                .OrderBy(p => p.PurchaseOrderNo)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetPurchaseOrderListAsyncBySupplier(int supplierId, CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(p => p.SupplierId == supplierId && !p.IsReceived)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetPurchaseOrderListAsyncByProduct(int productId, CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(p => p.ProductId == productId && !p.IsReceived)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<string> GenerateCodeForSubPoAsync(string purchaseOrderNo, string company, CancellationToken cancellationToken = default)
        {
            var latestSubPoCode = await _db.FilpridePurchaseOrders
                .Where(po => po.IsSubPo && po.Company == company && po.SubPoSeries.Contains(purchaseOrderNo))
                .OrderByDescending(po => po.SubPoSeries)
                .Select(po => po.SubPoSeries)
                .FirstOrDefaultAsync(cancellationToken);

            char nextLetter = 'A';
            if (!string.IsNullOrEmpty(latestSubPoCode))
            {
                nextLetter = (char)(latestSubPoCode[^1] + 1);
            }

            return $"{purchaseOrderNo}{nextLetter}";
        }
    }
}