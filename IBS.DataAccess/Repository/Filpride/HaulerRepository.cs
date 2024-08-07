using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class HaulerRepository : Repository<Hauler>, IHaulerRepository
    {
        private ApplicationDbContext _db;

        public HaulerRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string company, CancellationToken cancellationToken = default)
        {
            Hauler? lastHauler = await _db
                .Haulers
                .OrderBy(c => c.HaulerId)
                .Where(c => c.Company == company)
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

        public async Task<bool> IsHaulerNameExistAsync(string haulerName, string company, CancellationToken cancellationToken)
        {
            return await _db.Haulers
                .AnyAsync(c => c.Company == company && c.HaulerName == haulerName, cancellationToken);
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

        public async Task<List<SelectListItem>> GetHaulerListAsync(string company, CancellationToken cancellationToken = default)
        {
            return await _db.Haulers
                .OrderBy(h => h.HaulerId)
                .Where(h => h.Company == company)
                .Select(h => new SelectListItem
                {
                    Value = h.HaulerId.ToString(),
                    Text = h.HaulerCode + " " + h.HaulerName
                })
                .ToListAsync(cancellationToken);
        }
    }
}