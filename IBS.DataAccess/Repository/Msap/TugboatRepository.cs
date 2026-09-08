using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class TugboatRepository(ApplicationDbContext db): Repository<Tugboat>(db), ITugboatRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMsapTugboatsById(CancellationToken cancellationToken = default)
        {
            var tugBoats = await _db.MsapTugboats.OrderBy(s => s.TugboatNumber).Select(s => new SelectListItem
            {
                Value = s.TugboatId.ToString(),
                Text = s.TugboatNumber + " " + s.TugboatName
            }).ToListAsync(cancellationToken);

            return tugBoats;
        }

        public async Task<List<SelectListItem>> GetMsapCompanyOwnerSelectListById(CancellationToken cancellationToken = default)
        {
            var companyOwnerList = await _db.MsapTugboatOwners
                .OrderBy(dt => dt.TugboatOwnerNumber).Select(s => new SelectListItem
                {
                    Value = s.TugboatOwnerId.ToString(),
                    Text = $"{s.TugboatOwnerNumber} {s.TugboatOwnerName}"
                }).ToListAsync(cancellationToken);

            return companyOwnerList;
        }

        public async Task<IEnumerable<Tugboat>> GetTugboatsWithOwnersAsync(CancellationToken cancellationToken = default)
        {
            return await _db.MsapTugboats
                .Include(t => t.TugboatOwner)
                .ToListAsync(cancellationToken);
        }
    }
}


