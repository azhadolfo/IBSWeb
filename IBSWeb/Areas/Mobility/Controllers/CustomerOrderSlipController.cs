using System.Drawing;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Mobility;
using IBS.Models.Mobility.MasterFile;
using IBS.Models.Mobility.ViewModels;
using IBS.Services;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Quartz.Util;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class CustomerOrderSlipController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICloudStorageService _cloudStorageService;

        public CustomerOrderSlipController(ApplicationDbContext dbContext, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, ICloudStorageService cloudStorageService)
        {
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _cloudStorageService = cloudStorageService;
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

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            #region -- Get user department --

            var findUser = await _dbContext.ApplicationUsers
                .Where(user => user.Id == _userManager.GetUserId(this.User))
                .FirstOrDefaultAsync(cancellationToken);

            ViewBag.userDepartment = findUser?.Department;

            #endregion -- get user department --

            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            ViewData["CurrentStationCode"] = stationCodeClaims;
            ViewData["CurrentStationName"] = await _unitOfWork.GetMobilityStationNameAsync(stationCodeClaims, cancellationToken);

            IQueryable<MobilityCustomerOrderSlip> customerOrderSlip = _dbContext.MobilityCustomerOrderSlips
                .Include(c => c.Customer)
                .Include(p => p.Product)
                .Include(s => s.MobilityStation)
                .Where(record => record.StationCode == stationCodeClaims);

            if (User.IsInRole("Cashier"))
            {
                customerOrderSlip = customerOrderSlip.Where(cos => cos.Status == "Approved" || cos.Status == "Lifted");
            }

            // Apply sorting and execute the query
            var sortedCustomerOrderSlip = await customerOrderSlip
                .OrderBy(cos => cos.CustomerOrderSlipNo)
                .ThenBy(cos => cos.MobilityStation!.StationName)
                .ThenBy(cos => cos.Date)
                .ToListAsync(cancellationToken);

            return View(sortedCustomerOrderSlip);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return NotFound();
            }

            ViewData["CurrentStationCode"] = stationCodeClaims; // get
            ViewData["CurrentStationName"] = await _unitOfWork.GetMobilityStationNameAsync(stationCodeClaims, cancellationToken);

            MobilityCustomerOrderSlip model;
            List<MobilityCustomer> mobilityPOCustomers = await _dbContext.MobilityCustomers
                .Where(a => a.CustomerType == SD.CustomerType_PO)
                .ToListAsync(cancellationToken);

            model = new()
            {
                MobilityStations = await _unitOfWork.GetMobilityStationListWithCustomersAsyncByCode(mobilityPOCustomers, cancellationToken),
                Products = await _unitOfWork.GetProductListAsyncById(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MobilityCustomerOrderSlip model, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            ViewData["CurrentStationCode"] = stationCodeClaims;
            ViewData["CurrentStationName"] = await _unitOfWork.GetMobilityStationNameAsync(stationCodeClaims, cancellationToken);
            string stationCodeString = stationCodeClaims.ToString();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region -- Selected customer --

                    var selectedCustomer = await _dbContext.MobilityCustomers
                        .Where(c => c.CustomerId == model.CustomerId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (selectedCustomer == null)
                    {
                        return NotFound();
                    }

                    #endregion -- selected customer --

                    #region -- Get mobility station --

                    var getMobilityStation = await _dbContext.MobilityStations
                        .Where(s => s.StationCode == stationCodeClaims)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (getMobilityStation == null)
                    {
                        return NotFound();
                    }

                    #endregion -- get mobility station --

                    #region -- Generate COS No --

                    MobilityCustomerOrderSlip? lastCos = await _dbContext
                        .MobilityCustomerOrderSlips
                        .Where(c => c.StationCode == stationCodeClaims)
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

                    #region-- Deduct the Customer Credit Limit --

                    await _unitOfWork.MobilityCustomerOrderSlip.UpdateCustomerCreditLimitAsync(model.CustomerId, model.Quantity, cancellationToken: cancellationToken);

                    #endregion

                    model.CustomerOrderSlipNo = series;
                    model.StationCode = stationCodeClaims;
                    model.Status = "Pending";
                    model.Terms = selectedCustomer.CustomerTerms;
                    model.CreatedBy = _userManager.GetUserName(User);
                    model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model.StationId = getMobilityStation.StationId;
                    model.Address = selectedCustomer.CustomerAddress;

                    await _dbContext.MobilityCustomerOrderSlips.AddAsync(model, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    TempData["success"] = $"COS created successfully. Series Number: {model.CustomerOrderSlipNo}";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    model.Customers = await _unitOfWork.GetMobilityCustomerListAsyncById(stationCodeString, cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            model.Customers = await _unitOfWork.GetMobilityCustomerListAsyncById(stationCodeString, cancellationToken);
            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            ViewData["CurrentStationCode"] = stationCodeClaims;
            ViewData["CurrentStationName"] = await _unitOfWork.GetMobilityStationNameAsync(stationCodeClaims, cancellationToken);
            string stationCodeString = stationCodeClaims.ToString();
            List<MobilityCustomer> mobilityPOCustomers = await _dbContext.MobilityCustomers
                .Where(a => a.CustomerType == SD.CustomerType_PO)
                .ToListAsync(cancellationToken);

            var customerOrderSlip = await _dbContext.MobilityCustomerOrderSlips.FindAsync(id);

            if (customerOrderSlip == null)
            {
                return NotFound();
            }

            customerOrderSlip.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            customerOrderSlip.MobilityStations = await _unitOfWork.GetMobilityStationListWithCustomersAsyncByCode(mobilityPOCustomers, cancellationToken);
            customerOrderSlip.Customers = await GetInitialCustomers(customerOrderSlip.StationCode, cancellationToken);

            return View(customerOrderSlip);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MobilityCustomerOrderSlip model, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            ViewData["CurrentStationCode"] = stationCodeClaims;
            ViewData["CurrentStationName"] = await _unitOfWork.GetMobilityStationNameAsync(stationCodeClaims, cancellationToken);
            string stationCodeString = stationCodeClaims.ToString();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region -- Selected customer --

                    var selectedCustomer = await _dbContext.MobilityCustomers
                        .Where(c => c.CustomerId == model.CustomerId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (selectedCustomer == null)
                    {
                        return NotFound();
                    }

                    #endregion -- selected customer --

                    #region -- GetMobilityStation --

                    var getMobilityStation = await _dbContext.MobilityStations
                                                .Where(s => s.StationCode == stationCodeClaims)
                                                .FirstOrDefaultAsync(cancellationToken);

                    if (getMobilityStation == null)
                    {
                        return NotFound();
                    }

                    #endregion -- getMobilityStation --

                    #region -- Assign New Values --

                    var existingModel = await _dbContext.MobilityCustomerOrderSlips.FindAsync(model.CustomerOrderSlipId);

                    if (existingModel == null)
                    {
                        return NotFound();
                    }

                    existingModel.Date = model.Date;
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
                    existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    existingModel.Status = "Pending";

                    #endregion -- Assign New Values --

                    #region-- Deduct the Customer Credit Limit --

                    await _unitOfWork.MobilityCustomerOrderSlip.UpdateCustomerCreditLimitAsync(model.CustomerId, model.Quantity, existingModel.Quantity, cancellationToken: cancellationToken);
                    existingModel.Quantity = model.Quantity;

                    #endregion

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    TempData["success"] = "Edit Complete!";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    List<MobilityCustomer> mobilityPOCustomers = await _dbContext.MobilityCustomers
                        .Where(a => a.CustomerType == SD.CustomerType_PO)
                        .ToListAsync(cancellationToken);

                    await transaction.RollbackAsync(cancellationToken);
                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    model.MobilityStations = await _unitOfWork.GetMobilityStationListWithCustomersAsyncByCode(mobilityPOCustomers, cancellationToken);
                    model.Customers = await GetInitialCustomers(stationCodeString, cancellationToken);

                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }
            else
            {
                List<MobilityCustomer> mobilityPOCustomers = await _dbContext.MobilityCustomers
                    .Where(a => a.CustomerType == SD.CustomerType_PO)
                    .ToListAsync(cancellationToken);

                model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                model.Customers = await _unitOfWork.GetMobilityCustomerListAsyncById(stationCodeString, cancellationToken);
                model.MobilityStations = await _unitOfWork.GetMobilityStationListWithCustomersAsyncByCode(mobilityPOCustomers, cancellationToken);
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            ViewData["StationCode"] = stationCodeClaims;
            ViewData["CurrentStationName"] = await _unitOfWork.GetMobilityStationNameAsync(stationCodeClaims, cancellationToken);

            #region -- Get user department --

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
                .FirstOrDefaultAsync(cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            model.Products = await _unitOfWork.GetMobilityProductListAsyncByCode(cancellationToken);

            if (!string.IsNullOrEmpty(model.SavedFileName))
            {
                await GenerateSignedUrl(model);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Print(MobilityCustomerOrderSlip model, IFormFile file, DateTime loadDate, string tripTicket, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (file.ContentType.StartsWith("image"))
                    {
                        if (file.Length <= 20000000)
                        {
                            var existingModel = await _dbContext.MobilityCustomerOrderSlips
                                .Include(m => m.Customer)
                                .Include(m => m.Product)
                                .FirstOrDefaultAsync(m => m.CustomerOrderSlipId == model.CustomerOrderSlipId, cancellationToken);

                            if (existingModel == null)
                            {
                                return NotFound();
                            }

                            existingModel.SavedFileName = GenerateFileNameToSave(file.FileName);
                            existingModel.SavedUrl = await _cloudStorageService.UploadFileAsync(file, existingModel.SavedFileName!);

                            if (model.CheckPicture != null)
                            {
                                if (model.CheckPicture.Length > 20000000 || model.CheckPicture.ContentType.StartsWith("image"))
                                {
                                    existingModel.CheckPictureSavedFileName = GenerateFileNameToSave(model.CheckPicture.FileName);
                                    existingModel.CheckPictureSavedUrl = await _cloudStorageService.UploadFileAsync(model.CheckPicture, existingModel.CheckPictureSavedFileName!);
                                    existingModel.CheckNo = model.CheckNo;
                                }
                                else
                                {
                                    TempData["error"] = "Error on uploading check details";
                                    return RedirectToAction(nameof(Print), new { model.CustomerOrderSlipId });
                                }
                            }

                            existingModel.LoadDate = loadDate;
                            existingModel.TripTicket = tripTicket;
                            existingModel.Status = "Lifted";
                            existingModel.UploadedBy = _userManager.GetUserName(User);
                            existingModel.UploadedDate = DateTimeHelper.GetCurrentPhilippineTime();

                            await _dbContext.SaveChangesAsync(cancellationToken);

                            TempData["success"] = "Record Updated Successfully!";

                            return RedirectToAction(nameof(Index));
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Print), new { model.CustomerOrderSlipId });
                }
            }

            TempData["warning"] = "Please upload an image file only!";

            return RedirectToAction(nameof(Print), new { model.CustomerOrderSlipId });
        }

        public async Task<IActionResult> ApproveCOS(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.MobilityCustomerOrderSlips.FindAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            model.Status = "Approved";
            model.ApprovedBy = _userManager.GetUserName(User);
            model.ApprovedDate = DateTimeHelper.GetCurrentPhilippineTime();
            model.DisapprovalRemarks = "";

            await _dbContext.SaveChangesAsync();

            TempData["success"] = "COS entry approved!";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DisapproveCOS(int id, string message, CancellationToken cancellationToken)
        {
            var model = await _dbContext.MobilityCustomerOrderSlips.FindAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            model.Status = "Disapproved";
            model.DisapprovalRemarks = message;
            model.DisapprovedBy = _userManager.GetUserName(User);
            model.DisapprovedDate = DateTimeHelper.GetCurrentPhilippineTime();

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
                Value = si.CustomerId.ToString(),
                Text = si.CustomerName
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
                Value = si.CustomerId.ToString(),
                Text = si.CustomerName
            }).ToList();

            return invoiceList;
        }

        private string? GenerateFileNameToSave(string incomingFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{extension}";
        }

        private async Task GenerateSignedUrl(MobilityCustomerOrderSlip model)
        {
            // Get Signed URL only when Saved File Name is available.
            if (!string.IsNullOrWhiteSpace(model.SavedFileName))
            {
                model.SignedUrl = await _cloudStorageService.GetSignedUrlAsync(model.SavedFileName);
                model.CheckPictureSignedUrl = await _cloudStorageService.GetSignedUrlAsync(model.CheckPictureSavedFileName!);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerOrderSlipList(string statusFilter, string stationFilter, string currentStationCode, CancellationToken cancellationToken)
        {
            var item = new List<MobilityCustomerOrderSlip>();

            IQueryable<MobilityCustomerOrderSlip> query = _dbContext.MobilityCustomerOrderSlips
                .Include(c => c.Customer)
                .Include(p => p.Product)
                .Include(s => s.MobilityStation);

            if (statusFilter != null)
            {
                query = query.Where(cos => cos.Status == statusFilter);
            }
            if (stationFilter != null)
            {
                query = query.Where(cos => cos.StationCode == stationFilter);
            }

            query = query.Where(cos => cos.StationCode == currentStationCode);

            if (User.IsInRole("Cashier"))
            {
                query = query.Where(cos => cos.Status == "Approved" || cos.Status == "Lifted");
            }

            item = await query.OrderBy(cos => cos.Date)
                .ThenBy(cos => cos.CustomerOrderSlipNo)
                .ToListAsync(cancellationToken);

            return Json(item);
        }

        [HttpPost]
        [Area("Mobility")]
        public async Task<IActionResult> GenerateIndexExcel(string jsonModel, CancellationToken cancellationToken)
        {
            var findUser = await _dbContext.ApplicationUsers
                .Where(user => user.Id == _userManager.GetUserId(this.User))
                .FirstOrDefaultAsync(cancellationToken);

            try
            {
                if (string.IsNullOrWhiteSpace(jsonModel) || jsonModel == "[]")
                {
                    TempData["info"] = "The data is empty or invalid.";
                    return Json(new { success = false, error = "The data is empty or invalid." });
                }

                List<MobilityCustomerOrderSlip> model;

                try
                {
                    model = JsonConvert.DeserializeObject<List<MobilityCustomerOrderSlip>>(jsonModel)!;
                }
                catch (JsonSerializationException)
                {
                    return Json(new { success = false, error = "Failed to deserialize the input JSON model." });
                }

                if (model != null && model.Count > 0)
                {
                    using var package = new ExcelPackage();
                    string currencyFormat = "#,##0.00";
                    var worksheet = package.Workbook.Worksheets.Add("Customer Order Slip List");

                    // [Existing Excel generation code remains unchanged]
                    var mergedCells = worksheet.Cells["A1:C1"];
                    mergedCells.Merge = true;
                    mergedCells.Value = "Mobility Customer Order Slip";
                    mergedCells.Style.Font.Bold = true;
                    mergedCells.Style.Font.Size = 15;
                    mergedCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Row(1).Height = 20;

                    worksheet.Cells[2, 1].Value = "Date Exported:";
                    worksheet.Cells[2, 2].Value = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime());
                    worksheet.Cells[3, 1].Value = "Exported by:";
                    worksheet.Cells[3, 2].Value = findUser?.Name;

                    mergedCells = worksheet.Cells["A2:B3"];
                    mergedCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    mergedCells = worksheet.Cells["A2:A3"];
                    mergedCells.Style.Font.Bold = true;

                    var row = 5;

                    worksheet.Cells[row, 1].Value = "COS No.";
                    worksheet.Cells[row, 2].Value = "Station";
                    worksheet.Cells[row, 3].Value = "Date";
                    worksheet.Cells[row, 4].Value = "Customer";
                    worksheet.Cells[row, 5].Value = "Driver Name";
                    worksheet.Cells[row, 6].Value = "Plate Number";
                    worksheet.Cells[row, 7].Value = "Product";
                    worksheet.Cells[row, 8].Value = "Price";
                    worksheet.Cells[row, 9].Value = "Quantity";
                    worksheet.Cells[row, 10].Value = "Amount";
                    worksheet.Cells[row, 11].Value = "Status";

                    using (var range = worksheet.Cells[row, 1, row, 11])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                        range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    row++;
                    var startingRow = row;
                    var endingRow = row;

                    foreach (var item in model)
                    {
                        worksheet.Cells[row, 1].Value = item.CustomerOrderSlipNo ?? "N/A";
                        worksheet.Cells[row, 2].Value = item.MobilityStation?.StationName ?? "N/A";
                        worksheet.Cells[row, 3].Value = item.Date.ToString("O") ?? "N/A";
                        worksheet.Cells[row, 4].Value = item.Customer?.CustomerName ?? "N/A";
                        worksheet.Cells[row, 5].Value = item.Driver ?? "N/A";
                        worksheet.Cells[row, 6].Value = item.PlateNo ?? "N/A";
                        worksheet.Cells[row, 7].Value = item.Product?.ProductName;
                        worksheet.Cells[row, 8].Value = item.PricePerLiter;
                        worksheet.Cells[row, 9].Value = item.Quantity;
                        worksheet.Cells[row, 10].Value = item.Amount;
                        worksheet.Cells[row, 11].Value = item.Status ?? "N/A";

                        worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;

                        endingRow = row;
                        row++;
                    }

                    using (var range = worksheet.Cells[startingRow, 1, endingRow, 11])
                    {
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var excelBytes = package.GetAsByteArray();
                    var fileName = $"Mobility_Customer_Order_Slip_{DateTimeHelper.GetCurrentPhilippineTime().ToString("yyyyMMddhhmm")}.xlsx";

                    Response.Headers.Append("Content-Disposition", new System.Net.Mime.ContentDisposition
                    {
                        FileName = fileName,
                        Inline = false
                    }.ToString());
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                }

                TempData["info"] = "The data is empty or invalid.";
                return Json(new { success = false, error = "The data is empty or invalid." });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult PrintCosList(string jsonModel, CancellationToken cancellationToken)
        {
            try
            {
                if (jsonModel.IsNullOrWhiteSpace() || jsonModel == "[]")
                {
                    TempData["info"] = "The data is empty or invalid.";
                    return RedirectToAction(nameof(Index));
                }

                try
                {
                    var model = JsonConvert.DeserializeObject<List<MobilityCustomerOrderSlip>>(jsonModel)!;
                    return View(model);
                }
                catch (JsonSerializationException)
                {
                    TempData["error"] = "Failed to deserialize the input JSON model.";
                return RedirectToAction(nameof(Index));
                }

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
