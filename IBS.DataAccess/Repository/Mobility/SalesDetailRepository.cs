using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;

namespace IBS.DataAccess.Repository.Mobility
{
    public class SalesDetailRepository : Repository<SalesDetail>, ISalesDetailRepository
    {
        private ApplicationDbContext _db;

        public SalesDetailRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
    }
}