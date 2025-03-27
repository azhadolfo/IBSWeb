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
                    var user = await _userManager.GetUserAsync(User);
                    model.CreatedBy = user?.UserName;
                    model.CreatedDate = DateTime.Now;

                    await _dbContext.MMSITariffRates.AddAsync(model, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["error"] = "The entry is invalid, please try again.";

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

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
                model.Terminals = await _dispatchTicketRepository.GetMMSITerminalsById(model.Terminal.Port.PortId, cancellationToken);
            }

            return model;
        }
    }
}
