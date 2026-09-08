using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;

namespace IBS.Services.Msap
{
    public class TerminalService(IUnitOfWork unitOfWork, ILogger<TerminalService> logger) : ITerminalService
    {
        public async Task<IEnumerable<Terminal>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await unitOfWork.Terminal.GetAllAsync(null, cancellationToken);
        }

        public async Task<Terminal?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.Terminal.GetAsync(t => t.TerminalId == id, cancellationToken);
        }

        public async Task<Terminal> PopulateSelectListsAsync(Terminal? model, CancellationToken cancellationToken)
        {
            model ??= new Terminal();
            model.Ports = await unitOfWork.Port.GetMsapPortsSelectList(cancellationToken);
            return model;
        }

        public async Task<ServiceResult<int>> CreateAsync(Terminal model, string username, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.Terminal.AddAsync(model, cancellationToken);
                
                var auditTrail = new AuditTrail(username, $"Created new Terminal #{model.TerminalNumber}", "Terminal");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);
                
                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult<int>.Success(model.TerminalId, "Terminal created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Terminal");
                return ServiceResult<int>.Failure($"Failed to create terminal: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateAsync(Terminal model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var existingTerminal = await unitOfWork.Terminal.GetAsync(t => t.TerminalId == model.TerminalId, cancellationToken);
                if (existingTerminal == null)
                {
                    return ServiceResult.Failure("Terminal not found.", ServiceResultStatus.NotFound);
                }

                existingTerminal.TerminalNumber = model.TerminalNumber;
                existingTerminal.TerminalName = model.TerminalName;
                existingTerminal.PortId = model.PortId;

                var auditTrail = new AuditTrail(username, $"Updated Terminal #{model.TerminalNumber}", "Terminal");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Terminal updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating Terminal {TerminalId}", model.TerminalId);
                return ServiceResult.Failure($"Failed to update terminal: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var terminal = await unitOfWork.Terminal.GetAsync(t => t.TerminalId == id, cancellationToken);
                if (terminal == null)
                {
                    return ServiceResult.Failure("Terminal not found.", ServiceResultStatus.NotFound);
                }

                await unitOfWork.Terminal.RemoveAsync(terminal, cancellationToken);

                var auditTrail = new AuditTrail(username, $"Deleted Terminal #{terminal.TerminalNumber}", "Terminal");
                await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Terminal deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting Terminal {TerminalId}", id);
                return ServiceResult.Failure($"Failed to delete terminal: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<IEnumerable<object>> GetTerminalsByPortAsync(int portId, CancellationToken cancellationToken)
        {
            var terminals = await unitOfWork.Terminal.GetAllAsync(t => t.PortId == portId, cancellationToken);
            return terminals
                .OrderBy(t => t.TerminalName)
                .Select(t => new
                {
                    value = t.TerminalId.ToString(),
                    text = t.TerminalName
                });
        }
    }
}
