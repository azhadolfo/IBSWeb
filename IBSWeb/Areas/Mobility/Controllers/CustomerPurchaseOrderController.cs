using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class CustomerPurchaseOrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<IdentityUser> _userManager;

        public CustomerPurchaseOrderController(ApplicationDbContext dbContext, IWebHostEnvironment webHostEnvironment, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
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
            List<MobilityCustomerPurchaseOrder> customerPurchaseOrder;

            customerPurchaseOrder = await _dbContext.MobilityCustomerPurchaseOrders
                    .Include(c => c.Customer)
                    .Include(p => p.Product)
                    .Include(s => s.MobilityStation)
                    .ToListAsync(cancellationToken);
            return View(customerPurchaseOrder);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            ViewData["StationCode"] = stationCodeClaims;

            MobilityCustomerPurchaseOrder model = new()
            {
                Customers = await _unitOfWork.GetMobilityCustomerListAsyncById(stationCodeClaims, cancellationToken),
                MobilityStations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken),
                Products = await _unitOfWork.GetProductListAsyncById(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MobilityCustomerPurchaseOrder model, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            ViewData["StationCode"] = stationCodeClaims;
            if (ModelState.IsValid)
            {
                try
                {
                    #region -- selected customer --

                    var selectedCustomer = await _dbContext.MobilityCustomers
                                        .Where(c => c.CustomerId == model.CustomerId)
                                        .FirstOrDefaultAsync(cancellationToken);

                    #endregion -- selected customer --

                    #region -- get mobility station --

                    var getMobilityStation = await _dbContext.MobilityStations
                                        .Where(s => s.StationCode == stationCodeClaims)
                                        .FirstOrDefaultAsync(cancellationToken);

                    if (getMobilityStation == null)
                    {
                        return NotFound();
                    }

                    #endregion -- get mobility station --

                    #region -- Generate COS No --

                    MobilityCustomerPurchaseOrder? lastPo = await _dbContext
                        .MobilityCustomerPurchaseOrders
                        .OrderBy(c => c.CustomerPurchaseOrderNo)
                        .LastOrDefaultAsync(cancellationToken);

                    var series = "";
                    if (lastPo != null)
                    {
                        string lastSeries = lastPo.CustomerPurchaseOrderNo;
                        string numericPart = lastSeries.Substring(3);
                        int incrementedNumber = int.Parse(numericPart) + 1;

                        series = lastSeries.Substring(0, 3) + incrementedNumber.ToString("D10");
                    }
                    else
                    {
                        series = "PO0000000001";
                    }

                    #endregion -- Generate COS No --

                    model.CustomerPurchaseOrderNo = series;
                    model.StationCode = stationCodeClaims;
                    model.StationId = getMobilityStation.StationId;

                    await _dbContext.MobilityCustomerPurchaseOrders.AddAsync(model, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    TempData["success"] = $"Purchase Order created successfully. Series No. {model.CustomerPurchaseOrderNo}";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    model.Customers = await _unitOfWork.GetMobilityCustomerListAsyncById(stationCodeClaims, cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }
            else
            {
                model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                model.Customers = await _unitOfWork.GetMobilityCustomerListAsyncById(stationCodeClaims, cancellationToken);
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            #region -- get user department --

            var findUser = await _dbContext.ApplicationUsers
                .Where(user => user.Id == _userManager.GetUserId(this.User))
                .FirstOrDefaultAsync();

            ViewBag.GetUserDepartment = findUser?.Department;

            #endregion -- get user department --

            var model = await _dbContext.MobilityCustomerPurchaseOrders
                .Include(c => c.Customer)
                .Include(p => p.Product)
                .Include(s => s.MobilityStation)
                .Where(cos => cos.CustomerPurchaseOrderId == id)
                .FirstOrDefaultAsync();

            if (model == null)
            {
                return NotFound();
            }

            model.Products = await _unitOfWork.GetMobilityProductListAsyncByCode(cancellationToken);

            return View(model);
        }
    }
}
