using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area("Filpride")]
    public class ReceivingReportController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        public ReceivingReportController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var rrList = await _unitOfWork.FilprideReceivingReport
                .GetAllAsync(null, cancellationToken);

            return View(rrList);
        }

        //TODO create crud operation of RR
        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            ReceivingReportViewModel viewModel = new()
            {
                PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsync(cancellationToken),
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ReceivingReportViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var purchaseOrder = await _unitOfWork.FilpridePurchaseOrder
                        .GetAsync(po => po.PurchaseOrderId == viewModel.PurchaseOrderId, cancellationToken);

                    FilprideReceivingReport model = new()
                    {
                        ReceivingReportNo = await _unitOfWork.FilprideReceivingReport.GenerateCodeAsync(cancellationToken),
                        Date = viewModel.Date,
                        DueDate = await _unitOfWork.FilprideReceivingReport.CalculateDueDateAsync(purchaseOrder.Terms, viewModel.Date, cancellationToken),
                        PurchaseOrderId = viewModel.PurchaseOrderId,
                        SupplierSiNo = viewModel.SupplierSiNo,
                        SupplierSiDate = viewModel.SupplierSiDate,
                        SupplierDrNo = viewModel.SupplierDrNo,
                        SupplierDrDate = viewModel.SupplierDrDate,
                        TruckOrVessels = viewModel.TruckOrVessels,
                        OtherReference = viewModel.OtherReference,
                        QuantityDelivered = viewModel.QuantityDelivered,
                        QuantityReceived = viewModel.QuantityReceived,
                        GainOrLoss = viewModel.QuantityDelivered - viewModel.QuantityReceived,
                        TotalAmount = viewModel.QuantityReceived * purchaseOrder.UnitCost,
                        Freight = viewModel.Freight,
                        TotalFreight = viewModel.TotalFreight,
                        Remarks = viewModel.Remarks,
                        CreatedBy = _userManager.GetUserName(User)
                    };

                    if (purchaseOrder.Supplier.VatType == SD.VatType_Vatable)
                    {
                        model.NetOfVatAmount = model.TotalAmount / 1.12m;
                        model.VatAmount = model.NetOfVatAmount * .12m;
                    }
                    else
                    {
                        model.NetOfVatAmount = model.TotalAmount;
                        model.VatAmount = 0;
                    }

                    if (purchaseOrder.Supplier.TaxType == SD.TaxType_WithTax)
                    {
                        model.NetOfTaxAmount = model.NetOfVatAmount * 0.01m;
                    }
                    else
                    {
                        model.NetOfTaxAmount = model.NetOfVatAmount;
                    }

                    await _unitOfWork.FilprideReceivingReport.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);
                    TempData["success"] = "Receiving report created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsync(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }
    }
}