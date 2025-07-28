using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride;

namespace IBS.DataAccess.Repository.Filpride
{
    public class FreightRepository : Repository<FilprideFreight>, IFreightRepository
    {
        private readonly ApplicationDbContext _db;

        public FreightRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
    }
}
