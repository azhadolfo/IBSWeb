using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository
{
    public class LubePurchaseDetailRepository : Repository<LubePurchaseDetail>, ILubePurchaseDetailRepository
    {
        private ApplicationDbContext _db;

        public LubePurchaseDetailRepository(ApplicationDbContext db)  : base(db)
        {
            _db = db;
        }
    }
}
