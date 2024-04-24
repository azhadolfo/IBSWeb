using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;

namespace IBS.DataAccess.Repository
{
    public class LubePurchaseDetailRepository : Repository<LubePurchaseDetail>, ILubePurchaseDetailRepository
    {
        private ApplicationDbContext _db;

        public LubePurchaseDetailRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
    }
}
