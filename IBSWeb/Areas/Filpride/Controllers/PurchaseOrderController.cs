using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
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
    [DepartmentAuthorize(SD.Department_Logistics, SD.Department_TradeAndSupply, SD.Department_Marketing, SD.Department_RCD)]
    public class PurchaseOrderController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        public PurchaseOrderController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
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
            if (view == nameof(DynamicView.PurchaseOrder))
            {
                var companyClaims = await GetCompanyClaimAsync();

                var purchaseOrders = await _unitOfWork.FilpridePurchaseOrder
                    .GetAllAsync(po => po.Company == companyClaims, cancellationToken);

                return View("ExportIndex", purchaseOrders);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetPurchaseOrders([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var purchaseOrders = await _unitOfWork.FilpridePurchaseOrder
                    .GetAllAsync(po => po.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    purchaseOrders = purchaseOrders
                    .Where(s =>
                        s.PurchaseOrderNo.ToLower().Contains(searchValue) ||
                        s.Supplier.SupplierName.ToLower().Contains(searchValue) ||
                        s.Product.ProductName.ToLower().Contains(searchValue) ||
                        s.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                        s.Terms.ToLower().Contains(searchValue) ||
                        s.Quantity.ToString().Contains(searchValue) ||
                        s.Remarks.ToLower().Contains(searchValue) ||
                        s.CreatedBy.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    purchaseOrders = purchaseOrders
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = purchaseOrders.Count();

                var pagedData = purchaseOrders
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
            var viewModel = new FilpridePurchaseOrder();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FilpridePurchaseOrder model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            model.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

            model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            model.Company = companyClaims;

            if (ModelState.IsValid)
            {
                try
                {
                    model.PurchaseOrderNo = await _unitOfWork.FilpridePurchaseOrder.GenerateCodeAsync(companyClaims, cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(this.User);
                    model.Amount = model.Quantity * model.Price;

                    await _dbContext.AddAsync(model, cancellationToken);

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    TempData["success"] = "Purchase Order created successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.FilpridePurchaseOrders == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var purchaseOrder = await _unitOfWork.FilpridePurchaseOrder.GetAsync(po => po.PurchaseOrderId == id, cancellationToken);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            purchaseOrder.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

            purchaseOrder.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            ViewBag.PurchaseOrders = purchaseOrder.Quantity;

            return View(purchaseOrder);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilpridePurchaseOrder model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingModel = await _dbContext.FilpridePurchaseOrders.FindAsync(model.PurchaseOrderId, cancellationToken);

                    var companyClaims = await GetCompanyClaimAsync();

                    if (existingModel == null)
                    {
                        return NotFound();
                    }

                    model.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

                    existingModel.Date = model.Date;
                    existingModel.SupplierId = model.SupplierId;
                    existingModel.ProductId = model.ProductId;
                    existingModel.Quantity = model.Quantity;
                    existingModel.Price = model.Price;
                    existingModel.Amount = model.Quantity * model.Price;
                    existingModel.SalesOrderNo = model.SalesOrderNo;
                    existingModel.Remarks = model.Remarks;
                    existingModel.Terms = model.Terms;

                    existingModel.EditedBy = _userManager.GetUserName(User);
                    existingModel.EditedDate = DateTime.Now;

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.EditedBy, $"Edited purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    TempData["success"] = "Purchase Order updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.FilprideReceivingReports == null)
            {
                return NotFound();
            }

            var purchaseOrder = await _unitOfWork.FilpridePurchaseOrder
                .GetAsync(po => po.PurchaseOrderId == id, cancellationToken);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            return View(purchaseOrder);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilpridePurchaseOrders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.PostedBy == null)
                {
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTime.Now;
                    model.Status = nameof(Status.Posted);

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.PostedBy, $"Posted purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Posted.";
                }
                return RedirectToAction(nameof(Print), new { id });
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilpridePurchaseOrders.FindAsync(id, cancellationToken);

            if (model != null)
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

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Voided.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilpridePurchaseOrders.FindAsync(id, cancellationToken);

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
                    FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Cancelled.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Preview(int? id, CancellationToken cancellationToken)
        {
            var po = await _unitOfWork.FilpridePurchaseOrder.GetAsync(po => po.PurchaseOrderId == id, cancellationToken);
            return PartialView("_PreviewPartialView", po);
        }

        [HttpGet]
        public async Task<IActionResult> ChangePrice(CancellationToken cancellationToken)
        {
            PurchaseChangePriceViewModel po = new();

            var companyClaims = await GetCompanyClaimAsync();

            po.PO = await _dbContext.FilpridePurchaseOrders
                .Where(po => po.FinalPrice == 0 && po.Company == companyClaims && po.PostedBy != null && po.QuantityReceived != 0)
                .Select(s => new SelectListItem
                {
                    Value = s.PurchaseOrderId.ToString(),
                    Text = s.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            return View(po);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePrice(PurchaseChangePriceViewModel model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingModel = await _dbContext.FilpridePurchaseOrders.FindAsync(model.POId, cancellationToken);

                    existingModel.FinalPrice = model.FinalPrice;

                    #region--Inventory Recording

                    await _unitOfWork.FilprideInventory.ChangePriceToInventoryAsync(model, cancellationToken);

                    #endregion

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Change Price updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    model.PO = await _dbContext.FilpridePurchaseOrders
                        .Where(po => po.Company == companyClaims && po.FinalPrice == 0 || po.FinalPrice == null && po.PostedBy != null && po.QuantityReceived != 0)
                        .Select(s => new SelectListItem
                        {
                            Value = s.PurchaseOrderId.ToString(),
                            Text = s.PurchaseOrderNo
                        })
                        .ToListAsync(cancellationToken);

                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }
            model.PO = await _dbContext.FilpridePurchaseOrders
                .Where(po => po.Company == companyClaims && po.FinalPrice == 0 || po.FinalPrice == null && po.PostedBy != null)
                .Select(s => new SelectListItem
                {
                    Value = s.PurchaseOrderId.ToString(),
                    Text = s.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            TempData["error"] = "The information provided was invalid.";
            return View(nameof(ChangePrice));
        }

        public async Task<IActionResult> ClosePO(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilpridePurchaseOrders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsClosed)
                {
                    model.IsClosed = true;
                    model.PostedBy = null;
                    model.CanceledBy = null;
                    model.VoidedBy = null;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Closed.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var po = await _unitOfWork.FilpridePurchaseOrder.GetAsync(x => x.PurchaseOrderId == id, cancellationToken);
            if (!po.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of purchase order# {po.PurchaseOrderNo}", "Purchase Order", ipAddress, po.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                po.IsPrinted = true;
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

            // Retrieve the selected invoices from the database
            var selectedList = await _dbContext.FilpridePurchaseOrders
                .Where(po => recordIds.Contains(po.PurchaseOrderId))
                .OrderBy(po => po.PurchaseOrderNo)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("PurchaseOrder");

            worksheet.Cells["A1"].Value = "Date";
            worksheet.Cells["B1"].Value = "Terms";
            worksheet.Cells["C1"].Value = "Quantity";
            worksheet.Cells["D1"].Value = "Price";
            worksheet.Cells["E1"].Value = "Amount";
            worksheet.Cells["F1"].Value = "FinalPrice";
            worksheet.Cells["G1"].Value = "QuantityReceived";
            worksheet.Cells["H1"].Value = "IsReceived";
            worksheet.Cells["I1"].Value = "ReceivedDate";
            worksheet.Cells["J1"].Value = "Remarks";
            worksheet.Cells["K1"].Value = "CreatedBy";
            worksheet.Cells["L1"].Value = "CreatedDate";
            worksheet.Cells["M1"].Value = "IsClosed";
            worksheet.Cells["N1"].Value = "CancellationRemarks";
            worksheet.Cells["O1"].Value = "OriginalProductId";
            worksheet.Cells["P1"].Value = "OriginalSeriesNumber";
            worksheet.Cells["Q1"].Value = "OriginalSupplierId";
            worksheet.Cells["R1"].Value = "OriginalDocumentId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 2].Value = item.Terms;
                worksheet.Cells[row, 3].Value = item.Quantity;
                worksheet.Cells[row, 4].Value = item.Price;
                worksheet.Cells[row, 5].Value = item.Amount;
                worksheet.Cells[row, 6].Value = item.FinalPrice;
                worksheet.Cells[row, 7].Value = item.QuantityReceived;
                worksheet.Cells[row, 8].Value = item.IsReceived;
                worksheet.Cells[row, 9].Value = item.ReceivedDate != default ? item.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") : default;
                worksheet.Cells[row, 10].Value = item.Remarks;
                worksheet.Cells[row, 11].Value = item.CreatedBy;
                worksheet.Cells[row, 12].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 13].Value = item.IsClosed;
                worksheet.Cells[row, 14].Value = item.CancellationRemarks;
                worksheet.Cells[row, 15].Value = item.ProductId;
                worksheet.Cells[row, 16].Value = item.PurchaseOrderNo;
                worksheet.Cells[row, 17].Value = item.SupplierId;
                worksheet.Cells[row, 18].Value = item.PurchaseOrderId;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PurchaseOrderList.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllPurchaseOrderIds()
        {
            var poIds = _dbContext.FilpridePurchaseOrders
                                     .Select(po => po.PurchaseOrderId) // Assuming Id is the primary key
                                     .ToList();

            return Json(poIds);
        }
    }
}