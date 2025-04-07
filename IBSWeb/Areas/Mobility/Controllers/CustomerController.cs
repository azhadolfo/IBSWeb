using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
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
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork,  ILogger<CustomerController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private async Task<string> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode").Value;
        }

        [HttpGet]
        public async Task<IActionResult> Activate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            MobilityCustomer customer = await _unitOfWork
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

            MobilityCustomer customer = await _unitOfWork
                .MobilityCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                customer.IsActive = true;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Customer has been activated";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var model = await _dbContext.MobilityCustomers.ToListAsync(cancellationToken);
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
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var stationCodeClaims = await GetStationCodeClaimAsync();

                    //bool IsTinExist = await _unitOfWork.FilprideCustomer.IsTinNoExistAsync(model.CustomerTin, companyClaims, cancellationToken);
                    bool IsTinExist = false;
                    if (!IsTinExist)
                    {
                        model.StationCode = stationCodeClaims;
                        model.CustomerCode = await _unitOfWork.FilprideCustomer.GenerateCodeAsync(model.CustomerType, stationCodeClaims, cancellationToken);
                        model.CreatedBy = _userManager.GetUserName(User);
                        await _dbContext.MobilityCustomers.AddAsync(model, cancellationToken);
                        await _unitOfWork.SaveAsync(cancellationToken);

                        FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new customer {model.CustomerCode}", "Customer", "", model.StationCode);
                        await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                        ViewData["StationCode"] = stationCodeClaims;

                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Customer created successfully";
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError("CustomerTin", "Tin No already exist.");
                    return View(model);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create customer master file. Created by: {UserName}", _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }
            ModelState.AddModelError("", "Make sure to fill all the required details.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            MobilityCustomer customer = await _unitOfWork
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

            MobilityCustomer customer = await _unitOfWork
                .MobilityCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                customer.IsActive = false;
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Customer has been deactivated";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
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
            customer.MobilityStations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken);

            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MobilityCustomer model, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            ViewData["StationCode"] = stationCodeClaims;
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region -- getMobilityStation --

                    var stationCode = stationCodeClaims == "ALL" ? model.StationCode : stationCodeClaims;

                    var getMobilityStation = await _dbContext.MobilityStations
                                                .Where(s => s.StationCode == stationCode)
                                                .FirstOrDefaultAsync(cancellationToken);

                    #endregion -- getMobilityStation --

                    #region -- Assign New Values --

                    await _unitOfWork.MobilityCustomer.UpdateAsync(model, cancellationToken);

                    #endregion -- Assign New Values --

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Customer updated successfully";
                    await transaction.CommitAsync(cancellationToken);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Failed to edit customer master file. Created by: {UserName}", _userManager.GetUserName(User));
                    model.MobilityStations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken);
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return View(model);
                }
            }
            else
            {
                model.MobilityStations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken);
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }
    }
}
