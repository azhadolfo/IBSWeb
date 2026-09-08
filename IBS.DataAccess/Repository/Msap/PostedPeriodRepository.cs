using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.Msap.MasterFile;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class PostedPeriodRepository(ApplicationDbContext db) : IPostedPeriodRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task<bool> IsMonthClosedAsync(int year, int month, CancellationToken cancellationToken = default)
        {
            var period = await _db.MsapPostedPeriods
                .FirstOrDefaultAsync(p => p.Year == year && p.Month == month, cancellationToken);
            return period?.IsClosed ?? false;
        }

        public async Task<IEnumerable<MsapPostedPeriod>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.MsapPostedPeriods
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ToListAsync(cancellationToken);
        }

        public async Task<MsapPostedPeriod?> GetByYearMonthAsync(int year, int month, CancellationToken cancellationToken = default)
        {
            return await _db.MsapPostedPeriods
                .FirstOrDefaultAsync(p => p.Year == year && p.Month == month, cancellationToken);
        }

        public async Task AddAsync(MsapPostedPeriod period, CancellationToken cancellationToken = default)
        {
            await _db.MsapPostedPeriods.AddAsync(period, cancellationToken);
        }

        public Task UpdateAsync(MsapPostedPeriod period, CancellationToken cancellationToken = default)
        {
            _db.MsapPostedPeriods.Update(period);
            return Task.CompletedTask;
        }
    }
}