using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MMSI;
using IBS.Services.Attributes;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class TariffRateController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TariffRateController> _logger;

        public TariffRateController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager, ILogger<TariffRateController> logger)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var tariffRates = await _unitOfWork.TariffTable.GetAllAsync(null, cancellationToken);
            return View(tariffRates);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var model = new MMSITariffRate();
            model = await GetSelectLists(model, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSITariffRate model, CancellationToken cancellationToken = default)
        {
            model = await GetSelectLists(model, cancellationToken);
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            if (model.Dispatch <= 0 && model.BAF <= 0)
            {
                TempData["warning"] = "Dispatch and BAF value cannot be both zero.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var existingModel = await _unitOfWork.TariffTable
                    .GetAsync(t => t.AsOfDate == model.AsOfDate &&
                                   t.CustomerId == model.CustomerId &&
                                   t.TerminalId == model.TerminalId &&
                                   t.ServiceId == model.ServiceId, cancellationToken);

                if (existingModel != null)
                {
                    existingModel.Dispatch = model.Dispatch;
                    existingModel.BAF = model.BAF;
                    existingModel.DispatchDiscount = model.DispatchDiscount;
                    existingModel.BAFDiscount = model.BAFDiscount;
                    existingModel.UpdateBy = user?.UserName;
                    existingModel.UpdateDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model = existingModel;
                    await _unitOfWork.TariffTable.SaveAsync(cancellationToken);
                    TempData["success"] = "Tariff rate updated successfully.";
                }
                else
                {
                    model.CreatedBy = user?.UserName;
                    model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    await _unitOfWork.TariffTable.AddAsync(model, cancellationToken);
                    TempData["success"] = "Tariff rate created successfully.";
                }

                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create tariff rate.");
                TempData["error"] = ex.Message;
                model = await GetSelectLists(model, cancellationToken);
                model.Terminal = await _unitOfWork.Terminal.GetAsync(t => t.TerminalId == model.TerminalId, cancellationToken);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var model = await _unitOfWork.TariffTable.GetAsync(t => t.TariffRateId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            model = await GetSelectLists(model, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSITariffRate model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            var currentModel = await _unitOfWork.TariffTable.GetAsync(t => t.TariffRateId == model.TariffRateId, cancellationToken);

            if (currentModel == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                currentModel.AsOfDate = model.AsOfDate;
                currentModel.CustomerId = model.CustomerId;
                currentModel.ServiceId = model.ServiceId;
                currentModel.TerminalId = model.TerminalId;
                currentModel.Dispatch = model.Dispatch;
                currentModel.BAF = model.BAF;
                await _unitOfWork.TariffTable.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Entry edited successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update tariff rate.");
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken = default)
        {
            var terminals = await _unitOfWork.Terminal.GetAllAsync(t => t.PortId == portId, cancellationToken);

            var terminalsList = terminals.Select(t => new SelectListItem
            {
                Value = t.TerminalId.ToString(),
                Text = t.TerminalName
            }).ToList();

            return Json(terminalsList);
        }

        public async Task<MMSITariffRate> GetSelectLists(MMSITariffRate model, CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();
            model.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
            model.Ports = await _unitOfWork.Port.GetMMSIPortsSelectList(cancellationToken);
            model.Services = await _unitOfWork.Service.GetMMSIActivitiesServicesById(cancellationToken);
            if (model.TerminalId == 0) return model;
            model.Terminal = await _unitOfWork.Terminal.GetAsync(t => t.TerminalId == model.TerminalId, cancellationToken);
            model.Terminals = await _unitOfWork.Terminal.GetMMSITerminalsSelectList(model.Terminal!.PortId, cancellationToken);
            return model;
        }

        [HttpPost]
        public async Task<bool> CheckIfExisting(DateOnly date, int customerId, int terminalId, int activityServiceId, decimal dispatch, decimal baf, CancellationToken cancellationToken = default)
        {
            var model = await _unitOfWork.TariffTable
                .GetAsync(t => t.AsOfDate == date && t.CustomerId == customerId && t.TerminalId == terminalId && t.ServiceId == activityServiceId, cancellationToken);
            return (model != null);
        }
    }
}
