using IBS.DataAccess.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.EntityFrameworkCore;

namespace IBS.Services
{
    public interface IUserAccessService
    {
        Task<bool> CheckAccess(string id, string moduleName, CancellationToken cancellationToken = default);
    }

    public class UserAccessService : IUserAccessService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public UserAccessService(UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<bool> CheckAccess(string id, string moduleName, CancellationToken cancellationToken = default)
        {
            var userAccess = await _dbContext.MMSIUserAccesses?
                .FirstOrDefaultAsync(a => a.UserId == id, cancellationToken);

            if (userAccess == null)
            {
                return false;
            }

            switch (moduleName)
            {
                case "Create service request":
                    return userAccess.CanCreateServiceRequest;
                case "Post service request":
                    return userAccess.CanPostServiceRequest;
                case "Create dispatch ticket":
                    return userAccess.CanCreateDispatchTicket;
                case "Set tariff":
                    return userAccess.CanSetTariff;
                case "Approve tariff":
                    return userAccess.CanApproveTariff;
                case "Create billing":
                    return userAccess.CanCreateBilling;
                case "Create collection":
                    return userAccess.CanCreateCollection;
                default:
                    return false;
            }
        }
    }
}
