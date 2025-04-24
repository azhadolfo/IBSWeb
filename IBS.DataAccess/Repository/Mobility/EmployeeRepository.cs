using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility.MasterFile;

namespace IBS.DataAccess.Repository.Mobility
{
    public class EmployeeRepository : Repository<MobilityEmployee>, IEmployeeRepository
    {
        private ApplicationDbContext _db;

        public EmployeeRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }


    }
}
