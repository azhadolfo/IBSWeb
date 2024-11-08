using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Models.Mobility;
using IBS.Models.Mobility.MasterFile;
using IBS.Models.Mobility.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.EntityFrameworkCore;
using NuGet.DependencyResolver;
using System.IO;
using System.Linq.Expressions;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class CustomerOrderSlipController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<IdentityUser> _userManager;
        public CustomerOrderSlipController(ApplicationDbContext dbContext, IWebHostEnvironment webHostEnvironment, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        private async Task<string> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode").Value;
        }

        


        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            #region -- get user department --

            var findUser = await _dbContext.ApplicationUsers
                .Where(user => user.Id == _userManager.GetUserId(this.User))
                .FirstOrDefaultAsync();

            ViewBag.userDepartment = findUser?.Department;

            #endregion -- get user department --

            var stationCodeClaims = await GetStationCodeClaimAsync();
            ViewData["StationCode"] = stationCodeClaims;
            ViewData["StationName"] = await _unitOfWork.GetMobilityStationNameAsync(stationCodeClaims, cancellationToken);

            List<MobilityCustomerOrderSlip> customerOrderSlip = new List<MobilityCustomerOrderSlip>();

            if (stationCodeClaims != "ALL")
            {
                customerOrderSlip = await _dbContext.MobilityCustomerOrderSlips
                    .Include(c => c.Customer)
                    .Include(p => p.Product)
                    .Include(s => s.MobilityStation)
                    .Where(record => record.StationCode == stationCodeClaims)
                    .ToListAsync(cancellationToken);
            }
            if (stationCodeClaims == "ALL" && findUser?.Department != "Station Cashier")
            {
                customerOrderSlip = await _dbContext.MobilityCustomerOrderSlips
                    .Include(c => c.Customer)
                    .Include(p => p.Product)
                    .Include(s => s.MobilityStation)
                    .ToListAsync(cancellationToken);
            }
            return View(customerOrderSlip);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            string stationCodeString = stationCodeClaims.ToString();
            ViewData["StationCode"] = stationCodeClaims;
            ViewData["StationName"] = await _unitOfWork.GetMobilityStationNameAsync(stationCodeClaims, cancellationToken);

            MobilityCustomerOrderSlip model;

            if (stationCodeClaims != "ALL")
            {
                model = new()
                {
                    MobilityStations = await _unitOfWork.GetMobilityStationListWithCustomersAsyncByCode(cancellationToken),
                    Products = await _unitOfWork.GetProductListAsyncById(cancellationToken)
                };
            }
            else
            {
                model = new()
                {
                    Customers = await _unitOfWork.GetMobilityCustomerListAsyncByIdAll(stationCodeString, cancellationToken),
                    MobilityStations = await _unitOfWork.GetMobilityStationListWithCustomersAsyncByCode(cancellationToken),
                    Products = await _unitOfWork.GetProductListAsyncById(cancellationToken)
                };
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MobilityCustomerOrderSlip model, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            ViewData["StationCode"] = stationCodeClaims;
            ViewData["StationName"] = await _unitOfWork.GetMobilityStationNameAsync(stationCodeClaims, cancellationToken);
            string stationCodeString = stationCodeClaims.ToString();

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

                    var stationCode = stationCodeClaims == "ALL" ? model.StationCode : stationCodeClaims;

                    var getMobilityStation = await _dbContext.MobilityStations
                        .Where(s => s.StationCode == stationCode)
                        .FirstOrDefaultAsync(cancellationToken);

                    #endregion -- selected customer --

                    #region -- Generate COS No --

                    MobilityCustomerOrderSlip? lastCos = await _dbContext
                        .MobilityCustomerOrderSlips
                        .Where(c => c.StationCode == stationCode)
                        .OrderBy(c => c.CustomerOrderSlipNo)
                        .LastOrDefaultAsync(cancellationToken);

                    var series = "";
                    if (lastCos != null)
                    {
                        string lastSeries = lastCos.CustomerOrderSlipNo;
                        string numericPart = lastSeries.Substring(3);
                        int incrementedNumber = int.Parse(numericPart) + 1;

                        series = lastSeries.Substring(0, 3) + incrementedNumber.ToString("D10");
                    }
                    else
                    {
                        series = "COS0000000001";
                    }

                    #endregion -- Generate COS No --

                    model.CustomerOrderSlipNo = series;
                    model.StationCode = stationCodeClaims;
                    model.Status = "Pending";
                    model.Terms = selectedCustomer.CustomerTerms;
                    model.CreatedBy = _userManager.GetUserName(User);
                    model.CreatedDate = DateTime.Now;
                    model.StationId = getMobilityStation.StationId;
                    model.Address = selectedCustomer.CustomerAddress;
                    if (stationCodeClaims == "ALL")
                    {
                        model.StationCode = getMobilityStation.StationCode;
                    }

                    await _dbContext.MobilityCustomerOrderSlips.AddAsync(model, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    TempData["success"] = "Creation Succeed!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    model.Customers = await _unitOfWork.GetMobilityCustomerListAsyncById(stationCodeString, cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }
            else
            {
                model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                model.Customers = await _unitOfWork.GetMobilityCustomerListAsyncById(stationCodeString, cancellationToken);
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            ViewData["StationCode"] = stationCodeClaims;
            ViewData["StationName"] = await _unitOfWork.GetMobilityStationNameAsync(stationCodeClaims, cancellationToken);
            string stationCodeString = stationCodeClaims.ToString();

            var customerOrderSlip = await _dbContext.MobilityCustomerOrderSlips.FindAsync(id);
            customerOrderSlip.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            customerOrderSlip.MobilityStations = await _unitOfWork.GetMobilityStationListWithCustomersAsyncByCode(cancellationToken);
            customerOrderSlip.Customers = await GetInitialCustomers(customerOrderSlip.StationCode, cancellationToken);

            return View(customerOrderSlip);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MobilityCustomerOrderSlip model, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            ViewData["StationCode"] = stationCodeClaims;
            ViewData["StationName"] = await _unitOfWork.GetMobilityStationNameAsync(stationCodeClaims, cancellationToken);
            string stationCodeString = stationCodeClaims.ToString();

            if (ModelState.IsValid)
            {
                try
                {
                    #region -- selected customer --

                    var selectedCustomer = await _dbContext.MobilityCustomers
                        .Where(c => c.CustomerId == model.CustomerId)
                        .FirstOrDefaultAsync(cancellationToken);

                    #endregion -- selected customer --

                    #region -- getMobilityStation --

                    var stationCode = stationCodeClaims == "ALL" ? model.StationCode : stationCodeClaims;

                    var getMobilityStation = await _dbContext.MobilityStations
                                                .Where(s => s.StationCode == stationCode)
                                                .FirstOrDefaultAsync(cancellationToken);

                    #endregion -- getMobilityStation --

                    #region -- Assign New Values --

                    var existingModel = await _dbContext.MobilityCustomerOrderSlips.FindAsync(model.CustomerOrderSlipId);
                    existingModel.Date = model.Date;
                    existingModel.Quantity = model.Quantity;
                    existingModel.PricePerLiter = model.PricePerLiter;  
                    existingModel.Address = model.Address;
                    existingModel.ProductId = model.ProductId;
                    existingModel.Product = model.Product;
                    existingModel.Amount = model.Amount;
                    existingModel.PlateNo = model.PlateNo;
                    existingModel.Driver = model.Driver;
                    existingModel.CustomerId = model.CustomerId;
                    existingModel.StationCode = getMobilityStation.StationCode;
                    existingModel.Terms = selectedCustomer.CustomerTerms;
                    existingModel.StationId = getMobilityStation.StationId;
                    existingModel.Address = selectedCustomer.CustomerAddress;
                    existingModel.EditedBy = _userManager.GetUserName(User);
                    existingModel.EditedDate = DateTime.Now;
                    existingModel.Status = "Pending";

                    #endregion -- Assign New Values --

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Edit Complete!";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    model.Customers = await _unitOfWork.GetMobilityCustomerListAsyncById(stationCodeString, cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }
            else
            {
                model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                model.Customers = await _unitOfWork.GetMobilityCustomerListAsyncById(stationCodeString, cancellationToken);
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

            var model = await _dbContext.MobilityCustomerOrderSlips
                .Include(c => c.Customer)
                .Include(p => p.Product)
                .Include(s => s.MobilityStation)
                .Where(cos => cos.CustomerOrderSlipId == id)
                .FirstOrDefaultAsync();

            model.Products = await _unitOfWork.GetProductListAsyncByCode(cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Print(int id, IFormFile file, DateTime loadDate, string tripTicket, CancellationToken cancellationToken)
        {
            if (file.ContentType == "image/png" || file.ContentType == "image/jpg" || file.ContentType == "image/jpeg")
            {
                if (file.Length <= 20000000)
                {
                    var model = await _dbContext.MobilityCustomerOrderSlips
                        .Include(m => m.Customer)
                        .Include(m => m.Product)
                        .FirstOrDefaultAsync(m => m.CustomerOrderSlipId == id);
                    string localPath = Path.Combine(_webHostEnvironment.WebRootPath, "Mobility COS", "Uploads");

                    #region --check folder, delete old file--
                    if (!Directory.Exists(localPath))
                    {
                        Directory.CreateDirectory(localPath);
                    }

                    //if a file already exists, will delete
                    if (!string.IsNullOrEmpty(model.Upload))
                    {
                        string oldFilePath = Path.Combine(localPath, model.Upload);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error deleting file: {ex.Message}");
                            }
                        }
                    }
                    #endregion --operate folder, delete old file--

                    string newFileExtension = Path.GetExtension(file.FileName);
                    string newFileName = string.Concat(model.Customer.CustomerCodeName, "_", model.Product.ProductName, "_", model.Date.ToString("yyyyMMdd"), DateTime.Now.ToString("HHmmss"), newFileExtension);
                    string fileSavePath = Path.Combine(localPath, newFileName);

                    await using (FileStream stream = new(fileSavePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream, cancellationToken);
                    }

                    model.Upload = newFileName;
                    model.LoadDate = loadDate;
                    model.TripTicket = tripTicket;
                    model.Status = "Lifted";
                    model.UploadedBy = _userManager.GetUserName(User);
                    model.UploadedDate = DateTime.Now;

                    await _dbContext.SaveChangesAsync();

                    TempData["success"] = "Record Updated Successfully!";

                    return RedirectToAction(nameof(Index));
                }
            }

            TempData["error"] = "Please upload an image file only!";

            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> ApproveCOS(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.MobilityCustomerOrderSlips.FindAsync(id);
            model.Status = "Approved";
            model.ApprovedBy = _userManager.GetUserName(User);
            model.ApprovedDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            TempData["success"] = "COS entry approved!";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DisapproveCOS(int id, string message, CancellationToken cancellationToken)
        {
            var model = await _dbContext.MobilityCustomerOrderSlips.FindAsync(id);
            model.Status = "Disapproved";
            model.Remarks = message;
            model.DisapprovedBy = _userManager.GetUserName(User);
            model.DisapprovedDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            TempData["success"] = "COS entry disapproved";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers(string stationCode, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var invoices = await _dbContext
                .MobilityCustomers
                .Where(si => si.StationCode == stationCode)
                .OrderBy(si => si.CustomerId)
                .ToListAsync(cancellationToken);

            var invoiceList = invoices.Select(si => new SelectListItem
            {
                Value = si.CustomerId.ToString(),   // Replace with your actual ID property
                Text = si.CustomerName              // Replace with your actual property for display text
            }).ToList();

            return Json(invoiceList);
        }

        [HttpGet]
        public async Task<List<SelectListItem>> GetInitialCustomers(string stationCode, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var invoices = await _dbContext
                .MobilityCustomers
                .Where(si => si.StationCode == stationCode)
                .OrderBy(si => si.CustomerId)
                .ToListAsync(cancellationToken);

            var invoiceList = invoices.Select(si => new SelectListItem
            {
                Value = si.CustomerId.ToString(),   // Replace with your actual ID property
                Text = si.CustomerName              // Replace with your actual property for display text
            }).ToList();

            return invoiceList;
        }

    }
}