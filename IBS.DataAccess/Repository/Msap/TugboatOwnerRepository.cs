using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class TugboatOwnerRepository(ApplicationDbContext db) : Repository<TugboatOwner>(db), ITugboatOwnerRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task<List<SelectListItem>> GetMsapTugboatOwnersSelectList(CancellationToken cancellationToken = default)
        {
            return await _db.MsapTugboatOwners
                .OrderBy(t => t.TugboatOwnerName)
                .Select(t => new SelectListItem
                {
                    Value = t.TugboatOwnerId.ToString(),
                    Text = t.TugboatOwnerName
                })
                .ToListAsync(cancellationToken);
        }
    }
}


