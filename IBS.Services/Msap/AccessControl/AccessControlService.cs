using IBS.Models;
using IBS.Models.Msap.Enums;
using Microsoft.AspNetCore.Identity;

namespace IBS.Services.Msap.AccessControl
{
    public interface IAccessControlService
    {
        Task<bool> HasAccessAsync(string userId, params ProcedureEnum[] procedures);
        Task<bool> HasAnyAccessAsync(string userId, params ProcedureEnum[] procedures);
    }

    public class AccessControlService(UserManager<ApplicationUser> userManager, IUserAccessService userAccessService)
        : IAccessControlService
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public async Task<bool> HasAnyAccessAsync(string userId, params ProcedureEnum[] procedures)
        {
            if (procedures == null || procedures.Length == 0)
            {
                return false;
            }

            foreach (var procedure in procedures)
            {
                if (await userAccessService.CheckAccess(userId, procedure, default))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> HasAccessAsync(string userId, params ProcedureEnum[] procedures)
        {
            return await HasAnyAccessAsync(userId, procedures);
        }
    }

    public static class AccessControlExtensions
    {
        #region -- MSAP Workflow --

        public static async Task<bool> HasServiceRequestAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.CreateServiceRequest,
                ProcedureEnum.PostServiceRequest);
        }

        #endregion -- MSAP Workflow --

        #region -- MSAP Import --

        public static async Task<bool> HasMsapImportAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.ManageMsapImport);
        }

        #endregion -- MSAP Import --

        #region -- Reports --

        public static async Task<bool> HasMaritimeReportAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.ViewMaritimeReport);
        }

        #endregion -- Reports --
    }
}
