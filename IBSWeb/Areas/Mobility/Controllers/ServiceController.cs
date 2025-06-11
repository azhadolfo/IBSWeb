using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility.MasterFile;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<ServiceController> _logger;

        public ServiceController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, ILogger<ServiceController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private async Task<string?> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            var services = await _dbContext.MobilityServices.Where(s => s.StationCode == stationCodeClaims).ToListAsync(cancellationToken);

            return View(services);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new MobilityService();

            viewModel.CurrentAndPreviousTitles = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.UnearnedTitles = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MobilityService services, CancellationToken cancellationToken)
        {
            services.CurrentAndPreviousTitles = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            services.UnearnedTitles = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    if (await _unitOfWork.MobilityService.IsServicesExist(services.Name, stationCodeClaims, cancellationToken))
                    {
                        ModelState.AddModelError("Name", "Services already exist!");
                        return View(services);
                    }

                    var currentAndPrevious = await _dbContext.FilprideChartOfAccounts
                        .FindAsync(services.CurrentAndPreviousId, cancellationToken) ?? throw new NullReferenceException();

                    var unearned = await _dbContext.FilprideChartOfAccounts
                        .FindAsync(services.UnearnedId, cancellationToken) ?? throw new NullReferenceException();

                    services.CurrentAndPreviousNo = currentAndPrevious.AccountNumber;
                    services.CurrentAndPreviousTitle = currentAndPrevious.AccountName;

                    services.UnearnedNo = unearned.AccountNumber;
                    services.UnearnedTitle = unearned.AccountName;

                    services.StationCode = stationCodeClaims;

                    services.CreatedBy = _userManager.GetUserName(User)!.ToUpper();

                    services.ServiceNo = await _unitOfWork.MobilityService.GetLastNumber(stationCodeClaims, cancellationToken);

                    TempData["success"] = "Services created successfully";

                    await _dbContext.AddAsync(services, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Failed to create service master file. Created by: {UserName}", _userManager.GetUserName(User));
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return View(services);
                }
            }
            return View(services);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.MobilityServices == null)
            {
                return NotFound();
            }

            var services = await _dbContext.MobilityServices.FindAsync(id, cancellationToken);
            if (services == null)
            {
                return NotFound();
            }
            return View(services);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MobilityService services, CancellationToken cancellationToken)
        {
            if (id != services.ServiceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingModel = await _dbContext.MobilityServices.FindAsync(id, cancellationToken);
                    if (existingModel != null)
                    {
                        existingModel.Name = services.Name;
                        existingModel.Percent = services.Percent;
                        TempData["success"] = "Services updated successfully";

                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }

                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Failed to edit service master file. Edited by: {UserName}", _userManager.GetUserName(User));
                    if (!ServicesExists(services.ServiceId))
                    {
                        return NotFound();
                    }

                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(services);
        }

        private bool ServicesExists(int id)
        {
            return (_dbContext.MobilityServices?.Any(e => e.ServiceId == id)).GetValueOrDefault();
        }
    }
}
