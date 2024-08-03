using IBS.DataAccess.Data;
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

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var purchaseOrders = await _unitOfWork.FilpridePurchaseOrderRepo.GetAllAsync(null, cancellationToken);

            foreach (var po in purchaseOrders)
            {
                var rrList = await _dbContext.ReceivingReports
                    .Where(rr => rr.PONo == po.PurchaseOrderNo)
                    .ToListAsync(cancellationToken);

                po.RrList = rrList;
            }

            return View(purchaseOrders);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new PurchaseOrder();
            viewModel.Suppliers = await _dbContext.FilprideSuppliers
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName
                })
                .ToListAsync(cancellationToken);

            viewModel.Products = await _dbContext.Products
                .Select(s => new SelectListItem
                {
                    Value = s.ProductId.ToString(),
                    Text = s.ProductName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrder model, CancellationToken cancellationToken)
        {
            model.Suppliers = await _dbContext.FilprideSuppliers
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName
                })
                .ToListAsync(cancellationToken);

            model.Products = await _dbContext.Products
                .Select(s => new SelectListItem
                {
                    Value = s.ProductId.ToString(),
                    Text = s.ProductName
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                var generatedPO = await _unitOfWork.FilpridePurchaseOrderRepo.GenerateCodeAsync(cancellationToken);

                model.PurchaseOrderNo = generatedPO;
                model.CreatedBy = _userManager.GetUserName(this.User);
                model.Amount = model.Quantity * model.Price;

                ///PENDING - leo
                //model.SupplierNo = await _unitOfWork.GetSupplierNoAsync(model.SupplierId, cancellationToken);
                //model.ProductNo = await _purchaseOrderRepo.GetProductNoAsync(model.ProductId, cancellationToken);

                await _dbContext.AddAsync(model, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
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

            var purchaseOrder = await _unitOfWork.FilpridePurchaseOrderRepo.GetAsync(po => po.PurchaseOrderId == id, cancellationToken);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            purchaseOrder.Suppliers = await _dbContext.FilprideSuppliers
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName
                })
                .ToListAsync(cancellationToken);

            purchaseOrder.Products = await _dbContext.Products
                .Select(s => new SelectListItem
                {
                    Value = s.ProductId.ToString(),
                    Text = s.ProductName
                })
                .ToListAsync(cancellationToken);

            ViewBag.PurchaseOrders = purchaseOrder.Quantity;

            return View(purchaseOrder);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PurchaseOrder model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                var existingModel = await _dbContext.PurchaseOrders.FindAsync(model.PurchaseOrderId, cancellationToken);

                if (existingModel == null)
                {
                    return NotFound();
                }

                model.Suppliers = await _dbContext.FilprideSuppliers
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName
                })
                .ToListAsync(cancellationToken);

                model.Products = await _dbContext.Products
                .Select(s => new SelectListItem
                {
                    Value = s.ProductId.ToString(),
                    Text = s.ProductName
                })
                .ToListAsync(cancellationToken);

                existingModel.Date = model.Date;
                existingModel.SupplierId = model.SupplierId;
                existingModel.ProductId = model.ProductId;
                existingModel.Quantity = model.Quantity;
                existingModel.Price = model.Price;
                existingModel.Amount = model.Quantity * model.Price;
                existingModel.Remarks = model.Remarks;

                ///PENDING - leo
                //existingModel.SupplierNo = await _purchaseOrderRepo.GetSupplierNoAsync(model.SupplierId, cancellationToken);
                //existingModel.ProductNo = await _purchaseOrderRepo.GetProductNoAsync(model.ProductId, cancellationToken);

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

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
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

            po.PO = await _dbContext.PurchaseOrders
                .Where(po => po.FinalPrice == 0 || po.FinalPrice == null && po.PostedBy != null)
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
                        .Where(po => po.FinalPrice == 0 || po.FinalPrice == null && po.PostedBy != null)
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
                .Where(po => po.FinalPrice == 0 || po.FinalPrice == null && po.PostedBy != null)
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