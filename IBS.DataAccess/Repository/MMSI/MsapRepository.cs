using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class MsapRepository : IMsapRepository
    {
        public readonly ApplicationDbContext _dbContext;

        public MsapRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<SelectListItem>> GetMMSIUsersSelectListById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> list = await _dbContext.Users
                .OrderBy(dt => dt.UserName).Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.UserName}"
                }).ToListAsync(cancellationToken);

            return list;
        }
    }
}
