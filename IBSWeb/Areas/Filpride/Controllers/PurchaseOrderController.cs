﻿using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area("Filpride")]
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
            var poList = await _unitOfWork.FilpridePurchaseOrder
                .GetAllAsync(null, cancellationToken);

            return View(poList);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            PurchaseOrderViewModel viewModel = new()
            {
                Suppliers = await _unitOfWork.GetSupplierListAsyncById(cancellationToken),
                Products = await _unitOfWork.GetProductListAsyncById(cancellationToken)
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
                    FilpridePurchaseOrder model = new FilpridePurchaseOrder
                    {
                        PurchaseOrderNo = await _unitOfWork.FilpridePurchaseOrder.GenerateCodeAsync(cancellationToken),
                        Date = viewModel.Date,
                        SupplierId = viewModel.SupplierId,
                        ProductId = viewModel.ProductId,
                        Quantity = viewModel.Quantity,
                        UnitCost = viewModel.UnitCost,
                        TotalAmount = viewModel.Quantity * viewModel.UnitCost,
                        Terms = viewModel.Terms,
                        Remarks = viewModel.Remarks,
                        CreatedBy = _userManager.GetUserName(User)
                    };

                    await _unitOfWork.FilpridePurchaseOrder.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);

                    TempData["success"] = "Purchase order created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.Suppliers = await _unitOfWork.GetSupplierListAsyncById(cancellationToken);
                    viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Suppliers = await _unitOfWork.GetSupplierListAsyncById(cancellationToken);
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
                var existingRecord = await _unitOfWork.FilpridePurchaseOrder
                    .GetAsync(po => po.PurchaseOrderNo == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                PurchaseOrderViewModel viewModel = new PurchaseOrderViewModel
                {
                    PurchaseOrderId = existingRecord.PurchaseOrderId,
                    Date = existingRecord.Date,
                    ProductId = existingRecord.ProductId,
                    SupplierId = existingRecord.SupplierId,
                    Terms = existingRecord.Terms,
                    Quantity = existingRecord.Quantity,
                    UnitCost = existingRecord.UnitCost,
                    Remarks = existingRecord.Remarks,
                    Suppliers = await _unitOfWork.GetSupplierListAsyncById(cancellationToken),
                    Products = await _unitOfWork.GetProductListAsyncById(cancellationToken)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PurchaseOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                }
                catch (Exception ex)
                {
                    viewModel.Suppliers = await _unitOfWork.GetSupplierListAsyncById(cancellationToken);
                    viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Suppliers = await _unitOfWork.GetSupplierListAsyncById(cancellationToken);
            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }
    }
}