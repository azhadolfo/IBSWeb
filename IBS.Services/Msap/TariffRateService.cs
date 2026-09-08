using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace IBS.Services.Msap
{
    public class TariffRateService(IUnitOfWork unitOfWork, ILogger<TariffRateService> logger) : ITariffRateService
    {
        public async Task<IEnumerable<TariffRate>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await unitOfWork.TariffTable.GetAllAsync(null, cancellationToken);
        }

        public async Task<TariffRate?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.TariffTable.GetAsync(t => t.TariffRateId == id, cancellationToken);
        }

        public async Task<TariffRate> PopulateSelectListsAsync(TariffRate? model, CancellationToken cancellationToken)
        {
            model ??= new TariffRate();
            model.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
            model.Ports = await unitOfWork.Port.GetMsapPortsSelectList(cancellationToken);
            model.Services = await unitOfWork.Service.GetMsapActivitiesServicesById(cancellationToken);

            if (model.TerminalId != 0)
            {
                var terminal = await unitOfWork.Terminal.GetAsync(t => t.TerminalId == model.TerminalId, cancellationToken);
                if (terminal != null)
                {
                    model.PortId = terminal.PortId;
                    model.Terminals = await unitOfWork.Terminal.GetMsapTerminalsSelectList(terminal.PortId, cancellationToken);
                }
            }
            else if (model.PortId != 0)
            {
                model.Terminals = await unitOfWork.Terminal.GetMsapTerminalsSelectList(model.PortId, cancellationToken);
            }
            else
            {
                model.Terminals = new List<SelectListItem>();
            }

            return model;
        }

        public async Task<List<SelectListItem>> GetTerminalsByPortAsync(int portId, CancellationToken cancellationToken)
        {
            var terminals = await unitOfWork.Terminal.GetAllAsync(t => t.PortId == portId, cancellationToken);
            return terminals.OrderBy(t => t.TerminalName).Select(t => new SelectListItem
            {
                Value = t.TerminalId.ToString(),
                Text = t.TerminalName
            }).ToList();
        }

        public async Task<ServiceResult<int>> UpsertAsync(TariffRate model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var existingModel = await unitOfWork.TariffTable
                    .GetAsync(t => t.AsOfDate == model.AsOfDate &&
                                   t.CustomerId == model.CustomerId &&
                                   t.TerminalId == model.TerminalId &&
                                   t.ServiceId == model.ServiceId,
                        cancellationToken);

                if (existingModel != null && model.TariffRateId == 0)
                {
                    existingModel.PortId = model.PortId;
                    existingModel.Dispatch = model.Dispatch;
                    existingModel.BAF = model.BAF;
                    existingModel.DispatchDiscount = model.DispatchDiscount;
                    existingModel.BAFDiscount = model.BAFDiscount;
                    existingModel.UpdateBy = username;
                    existingModel.UpdateDate = DateTimeHelper.GetCurrentPhilippineTime();

                    await RecordAuditAsync($"Updated existing Tariff Rate for Customer {existingModel.CustomerId}", username, cancellationToken);
                }
                else if (model.TariffRateId != 0)
                {
                    var current = await unitOfWork.TariffTable.GetAsync(t => t.TariffRateId == model.TariffRateId, cancellationToken);
                    if (current == null)
                    {
                        return ServiceResult<int>.Failure("Not found", ServiceResultStatus.NotFound);
                    }

                    current.AsOfDate = model.AsOfDate;
                    current.CustomerId = model.CustomerId;
                    current.ServiceId = model.ServiceId;
                    current.PortId = model.PortId;
                    current.TerminalId = model.TerminalId;
                    current.Dispatch = model.Dispatch;
                    current.BAF = model.BAF;
                    current.DispatchDiscount = model.DispatchDiscount;
                    current.BAFDiscount = model.BAFDiscount;
                    current.UpdateBy = username;
                    current.UpdateDate = DateTimeHelper.GetCurrentPhilippineTime();

                    await RecordAuditAsync($"Edited Tariff Rate #{current.TariffRateId}", username, cancellationToken);
                }
                else
                {
                    model.CreatedBy = username;
                    model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    await unitOfWork.TariffTable.AddAsync(model, cancellationToken);
                    await RecordAuditAsync($"Created new Tariff Rate", username, cancellationToken);
                }

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult<int>.Success(model.TariffRateId, "Tariff Rate saved successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving Tariff Rate");
                return ServiceResult<int>.Failure($"Failed to save tariff rate: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.TariffTable.GetAsync(t => t.TariffRateId == id, cancellationToken);
                if (model == null)
                {
                    return ServiceResult.Failure("Not found", ServiceResultStatus.NotFound);
                }

                await unitOfWork.TariffTable.RemoveAsync(model, cancellationToken);
                await RecordAuditAsync($"Deleted Tariff Rate #{id}", username, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Tariff Rate deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting Tariff Rate {TariffRateId}", id);
                return ServiceResult.Failure($"Failed to delete tariff rate: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        private async Task RecordAuditAsync(string activity, string username, CancellationToken cancellationToken)
        {
            var audit = new AuditTrail(username, activity, "Tariff Rate");
            await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);
        }
    }
}
