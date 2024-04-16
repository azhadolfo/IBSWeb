﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<T> dbSet;

        public Repository(ApplicationDbContext db)
        {
            _db = db;
            this.dbSet = _db.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<T> GetAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            dbSet.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public bool IsJournalEntriesBalanced(IEnumerable<GeneralLedger> journals)
        {
            try
            {
                decimal totalDebit = journals.Sum(j => j.Debit);
                decimal totalCredit = journals.Sum(j => j.Credit);

                return totalDebit == totalCredit;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        public async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
        {
            dbSet.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            dbSet.RemoveRange(entities);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<ProductDto> MapProductToDTO(string productCode, CancellationToken cancellationToken = default)
        {
            return await _db.Set<Product>()
                .Where(p => p.ProductCode == productCode)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<StationDto> MapStationToDTO(string stationCode, CancellationToken cancellationToken = default)
        {
            return await _db.Set<Station>()
                .Where(s => s.StationCode == stationCode)
                .Select(s => new StationDto
                {
                    StationId = s.StationId,
                    StationCode = s.StationCode,
                    StationName = s.StationName,
                    StationPOSCode = s.PosCode
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<SupplierDto> MapSupplierToDTO(string supplierCode, CancellationToken cancellationToken = default)
        {
            return await _db.Set<Supplier>()
                .Where(s => s.SupplierCode == supplierCode)
                .Select(s => new SupplierDto
                {
                    SupplierId = s.SupplierId,
                    SupplierCode = s.SupplierCode,
                    SupplierName = s.SupplierName
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

    }
}