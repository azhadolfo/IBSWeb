using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.MasterFile;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MasterFile
{
    public class HaulerRepository : Repository<Hauler>, IHaulerRepository
    {
        private ApplicationDbContext _db;

        public HaulerRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            Hauler? lastHauler = await _db
                .Haulers
                .OrderBy(c => c.HaulerId)
                .LastOrDefaultAsync(cancellationToken);

            if (lastHauler != null)
            {
                string lastCode = lastHauler.HaulerCode;
                string numericPart = lastCode.Substring(1);

                int incrementedNumber = int.Parse(numericPart) + 1;

                return $"{lastCode[0]}{incrementedNumber:D2}";
            }
            else
            {
                return "H01";
            }
        }

        public async Task<bool> IsHaulerNameExistAsync(string haulerName, CancellationToken cancellationToken)
        {
            return await _db.Haulers
                .AnyAsync(c => c.HaulerName == haulerName, cancellationToken);
        }

        public async Task UpdateAsync(Hauler model, CancellationToken cancellationToken = default)
        {
            var existingRecord = await GetAsync(h => h.HaulerId == model.HaulerId, cancellationToken);

            existingRecord.HaulerName = model.HaulerName;
            existingRecord.HaulerAddress = model.HaulerAddress;
            existingRecord.ContactNo = model.ContactNo;

            if (_db.ChangeTracker.HasChanges())
            {
                existingRecord.EditedBy = model.EditedBy;
                existingRecord.EditedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }
    }
}