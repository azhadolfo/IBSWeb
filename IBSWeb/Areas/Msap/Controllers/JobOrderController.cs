using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Msap;
using IBS.Models.Msap.ViewModels;
using IBS.Services.Msap;
using IBS.Services.Msap.AccessControl;
using IBS.Services.Msap.Attributes;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IBS.Utility.Constants;

namespace IBSWeb.Areas.Msap.Controllers
{
    /// <summary>
    /// Controller for managing Job Orders in the MMSI system.
    /// </summary>
    [Area("Msap")]
    public class JobOrderController(
        IUnitOfWork unitOfWork,
        JobOrderService jobOrderService,
        DispatchTicketService dispatchTicketService,
        ITerminalService terminalService,
        ICloudStorageService cloudStorageService,
        IAccessControlService accessControl,
        ILogger<JobOrderController> logger) : Controller
    {
        #region Index

        /// <summary>
        /// Displays the list of all Job Orders.
        /// </summary>
        [RequireAnyAccess(
            "Access denied. You don't have permission to access Job Orders.",
            ProcedureEnum.CreateJobOrder,
            ProcedureEnum.EditJobOrder,
            ProcedureEnum.DeleteJobOrder,
            ProcedureEnum.CloseJobOrder)]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.CanEdit = userId != null && await accessControl.HasAccessAsync(userId, ProcedureEnum.EditJobOrder);
            ViewBag.CanCreate = userId != null && await accessControl.HasAccessAsync(userId, ProcedureEnum.CreateJobOrder);
            return View();
        }

