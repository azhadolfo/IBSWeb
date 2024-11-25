using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_Logistics, SD.Department_TradeAndSupply, SD.Department_Marketing, SD.Department_RCD, SD.Department_CreditAndCollection)]
    public class ReceivingReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        public ReceivingReportController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            if (view == nameof(DynamicView.ReceivingReport))
            {
                var companyClaims = await GetCompanyClaimAsync();

                var receivingReports = await _unitOfWork.FilprideReceivingReport
                    .GetAllAsync(rr => rr.Company == companyClaims, cancellationToken);

                return View("ExportIndex", receivingReports);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetReceivingReports([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var receivingReports = await _unitOfWork.FilprideReceivingReport
                    .GetAllAsync(rr => rr.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    receivingReports = receivingReports
                    .Where(s =>
                        s.ReceivingReportNo.ToLower().Contains(searchValue) ||
                        s.PurchaseOrder.PurchaseOrderNo.ToLower().Contains(searchValue) ||
                        s.DeliveryReceipt?.DeliveryReceiptNo.ToLower().Contains(searchValue) == true ||
                        s.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                        s.QuantityReceived.ToString().Contains(searchValue) ||
                        s.Amount.ToString().Contains(searchValue) ||
                        s.CreatedBy.ToLower().Contains(searchValue) ||
                        s.Remarks.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    receivingReports = receivingReports
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = receivingReports.Count();

                var pagedData = receivingReports
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
            var viewModel = new FilprideReceivingReport();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.PurchaseOrders = await _dbContext.FilpridePurchaseOrders
                .Where(po => po.Company == companyClaims && !po.IsReceived && po.PostedBy != null && !po.IsClosed)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FilprideReceivingReport model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            model.PurchaseOrders = await _dbContext.FilpridePurchaseOrders
                .Where(po => po.Company == companyClaims && !po.IsReceived && po.PostedBy != null)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
            if (ModelState.IsValid)
            {
                try
                {
                    #region --Retrieve PO

                    var existingPo = await _unitOfWork.FilpridePurchaseOrder.GetAsync(po => po.PurchaseOrderId == model.POId, cancellationToken);

                    #endregion --Retrieve PO

                    var totalAmountRR = existingPo.Quantity - existingPo.QuantityReceived;

                    if (model.QuantityDelivered > totalAmountRR)
                    {
                        TempData["error"] = "Input is exceed to remaining quantity delivered";
                        return View(model);
                    }

                    model.ReceivingReportNo = await _unitOfWork.FilprideReceivingReport.GenerateCodeAsync(companyClaims, existingPo.Type, cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(this.User);
                    model.GainOrLoss = model.QuantityReceived - model.QuantityDelivered;
                    model.PONo = existingPo.PurchaseOrderNo;
                    model.DueDate = await _unitOfWork.FilprideReceivingReport.ComputeDueDateAsync(model.POId, model.Date, cancellationToken);
                    model.Amount = model.QuantityReceived * existingPo.Price;
                    model.Company = companyClaims;
                    model.Type = existingPo.Type;

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new receiving report# {model.ReceivingReportNo}", "Receiving Report", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.AddAsync(model, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    TempData["success"] = "Receiving Report created successfully";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.FilprideReceivingReports == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var receivingReport = await _dbContext.FilprideReceivingReports.FindAsync(id, cancellationToken);
            if (receivingReport == null)
            {
                return NotFound();
            }

            receivingReport.PurchaseOrders = await _dbContext.FilpridePurchaseOrders
                .Where(rr => rr.Company == companyClaims)
                .Select(s => new SelectListItem
                {
                    Value = s.PurchaseOrderId.ToString(),
                    Text = s.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            return View(receivingReport);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilprideReceivingReport model, CancellationToken cancellationToken)
        {
            var existingModel = await _dbContext.FilprideReceivingReports.FindAsync(model.ReceivingReportId, cancellationToken);
            var companyClaims = await GetCompanyClaimAsync();

            existingModel.PurchaseOrders = await _dbContext.FilpridePurchaseOrders
                .Where(s => s.Company == companyClaims)
                .Select(s => new SelectListItem
                {
                    Value = s.PurchaseOrderId.ToString(),
                    Text = s.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (existingModel == null)
                    {
                        return NotFound();
                    }

                    #region --Retrieve PO

                    var po = await _dbContext
                                .FilpridePurchaseOrders
                                .Include(po => po.Supplier)
                                .Include(po => po.Product)
                                .FirstOrDefaultAsync(po => po.PurchaseOrderId == model.POId, cancellationToken);

                    #endregion --Retrieve PO

                    var rr = _dbContext.FilprideReceivingReports
                    .Where(rr => rr.Company == companyClaims && rr.PONo == po.PurchaseOrderNo)
                    .ToList();

                    var totalAmountRR = po.Quantity - po.QuantityReceived;

                    if (model.QuantityDelivered > totalAmountRR && existingModel.PostedBy == null)
                    {
                        TempData["error"] = "Input is exceed to remaining quantity delivered";
                        return View(model);
                    }

                    existingModel.Date = model.Date;
                    existingModel.POId = model.POId;

                    var existingPo = await _unitOfWork.FilpridePurchaseOrder.GetAsync(po => po.PurchaseOrderId == model.POId);

                    existingModel.PONo = existingPo.PurchaseOrderNo;

                    existingModel.DueDate = await _unitOfWork.FilprideReceivingReport.ComputeDueDateAsync(model.POId, model.Date, cancellationToken);
                    existingModel.SupplierInvoiceNumber = model.SupplierInvoiceNumber;
                    existingModel.SupplierInvoiceDate = model.SupplierInvoiceDate;
                    existingModel.SupplierDrNo = model.SupplierDrNo;
                    existingModel.WithdrawalCertificate = model.WithdrawalCertificate;
                    existingModel.TruckOrVessels = model.TruckOrVessels;
                    existingModel.QuantityDelivered = model.QuantityDelivered;
                    existingModel.QuantityReceived = model.QuantityReceived;
                    existingModel.GainOrLoss = model.QuantityReceived - model.QuantityDelivered;
                    existingModel.AuthorityToLoadNo = model.AuthorityToLoadNo;
                    existingModel.Remarks = model.Remarks;
                    existingModel.ReceivedDate = model.ReceivedDate;
                    existingModel.Amount = model.QuantityReceived * po.Price;

                    existingModel.EditedBy = _userManager.GetUserName(User);
                    existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy, $"Edited receiving report# {model.ReceivingReportNo}", "Receiving Report", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Receiving Report updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            return View(existingModel);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.FilprideReceivingReports == null)
            {
                return NotFound();
            }

            var receivingReport = await _unitOfWork.FilprideReceivingReport.GetAsync(rr => rr.ReceivingReportId == id, cancellationToken);

            if (receivingReport == null)
            {
                return NotFound();
            }

            return View(receivingReport);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideReceivingReport.GetAsync(rr => rr.ReceivingReportId == id, cancellationToken);

            if (model != null)
            {
                try
                {
                    if (model.ReceivedDate == null)
                    {
                        TempData["error"] = "Please indicate the received date.";
                        return RedirectToAction(nameof(Index));
                    }

                    if (model.PostedBy == null)
                    {
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Posted);

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.PostedBy, $"Posted receiving report# {model.ReceivingReportNo}", "Receiving Report", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _unitOfWork.FilprideReceivingReport.PostAsync(model, cancellationToken);

                        TempData["success"] = "Receiving Report has been Posted.";
                        return RedirectToAction(nameof(Print), new { id });
                    }
                    else
                    {
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideReceivingReports
                .FindAsync(id, cancellationToken);

            var existingInventory = await _dbContext.FilprideInventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Reference == model.ReceivingReportNo && i.Company == model.Company);

            if (model != null && existingInventory != null)
            {
                var hasAlreadyBeenUsed = await _dbContext.FilprideSalesInvoices
                    .AnyAsync(si => si.ReceivingReportId == model.ReceivingReportId && si.Status != nameof(Status.Voided), cancellationToken);

                if (hasAlreadyBeenUsed)
                {
                    TempData["error"] = "Please note that this record has already been utilized in a sales invoice. As a result, voiding it is not permitted.";
                    return RedirectToAction(nameof(Index));
                }

                if (model.VoidedBy == null)
                {
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                    if (model.PostedBy != null)
                    {
                        model.PostedBy = null;
                    }

                    try
                    {
                        model.VoidedBy = _userManager.GetUserName(this.User);
                        model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Voided);

                        await _unitOfWork.FilprideReceivingReport.RemoveRecords<FilpridePurchaseBook>(pb => pb.DocumentNo == model.ReceivingReportNo, cancellationToken);
                        await _unitOfWork.FilprideReceivingReport.RemoveRecords<FilprideGeneralLedgerBook>(pb => pb.Reference == model.ReceivingReportNo, cancellationToken);
                        await _unitOfWork.FilprideInventory.VoidInventory(existingInventory, cancellationToken);
                        await _unitOfWork.FilprideReceivingReport.RemoveQuantityReceived(model.POId, model.QuantityReceived, cancellationToken);
                        model.QuantityReceived = 0;

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided receiving report# {model.ReceivingReportNo}", "Receiving Report", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Receiving Report has been Voided.";
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        TempData["error"] = ex.Message;
                        return RedirectToAction(nameof(Index));
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideReceivingReports.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model.CanceledQuantity = model.QuantityDelivered < model.QuantityReceived ? model.QuantityDelivered : model.QuantityReceived;
                    model.QuantityDelivered = 0;
                    model.QuantityReceived = 0;
                    model.Status = nameof(Status.Canceled);
                    model.CancellationRemarks = cancellationRemarks;

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled receiving report# {model.ReceivingReportNo}", "Receiving Report", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Receiving Report has been Cancelled.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetLiquidations(int id, CancellationToken cancellationToken)
        {
            var po = await _unitOfWork.FilpridePurchaseOrder.GetAsync(po => po.PurchaseOrderId == id, cancellationToken);

            var rrPostedOnly = await _dbContext
                .FilprideReceivingReports
                .Where(rr => rr.Company == po.Company && rr.PONo == po.PurchaseOrderNo && rr.PostedBy != null)
                .ToListAsync(cancellationToken);

            var rr = await _dbContext
                .FilprideReceivingReports
                .Where(rr => rr.Company == po.Company && rr.PONo == po.PurchaseOrderNo)
                .ToListAsync(cancellationToken);

            var rrNotPosted = await _dbContext
                .FilprideReceivingReports
                .Where(rr => rr.Company == po.Company && rr.PONo == po.PurchaseOrderNo && rr.PostedBy == null && rr.CanceledBy == null)
                .ToListAsync(cancellationToken);

            var rrCanceled = await _dbContext
                .FilprideReceivingReports
                .Where(rr => rr.Company == po.Company && rr.PONo == po.PurchaseOrderNo && rr.CanceledBy != null)
                .ToListAsync(cancellationToken);

            if (po != null)
            {
                return Json(new
                {
                    poNo = po.PurchaseOrderNo,
                    poQuantity = po.Quantity.ToString("N4"),
                    rrList = rr,
                    rrListPostedOnly = rrPostedOnly,
                    rrListNotPosted = rrNotPosted,
                    rrListCanceled = rrCanceled
                });
            }
            else
            {
                return Json(null);
            }
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var rr = await _unitOfWork.FilprideReceivingReport.GetAsync(x => x.ReceivingReportId == id, cancellationToken);
            if (!rr.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of receiving report# {rr.ReceivingReportNo}", "Receiving Report", ipAddress, rr.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                rr.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }

        //Download as .xlsx file.(Export)

        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected records from the database
            var selectedList = await _dbContext.FilprideReceivingReports
                .Where(rr => recordIds.Contains(rr.ReceivingReportId))
                .OrderBy(rr => rr.ReceivingReportNo)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("ReceivingReport");

            worksheet.Cells["A1"].Value = "Date";
            worksheet.Cells["B1"].Value = "DueDate";
            worksheet.Cells["C1"].Value = "SupplierInvoiceNumber";
            worksheet.Cells["D1"].Value = "SupplierInvoiceDate";
            worksheet.Cells["E1"].Value = "TruckOrVessels";
            worksheet.Cells["F1"].Value = "QuantityDelivered";
            worksheet.Cells["G1"].Value = "QuantityReceived";
            worksheet.Cells["H1"].Value = "GainOrLoss";
            worksheet.Cells["I1"].Value = "Amount";
            worksheet.Cells["J1"].Value = "OtherRef";
            worksheet.Cells["K1"].Value = "Remarks";
            worksheet.Cells["L1"].Value = "AmountPaid";
            worksheet.Cells["M1"].Value = "IsPaid";
            worksheet.Cells["N1"].Value = "PaidDate";
            worksheet.Cells["O1"].Value = "CanceledQuantity";
            worksheet.Cells["P1"].Value = "CreatedBy";
            worksheet.Cells["Q1"].Value = "CreatedDate";
            worksheet.Cells["R1"].Value = "CancellationRemarks";
            worksheet.Cells["S1"].Value = "ReceivedDate";
            worksheet.Cells["T1"].Value = "OriginalPOId";
            worksheet.Cells["U1"].Value = "OriginalSeriesNumber";
            worksheet.Cells["V1"].Value = "OriginalDocumentId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 2].Value = item.DueDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 3].Value = item.SupplierInvoiceNumber;
                worksheet.Cells[row, 4].Value = item.SupplierInvoiceDate;
                worksheet.Cells[row, 5].Value = item.TruckOrVessels;
                worksheet.Cells[row, 6].Value = item.QuantityDelivered;
                worksheet.Cells[row, 7].Value = item.QuantityReceived;
                worksheet.Cells[row, 8].Value = item.GainOrLoss;
                worksheet.Cells[row, 9].Value = item.Amount;
                worksheet.Cells[row, 10].Value = item.AuthorityToLoadNo;
                worksheet.Cells[row, 11].Value = item.Remarks;
                worksheet.Cells[row, 12].Value = item.AmountPaid;
                worksheet.Cells[row, 13].Value = item.IsPaid;
                worksheet.Cells[row, 14].Value = item.PaidDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 15].Value = item.CanceledQuantity;
                worksheet.Cells[row, 16].Value = item.CreatedBy;
                worksheet.Cells[row, 17].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 18].Value = item.CancellationRemarks;
                worksheet.Cells[row, 19].Value = item.ReceivedDate?.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 20].Value = item.POId;
                worksheet.Cells[row, 21].Value = item.ReceivingReportNo;
                worksheet.Cells[row, 22].Value = item.ReceivingReportId;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReceivingReportList.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllReceivingReportIds()
        {
            var rrIds = _dbContext.FilprideReceivingReports
                                     .Select(rr => rr.ReceivingReportId) // Assuming Id is the primary key
                                     .ToList();

            return Json(rrIds);
        }
    }
}