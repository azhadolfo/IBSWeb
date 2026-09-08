using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.Msap.MasterFile;

namespace IBS.DataAccess.Repository.Msap
{
    public class MsapEmployeeRepository(ApplicationDbContext db): Repository<MsapEmployee>(db), IMsapEmployeeRepository
    {
        private readonly ApplicationDbContext _db = db;
    }
}