        /// <summary>
        /// Returns a paged, filtered, sorted list of Job Orders for DataTables server-side processing.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAnyAccess(
            "Access denied.",
            ProcedureEnum.CreateJobOrder,
            ProcedureEnum.EditJobOrder,
            ProcedureEnum.DeleteJobOrder,
            ProcedureEnum.CloseJobOrder)]
        public async Task<IActionResult> GetJobOrderList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var (data, filtered, total) = await jobOrderService.GetPagedJobOrdersAsync(parameters, cancellationToken);

                var closedMonths = (await unitOfWork.PostedPeriod.GetAllAsync(cancellationToken))
                    .Where(p => p.IsClosed)
                    .Select(p => (p.Year, p.Month))
                    .ToHashSet();
                foreach (var item in data)
                {
                    item.IsMonthClosed = closedMonths.Contains((item.Date.Year, item.Date.Month));
                }

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = total,
                    recordsFiltered = filtered,
                    data
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get job orders.");
                return Json(new { draw = parameters.Draw, error = ex.Message });
            }
        }

        #endregion

        #region Create

        /// <summary>
        /// Displays the form to create a new Job Order.
        /// </summary>
        [HttpGet]
        [RequireAccess(ProcedureEnum.CreateJobOrder, "Access denied. You don't have permission to create Job Orders.")]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = await jobOrderService.PopulateJobOrderViewModelAsync(null, cancellationToken);

            return View(viewModel);
        }

        /// <summary>
        /// Processes the creation of a new Job Order.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.CreateJobOrder, "Access denied. You don't have permission to create Job Orders.")]
        public async Task<IActionResult> Create(JobOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            var jobOrder = MapToEntity(viewModel);

            var result = await jobOrderService.CreateJobOrderAsync(jobOrder, User.Identity?.Name ?? "Unknown", cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Details), new { id = result.Data });
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "An error occurred.");
            await jobOrderService.PopulateJobOrderViewModelAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        #endregion

        #region Details

        /// <summary>
        /// Displays the details of a specific Job Order.
        /// </summary>
        [RequireAnyAccess(
            "Access denied. You don't have permission to view Job Orders.",
            ProcedureEnum.CreateJobOrder,
            ProcedureEnum.EditJobOrder,
            ProcedureEnum.DeleteJobOrder,
            ProcedureEnum.CloseJobOrder)]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var jobOrder = await jobOrderService.GetJobOrderByIdAsync(id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            var billing = await unitOfWork.Billing.GetAsync(b => b.JobOrderId == id, cancellationToken);
            ViewData["HasBilling"] = billing != null;
            ViewData["BillingNumber"] = billing?.MsapBillingNumber;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.CanSetTariff = userId != null && await accessControl.HasAccessAsync(userId, ProcedureEnum.SetTariff);
            ViewBag.CanApproveTariff = userId != null && await accessControl.HasAccessAsync(userId, ProcedureEnum.ApproveTariff);
            ViewBag.CanEditTicket = userId != null && await accessControl.HasAccessAsync(userId, ProcedureEnum.EditDispatchTicket);
            ViewBag.CanDeleteTicket = userId != null && await accessControl.HasAccessAsync(userId, ProcedureEnum.DeleteDispatchTicket);
            ViewBag.CanCreateDispatchTicket = userId != null && await accessControl.HasAccessAsync(userId, ProcedureEnum.CreateDispatchTicket);
            ViewBag.CanEditJobOrder = userId != null && await accessControl.HasAccessAsync(userId, ProcedureEnum.EditJobOrder);
            ViewBag.CanCreateServiceRequest = userId != null && await accessControl.HasAccessAsync(userId, ProcedureEnum.CreateServiceRequest);
            ViewBag.CanPostServiceRequest = userId != null && await accessControl.HasAccessAsync(userId, ProcedureEnum.PostServiceRequest);

            var ticketViewModel = await dispatchTicketService.PopulateDispatchTicketViewModelAsync(null, id, cancellationToken);
            ViewData["TicketViewModel"] = ticketViewModel;

            foreach (var ticket in jobOrder.DispatchTickets)
            {
                if (!string.IsNullOrEmpty(ticket.ImageName))
                {
                    try { ticket.ImageSignedUrl = await cloudStorageService.GetSignedUrlAsync(ticket.ImageName); }
                    catch (Exception ex) { logger.LogWarning(ex, "Image file not found: {Name}", ticket.ImageName); }
                }
                if (!string.IsNullOrEmpty(ticket.VideoName))
                {
                    try { ticket.VideoSignedUrl = await cloudStorageService.GetSignedUrlAsync(ticket.VideoName); }
                    catch (Exception ex) { logger.LogWarning(ex, "Video file not found: {Name}", ticket.VideoName); }
                }
            }

            return View(jobOrder);
        }

        #endregion

        #region Edit

        /// <summary>
        /// Displays the form to edit an existing Job Order.
        /// </summary>
        [HttpGet]
        [RequireAccess(ProcedureEnum.EditJobOrder, "Access denied. You don't have permission to edit Job Orders.")]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var jobOrder = await jobOrderService.GetJobOrderByIdAsync(id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            ViewData["JobOrderNumber"] = jobOrder.JobOrderNumber;

            // Prevent editing if the Job Order is already Closed.
            if (jobOrder.Status == SD.JobOrderStatus.Closed)
            {
                TempData["error"] = $"Job Order #{jobOrder.JobOrderNumber} is closed and cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var viewModel = new JobOrderViewModel
            {
                JobOrderId = jobOrder.JobOrderId,
                Date = jobOrder.Date,
                CustomerId = jobOrder.CustomerId,
                VesselId = jobOrder.VesselId,
                PortId = jobOrder.PortId,
                TerminalId = jobOrder.TerminalId,
                COSNumber = jobOrder.COSNumber,
                VoyageNumber = jobOrder.VoyageNumber,
                PlannedStartTime = jobOrder.PlannedStartTime,
                PlannedEndTime = jobOrder.PlannedEndTime,
                Remarks = jobOrder.Remarks
            };

            await jobOrderService.PopulateJobOrderViewModelAsync(viewModel, cancellationToken);
            ViewData["HasTickets"] = jobOrder.DispatchTickets.Any();

            return View(viewModel);
        }

        /// <summary>
        /// Processes the update of an existing Job Order.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.EditJobOrder, "Access denied. You don't have permission to edit Job Orders.")]
        public async Task<IActionResult> Edit(JobOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            var jobOrder = MapToEntity(viewModel);

            var result = await jobOrderService.UpdateJobOrderAsync(jobOrder, User.Identity?.Name ?? "Unknown", cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
            }

            if (result.Status == ServiceResultStatus.NotFound)
            {
                return NotFound();
            }

            var jo = await jobOrderService.GetJobOrderByIdAsync(viewModel.JobOrderId, cancellationToken);
            ViewData["JobOrderNumber"] = jo?.JobOrderNumber ?? "";
            ViewData["HasTickets"] = jo?.DispatchTickets.Any() ?? false;
            await jobOrderService.PopulateJobOrderViewModelAsync(viewModel, cancellationToken);
            ModelState.AddModelError(string.Empty, result.Message ?? "An error occurred.");
            return View(viewModel);
        }

        #endregion

        #region AJAX Endpoints

        /// <summary>
        /// Retrieves terminals associated with a specific port. Used for dynamic dropdowns.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess("Access denied.", ProcedureEnum.CreateJobOrder, ProcedureEnum.EditJobOrder)]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var list = await terminalService.GetTerminalsByPortAsync(portId, cancellationToken);
            return Json(list);
        }

        /// <summary>
        /// Retrieves detailed information for a specific Dispatch Ticket.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess("Access denied.", ProcedureEnum.CreateJobOrder, ProcedureEnum.EditJobOrder)]
        public async Task<IActionResult> GetTicketDetails(int id, CancellationToken cancellationToken)
        {
            var details = await dispatchTicketService.GetTicketDetailsAsync(id, cancellationToken);
            if (details == null)
            {
                return NotFound();
            }

            return Json(details);
        }

        /// <summary>
        /// Searches for customers matching a search term.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess("Access denied.", ProcedureEnum.CreateJobOrder, ProcedureEnum.EditJobOrder)]
        public async Task<JsonResult> SearchCustomers(string? term, CancellationToken cancellationToken)
        {
            var result = await unitOfWork.Customer.SearchCustomersDtoAsync(term ?? string.Empty, 10, cancellationToken);
            return Json(result);
        }

        /// <summary>
        /// Assigns a preferred tugboat to a Job Order.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAnyAccess("Access denied.", ProcedureEnum.EditJobOrder)]
        public async Task<IActionResult> AssignTugboat(int jobOrderId, int tugboatId, CancellationToken cancellationToken)
        {
            return await HandleTugboatAssignmentAsync(jobOrderId, tugboatId, true, cancellationToken);
        }

        /// <summary>
        /// Unassigns a tugboat from a Job Order.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAnyAccess("Access denied.", ProcedureEnum.EditJobOrder)]
        public async Task<IActionResult> UnassignTugboat(int jobOrderId, int tugboatId, CancellationToken cancellationToken)
        {
            return await HandleTugboatAssignmentAsync(jobOrderId, tugboatId, false, cancellationToken);
        }

        #endregion

        private static JobOrder MapToEntity(JobOrderViewModel viewModel)
        {
            return new JobOrder
            {
                JobOrderId = viewModel.JobOrderId,
                Date = viewModel.Date,
                CustomerId = viewModel.CustomerId,
                VesselId = viewModel.VesselId,
                PortId = viewModel.PortId,
                TerminalId = viewModel.TerminalId,
                COSNumber = viewModel.COSNumber,
                VoyageNumber = viewModel.VoyageNumber,
                PlannedStartTime = viewModel.PlannedStartTime,
                PlannedEndTime = viewModel.PlannedEndTime,
                Remarks = viewModel.Remarks
            };
        }

        private async Task<IActionResult> HandleTugboatAssignmentAsync(int jobOrderId, int tugboatId, bool isAssign, CancellationToken cancellationToken)
        {
            var result = isAssign
                ? await jobOrderService.AssignTugboatAsync(jobOrderId, tugboatId, User.Identity?.Name ?? "Unknown", cancellationToken)
                : await jobOrderService.UnassignTugboatAsync(jobOrderId, tugboatId, User.Identity?.Name ?? "Unknown", cancellationToken);

            if (result.IsSuccess)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = result.Message });
        }
    }
}
