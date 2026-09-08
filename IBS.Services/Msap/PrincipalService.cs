using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;

namespace IBS.Services.Msap
{
    public class PrincipalService(IUnitOfWork unitOfWork, ILogger<PrincipalService> logger) : IPrincipalService
    {
        public async Task<IEnumerable<Principal>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await unitOfWork.Principal.GetAllAsync(null, cancellationToken);
        }

        public async Task<Principal?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.Principal.GetAsync(p => p.PrincipalId == id, cancellationToken);
        }

        public async Task<Principal> PopulateSelectListsAsync(Principal? model, CancellationToken cancellationToken)
        {
            model ??= new Principal();
            model.CustomerSelectList = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
            return model;
        }

        public async Task<ServiceResult<int>> CreateAsync(Principal model, string username, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.Principal.AddAsync(model, cancellationToken);
                
                var auditTrail = new AuditTrail(username, $"Created new Principal #{model.PrincipalNumber}", "Principal");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);
                
                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult<int>.Success(model.PrincipalId, "Principal created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Principal");
                return ServiceResult<int>.Failure($"Failed to create principal: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateAsync(Principal model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var existingPrincipal = await unitOfWork.Principal.GetAsync(p => p.PrincipalId == model.PrincipalId, cancellationToken);
                if (existingPrincipal == null)
                {
                    return ServiceResult.Failure("Principal not found.", ServiceResultStatus.NotFound);
                }

                existingPrincipal.PrincipalNumber = model.PrincipalNumber;
                existingPrincipal.PrincipalName = model.PrincipalName;
                existingPrincipal.Agent = model.Agent;
                existingPrincipal.Address1 = model.Address1;
                existingPrincipal.Address2 = model.Address2;
                existingPrincipal.Address3 = model.Address3;
                existingPrincipal.BusinessType = model.BusinessType;
                existingPrincipal.Terms = model.Terms;
                existingPrincipal.TIN = model.TIN;
                existingPrincipal.Landline1 = model.Landline1;
                existingPrincipal.Landline2 = model.Landline2;
                existingPrincipal.Mobile1 = model.Mobile1;
                existingPrincipal.Mobile2 = model.Mobile2;
                existingPrincipal.IsActive = model.IsActive;
                existingPrincipal.IsVatable = model.IsVatable;
                existingPrincipal.CustomerId = model.CustomerId;

                var auditTrail = new AuditTrail(username, $"Updated Principal #{model.PrincipalNumber}", "Principal");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Principal updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating Principal {PrincipalId}", model.PrincipalId);
                return ServiceResult.Failure($"Failed to update principal: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var principal = await unitOfWork.Principal.GetAsync(p => p.PrincipalId == id, cancellationToken);
                if (principal == null)
                {
                    return ServiceResult.Failure("Principal not found.", ServiceResultStatus.NotFound);
                }

                await unitOfWork.Principal.RemoveAsync(principal, cancellationToken);

                var auditTrail = new AuditTrail(username, $"Deleted Principal #{principal.PrincipalNumber}", "Principal");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Principal deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting Principal {PrincipalId}", id);
                return ServiceResult.Failure($"Failed to delete principal: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }
    }
}
