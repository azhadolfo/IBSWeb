using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;

namespace IBS.DataAccess.Repository
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