using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class PurchaseOrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        public PurchaseOrderController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var purchaseOrders = await _unitOfWork.MobilityPurchaseOrder
                .GetAllAsync(null, cancellationToken);

            return View(purchaseOrders);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            PurchaseOrderViewModel viewModel = new()
            {
                Products = await _unitOfWork.GetProductListAsyncById(cancellationToken),
                Suppliers = await _unitOfWork.GetMobilitySupplierListAsyncById(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PurchaseOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    var claims = await _userManager.GetClaimsAsync(user);
                    var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode")?.Value ?? "ALL";

                    MobilityPurchaseOrder model = new()
                    {
                        PurchaseOrderNo = await _unitOfWork.MobilityPurchaseOrder.GenerateCodeAsync(stationCodeClaim, cancellationToken),
                        Date = viewModel.Date,
                        SupplierId = viewModel.SupplierId,
                        ProductId = viewModel.ProductId,
                        Quantity = viewModel.Quantity,
                        UnitPrice = viewModel.UnitPrice,
                        Amount = viewModel.Quantity * viewModel.UnitPrice,
                        Discount = viewModel.Discount,
                        Remarks = viewModel.Remarks,
                        StationCode = stationCodeClaim,
                        CreatedBy = _userManager.GetUserName(User),
                    };

                    model.TotalAmount = model.Amount - model.Discount;

                    await _unitOfWork.MobilityPurchaseOrder.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);

                    TempData["success"] = "Purchase order created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.Suppliers = await _unitOfWork.GetMobilitySupplierListAsyncById(cancellationToken);
                    viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }
            viewModel.Suppliers = await _unitOfWork.GetMobilitySupplierListAsyncById(cancellationToken);
            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.MobilityPurchaseOrder
                    .GetAsync(po => po.PurchaseOrderNo == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                PurchaseOrderViewModel viewModel = new()
                {
                    PurchaseOrderId = existingRecord.PurchaseOrderId,
                    Date = existingRecord.Date,
                    SupplierId = existingRecord.SupplierId,
                    Suppliers = await _unitOfWork.GetMobilitySupplierListAsyncById(cancellationToken),
                    ProductId = existingRecord.ProductId,
                    Products = await _unitOfWork.GetProductListAsyncById(cancellationToken),
                    Quantity = existingRecord.Quantity,
                    UnitPrice = existingRecord.UnitPrice,
                    Discount = existingRecord.Discount,
                    Remarks = existingRecord.Remarks
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PurchaseOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    viewModel.CurrentUser = _userManager.GetUserName(User);
                    await _unitOfWork.MobilityPurchaseOrder.UpdateAsync(viewModel, cancellationToken);

                    TempData["success"] = "Purchase order updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.Suppliers = await _unitOfWork.GetMobilitySupplierListAsyncById(cancellationToken);
                    viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Suppliers = await _unitOfWork.GetMobilitySupplierListAsyncById(cancellationToken);
            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> Preview(string? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.MobilityPurchaseOrder
                    .GetAsync(po => po.PurchaseOrderNo == id, cancellationToken);

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

        public async Task<IActionResult> Post(string? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.MobilityPurchaseOrder
                    .GetAsync(po => po.PurchaseOrderNo == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.PostedBy == null)
                {
                    existingRecord.PostedBy = _userManager.GetUserName(User);
                    existingRecord.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    await _unitOfWork.MobilityPurchaseOrder.PostAsync(existingRecord, cancellationToken);
                }

                TempData["success"] = "Purchase order approved successfully.";
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Preview), new { id });
            }
        }
    }
}
