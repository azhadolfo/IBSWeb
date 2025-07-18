using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.Mobility.MasterFile;
using IBS.Services.Attributes;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class PickupPointController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<PickupPointController> _logger;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        public PickupPointController(IUnitOfWork unitOfWork, ILogger<PickupPointController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
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
            var pickupPoints = await _dbContext.MobilityPickUpPoints
                .Where(p => p.StationCode == stationCodeClaims)
                .Include(p => p.Supplier)
                .ToListAsync(cancellationToken);

            return View(pickupPoints);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            var model = new MobilityPickUpPoint();
            model.Suppliers = await _unitOfWork.MobilityPickUpPoint.GetMobilityTradeSupplierListAsyncById(stationCodeClaims, cancellationToken);
            model.StationCode = stationCodeClaims;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MobilityPickUpPoint model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    model.CreatedBy = _userManager.GetUserName(User)!;
                    model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    await _dbContext.MobilityPickUpPoints.AddAsync(model, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    TempData["success"] = "Pickup point created successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create pickup point master file. Created by: {UserName}", _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            model.Suppliers = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(model.StationCode, cancellationToken);
            ModelState.AddModelError("", "Make sure to fill all the required details.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            try
            {
                if (id == null || id == 0)
                {
                    return NotFound();
                }

                var stationCodeClaims = await GetStationCodeClaimAsync();

                if (stationCodeClaims == null)
                {
                    return BadRequest();
                }

                var model = await _unitOfWork.MobilityPickUpPoint
                    .GetAsync(p => p.PickUpPointId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                model.Suppliers = await _unitOfWork.MobilityPickUpPoint.GetMobilityTradeSupplierListAsyncById(stationCodeClaims, cancellationToken);

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MobilityPickUpPoint model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var user = await _userManager.GetUserAsync(User);

                try
                {
                    var selected = await _unitOfWork.MobilityPickUpPoint
                        .GetAsync(p => p.PickUpPointId == model.PickUpPointId, cancellationToken);

                    if (selected == null)
                    {
                        return NotFound();
                    }

                    FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Edited pickup point {selected.Depot} to {model.Depot}", "Customer", nameof(Mobility));
                    await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                    selected.Depot = model.Depot;
                    selected.SupplierId = model.SupplierId;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    TempData["success"] = "Pickup point updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, $"Failed to edit pickup point master file. Created by: {user}", _userManager.GetUserName(User));
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return View(model);
                }
            }

            return View(model);
        }
    }
}
