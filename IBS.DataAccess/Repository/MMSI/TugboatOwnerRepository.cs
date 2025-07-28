using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI.MasterFile;

namespace IBS.DataAccess.Repository.MMSI
{
    public class TugboatOwnerRepository : Repository<MMSITugboatOwner>, ITugboatOwnerRepository
    {
        private readonly ApplicationDbContext _db;

        public TugboatOwnerRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
    }
}
