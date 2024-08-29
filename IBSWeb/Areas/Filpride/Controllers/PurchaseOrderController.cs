using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetPurchaseOrders([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var purchaseOrders = await _unitOfWork.FilpridePurchaseOrderRepository
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
                        s.Date.ToString().Contains(searchValue) ||
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
                model.PurchaseOrderNo = await _unitOfWork.FilpridePurchaseOrderRepository.GenerateCodeAsync(companyClaims, cancellationToken);
                model.CreatedBy = _userManager.GetUserName(this.User);
                model.Amount = model.Quantity * model.Price;

                await _dbContext.AddAsync(model, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Purchase Order created successfully";
                return RedirectToAction("Index");
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

            var purchaseOrder = await _unitOfWork.FilpridePurchaseOrderRepository.GetAsync(po => po.PurchaseOrderId == id, cancellationToken);
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
                existingModel.Remarks = model.Remarks;
                existingModel.Terms = model.Terms;
                existingModel.Port = model.Port;

                existingModel.EditedBy = _userManager.GetUserName(User);
                existingModel.EditedDate = DateTime.Now;

                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Purchase Order updated successfully";
                return RedirectToAction("Index");
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

            var purchaseOrder = await _unitOfWork.FilpridePurchaseOrderRepository
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

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Voided.";
                }
                return RedirectToAction("Index");
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

                    ///PENDING - leo
                    //model.CancellationRemarks = cancellationRemarks;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Cancelled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Preview(int? id, CancellationToken cancellationToken)
        {
            var po = await _unitOfWork.FilpridePurchaseOrderRepository.GetAsync(po => po.PurchaseOrderId == id, cancellationToken);
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
                    return RedirectToAction("Index");
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
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.FilpridePurchaseOrderRepository.GetAsync(x => x.PurchaseOrderId == id, cancellationToken);
            if (cv?.IsPrinted == false)
            {
                #region --Audit Trail Recording

                //var printedBy = _userManager.GetUserName(this.User);
                //AuditTrail auditTrail = new(printedBy, $"Printed original copy of cv# {cv.CVNo}", "Check Vouchers");
                //await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }
    }
}