using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.Integrated;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class AuthorityToLoadRepository : Repository<FilprideAuthorityToLoad>, IAuthorityToLoadRepository
    {
        private ApplicationDbContext _db;

        public AuthorityToLoadRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateAtlNo(CancellationToken cancellationToken)
        {
            FilprideAuthorityToLoad? lastAtl = await _db
                .FilprideAuthorityToLoads
                .OrderBy(c => c.AuthorityToLoadNo)
                .LastOrDefaultAsync(cancellationToken);

            var yearToday = DateTime.Now.Year;

            if (lastAtl != null)
            {
                var lastAtlNo = lastAtl.AuthorityToLoadNo;
                var lastAtlParts = lastAtlNo.Split('-');
                if (int.TryParse(lastAtlParts.Last(), out var lastIncrement))
                {
                    var newIncrement = lastIncrement + 1;
                    return $"{yearToday}-{newIncrement}";
                }
            }

            return $"{yearToday}-1";
        }
    }
}