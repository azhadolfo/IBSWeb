using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.MasterFile;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class CustomerBranchRepository : Repository<FilprideCustomerBranch>, ICustomerBranchRepository
    {
        private readonly ApplicationDbContext _db;

        public CustomerBranchRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task UpdateAsync(FilprideCustomerBranch model, CancellationToken cancellationToken)
        {
            var currentModel = await _db
                .FilprideCustomerBranches.FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            if (currentModel == null)
            {
                throw new NullReferenceException("Customer branch not found");
            }

            currentModel.CustomerId = model.CustomerId;
            currentModel.BranchName = model.BranchName;
            currentModel.BranchAddress = model.BranchAddress;
            currentModel.BranchTin = model.BranchTin;

            await _db.SaveChangesAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideCustomerBranch>> GetAllAsync(Expression<Func<FilprideCustomerBranch, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideCustomerBranch> query = dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query
                .Include(b => b.Customer)
                .ToListAsync(cancellationToken);
        }
    }
}
