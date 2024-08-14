using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_Logistics, SD.Department_TradeAndSupply, SD.Department_Marketing, SD.Department_RCD)]
    public class PurchaseOrdersController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        public PurchaseOrdersController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
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

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var purchaseOrders = await _unitOfWork.FilpridePurchaseOrderRepo.GetAllAsync(po => po.Company == companyClaims, cancellationToken);

            foreach (var po in purchaseOrders)
            {
                po.RrList = await _dbContext.ReceivingReports
                    .Where(rr => rr.Company == companyClaims && rr.PONo == po.PurchaseOrderNo)
                    .ToListAsync(cancellationToken);
            }

            var findUser = await _dbContext.ApplicationUsers
                .Where(user => user.Id == _userManager.GetUserId(this.User))
                .FirstOrDefaultAsync();

            ViewBag.GetUserDepartment = findUser?.Department;

            return View(purchaseOrders);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new PurchaseOrder();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrder model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            model.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

            model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            model.Company = companyClaims;

            if (ModelState.IsValid)
            {
                model.PurchaseOrderNo = await _unitOfWork.FilpridePurchaseOrderRepo.GenerateCodeAsync(companyClaims, cancellationToken);
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
            if (id == null || _dbContext.PurchaseOrders == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var purchaseOrder = await _unitOfWork.FilpridePurchaseOrderRepo.GetAsync(po => po.PurchaseOrderId == id, cancellationToken);
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
        public async Task<IActionResult> Edit(PurchaseOrder model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                var existingModel = await _dbContext.PurchaseOrders.FindAsync(model.PurchaseOrderId, cancellationToken);

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

                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Purchase Order updated successfully";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.ReceivingReports == null)
            {
                return NotFound();
            }

            var purchaseOrder = await _unitOfWork.FilpridePurchaseOrderRepo
                .GetAsync(po => po.PurchaseOrderId == id, cancellationToken);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            return View(purchaseOrder);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.PurchaseOrders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.PostedBy == null)
                {
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTime.Now;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Posted.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.PurchaseOrders.FindAsync(id, cancellationToken);

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

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.PurchaseOrders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

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
            var po = await _unitOfWork.FilpridePurchaseOrderRepo.GetAsync(po => po.PurchaseOrderId == id, cancellationToken);
            return PartialView("_PreviewPartialView", po);
        }

        [HttpGet]
        public async Task<IActionResult> ChangePrice(CancellationToken cancellationToken)
        {
            PurchaseChangePriceViewModel po = new();

            var companyClaims = await GetCompanyClaimAsync();

            po.PO = await _dbContext.PurchaseOrders
                .Where(po => po.FinalPrice == 0 || po.FinalPrice == null && po.Company == companyClaims && po.PostedBy != null)
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
                    var existingModel = await _dbContext.PurchaseOrders.FindAsync(model.POId, cancellationToken);

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
                    model.PO = await _dbContext.PurchaseOrders
                        .Where(po => po.Company == companyClaims && po.FinalPrice == 0 || po.FinalPrice == null && po.PostedBy != null)
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
            model.PO = await _dbContext.PurchaseOrders
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
            var model = await _dbContext.PurchaseOrders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsClosed)
                {
                    model.IsClosed = true;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Closed.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }
    }
}