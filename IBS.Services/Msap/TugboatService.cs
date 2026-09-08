using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;

namespace IBS.Services.Msap
{
    public class TugboatService(IUnitOfWork unitOfWork, ILogger<TugboatService> logger) : ITugboatService
    {
        public async Task<IEnumerable<Tugboat>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await unitOfWork.Tugboat.GetAllAsync(null, cancellationToken);
        }

        public async Task<Tugboat?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.Tugboat.GetAsync(t => t.TugboatId == id, cancellationToken);
        }

        public async Task<Tugboat> PopulateSelectListsAsync(Tugboat? model, CancellationToken cancellationToken)
        {
            model ??= new Tugboat();
            model.PortList = await unitOfWork.Port.GetMsapPortsSelectList(cancellationToken);
            model.CompanyList = await unitOfWork.TugboatOwner.GetMsapTugboatOwnersSelectList(cancellationToken);
            return model;
        }

        public async Task<ServiceResult<int>> CreateAsync(Tugboat model, string username, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.Tugboat.AddAsync(model, cancellationToken);
                
                var auditTrail = new AuditTrail(username, $"Created new Tugboat #{model.TugboatNumber}", "Tugboat");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);
                
                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult<int>.Success(model.TugboatId, "Tugboat created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Tugboat");
                return ServiceResult<int>.Failure($"Failed to create tugboat: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateAsync(Tugboat model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var existingTugboat = await unitOfWork.Tugboat.GetAsync(t => t.TugboatId == model.TugboatId, cancellationToken);
                if (existingTugboat == null)
                {
                    return ServiceResult.Failure("Tugboat not found.", ServiceResultStatus.NotFound);
                }

                existingTugboat.TugboatNumber = model.TugboatNumber;
                existingTugboat.TugboatName = model.TugboatName;
                existingTugboat.IsCompanyOwned = model.IsCompanyOwned;
                existingTugboat.TugboatOwnerId = model.TugboatOwnerId;
                existingTugboat.PortId = model.PortId;

                var auditTrail = new AuditTrail(username, $"Updated Tugboat #{model.TugboatNumber}", "Tugboat");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Tugboat updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating Tugboat {TugboatId}", model.TugboatId);
                return ServiceResult.Failure($"Failed to update tugboat: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var tugboat = await unitOfWork.Tugboat.GetAsync(t => t.TugboatId == id, cancellationToken);
                if (tugboat == null)
                {
                    return ServiceResult.Failure("Tugboat not found.", ServiceResultStatus.NotFound);
                }

                await unitOfWork.Tugboat.RemoveAsync(tugboat, cancellationToken);

                var auditTrail = new AuditTrail(username, $"Deleted Tugboat #{tugboat.TugboatNumber}", "Tugboat");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Tugboat deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting Tugboat {TugboatId}", id);
                return ServiceResult.Failure($"Failed to delete tugboat: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }
    }
}
