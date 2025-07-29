using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI.MasterFile;

namespace IBS.DataAccess.Repository.MMSI
{
    public class TugMasterRepository : Repository<MMSITugMaster>, ITugMasterRepository
    {
        private readonly ApplicationDbContext _db;

        public TugMasterRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
