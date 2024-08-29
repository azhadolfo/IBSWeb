﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class JournalVoucherRepository : Repository<FilprideJournalVoucherHeader>, IJournalVoucherRepository
    {
        private ApplicationDbContext _db;

        public JournalVoucherRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string company, CancellationToken cancellationToken = default)
        {
            FilprideJournalVoucherHeader? lastJv = await _db
                .FilprideJournalVoucherHeaders
                .Where(c => c.Company == company)
                .OrderBy(c => c.JournalVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastJv != null)
            {
                string lastSeries = lastJv.JournalVoucherHeaderNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "JV0000000001";
            }
        }

        public override async Task<FilprideJournalVoucherHeader> GetAsync(Expression<Func<FilprideJournalVoucherHeader, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(cv => cv.CheckVoucherHeader)
                .ThenInclude(supplier => supplier.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideJournalVoucherHeader>> GetAllAsync(Expression<Func<FilprideJournalVoucherHeader, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideJournalVoucherHeader> query = dbSet
                .Include(cv => cv.CheckVoucherHeader)
                .ThenInclude(supplier => supplier.Supplier);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}