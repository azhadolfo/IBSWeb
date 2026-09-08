using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;

namespace IBS.Services.Msap
{
    public class VesselService(IUnitOfWork unitOfWork, ILogger<VesselService> logger) : IVesselService
    {
        public async Task<IEnumerable<Vessel>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await unitOfWork.Vessel.GetAllAsync(null, cancellationToken);
        }

        public async Task<Vessel?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.Vessel.GetAsync(v => v.VesselId == id, cancellationToken);
        }

        public async Task<ServiceResult<int>> CreateAsync(Vessel model, string username, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.Vessel.AddAsync(model, cancellationToken);

                var auditTrail = new AuditTrail(username, $"Created new Vessel #{model.VesselNumber}", "Vessel");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult<int>.Success(model.VesselId, "Vessel created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Vessel");
                return ServiceResult<int>.Failure($"Failed to create vessel: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateAsync(Vessel model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var existingVessel = await unitOfWork.Vessel.GetAsync(v => v.VesselId == model.VesselId, cancellationToken);
                if (existingVessel == null)
                {
                    return ServiceResult.Failure("Vessel not found.", ServiceResultStatus.NotFound);
                }

                var oldNumber = existingVessel.VesselNumber;
                existingVessel.VesselNumber = model.VesselNumber;
                existingVessel.VesselName = model.VesselName;
                existingVessel.VesselType = model.VesselType;

                var auditTrail = new AuditTrail(username, $"Updated Vessel #{oldNumber} to #{model.VesselNumber}", "Vessel");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Vessel updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating Vessel {VesselId}", model.VesselId);
                return ServiceResult.Failure($"Failed to update vessel: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var vessel = await unitOfWork.Vessel.GetAsync(v => v.VesselId == id, cancellationToken);
                if (vessel == null)
                {
                    return ServiceResult.Failure("Vessel not found.", ServiceResultStatus.NotFound);
                }

                await unitOfWork.Vessel.RemoveAsync(vessel, cancellationToken);

                var auditTrail = new AuditTrail(username, $"Deleted Vessel #{vessel.VesselNumber}", "Vessel");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Vessel deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting Vessel {VesselId}", id);
                return ServiceResult.Failure($"Failed to delete vessel: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }
    }
}
