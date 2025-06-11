using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;

namespace IBS.DataAccess.Repository.Mobility
{
    public class LubePurchaseDetailRepository : Repository<MobilityLubePurchaseDetail>, ILubePurchaseDetailRepository
    {
        private ApplicationDbContext _db;

        public LubePurchaseDetailRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
    }
}
