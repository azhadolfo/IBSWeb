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
    public class UserAccessController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserAccessController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserAccessController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork, ILogger<UserAccessController> logger, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        // GET
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var model = await _unitOfWork.UserAccess.GetAllAsync(null, cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            MMSIUserAccess model = new MMSIUserAccess
            {
                Users = await _unitOfWork.Msap.GetMMSIUsersSelectListById(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSIUserAccess model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid input please try again.";
                return RedirectToAction(nameof(Index));
            }

            var tempModel = await _unitOfWork.UserAccess.GetAsync(ua => ua.UserId == model.UserId, cancellationToken);

            if (tempModel != null)
            {
                throw new Exception($"Access for {tempModel.UserName} already exists.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var selectedUser = _dbContext.Users.FirstOrDefault(u => u.Id == model.UserId);
                model.UserName = selectedUser!.UserName;
                await _unitOfWork.UserAccess.AddAsync(model, cancellationToken);

                #region -- Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Created User Access for {model.UserName}", "User Access", nameof(MMSI));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "User access created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user access.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                model.Users = await _unitOfWork.Msap.GetMMSIUsersSelectListById(cancellationToken);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var model = await _unitOfWork.UserAccess.GetAsync(ua => ua.Id == id, cancellationToken);

            if (model == null)
            {
                TempData["info"] = "User access not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIUserAccess model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid input please try again.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var tempModel = await _unitOfWork.UserAccess.GetAsync(ua => ua.Id == model.Id, cancellationToken);

                if (tempModel == null)
                {
                    return NotFound();
                }

                #region -- Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Edited User Access for {model.UserName}", "User Access", nameof(MMSI));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                tempModel.CanCreateServiceRequest = model.CanCreateServiceRequest;
                tempModel.CanPostServiceRequest = model.CanPostServiceRequest;
                tempModel.CanCreateDispatchTicket = model.CanCreateDispatchTicket;
                tempModel.CanSetTariff = model.CanSetTariff;
                tempModel.CanApproveTariff = model.CanApproveTariff;
                tempModel.CanCreateBilling = model.CanCreateBilling;
                tempModel.CanCreateCollection = model.CanCreateCollection;
                await _unitOfWork.SaveAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "User access edited successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit user access.");
                TempData["error"] = ex.Message;
                await transaction.RollbackAsync(cancellationToken);
                model.Users = await _unitOfWork.Msap.GetMMSIUsersSelectListById(cancellationToken);
                return View(model);
            }
        }
    }
}
