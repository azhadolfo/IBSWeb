using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.Mobility.MasterFile;
using IBS.Utility;
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
        public async Task<IActionResult> Create()
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            ViewData["StationCode"] = stationCodeClaims;
            return View();
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

                    model.StationCode = stationCodeClaims;
                    //model.CustomerCode = await _unitOfWork.FilprideCustomer.GenerateCodeAsync(model.CustomerType, stationCodeClaims, cancellationToken);
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
    }
}