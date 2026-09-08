using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap.Enums;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace IBS.Services.Msap
{
    public class UserAccessService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        ILogger<UserAccessService> logger)
        : IUserAccessService
    {
        public async Task<bool> CheckAccess(string id, ProcedureEnum procedure, CancellationToken cancellationToken = default)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user != null && (await userManager.IsInRoleAsync(user, "Admin") || await userManager.IsInRoleAsync(user, "SuperAdmin")))
            {
                return true;
            }

            var userAccess = await unitOfWork.UserAccess
                .GetAsync(a => a.UserId == id, cancellationToken);

            if (userAccess == null)
            {
                return false;
            }

            return procedure switch
            {
                ProcedureEnum.CreateServiceRequest => userAccess.CanCreateServiceRequest,
                ProcedureEnum.PostServiceRequest => userAccess.CanPostServiceRequest,
                ProcedureEnum.CreateDispatchTicket => userAccess.CanCreateDispatchTicket,
                ProcedureEnum.EditDispatchTicket => userAccess.CanEditDispatchTicket,
                ProcedureEnum.DeleteDispatchTicket => userAccess.CanDeleteDispatchTicket,
                ProcedureEnum.SetTariff => userAccess.CanSetTariff,
                ProcedureEnum.ApproveTariff => userAccess.CanApproveTariff,
                ProcedureEnum.CreateBilling => userAccess.CanCreateBilling,
                ProcedureEnum.EditBilling => userAccess.CanEditBilling,
                ProcedureEnum.DeleteBilling => userAccess.CanDeleteBilling,
                ProcedureEnum.ReverseBilling => userAccess.CanReverseBilling,
                ProcedureEnum.CreateCollection => userAccess.CanCreateCollection,
                ProcedureEnum.CreateJobOrder => userAccess.CanCreateJobOrder,
                ProcedureEnum.EditJobOrder => userAccess.CanEditJobOrder,
                ProcedureEnum.DeleteJobOrder => userAccess.CanDeleteJobOrder,
                ProcedureEnum.CloseJobOrder => userAccess.CanCloseJobOrder,
                ProcedureEnum.AccessTreasury => userAccess.CanAccessTreasury,
                ProcedureEnum.CreateDisbursement => userAccess.CanCreateDisbursement,
                ProcedureEnum.ManageMsapImport => userAccess.CanManageMsapImport,
                ProcedureEnum.ManageMaritimeMasterFile => userAccess.CanManageMaritimeMasterFile,
                ProcedureEnum.ViewGeneralLedger => userAccess.CanViewGeneralLedger,
                ProcedureEnum.ViewInventoryReport => userAccess.CanViewInventoryReport,
                ProcedureEnum.ViewMaritimeReport => userAccess.CanViewMaritimeReport,
                _ => false
            };
        }

        public async Task<IEnumerable<UserAccess>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await unitOfWork.UserAccess.GetAllAsync(null, cancellationToken);
        }

        public async Task<UserAccess?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.UserAccess.GetAsync(ua => ua.Id == id, cancellationToken);
        }

        public async Task<UserAccess> PopulateUsersAsync(UserAccess? model, CancellationToken cancellationToken)
        {
            model ??= new UserAccess();
            model.Users = await unitOfWork.Msap.GetMsapUsersSelectListById(cancellationToken);
            return model;
        }

        public async Task<ServiceResult> CreateAsync(UserAccess model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var existing = await unitOfWork.UserAccess.GetAsync(ua => ua.UserId == model.UserId, cancellationToken);
                if (existing != null)
                {
                    return ServiceResult.Failure($"Access for {existing.UserName} already exists.");
                }

                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var selectedUser = await userManager.FindByIdAsync(model.UserId);
                    model.UserName = selectedUser?.UserName;

                    await unitOfWork.UserAccess.AddAsync(model, cancellationToken);
                    await unitOfWork.SaveAsync(cancellationToken);

                    AuditTrail auditTrail = new(username, $"Created User Access for {model.UserName}", "User Access", model.Id);
                    await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                    await unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                return ServiceResult.Success("User access created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create user access.");
                return ServiceResult.Failure($"Failed to create user access: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateAsync(UserAccess model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var existing = await GetByIdAsync(model.Id, cancellationToken);
                if (existing == null)
                {
                    return ServiceResult.Failure("User access not found.", ServiceResultStatus.NotFound);
                }

                var changes = new List<string>();
                if (existing.CanCreateServiceRequest != model.CanCreateServiceRequest) changes.Add($"Create SR: {existing.CanCreateServiceRequest} → {model.CanCreateServiceRequest}");
                if (existing.CanPostServiceRequest != model.CanPostServiceRequest) changes.Add($"Post SR: {existing.CanPostServiceRequest} → {model.CanPostServiceRequest}");
                if (existing.CanCreateDispatchTicket != model.CanCreateDispatchTicket) changes.Add($"Create DT: {existing.CanCreateDispatchTicket} → {model.CanCreateDispatchTicket}");
                if (existing.CanEditDispatchTicket != model.CanEditDispatchTicket) changes.Add($"Edit DT: {existing.CanEditDispatchTicket} → {model.CanEditDispatchTicket}");
                if (existing.CanDeleteDispatchTicket != model.CanDeleteDispatchTicket) changes.Add($"Delete DT: {existing.CanDeleteDispatchTicket} → {model.CanDeleteDispatchTicket}");
                if (existing.CanSetTariff != model.CanSetTariff) changes.Add($"Set Tariff: {existing.CanSetTariff} → {model.CanSetTariff}");
                if (existing.CanApproveTariff != model.CanApproveTariff) changes.Add($"Approve Tariff: {existing.CanApproveTariff} → {model.CanApproveTariff}");
                if (existing.CanCreateBilling != model.CanCreateBilling) changes.Add($"Create Billing: {existing.CanCreateBilling} → {model.CanCreateBilling}");
                if (existing.CanEditBilling != model.CanEditBilling) changes.Add($"Edit Billing: {existing.CanEditBilling} → {model.CanEditBilling}");
                if (existing.CanDeleteBilling != model.CanDeleteBilling) changes.Add($"Delete Billing: {existing.CanDeleteBilling} → {model.CanDeleteBilling}");
                if (existing.CanReverseBilling != model.CanReverseBilling) changes.Add($"Reverse Billing: {existing.CanReverseBilling} → {model.CanReverseBilling}");
                if (existing.CanCreateCollection != model.CanCreateCollection) changes.Add($"Create Collection: {existing.CanCreateCollection} → {model.CanCreateCollection}");
                if (existing.CanCreateJobOrder != model.CanCreateJobOrder) changes.Add($"Create JO: {existing.CanCreateJobOrder} → {model.CanCreateJobOrder}");
                if (existing.CanEditJobOrder != model.CanEditJobOrder) changes.Add($"Edit JO: {existing.CanEditJobOrder} → {model.CanEditJobOrder}");
                if (existing.CanDeleteJobOrder != model.CanDeleteJobOrder) changes.Add($"Delete JO: {existing.CanDeleteJobOrder} → {model.CanDeleteJobOrder}");
                if (existing.CanCloseJobOrder != model.CanCloseJobOrder) changes.Add($"Close JO: {existing.CanCloseJobOrder} → {model.CanCloseJobOrder}");
                if (existing.CanAccessTreasury != model.CanAccessTreasury) changes.Add($"Access Treasury: {existing.CanAccessTreasury} → {model.CanAccessTreasury}");
                if (existing.CanCreateDisbursement != model.CanCreateDisbursement) changes.Add($"Create Disbursement: {existing.CanCreateDisbursement} → {model.CanCreateDisbursement}");
                if (existing.CanManageMsapImport != model.CanManageMsapImport) changes.Add($"Manage Import: {existing.CanManageMsapImport} → {model.CanManageMsapImport}");
                if (existing.CanManageMaritimeMasterFile != model.CanManageMaritimeMasterFile) changes.Add($"Manage Maritime MF: {existing.CanManageMaritimeMasterFile} → {model.CanManageMaritimeMasterFile}");
                if (existing.CanViewGeneralLedger != model.CanViewGeneralLedger) changes.Add($"View GL: {existing.CanViewGeneralLedger} → {model.CanViewGeneralLedger}");
                if (existing.CanViewInventoryReport != model.CanViewInventoryReport) changes.Add($"View Inventory: {existing.CanViewInventoryReport} → {model.CanViewInventoryReport}");
                if (existing.CanViewMaritimeReport != model.CanViewMaritimeReport) changes.Add($"View Maritime: {existing.CanViewMaritimeReport} → {model.CanViewMaritimeReport}");

                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    if (changes.Any())
                    {
                        AuditTrail auditTrail = new(username, $"Edited User Access for {existing.UserName}: {string.Join("; ", changes)}", "User Access", existing.Id);
                        await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);
                    }

                    existing.CanCreateServiceRequest = model.CanCreateServiceRequest;
                    existing.CanPostServiceRequest = model.CanPostServiceRequest;
                    existing.CanCreateDispatchTicket = model.CanCreateDispatchTicket;
                    existing.CanEditDispatchTicket = model.CanEditDispatchTicket;
                    existing.CanDeleteDispatchTicket = model.CanDeleteDispatchTicket;
                    existing.CanSetTariff = model.CanSetTariff;
                    existing.CanApproveTariff = model.CanApproveTariff;
                    existing.CanCreateBilling = model.CanCreateBilling;
                    existing.CanEditBilling = model.CanEditBilling;
                    existing.CanDeleteBilling = model.CanDeleteBilling;
                    existing.CanReverseBilling = model.CanReverseBilling;
                    existing.CanCreateCollection = model.CanCreateCollection;
                    existing.CanCreateJobOrder = model.CanCreateJobOrder;
                    existing.CanEditJobOrder = model.CanEditJobOrder;
                    existing.CanDeleteJobOrder = model.CanDeleteJobOrder;
                    existing.CanCloseJobOrder = model.CanCloseJobOrder;
                    existing.CanAccessTreasury = model.CanAccessTreasury;
                    existing.CanCreateDisbursement = model.CanCreateDisbursement;
                    existing.CanManageMsapImport = model.CanManageMsapImport;
                    existing.CanManageMaritimeMasterFile = model.CanManageMaritimeMasterFile;
                    existing.CanViewGeneralLedger = model.CanViewGeneralLedger;
                    existing.CanViewInventoryReport = model.CanViewInventoryReport;
                    existing.CanViewMaritimeReport = model.CanViewMaritimeReport;

                    await unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                return ServiceResult.Success("User access edited successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit user access.");
                return ServiceResult.Failure($"Failed to edit user access: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }
    }
}
