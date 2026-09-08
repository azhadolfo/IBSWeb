using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class MsapRepository(ApplicationDbContext db): IMsapRepository
    {
        public async Task<List<SelectListItem>> GetMsapUsersSelectListById(CancellationToken cancellationToken = default)
        {
            var existingUserIds = await db.MsapUserAccesses
                .Select(ua => ua.UserId)
                .ToListAsync(cancellationToken);

            var list = await db.Users
                .Where(u => !existingUserIds.Contains(u.Id))
                .OrderBy(dt => dt.UserName).Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.UserName}"
                }).ToListAsync(cancellationToken);

            return list;
        }
    }
}
