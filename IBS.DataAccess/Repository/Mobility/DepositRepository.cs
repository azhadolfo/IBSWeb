using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;

namespace IBS.DataAccess.Repository.Mobility
{
    public class DepositRepository : Repository<MobilityFMSDeposit>, IDepositRepository
    {
        private ApplicationDbContext _db;

        public DepositRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
    }
}
