using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.Books;

namespace IBS.DataAccess.Repository.Filpride
{
    public class AuditTrailRepository : Repository<FilprideAuditTrail>, IAuditTrailRepository
    {
        private ApplicationDbContext _db;

        public AuditTrailRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
    }
}
