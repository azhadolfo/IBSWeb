using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.MasterFile;

namespace IBS.DataAccess.Repository.Filpride
{
    public class CustomerBranchRepository : Repository<FilprideCustomerBranch>, ICustomerBranchRepository
    {
        private ApplicationDbContext _dbContext;

        public CustomerBranchRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task UpdateAsync(FilprideCustomerBranch model, CancellationToken cancellationToken)
        {
            var currentModel = await _dbContext.FilprideCustomerBranches.FindAsync(model.Id, cancellationToken);

            if (currentModel == null)
            {
                throw new NullReferenceException("Customer branch not found");
            }

            currentModel.CustomerId = model.CustomerId;
            currentModel.BranchName = model.BranchName;
            currentModel.BranchAddress = model.BranchAddress;
            currentModel.BranchTin = model.BranchTin;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
