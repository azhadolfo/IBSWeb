using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ServiceController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public ServiceController(ApplicationDbContext dbContext, ILogger<ServiceController> logger, IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var activitiesServices = _dbContext.MMSIServices.ToList();
            return View(activitiesServices);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSIService model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _unitOfWork.Service.AddAsync(model, cancellationToken);

                #region -- Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Created new Service #{model.ServiceNumber}", "Service", nameof(MMSI));
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Creation Succeed!";
                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create service.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _unitOfWork.Service.GetAsync( i => i.ServiceId == id, cancellationToken);

                if (model == null) return NotFound();

                _dbContext.MMSIServices.Remove(model);
                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Entry deleted successfully";
                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete service.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.Service.GetAsync(a => a.ServiceId == id, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIService model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            var currentModel = await _unitOfWork.Service.GetAsync(s => s.ServiceId == model.ServiceId, cancellationToken);

            if (currentModel == null) return NotFound();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Edited Service #{currentModel.ServiceNumber} => {model.ServiceNumber}", "Service", nameof(MMSI));
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                currentModel.ServiceNumber = model.ServiceNumber;
                currentModel.ServiceName = model.ServiceName;
                await _unitOfWork.Service.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit service.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
    }
}
