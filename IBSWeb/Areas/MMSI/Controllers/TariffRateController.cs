using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI;
using IBS.Models.MMSI;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace IBSWeb.Areas.MMSI
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class TariffRateController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly DispatchTicketRepository _dispatchTicketRepository;
        private readonly UserManager<IdentityUser> _userManager;

        public TariffRateController(ApplicationDbContext dbContext, DispatchTicketRepository dispatchTicketRepository, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _dispatchTicketRepository = dispatchTicketRepository;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var tariffRates = await _dbContext.MMSITariffRates
                .Include(t => t.Customer)
                .Include(t => t.Terminal).ThenInclude(t => t.Port)
                .Include(t => t.ActivityService)
                .ToListAsync(cancellationToken);

            return View(tariffRates);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            MMSITariffRate model = new MMSITariffRate();
            model = await GetSelectLists(model, cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSITariffRate model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model.Dispatch <= 0)
                    {
                        throw new Exception("Dispatch value cannot be zero.");
                    }

                    var user = await _userManager.GetUserAsync(User);
                    var existingModel = await _dbContext.MMSITariffRates
                        .Where(t => t.AsOfDate == model.AsOfDate && t.CustomerId == model.CustomerId && t.TerminalId == model.TerminalId && t.ActivityServiceId == model.ActivityServiceId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existingModel != null)
                    {
                        existingModel.Dispatch = model.Dispatch;
                        existingModel.BAF = model.BAF;
                        existingModel.UpdateBy = user?.UserName;
                        existingModel.UpdateDate = DateTime.Now;
                        model = existingModel;
                        model.Terminal = default;
                        TempData["success"] = "Tariff rate updated successfully.";
                    }
                    else
                    {
                        model.CreatedBy = user?.UserName;
                        model.CreatedDate = DateTime.Now;
                        model.Terminal = default;
                        await _dbContext.MMSITariffRates.AddAsync(model, cancellationToken);
                        TempData["success"] = "Tariff rate created successfully.";
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    throw new Exception("There is an error with the entry, please try again.");
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                model = await GetSelectLists(model, cancellationToken);
                model.Terminal = await _dbContext.MMSITerminals
                    .Include(t => t.Port)
                    .Where(t => t.TerminalId == model.TerminalId)
                    .FirstOrDefaultAsync(cancellationToken);

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var model = await _dbContext.MMSITariffRates
                .Include(t => t.Terminal).ThenInclude(t => t.Port)
                .FirstOrDefaultAsync(t => t.ActivityServiceId == id, cancellationToken);
            model = await GetSelectLists(model, cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSITariffRate model, CancellationToken cancellationToken = default)
        {
            var currentModel = await _dbContext.MMSITariffRates.FindAsync(model.TariffRateId, cancellationToken);

            currentModel.AsOfDate = model.AsOfDate;
            currentModel.CustomerId = model.CustomerId;
            currentModel.ActivityServiceId = model.ActivityServiceId;
            currentModel.TerminalId = model.TerminalId;
            currentModel.Dispatch = model.Dispatch;
            currentModel.BAF = model.BAF;

            await _dbContext.SaveChangesAsync(cancellationToken);

            TempData["success"] = "Entry edited successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken = default)
        {
            var terminals = await _dbContext
                .MMSITerminals
                .Where(t => t.PortId == portId)
                .OrderBy(t => t.TerminalId)
                .ToListAsync(cancellationToken);

            var terminalsList = terminals.Select(t => new SelectListItem
            {
                Value = t.TerminalId.ToString(),
                Text = t.TerminalNumber + " " + t.TerminalName
            }).ToList();

            return Json(terminalsList);
        }

        public async Task<MMSITariffRate> GetSelectLists(MMSITariffRate model, CancellationToken cancellationToken = default)
        {
            model.Customers = await _dispatchTicketRepository.GetMMSICustomersById(cancellationToken);
            model.Ports = await _dispatchTicketRepository.GetMMSIPortsById(cancellationToken);
            model.ActivitiesServices = await _dispatchTicketRepository.GetMMSIActivitiesServicesById(cancellationToken);
            if (model.TerminalId != default)
            {
                var terminal = await _dbContext.MMSITerminals
                    .Where(t => t.TerminalId == model.TerminalId)
                    .Include(t => t.Port)
                    .FirstOrDefaultAsync(cancellationToken);
                model.Terminal = terminal;
                model.Terminals = await _dispatchTicketRepository.GetMMSITerminalsById(terminal.PortId, cancellationToken);
            }

            return model;
        }

        [HttpPost]
        public async Task<bool> CheckIfExisting(DateOnly date, int customerId, int terminalId, int activityServiceId, decimal dispatch, decimal baf, CancellationToken cancellationToken = default)
        {
            var model = await _dbContext.MMSITariffRates
                .Where(t => t.AsOfDate == date && t.CustomerId == customerId && t.TerminalId == terminalId && t.ActivityServiceId == activityServiceId)
                .FirstOrDefaultAsync(cancellationToken);
            return (model != null);
        }
    }
}
