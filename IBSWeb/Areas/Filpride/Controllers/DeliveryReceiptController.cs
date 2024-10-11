using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_RCD, SD.Department_Finance, SD.Department_Marketing, SD.Department_TradeAndSupply, SD.Department_Logistics, SD.Department_CreditAndCollection)]
    public class DeliveryReceiptController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public DeliveryReceiptController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetDeliveryReceipts([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var drList = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAllAsync(cos => cos.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    drList = drList
                    .Where(s =>
                        s.DeliveryReceiptNo.ToLower().Contains(searchValue) ||
                        s.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                        s.Customer.CustomerName?.ToLower().Contains(searchValue) == true ||
                        s.Quantity.ToString().Contains(searchValue) == true ||
                        s.TotalAmount.ToString().Contains(searchValue) == true ||
                        s.Remarks?.ToLower().Contains(searchValue) == true
                        )
                    .ToList();

                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    drList = drList
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = drList.Count();

                var pagedData = drList
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedData
                });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            DeliveryReceiptViewModel viewModel = new()
            {
                Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken),
                CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken),
                Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    FilprideDeliveryReceipt model = new()
                    {
                        DeliveryReceiptNo = await _unitOfWork.FilprideDeliveryReceipt.GenerateCodeAsync(cancellationToken),
                        Date = viewModel.Date,
                        EstimatedTimeOfArrival = viewModel.ETA,
                        CustomerOrderSlipId = viewModel.CustomerOrderSlipId,
                        CustomerId = viewModel.CustomerId,
                        Remarks = viewModel.Remarks,
                        Quantity = viewModel.Volume,
                        TotalAmount = viewModel.TotalAmount,
                        Company = companyClaims,
                        CreatedBy = _userManager.GetUserName(User),
                        ManualDrNo = viewModel.ManualDrNo,
                        Freight = viewModel.Freight,
                        Demuragge = viewModel.Demuragge,
                        ECC = viewModel.ECC,
                        Driver = viewModel.Driver,
                        PlateNo = viewModel.PlateNo,
                        HaulerId = viewModel.HaulerId,
                        Status = "Draft"
                    };

                    await _unitOfWork.FilprideDeliveryReceipt.AddAsync(model, cancellationToken);

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, model.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                    await _unitOfWork.SaveAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Delivery receipt created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
                    viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
            viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            try
            {
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                DeliveryReceiptViewModel viewModel = new()
                {
                    DeliverReceiptId = existingRecord.DeliveryReceiptId,
                    Date = existingRecord.Date,
                    ETA = existingRecord.EstimatedTimeOfArrival,
                    CustomerId = existingRecord.Customer.CustomerId,
                    Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken),
                    CustomerAddress = existingRecord.Customer.CustomerAddress,
                    CustomerTin = existingRecord.Customer.CustomerTin,
                    CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                    CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken),
                    Product = existingRecord.CustomerOrderSlip.PurchaseOrder.Product.ProductName,
                    CosVolume = existingRecord.CustomerOrderSlip.Quantity,
                    RemainingVolume = existingRecord.CustomerOrderSlip.BalanceQuantity,
                    Price = existingRecord.CustomerOrderSlip.DeliveredPrice,
                    Volume = existingRecord.Quantity,
                    TotalAmount = existingRecord.TotalAmount,
                    Remarks = existingRecord.Remarks,
                    ManualDrNo = existingRecord.ManualDrNo,
                    Freight = existingRecord.Freight,
                    Demuragge = existingRecord.Demuragge,
                    ECC = existingRecord.ECC,
                    Driver = existingRecord.Driver,
                    PlateNo = existingRecord.PlateNo,
                    HaulerId = existingRecord.HaulerId,
                    Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken)
                };

                ViewBag.DeliveryOption = existingRecord.CustomerOrderSlip.DeliveryOption;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    viewModel.CurrentUser = _userManager.GetUserName(User);
                    await _unitOfWork.FilprideDeliveryReceipt.UpdateAsync(viewModel, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Delivery receipt updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
                    viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
            viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> Preview(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                return View(existingRecord);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(cos => cos.DeliveryReceiptId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (!existingRecord.IsPrinted)
                {
                    existingRecord.IsPrinted = true;
                    await _unitOfWork.SaveAsync(cancellationToken);
                }

                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        public async Task<IActionResult> Post(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(cos => cos.DeliveryReceiptId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.PostedBy == null)
                {
                    existingRecord.PostedBy = _userManager.GetUserName(User);
                    existingRecord.PostedDate = DateTime.Now;
                    existingRecord.Status = nameof(Status.Posted);

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingRecord.PostedBy, $"Posted delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, existingRecord.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);
                }

                TempData["success"] = "Delivery receipt approved successfully.";
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        public async Task<IActionResult> GetCustomerDetails(int? id)
        {
            if (id == null)
            {
                return Json(null);
            }

            var customerDto = await _unitOfWork.FilprideDeliveryReceipt.MapCustomerToDTO(id, null);

            if (customerDto == null)
            {
                return Json(null);
            }

            return Json(new
            {
                Address = customerDto.CustomerAddress,
                TinNo = customerDto.CustomerTin
            });
        }

        public async Task<IActionResult> GetCustomerOrderSlipList(int customerId)
        {
            var orderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListPerCustomerNotDeliveredAsync(customerId);

            return Json(orderSlips);
        }

        public async Task<IActionResult> GetCosDetails(int? id)
        {
            if (id == null)
            {
                return Json(null);
            }

            var cos = await _unitOfWork.FilprideCustomerOrderSlip.GetAsync(cos => cos.CustomerOrderSlipId == id);

            if (cos == null)
            {
                return Json(null);
            }

            return Json(new
            {
                Product = cos.PurchaseOrder.Product?.ProductName,
                cos.Quantity,
                RemainingVolume = cos.BalanceQuantity,
                Price = cos.DeliveredPrice,
                cos.DeliveryOption,
                cos.Freight
            });
        }

        public async Task<IActionResult> Delivered(int? id, string deliveredDate, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(cos => cos.DeliveryReceiptId == id, cancellationToken);

            if (existingRecord == null)
            {
                return BadRequest();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                existingRecord.DeliveredDate = DateOnly.Parse(deliveredDate);
                existingRecord.Status = "Delivered";

                await _unitOfWork.FilprideReceivingReport.AutoGenerateReceivingReport(existingRecord, cancellationToken);
                await _unitOfWork.FilprideDeliveryReceipt.PostAsync(existingRecord, cancellationToken);

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User), $"Mark as delivered the delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = "Product has been delivered";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.Status = nameof(Status.Canceled);
                    model.CancellationRemarks = cancellationRemarks;

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, model.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _unitOfWork.SaveAsync(cancellationToken);
                    TempData["success"] = "Delivery Receipt has been canceled.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

            if (model != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (model.VoidedBy == null)
                    {
                        if (model.PostedBy != null)
                        {
                            model.PostedBy = null;
                        }

                        model.VoidedBy = _userManager.GetUserName(this.User);
                        model.VoidedDate = DateTime.Now;
                        model.Status = nameof(Status.Voided);

                        await _unitOfWork.FilprideDeliveryReceipt.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.DeliveryReceiptNo, cancellationToken);
                        await _unitOfWork.FilprideDeliveryReceipt.DeductTheVolumeToCos(model.CustomerOrderSlipId, model.Quantity, cancellationToken);

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, model.Company);
                        await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _unitOfWork.SaveAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Delivery receipt has been Voided.";
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return BadRequest();
        }

        public async Task<IActionResult> BookAuthorityToLoad(int id, string? supplierAtlNo, DateOnly bookedDate, CancellationToken cancellationToken)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

            if (existingRecord == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                FilprideAuthorityToLoad model = new()
                {
                    AuthorityToLoadNo = await _unitOfWork.FilprideAuthorityToLoad.GenerateAtlNo(cancellationToken),
                    DeliveryReceiptId = existingRecord.DeliveryReceiptId,
                    DateBooked = bookedDate,
                    ValidUntil = bookedDate.AddDays(5),
                    UppiAtlNo = supplierAtlNo,
                    Remarks = "Please secure delivery documents. FILPRIDE DR / SUPPLIER DR / WITHDRAWAL CERTIFICATE",
                    CreatedBy = _userManager.GetUserName(User),
                    CreatedDate = DateTime.Now,
                };

                await _unitOfWork.FilprideAuthorityToLoad.AddAsync(model, cancellationToken);

                existingRecord.AuthorityToLoadNo = model.AuthorityToLoadNo;
                existingRecord.Status = nameof(Status.Pending);

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User), $"Book ATL for delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                TempData["success"] = "ATL booked successfully";
                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(AuthorityToLoadController.Print), "AuthorityToLoad", new { area = nameof(Filpride), id = model.AuthorityToLoadId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> GenerateExcel(int id)
        {
            var deliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(dr => dr.DeliveryReceiptId == id);

            if (deliveryReceipt == null)
            {
                return NotFound();
            }

            var receivingReport = await _unitOfWork.FilprideReceivingReport.GetAsync(rr => rr.DeliveryReceiptId == deliveryReceipt.DeliveryReceiptId);

            // Get the full path to the template in the wwwroot folder
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, "templates", "DR Format.xlsx");
            byte[] fileBytes = System.IO.File.ReadAllBytes(templatePath);

            using (var package = new ExcelPackage(new MemoryStream(fileBytes)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0]; // Assuming the template has one sheet

                worksheet.Cells["H2"].Value = deliveryReceipt.AuthorityToLoadNo;
                worksheet.Cells["H7"].Value = receivingReport.ReceivingReportNo;
                worksheet.Cells["H9"].Value = deliveryReceipt.DeliveryReceiptNo;
                worksheet.Cells["H10"].Value = deliveryReceipt.Date.ToString("dd-MMM-yy");
                worksheet.Cells["H12"].Value = deliveryReceipt.CustomerOrderSlip.CustomerOrderSlipNo;
                worksheet.Cells["B11"].Value = deliveryReceipt.CustomerOrderSlip.PickUpPoint.Depot.ToUpper();
                worksheet.Cells["C12"].Value = deliveryReceipt.Customer.CustomerName.ToUpper();
                worksheet.Cells["C13"].Value = deliveryReceipt.Customer.CustomerAddress.ToUpper();
                worksheet.Cells["B17"].Value = deliveryReceipt.CustomerOrderSlip.Product.ProductName;
                worksheet.Cells["H17"].Value = deliveryReceipt.Quantity.ToString("N0");
                worksheet.Cells["H19"].Value = $"{deliveryReceipt.CustomerOrderSlip.PurchaseOrder.PurchaseOrderNo} {deliveryReceipt.CustomerOrderSlip.DeliveryOption}";

                var stream = new MemoryStream();
                package.SaveAs(stream);

                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{deliveryReceipt.DeliveryReceiptNo}.xlsx");
            }
        }
    }
}
