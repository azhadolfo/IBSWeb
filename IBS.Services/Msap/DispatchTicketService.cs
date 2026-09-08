using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap;
using IBS.Models.Msap.ViewModels;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace IBS.Services.Msap
{
    public class DispatchTicketService(
        IUnitOfWork unitOfWork,
        ICloudStorageService cloudStorageService,
        ILogger<DispatchTicketService> logger)
    {
        public async Task<DispatchTicket?> GetDispatchTicketByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.DispatchTicket.GetDispatchTicketWithDetailsAsync(id, cancellationToken);
        }

        public async Task<ServiceRequestViewModel> PopulateDispatchTicketViewModelAsync(ServiceRequestViewModel? viewModel, int? jobOrderId, CancellationToken cancellationToken)
        {
            viewModel ??= new ServiceRequestViewModel();

            if (jobOrderId.HasValue)
            {
                var jobOrder = await unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == jobOrderId.Value, cancellationToken);
                if (jobOrder != null)
                {
                    viewModel.JobOrderId = jobOrderId;
                    viewModel.CustomerId = jobOrder.CustomerId;
                    viewModel.VesselId = jobOrder.VesselId;
                    viewModel.PortId = jobOrder.PortId;
                    viewModel.TerminalId = jobOrder.TerminalId;
                    viewModel.VoyageNumber = jobOrder.VoyageNumber;
                    viewModel.COSNumber = jobOrder.COSNumber;
                    viewModel.Date = jobOrder.Date;
                }
            }

            viewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);

            return viewModel;
        }

        public async Task<ServiceResult<int>> CreateDispatchTicketAsync(ServiceRequestViewModel viewModel, IFormFile? imageFile, IFormFile? videoFile, string username, CancellationToken cancellationToken)
        {
            try
            {
                if (!viewModel.JobOrderId.HasValue)
                {
                    return ServiceResult<int>.Failure("Dispatch tickets must be created under a Job Order.");
                }

                if (!await IsJobOrderEditableAsync(viewModel.JobOrderId, cancellationToken))
                {
                    return ServiceResult<int>.Failure("Cannot add ticket — parent Job Order is cancelled or closed.");
                }

                if (viewModel.JobOrderId.HasValue && await unitOfWork.DispatchTicket.GetAsync(dt => dt.JobOrderId == viewModel.JobOrderId && dt.Status == SD.DispatchTicketStatus.Billed, cancellationToken) != null)
                {
                    return ServiceResult<int>.Failure("Cannot add ticket — Job Order already has billed tickets.");
                }

                var model = viewModel.ToEntity();

                if (imageFile is { Length: > 0 })
                {
                    var ext = Path.GetExtension(imageFile.FileName);
                    var name = Path.GetFileNameWithoutExtension(imageFile.FileName);
                    model.ImageName = $"{name}-img-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{ext}";
                    model.ImageSavedUrl = await cloudStorageService.UploadFileAsync(imageFile, model.ImageName);
                }

                if (videoFile is { Length: > 0 })
                {
                    var ext = Path.GetExtension(videoFile.FileName);
                    var name = Path.GetFileNameWithoutExtension(videoFile.FileName);
                    model.VideoName = $"{name}-vid-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{ext}";
                    model.VideoSavedUrl = await cloudStorageService.UploadFileAsync(videoFile, model.VideoName);
                }

                model.CreatedBy = username;
                model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

                if (model.JobOrderId.HasValue)
                {
                    var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(model.JobOrderId.Value, cancellationToken);
                    if (jobOrder != null)
                    {
                        model.CustomerId = jobOrder.CustomerId;
                        model.VesselId = jobOrder.VesselId;
                        model.PortId = jobOrder.PortId;
                        model.TerminalId = jobOrder.TerminalId;
                        model.VoyageNumber = jobOrder.VoyageNumber;
                        model.COSNumber = jobOrder.COSNumber;
                        model.Date = jobOrder.Date;
                    }
                }

                var guard = await GuardClosedPeriodAsync(model.Date, cancellationToken);
                if (guard != null)
                {
                    return ServiceResult<int>.Failure(guard.Message!);
                }

                if (model is { DateLeft: not null, DateArrived: not null, TimeLeft: not null, TimeArrived: not null })
                {
                    var start = model.DateLeft.Value.ToDateTime(model.TimeLeft.Value);
                    var end = model.DateArrived.Value.ToDateTime(model.TimeArrived.Value);

                    if (end <= start)
                    {
                        return ServiceResult<int>.Failure("Arrival Date/Time must be strictly after Departure Date/Time.");
                    }

                    model.Status = SD.DispatchTicketStatus.ForTariff;
                    var duration = (decimal)(end - start).TotalHours;
                    model.TotalHours = Math.Round(Math.Max(duration, 1m), 2);
                }
                else
                {
                    model.Status = SD.DispatchTicketStatus.ForTariff;
                }

                await unitOfWork.DispatchTicket.AddAsync(model, cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Create dispatch ticket #{model.DispatchNumber}", "Dispatch Ticket", model.DispatchTicketId, model.DispatchNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult<int>.Success(model.DispatchTicketId, $"Dispatch Ticket #{model.DispatchNumber} was successfully created.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create dispatch ticket.");
                return ServiceResult<int>.Failure($"Failed to create dispatch ticket: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateDispatchTicketAsync(ServiceRequestViewModel viewModel, IFormFile? imageFile, IFormFile? videoFile, string username, CancellationToken cancellationToken)
        {
            try
            {
                if (!await IsTicketJobOrderEditableAsync(viewModel.DispatchTicketId!.Value, cancellationToken))
                {
                    return ServiceResult.Failure("Cannot edit ticket — parent Job Order is cancelled or closed.");
                }

                var currentModel = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == viewModel.DispatchTicketId, cancellationToken);
                if (currentModel == null)
                {
                    return ServiceResult.Failure("Ticket not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(currentModel.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                if (currentModel.JobOrderId.HasValue && await unitOfWork.DispatchTicket.GetAsync(dt => dt.JobOrderId == currentModel.JobOrderId && dt.Status == SD.DispatchTicketStatus.Billed, cancellationToken) != null)
                {
                    return ServiceResult.Failure("Cannot edit ticket — Job Order already has billed tickets.");
                }

                var originalTotalHours = currentModel.TotalHours;
                var changes = new List<string>();

                void AddChange(string name, object? oldVal, object? newVal)
                {
                    if (!Equals(oldVal, newVal))
                    {
                        changes.Add($"{name}: '{oldVal}' -> '{newVal}'");
                    }
                }

                if (imageFile != null)
                {
                    if (!string.IsNullOrEmpty(currentModel.ImageName))
                    {
                        await cloudStorageService.DeleteFileAsync(currentModel.ImageName);
                    }

                    var ext = Path.GetExtension(imageFile.FileName);
                    var name = Path.GetFileNameWithoutExtension(imageFile.FileName);
                    currentModel.ImageName = $"{name}-img-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{ext}";
                    currentModel.ImageSavedUrl = await cloudStorageService.UploadFileAsync(imageFile, currentModel.ImageName);
                    changes.Add("Image updated");
                }

                if (videoFile != null)
                {
                    if (!string.IsNullOrEmpty(currentModel.VideoName))
                    {
                        await cloudStorageService.DeleteFileAsync(currentModel.VideoName);
                    }

                    var ext = Path.GetExtension(videoFile.FileName);
                    var name = Path.GetFileNameWithoutExtension(videoFile.FileName);
                    currentModel.VideoName = $"{name}-vid-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{ext}";
                    currentModel.VideoSavedUrl = await cloudStorageService.UploadFileAsync(videoFile, currentModel.VideoName);
                    changes.Add("Video updated");
                }

                if (viewModel is { DateLeft: not null, DateArrived: not null, TimeLeft: not null, TimeArrived: not null })
                {
                    var departure = viewModel.DateLeft.Value.ToDateTime(viewModel.TimeLeft.Value);
                    var arrival = viewModel.DateArrived.Value.ToDateTime(viewModel.TimeArrived.Value);
                    if (arrival <= departure)
                    {
                        return ServiceResult.Failure("Date/Time Left cannot be later than Date/Time Arrived!");
                    }

                    var newTotalHours = Math.Round(Math.Max((decimal)(arrival - departure).TotalHours, 1m), 2);
                    AddChange("TotalHours", originalTotalHours, newTotalHours);
                    currentModel.TotalHours = newTotalHours;
                }

                AddChange(nameof(viewModel.Date), currentModel.Date, viewModel.Date);
                AddChange(nameof(viewModel.DispatchNumber), currentModel.DispatchNumber, viewModel.DispatchNumber);
                AddChange(nameof(viewModel.COSNumber), currentModel.COSNumber, viewModel.COSNumber);
                AddChange(nameof(viewModel.VoyageNumber), currentModel.VoyageNumber, viewModel.VoyageNumber);
                AddChange(nameof(viewModel.CustomerId), currentModel.CustomerId, viewModel.CustomerId);
                AddChange(nameof(viewModel.DateLeft), currentModel.DateLeft, viewModel.DateLeft);
                AddChange(nameof(viewModel.TimeLeft), currentModel.TimeLeft, viewModel.TimeLeft);
                AddChange(nameof(viewModel.DateArrived), currentModel.DateArrived, viewModel.DateArrived);
                AddChange(nameof(viewModel.TimeArrived), currentModel.TimeArrived, viewModel.TimeArrived);
                AddChange(nameof(viewModel.TerminalId), currentModel.TerminalId, viewModel.TerminalId);
                AddChange(nameof(viewModel.PortId), currentModel.PortId, viewModel.PortId);
                AddChange(nameof(viewModel.ServiceId), currentModel.ServiceId, viewModel.ServiceId);
                AddChange(nameof(viewModel.TugBoatId), currentModel.TugBoatId, viewModel.TugBoatId);
                AddChange(nameof(viewModel.TugMasterId), currentModel.TugMasterId, viewModel.TugMasterId);
                AddChange(nameof(viewModel.VesselId), currentModel.VesselId, viewModel.VesselId);
                AddChange(nameof(viewModel.Remarks), currentModel.Remarks, viewModel.Remarks);

                currentModel.Date = viewModel.Date;
                currentModel.DispatchNumber = viewModel.DispatchNumber;
                currentModel.COSNumber = viewModel.COSNumber;
                currentModel.VoyageNumber = viewModel.VoyageNumber;
                currentModel.CustomerId = viewModel.CustomerId;
                currentModel.DateLeft = viewModel.DateLeft;
                currentModel.TimeLeft = viewModel.TimeLeft;
                currentModel.DateArrived = viewModel.DateArrived;
                currentModel.TimeArrived = viewModel.TimeArrived;
                currentModel.TerminalId = viewModel.TerminalId;
                currentModel.PortId = viewModel.PortId;
                currentModel.ServiceId = viewModel.ServiceId;
                currentModel.TugBoatId = viewModel.TugBoatId;
                currentModel.TugMasterId = viewModel.TugMasterId;
                currentModel.VesselId = viewModel.VesselId;
                currentModel.Remarks = viewModel.Remarks;
                currentModel.JobOrderId = viewModel.JobOrderId;
                currentModel.EditedBy = username;
                currentModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                var criticalFieldsChanged = changes.Any(c =>
                    c.StartsWith("ServiceId:") ||
                    c.StartsWith("TugBoatId:") ||
                    c.StartsWith("DateLeft:") ||
                    c.StartsWith("TimeLeft:") ||
                    c.StartsWith("DateArrived:") ||
                    c.StartsWith("TimeArrived:") ||
                    c.StartsWith("TotalHours:"));

                if (criticalFieldsChanged)
                {
                    currentModel.Status = SD.DispatchTicketStatus.ForTariff;
                    currentModel.DispatchRate = 0;
                    currentModel.DispatchBillingAmount = 0;
                    currentModel.DispatchDiscount = 0;
                    currentModel.DispatchNetRevenue = 0;
                    currentModel.BAFRate = 0;
                    currentModel.BAFBillingAmount = 0;
                    currentModel.BAFDiscount = 0;
                    currentModel.BAFNetRevenue = 0;
                    currentModel.TotalBilling = 0;
                    currentModel.TotalNetRevenue = 0;
                    currentModel.ApOtherTugs = 0;
                    currentModel.TariffBy = string.Empty;
                    currentModel.TariffDate = default;
                    currentModel.TariffEditedBy = string.Empty;
                    currentModel.TariffEditedDate = null;
                    changes.Add("Tariff data reset due to critical field change");
                }

                var auditMessage = changes.Any()
                    ? $"Edit dispatch ticket #{currentModel.DispatchNumber}, {string.Join(", ", changes)}"
                    : $"No changes detected for #{currentModel.DispatchNumber}";

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, auditMessage, "Dispatch Ticket", currentModel.DispatchTicketId, currentModel.DispatchNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Entry edited successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit ticket.");
                return ServiceResult.Failure($"Failed to edit ticket: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> SaveTariffAsync(DispatchTicket model, string chargeType, string chargeType2, string username, bool isEdit, CancellationToken cancellationToken)
        {
            try
            {
                if (!await IsTicketJobOrderEditableAsync(model.DispatchTicketId, cancellationToken))
                {
                    return ServiceResult.Failure("Cannot set/edit tariff — parent Job Order is cancelled or closed.");
                }

                var currentModel = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);
                if (currentModel == null)
                {
                    return ServiceResult.Failure("Ticket not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(currentModel.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                string auditMessage;
                string documentType = "Tariff";
                if (isEdit)
                {
                    var changes = new List<string>();
                    void AddChange(string name, object? oldVal, object? newVal)
                    {
                        if (!Equals(oldVal, newVal))
                        {
                            changes.Add($"{name}: {oldVal} -> {newVal}");
                        }
                    }

                    AddChange("CustomerId", currentModel.CustomerId, model.CustomerId);
                    AddChange("DispatchChargeType", currentModel.DispatchChargeType, chargeType);
                    AddChange("BAFChargeType", currentModel.BAFChargeType, chargeType2);
                    AddChange("DispatchRate", currentModel.DispatchRate, model.DispatchRate);
                    AddChange("BAFRate", currentModel.BAFRate, model.BAFRate);
                    AddChange("DispatchDiscount", currentModel.DispatchDiscount, model.DispatchDiscount);
                    AddChange("BAFDiscount", currentModel.BAFDiscount, model.BAFDiscount);
                    AddChange("ApOtherTugs", currentModel.ApOtherTugs, model.ApOtherTugs);

                    currentModel.TariffEditedBy = username;
                    currentModel.TariffEditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    auditMessage = changes.Any() ? $"Edit tariff #{currentModel.DispatchNumber} {string.Join(", ", changes)}" : $"No changes detected for tariff details #{currentModel.DispatchNumber}";
                }
                else
                {
                    currentModel.TariffBy = username;
                    currentModel.TariffDate = DateTimeHelper.GetCurrentPhilippineTime();
                    auditMessage = $"Set Tariff #{currentModel.DispatchNumber}";
                }

                decimal dispatchRate = model.DispatchRate;
                decimal dispatchDiscountPercent = model.DispatchDiscount;
                decimal dispatchDiscountAmount = dispatchRate * (dispatchDiscountPercent / 100);
                decimal bafRate = model.BAFRate;
                decimal bafDiscountPercent = model.BAFDiscount;
                decimal bafDiscountAmount = bafRate * (bafDiscountPercent / 100);

                decimal dispatchBilling;
                decimal dispatchRevenue;
                decimal bafBilling;
                decimal bafRevenue;

                var hours = Math.Round(currentModel.TotalHours, 2);

                if (chargeType == "Per hour")
                {
                    dispatchBilling = dispatchRate * hours;
                    dispatchRevenue = (dispatchRate - dispatchDiscountAmount) * hours;
                }
                else
                {
                    dispatchBilling = dispatchRate;
                    dispatchRevenue = dispatchRate - dispatchDiscountAmount;
                }

                if (chargeType2 == "Per hour")
                {
                    bafBilling = bafRate * hours;
                    bafRevenue = (bafRate - bafDiscountAmount) * hours;
                }
                else
                {
                    bafBilling = bafRate;
                    bafRevenue = bafRate - bafDiscountAmount;
                }

                currentModel.Status = SD.DispatchTicketStatus.ForApproval;
                currentModel.CustomerId = model.CustomerId;
                currentModel.DispatchChargeType = chargeType;
                currentModel.BAFChargeType = chargeType2;
                currentModel.DispatchRate = dispatchRate;
                currentModel.BAFRate = bafRate;
                currentModel.DispatchDiscount = dispatchDiscountPercent;
                currentModel.BAFDiscount = bafDiscountPercent;
                currentModel.DispatchBillingAmount = Math.Round(dispatchBilling, 2);
                currentModel.BAFBillingAmount = Math.Round(bafBilling, 2);
                currentModel.DispatchNetRevenue = Math.Round(dispatchRevenue, 2);
                currentModel.BAFNetRevenue = Math.Round(bafRevenue, 2);
                currentModel.ApOtherTugs = model.ApOtherTugs;
                currentModel.TotalBilling = Math.Round(dispatchBilling + bafBilling, 2);
                currentModel.TotalNetRevenue = Math.Round(dispatchRevenue + bafRevenue, 2);

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, auditMessage, documentType, currentModel.DispatchTicketId, currentModel.DispatchNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success(isEdit ? "Tariff updated successfully." : "Tariff set successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save tariff for ticket {DispatchTicketId}", model.DispatchTicketId);
                return ServiceResult.Failure($"Failed to save tariff: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> ApproveTariffAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                if (!await IsTicketJobOrderEditableAsync(id, cancellationToken))
                {
                    return ServiceResult.Failure("Cannot approve tariff — parent Job Order is cancelled or closed.");
                }

                var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
                if (model == null)
                {
                    return ServiceResult.Failure("Ticket not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(model.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                model.Status = SD.DispatchTicketStatus.ForBilling;
                model.EditedBy = username;
                model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Approved tariff for dispatch ticket #{model.DispatchNumber}", "Dispatch Ticket", model.DispatchTicketId, model.DispatchNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Tariff approved successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to approve tariff.");
                return ServiceResult.Failure($"Failed to approve tariff: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DisapproveTariffAsync(int id, string reason, string username, CancellationToken cancellationToken)
        {
            try
            {
                if (!await IsTicketJobOrderEditableAsync(id, cancellationToken))
                {
                    return ServiceResult.Failure("Cannot disapprove tariff — parent Job Order is cancelled or closed.");
                }

                if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
                {
                    return ServiceResult.Failure("Please provide a detailed reason (at least 10 characters)");
                }

                var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
                if (model == null)
                {
                    return ServiceResult.Failure("Ticket not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(model.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                model.Status = SD.DispatchTicketStatus.Disapproved;
                model.EditedBy = username;
                model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Remarks = string.IsNullOrEmpty(model.Remarks)
                    ? $"Disapproved: {reason}"
                    : $"{model.Remarks} | Disapproved: {reason}";

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Disapproved tariff for dispatch ticket #{model.DispatchNumber}. Reason: {reason}", "Dispatch Ticket", model.DispatchTicketId, model.DispatchNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Tariff disapproved successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to disapprove tariff.");
                return ServiceResult.Failure($"Failed to disapprove tariff: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        #region Batch Operations

        public async Task<ServiceResult> BatchApproveTariffAsync(List<int> ids, string username, CancellationToken cancellationToken)
        {
            try
            {
                var tickets = (await unitOfWork.DispatchTicket.GetAllAsync(
                    dt => ids.Contains(dt.DispatchTicketId), cancellationToken)).ToList();

                foreach (var ticket in tickets)
                {
                    var guard = await GuardClosedPeriodAsync(ticket.Date, cancellationToken);
                    if (guard != null)
                    {
                        return guard;
                    }
                }

                var toApprove = tickets.Where(t => t.Status == SD.DispatchTicketStatus.ForApproval).ToList();
                if (toApprove.Count == 0)
                {
                    return ServiceResult.Failure("No tickets in 'For Approval' status found among the selected.");
                }

                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    foreach (var ticket in toApprove)
                    {
                        ticket.Status = SD.DispatchTicketStatus.ForBilling;
                        ticket.EditedBy = username;
                        ticket.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                        await unitOfWork.AuditTrail.AddAsync(
                            new AuditTrail(username, $"Batch approved tariff for dispatch ticket #{ticket.DispatchNumber}", "Dispatch Ticket", ticket.DispatchTicketId, ticket.DispatchNumber),
                            cancellationToken);
                    }

                    foreach (var skipped in tickets.Where(t => t.Status != SD.DispatchTicketStatus.ForApproval))
                    {
                        await unitOfWork.AuditTrail.AddAsync(
                            new AuditTrail(username, $"Batch approve skipped for #{skipped.DispatchNumber} — current status is '{skipped.Status}'", "Dispatch Ticket", skipped.DispatchTicketId, skipped.DispatchNumber),
                            cancellationToken);
                    }

                    await unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                var skippedCount = tickets.Count - toApprove.Count;
                var msg = $"{toApprove.Count} tariff(s) approved successfully.";
                if (skippedCount > 0)
                {
                    msg += $" {skippedCount} ticket(s) skipped (not in 'For Approval' status).";
                }

                return ServiceResult.Success(msg);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to batch approve tariffs.");
                return ServiceResult.Failure($"Failed to batch approve tariffs: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> BatchSetTariffAsync(List<int> ids, decimal dispatchRate, decimal bafRate, string chargeType, string chargeType2, string username, CancellationToken cancellationToken)
        {
            try
            {
                var tickets = (await unitOfWork.DispatchTicket.GetAllAsync(
                    dt => ids.Contains(dt.DispatchTicketId), cancellationToken)).ToList();

                foreach (var ticket in tickets)
                {
                    var guard = await GuardClosedPeriodAsync(ticket.Date, cancellationToken);
                    if (guard != null)
                    {
                        return guard;
                    }
                }

                var toSet = tickets.Where(t => t.Status is SD.DispatchTicketStatus.ForTariff).ToList();
                if (toSet.Count == 0)
                {
                    return ServiceResult.Failure("No tickets eligible for tariff setting (status must be 'For Tariff').");
                }

                var now = DateTimeHelper.GetCurrentPhilippineTime();

                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    foreach (var ticket in toSet)
                    {
                        var hours = Math.Round(ticket.TotalHours, 2);

                        decimal dispatchBilling = chargeType == "Per hour" ? dispatchRate * hours : dispatchRate;
                        decimal dispatchRevenue = chargeType == "Per hour"
                            ? dispatchRate * hours
                            : dispatchRate;

                        decimal bafBilling = chargeType2 == "Per hour" ? bafRate * hours : bafRate;
                        decimal bafRevenue = chargeType2 == "Per hour"
                            ? bafRate * hours
                            : bafRate;

                        ticket.Status = SD.DispatchTicketStatus.ForApproval;
                        ticket.DispatchChargeType = chargeType;
                        ticket.BAFChargeType = chargeType2;
                        ticket.DispatchRate = dispatchRate;
                        ticket.BAFRate = bafRate;
                        ticket.DispatchDiscount = 0;
                        ticket.BAFDiscount = 0;
                        ticket.DispatchBillingAmount = Math.Round(dispatchBilling, 2);
                        ticket.BAFBillingAmount = Math.Round(bafBilling, 2);
                        ticket.DispatchNetRevenue = Math.Round(dispatchRevenue, 2);
                        ticket.BAFNetRevenue = Math.Round(bafRevenue, 2);
                        ticket.TotalBilling = Math.Round(dispatchBilling + bafBilling, 2);
                        ticket.TotalNetRevenue = Math.Round(dispatchRevenue + bafRevenue, 2);
                        ticket.TariffBy = username;
                        ticket.TariffDate = now;

                        await unitOfWork.AuditTrail.AddAsync(
                            new AuditTrail(username, $"Batch set tariff for #{ticket.DispatchNumber}: Dispatch={dispatchRate:N2}, BAF={bafRate:N2}, Total={ticket.TotalBilling:N2}", "Tariff", ticket.DispatchTicketId, ticket.DispatchNumber),
                            cancellationToken);
                    }

                    foreach (var skipped in tickets.Where(t => t.Status is not SD.DispatchTicketStatus.ForTariff))
                    {
                        await unitOfWork.AuditTrail.AddAsync(
                            new AuditTrail(username, $"Batch tariff skipped for #{skipped.DispatchNumber} — status is '{skipped.Status}'", "Tariff", skipped.DispatchTicketId, skipped.DispatchNumber),
                            cancellationToken);
                    }

                    await unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                var skippedCount = tickets.Count - toSet.Count;
                var msg = $"{toSet.Count} tariff(s) set successfully.";
                if (skippedCount > 0)
                {
                    msg += $" {skippedCount} ticket(s) skipped (not eligible).";
                }

                return ServiceResult.Success(msg);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to batch set tariffs.");
                return ServiceResult.Failure($"Failed to batch set tariffs: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        #endregion

        public async Task<ServiceResult> DeleteTicketAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                if (!await IsTicketJobOrderEditableAsync(id, cancellationToken))
                {
                    return ServiceResult.Failure("Cannot delete ticket — parent Job Order is closed or cancelled.");
                }

                var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
                if (model == null)
                {
                    return ServiceResult.Failure("Ticket not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(model.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                if (model.Status != SD.DispatchTicketStatus.ForTariff &&
                    model.Status != SD.DispatchTicketStatus.Disapproved)
                {
                    return ServiceResult.Failure($"Cannot delete ticket — status is '{model.Status}' and must be disapproved or have no tariff applied.");
                }

                model.Status = SD.DispatchTicketStatus.Deleted;
                model.EditedBy = username;
                model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Deleted dispatch ticket #{model.DispatchNumber}", "Dispatch Ticket", model.DispatchTicketId, model.DispatchNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success($"Dispatch Ticket #{model.DispatchNumber} deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete ticket.");
                return ServiceResult.Failure($"Failed to delete ticket: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> RestoreTicketAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                if (!await IsTicketJobOrderEditableAsync(id, cancellationToken))
                {
                    return ServiceResult.Failure("Cannot restore ticket — parent Job Order is closed or cancelled.");
                }

                var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
                if (model == null)
                {
                    return ServiceResult.Failure("Ticket not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(model.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                if (model.Status != SD.DispatchTicketStatus.Deleted)
                {
                    return ServiceResult.Failure("Ticket is not in a deleted state.");
                }

                model.Status = SD.DispatchTicketStatus.ForTariff;

                model.EditedBy = username;
                model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Restored dispatch ticket #{model.DispatchNumber}", "Dispatch Ticket", model.DispatchTicketId, model.DispatchNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success($"Dispatch Ticket #{model.DispatchNumber} restored successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to restore ticket.");
                return ServiceResult.Failure($"Failed to restore ticket: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DeleteImageAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
                if (model == null)
                {
                    return ServiceResult.Failure("Ticket not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(model.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                if (!string.IsNullOrEmpty(model.ImageName))
                {
                    await cloudStorageService.DeleteFileAsync(model.ImageName);
                }

                model.ImageName = null;
                model.ImageSavedUrl = null;
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Deleted image on dispatch ticket #{model.DispatchNumber}", "Dispatch Ticket", model.DispatchTicketId, model.DispatchNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Image Deleted Successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete image.");
                return ServiceResult.Failure($"Failed to delete image: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DeleteVideoAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
                if (model == null)
                {
                    return ServiceResult.Failure("Ticket not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(model.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                if (!string.IsNullOrEmpty(model.VideoName))
                {
                    await cloudStorageService.DeleteFileAsync(model.VideoName);
                }

                model.VideoName = null;
                model.VideoSavedUrl = null;
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Deleted video on dispatch ticket #{model.DispatchNumber}", "Dispatch Ticket", model.DispatchTicketId, model.DispatchNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Video Deleted Successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete video.");
                return ServiceResult.Failure($"Failed to delete video: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<(IEnumerable<DispatchTicket> Data, int RecordsFiltered, int TotalRecords)> GetPagedDispatchTicketsAsync(DataTablesParameters parameters, string filterType, CancellationToken cancellationToken)
        {
            return await unitOfWork.DispatchTicket.GetPagedDispatchTicketsAsync(parameters, filterType, cancellationToken);
        }

        public async Task<ServiceResult<object>> CheckForTariffRateAsync(int customerId, int dispatchTicketId, CancellationToken cancellationToken)
        {
            var dispatchModel = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == dispatchTicketId, cancellationToken);
            if (dispatchModel == null)
            {
                return ServiceResult<object>.Failure("Ticket not found.", ServiceResultStatus.NotFound);
            }

            var tariffRate =
                await unitOfWork.TariffTable.GetAsync(t =>
                        t.CustomerId == customerId &&
                        t.TerminalId == dispatchModel.TerminalId &&
                        t.ServiceId == dispatchModel.ServiceId &&
                        t.AsOfDate <= dispatchModel.DateLeft,
                    cancellationToken)
                ??
                await unitOfWork.TariffTable.GetAsync(t =>
                        t.CustomerId == customerId &&
                        t.TerminalId == dispatchModel.TerminalId &&
                        t.AsOfDate <= dispatchModel.DateLeft,
                    cancellationToken)
                ??
                await unitOfWork.TariffTable.GetAsync(t =>
                        t.CustomerId == customerId &&
                        t.AsOfDate <= dispatchModel.DateLeft,
                    cancellationToken);

            if (tariffRate != null)
            {
                return ServiceResult<object>.Success(new
                {
                    tariffRate.Dispatch,
                    tariffRate.BAF,
                    tariffRate.DispatchDiscount,
                    tariffRate.BAFDiscount,
                    Exists = true
                });
            }

            return ServiceResult<object>.Success(new { Exists = false });
        }

        public async Task<bool> IsJobOrderEditableAsync(int? jobOrderId, CancellationToken cancellationToken)
        {
            return await unitOfWork.DispatchTicket.IsJobOrderEditableAsync(jobOrderId, cancellationToken);
        }

        private async Task<bool> IsTicketJobOrderEditableAsync(int dispatchTicketId, CancellationToken cancellationToken)
        {
            var ticket = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == dispatchTicketId, cancellationToken);
            return ticket != null && await IsJobOrderEditableAsync(ticket.JobOrderId, cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCustomerSelectListAsync(CancellationToken cancellationToken)
        {
            return await unitOfWork.GetCustomerListAsyncById(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetTerminalsByPortAsync(int portId, CancellationToken cancellationToken)
        {
            var terminals = await unitOfWork.Terminal.GetAllAsync(t => t.PortId == portId, cancellationToken);
            return terminals.Select(t => new SelectListItem
            {
                Value = t.TerminalId.ToString(),
                Text = t.TerminalName
            }).ToList();
        }

        public async Task<ServiceRequestViewModel> PopulateSelectListsAsync(ServiceRequestViewModel viewModel, CancellationToken cancellationToken)
        {
            viewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
            return viewModel;
        }

        public async Task<IEnumerable<DispatchTicket>> GetDispatchTicketsByFilterAsync(string status, CancellationToken cancellationToken)
        {
            return status == "All"
                ? await unitOfWork.DispatchTicket.GetAllAsync(dt => dt.Status != "For Posting", cancellationToken)
                : await unitOfWork.DispatchTicket.GetAllAsync(dt => dt.Status == status, cancellationToken);
        }

        public async Task<object?> GetTicketDetailsAsync(int id, CancellationToken cancellationToken)
        {
            var ticket = await unitOfWork.DispatchTicket.GetDispatchTicketWithDetailsAsync(id, cancellationToken);
            if (ticket == null)
            {
                return null;
            }

            return new
            {
                id = ticket.DispatchTicketId,
                dispatchNumber = ticket.DispatchNumber,
                date = ticket.Date.ToString("MMM dd, yyyy"),
                serviceName = ticket.Service.ServiceName,
                tugboatName = ticket.Tugboat.TugboatName,
                tugMasterName = ticket.TugMaster?.TugMasterName,
                location = $"{ticket.Terminal.Port.PortName} - {ticket.Terminal.TerminalName}",
                timeStart = ticket is { DateLeft: not null, TimeLeft: not null }
                    ? $"{ticket.DateLeft.Value:MMM dd, yyyy} {ticket.TimeLeft.Value:HH:mm}"
                    : "-",
                timeEnd = ticket is { DateArrived: not null, TimeArrived: not null }
                    ? $"{ticket.DateArrived.Value:MMM dd, yyyy} {ticket.TimeArrived.Value:HH:mm}"
                    : "-",
                remarks = ticket.Remarks ?? "No remarks",
                status = ticket.Status,
                totalHours = ticket.TotalHours.ToString("N2"),
                dispatchRate = ticket.DispatchRate.ToString("N2"),
                dispatchDiscount = ticket.DispatchDiscount.ToString("N2"),
                dispatchBilling = ticket.DispatchBillingAmount.ToString("N2"),
                bafRate = ticket.BAFRate.ToString("N2"),
                bafDiscount = ticket.BAFDiscount.ToString("N2"),
                bafBilling = ticket.BAFBillingAmount.ToString("N2"),
                totalBilling = ticket.TotalBilling.ToString("N2"),
                totalNetRevenue = ticket.TotalNetRevenue.ToString("N2")
            };
        }

        private async Task<ServiceResult?> GuardClosedPeriodAsync(DateOnly date, CancellationToken ct)
        {
            if (await unitOfWork.PostedPeriod.IsMonthClosedAsync(date.Year, date.Month, ct))
            {
                return ServiceResult.Failure($"Cannot modify: {date:MMMM yyyy} is closed.");
            }

            return null;
        }
    }
}
