using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
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
    public class CustomerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, ILogger<CustomerController> logger)
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

        [HttpGet]
        public async Task<IActionResult> Activate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var customer = await _unitOfWork
                .MobilityCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                return View(customer);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Activate")]
        public async Task<IActionResult> ActivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var customer = await _unitOfWork.MobilityCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                customer.IsActive = true;
                await _unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Activated customer {customer.CustomerCode}", "Customer", nameof(Mobility));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer has been activated";
                return RedirectToAction(nameof(Index));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to activate customer master file. Created by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Activate), new { id = id });
            }
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            IEnumerable<MobilityCustomer> model = await _dbContext.MobilityCustomers
                .Where(c => c.StationCode == stationCodeClaims)
                .ToListAsync(cancellationToken);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            ViewData["StationCode"] = stationCodeClaims;
            MobilityCustomer model = new()
            {
                MobilityStations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken)
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MobilityCustomer model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Make sure to fill all the required details.");
                return View(model);
            }

            //bool IsTinExist = await _unitOfWork.FilprideCustomer.IsTinNoExistAsync(model.CustomerTin, companyClaims, cancellationToken);
            bool IsTinExist = false;

            if (IsTinExist)
            {
                ModelState.AddModelError("CustomerTin", "Tin No already exist.");
                return View(model);
            }

            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.StationCode = stationCodeClaims;
                model.CustomerCode = await _unitOfWork.MobilityCustomer.GenerateCodeAsync(model.CustomerType, stationCodeClaims, cancellationToken);
                model.CreatedBy = _userManager.GetUserName(User);
                await _unitOfWork.MobilityCustomer.AddAsync(model, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);
                ViewData["StationCode"] = stationCodeClaims;

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Create new customer {model.CustomerCode}", "Customer", nameof(Mobility));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create customer master file. Created by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var customer = await _unitOfWork
                .MobilityCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                return View(customer);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Deactivate")]
        public async Task<IActionResult> DeactivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var customer = await _unitOfWork.MobilityCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                customer.IsActive = false;
                await _unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Deactivated customer {customer.CustomerCode}", "Customer", nameof(Mobility));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer has been deactivated";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create customer master file. Created by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Deactivate), new { id = id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var stationCodeClaims = await GetStationCodeClaimAsync();
            ViewData["StationCode"] = stationCodeClaims;

            var customer = await _dbContext.MobilityCustomers.FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            customer.MobilityStations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken);

            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MobilityCustomer model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                model.MobilityStations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken);
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }

            var existingCustomer = await _unitOfWork.MobilityCustomer
                .GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

            if (existingCustomer == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var stationCodeClaims = await GetStationCodeClaimAsync();
                ViewData["StationCode"] = stationCodeClaims;

                #region -- getMobilityStation --

                var getMobilityStation = await _dbContext.MobilityStations
                    .Where(s => s.StationCode == stationCodeClaims)
                    .FirstOrDefaultAsync(cancellationToken);

                #endregion -- getMobilityStation --

                #region -- Assign New Values --

                await _unitOfWork.MobilityCustomer.UpdateAsync(model, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                #endregion -- Assign New Values --

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Edited customer {existingCustomer.CustomerCode} => {model.CustomerCode}", "Customer", nameof(Mobility));
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit customer master file. Created by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                model.MobilityStations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken);
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }
    }
}
