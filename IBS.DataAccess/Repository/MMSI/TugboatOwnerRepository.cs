using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using IBS.Models.MMSI.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class TugboatOwnerRepository : Repository<MMSITugboatOwner>, ITugboatOwnerRepository
    {
        public readonly ApplicationDbContext _dbContext;

        public TugboatOwnerRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
