using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;

namespace IBS.Services.Msap
{
    public class TugMasterService(IUnitOfWork unitOfWork, ILogger<TugMasterService> logger) : ITugMasterService
    {
        public async Task<IEnumerable<TugMaster>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await unitOfWork.TugMaster.GetAllAsync(null, cancellationToken);
        }

        public async Task<TugMaster?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.TugMaster.GetAsync(t => t.TugMasterId == id, cancellationToken);
        }

        public async Task<ServiceResult<int>> CreateAsync(TugMaster model, string username, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.TugMaster.AddAsync(model, cancellationToken);
                
                var auditTrail = new AuditTrail(username, $"Created new Tug Master #{model.TugMasterNumber}", "Tug Master");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);
                
                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult<int>.Success(model.TugMasterId, "Tug Master created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Tug Master");
                return ServiceResult<int>.Failure($"Failed to create tug master: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateAsync(TugMaster model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var existingTugMaster = await unitOfWork.TugMaster.GetAsync(t => t.TugMasterId == model.TugMasterId, cancellationToken);
                if (existingTugMaster == null)
                {
                    return ServiceResult.Failure("Tug Master not found.", ServiceResultStatus.NotFound);
                }

                existingTugMaster.TugMasterNumber = model.TugMasterNumber;
                existingTugMaster.TugMasterName = model.TugMasterName;
                existingTugMaster.IsActive = model.IsActive;

                var auditTrail = new AuditTrail(username, $"Updated Tug Master #{model.TugMasterNumber}", "Tug Master");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Tug Master updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating Tug Master {TugMasterId}", model.TugMasterId);
                return ServiceResult.Failure($"Failed to update tug master: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var tugMaster = await unitOfWork.TugMaster.GetAsync(t => t.TugMasterId == id, cancellationToken);
                if (tugMaster == null)
                {
                    return ServiceResult.Failure("Tug Master not found.", ServiceResultStatus.NotFound);
                }

                await unitOfWork.TugMaster.RemoveAsync(tugMaster, cancellationToken);

                var auditTrail = new AuditTrail(username, $"Deleted Tug Master #{tugMaster.TugMasterNumber}", "Tug Master");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Tug Master deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting Tug Master {TugMasterId}", id);
                return ServiceResult.Failure($"Failed to delete tug master: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }
    }
}
