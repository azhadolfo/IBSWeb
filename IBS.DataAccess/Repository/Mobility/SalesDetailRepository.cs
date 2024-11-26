using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Utility;

namespace IBS.DataAccess.Repository.Mobility
{
    public class SalesDetailRepository : Repository<MobilitySalesDetail>, ISalesDetailRepository
    {
        private ApplicationDbContext _db;

        public SalesDetailRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task LogChangesAsync(int id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string modifiedBy, CancellationToken cancellationToken = default)
        {
            foreach (var change in changes)
            {
                var logReport = new MobilityLogReport
                {
                    Id = Guid.NewGuid(),
                    Reference = nameof(MobilitySalesDetail),
                    ReferenceId = id,
                    Description = change.Key,
                    Module = "Cashier Report",
                    OriginalValue = change.Value.OriginalValue,
                    AdjustedValue = change.Value.NewValue,
                    TimeStamp = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModifiedBy = modifiedBy
                };
                await _db.MobilityLogReports.AddAsync(logReport, cancellationToken);
            }
        }
    }
}