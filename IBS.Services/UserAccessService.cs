using IBS.DataAccess.Data;
using IBS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.EntityFrameworkCore;
using IBS.Utility.Enums;

namespace IBS.Services
{
    public interface IUserAccessService
    {
        Task<bool> CheckAccess(string id, ProcedureEnum procedure, CancellationToken cancellationToken = default);
    }

    public class UserAccessService : IUserAccessService
    {
        private readonly ApplicationDbContext _dbContext;

        public UserAccessService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> CheckAccess(string id, ProcedureEnum procedure, CancellationToken cancellationToken = default)
        {
            var userAccess = await _dbContext.MMSIUserAccesses
                .FirstOrDefaultAsync(a => a.UserId == id, cancellationToken);

            if (userAccess == null)
            {
                return false;
            }

            switch (procedure)
            {
                case ProcedureEnum.CreateServiceRequest:
                    return userAccess.CanCreateServiceRequest;
                case ProcedureEnum.PostServiceRequest:
                    return userAccess.CanPostServiceRequest;
                case ProcedureEnum.CreateDispatchTicket:
                    return userAccess.CanCreateDispatchTicket;
                case ProcedureEnum.SetTariff:
                    return userAccess.CanSetTariff;
                case ProcedureEnum.ApproveTariff:
                    return userAccess.CanApproveTariff;
                case ProcedureEnum.CreateBilling:
                    return userAccess.CanCreateBilling;
                case ProcedureEnum.CreateCollection:
                    return userAccess.CanCreateCollection;
                default:
                    return false;
            }
        }
    }
}
