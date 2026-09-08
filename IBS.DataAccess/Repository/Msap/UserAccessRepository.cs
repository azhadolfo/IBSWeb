using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.Msap.Enums;
using IBS.Models.Msap.MasterFile;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class UserAccessRepository(ApplicationDbContext db): Repository<UserAccess>(db), IUserAccessRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public override async Task<IEnumerable<UserAccess>> GetAllAsync(Expression<Func<UserAccess, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<UserAccess> query = dbSet
                .OrderBy(ua => ua.UserName);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<string>> GetUserIdsWithAccessAsync(ProcedureEnum procedure, CancellationToken cancellationToken = default)
        {
            var query = _db.MsapUserAccesses.AsNoTracking();

            query = procedure switch
            {
                ProcedureEnum.CreateServiceRequest => query.Where(u => u.CanCreateServiceRequest),
                ProcedureEnum.PostServiceRequest => query.Where(u => u.CanPostServiceRequest),
                ProcedureEnum.CreateDispatchTicket => query.Where(u => u.CanCreateDispatchTicket),
                ProcedureEnum.EditDispatchTicket => query.Where(u => u.CanEditDispatchTicket),
                ProcedureEnum.DeleteDispatchTicket => query.Where(u => u.CanDeleteDispatchTicket),
                ProcedureEnum.SetTariff => query.Where(u => u.CanSetTariff),
                ProcedureEnum.ApproveTariff => query.Where(u => u.CanApproveTariff),
                ProcedureEnum.CreateBilling => query.Where(u => u.CanCreateBilling),
                ProcedureEnum.EditBilling => query.Where(u => u.CanEditBilling),
                ProcedureEnum.DeleteBilling => query.Where(u => u.CanDeleteBilling),
                ProcedureEnum.ReverseBilling => query.Where(u => u.CanReverseBilling),
                ProcedureEnum.CreateCollection => query.Where(u => u.CanCreateCollection),
                ProcedureEnum.CreateJobOrder => query.Where(u => u.CanCreateJobOrder),
                ProcedureEnum.EditJobOrder => query.Where(u => u.CanEditJobOrder),
                ProcedureEnum.DeleteJobOrder => query.Where(u => u.CanDeleteJobOrder),
                ProcedureEnum.CloseJobOrder => query.Where(u => u.CanCloseJobOrder),
                ProcedureEnum.AccessTreasury => query.Where(u => u.CanAccessTreasury),
                ProcedureEnum.CreateDisbursement => query.Where(u => u.CanCreateDisbursement),
                ProcedureEnum.ManageMsapImport => query.Where(u => u.CanManageMsapImport),
                ProcedureEnum.ViewGeneralLedger => query.Where(u => u.CanViewGeneralLedger),
                ProcedureEnum.ViewInventoryReport => query.Where(u => u.CanViewInventoryReport),
                ProcedureEnum.ViewMaritimeReport => query.Where(u => u.CanViewMaritimeReport),
                _ => query.Where(u => false)
            };

            var userIds = await query.Select(u => u.UserId).ToListAsync(cancellationToken);

            // Also include all Admins
            var admins = await _db.UserRoles
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur, r })
                .Where(x => x.r.Name == "Admin")
                .Select(x => x.ur.UserId)
                .ToListAsync(cancellationToken);

            return userIds.Concat(admins).Distinct().ToList();
        }
    }
}


