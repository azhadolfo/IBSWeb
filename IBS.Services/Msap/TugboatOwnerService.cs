using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;

namespace IBS.Services.Msap
{
    public class TugboatOwnerService(IUnitOfWork unitOfWork, ILogger<TugboatOwnerService> logger) : ITugboatOwnerService
    {
        public async Task<IEnumerable<TugboatOwner>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await unitOfWork.TugboatOwner.GetAllAsync(null, cancellationToken);
        }

        public async Task<TugboatOwner?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.TugboatOwner.GetAsync(t => t.TugboatOwnerId == id, cancellationToken);
        }

        public async Task<ServiceResult<int>> CreateAsync(TugboatOwner model, string username, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.TugboatOwner.AddAsync(model, cancellationToken);
                
                var auditTrail = new AuditTrail(username, $"Created new Tugboat Owner #{model.TugboatOwnerNumber}", "Tugboat Owner");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);
                
                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult<int>.Success(model.TugboatOwnerId, "Tugboat Owner created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Tugboat Owner");
                return ServiceResult<int>.Failure($"Failed to create tugboat owner: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateAsync(TugboatOwner model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var existingOwner = await unitOfWork.TugboatOwner.GetAsync(t => t.TugboatOwnerId == model.TugboatOwnerId, cancellationToken);
                if (existingOwner == null)
                {
                    return ServiceResult.Failure("Tugboat Owner not found.", ServiceResultStatus.NotFound);
                }

                existingOwner.TugboatOwnerNumber = model.TugboatOwnerNumber;
                existingOwner.TugboatOwnerName = model.TugboatOwnerName;
                existingOwner.FixedRate = model.FixedRate;

                var auditTrail = new AuditTrail(username, $"Updated Tugboat Owner #{model.TugboatOwnerNumber}", "Tugboat Owner");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Tugboat Owner updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating Tugboat Owner {TugboatOwnerId}", model.TugboatOwnerId);
                return ServiceResult.Failure($"Failed to update tugboat owner: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var owner = await unitOfWork.TugboatOwner.GetAsync(t => t.TugboatOwnerId == id, cancellationToken);
                if (owner == null)
                {
                    return ServiceResult.Failure("Tugboat Owner not found.", ServiceResultStatus.NotFound);
                }

                await unitOfWork.TugboatOwner.RemoveAsync(owner, cancellationToken);

                var auditTrail = new AuditTrail(username, $"Deleted Tugboat Owner #{owner.TugboatOwnerNumber}", "Tugboat Owner");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Tugboat Owner deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting Tugboat Owner {TugboatOwnerId}", id);
                return ServiceResult.Failure($"Failed to delete tugboat owner: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }
    }
}
