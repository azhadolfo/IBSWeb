using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.Mobility.MasterFile;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading;


namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class CustomerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;
        public CustomerController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }
        private async Task<string> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode").Value;
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
                try
                {
                    var stationCodeClaims = await GetStationCodeClaimAsync();
                    ViewData["StationCode"] = stationCodeClaims;

                    model.CreatedBy = _userManager.GetUserName(User);
                    await _dbContext.MobilityCustomers.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);
                    TempData["success"] = "Customer created successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }
            ModelState.AddModelError("", "Make sure to fill all the required details.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
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
                try
                {
                    #region -- getMobilityStation --
                    var stationCode = stationCodeClaims == "ALL" ? model.StationCode : stationCodeClaims;

                    var getMobilityStation = await _dbContext.MobilityStations
                                                .Where(s => s.StationCode == stationCode)
                                                .FirstOrDefaultAsync(cancellationToken);
                    #endregion -- getMobilityStation --

                    #region -- Assign New Values --
                    var existingModel = await _dbContext.MobilityCustomers.FindAsync(model.CustomerId);
                    existingModel.CustomerName = model.CustomerName;
                    existingModel.CustomerCodeName = model.CustomerCodeName;
                    existingModel.StationCode = model.StationCode;
                    existingModel.CustomerTerms = model.CustomerTerms;
                    existingModel.CustomerAddress = model.CustomerAddress;
                    existingModel.AmountLimit = model.AmountLimit;
                    existingModel.QuantityLimit = model.QuantityLimit;
                    existingModel.EditedBy = _userManager.GetUserName(User);
                    existingModel.EditedDate = DateTime.Now;
                    #endregion -- Assign New Values --

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Edit Complete!";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    model.MobilityStations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken);
                    TempData["error"] = ex.Message;
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