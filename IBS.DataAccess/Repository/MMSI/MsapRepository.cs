using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class MsapRepository : IMsapRepository
    {
        private readonly ApplicationDbContext _db;

        public MsapRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<SelectListItem>> GetMMSIUsersSelectListById(CancellationToken cancellationToken = default)
        {
            var list = await _db.Users
                .OrderBy(dt => dt.UserName).Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.UserName}"
                }).ToListAsync(cancellationToken);

            return list;
        }
    }
}
