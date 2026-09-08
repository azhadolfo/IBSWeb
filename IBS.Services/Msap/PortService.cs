using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;

namespace IBS.Services.Msap
{
    public class PortService(IUnitOfWork unitOfWork, ILogger<PortService> logger) : IPortService
    {
        public async Task<IEnumerable<Port>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await unitOfWork.Port.GetAllAsync(cancellationToken: cancellationToken);
        }

        public async Task<Port?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.Port.GetAsync(p => p.PortId == id, cancellationToken);
        }

        public async Task<ServiceResult<int>> CreateAsync(Port model, string username, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.Port.AddAsync(model, cancellationToken);
                
                var auditTrail = new AuditTrail(username, $"Created new Port #{model.PortNumber}", "Port");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);
                
                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult<int>.Success(model.PortId, "Port created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Port");
                return ServiceResult<int>.Failure($"Failed to create port: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateAsync(Port model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var existingPort = await unitOfWork.Port.GetAsync(p => p.PortId == model.PortId, cancellationToken);
                if (existingPort == null)
                {
                    return ServiceResult.Failure("Port not found.", ServiceResultStatus.NotFound);
                }

                existingPort.PortNumber = model.PortNumber;
                existingPort.PortName = model.PortName;
                existingPort.HasSBMA = model.HasSBMA;

                var auditTrail = new AuditTrail(username, $"Updated Port #{model.PortNumber}", "Port");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Port updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating Port {PortId}", model.PortId);
                return ServiceResult.Failure($"Failed to update port: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var port = await unitOfWork.Port.GetAsync(p => p.PortId == id, cancellationToken);
                if (port == null)
                {
                    return ServiceResult.Failure("Port not found.", ServiceResultStatus.NotFound);
                }

                await unitOfWork.Port.RemoveAsync(port, cancellationToken);

                var auditTrail = new AuditTrail(username, $"Deleted Port #{port.PortNumber}", "Port");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Port deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting Port {PortId}", id);
                return ServiceResult.Failure($"Failed to delete port: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }
    }
}
