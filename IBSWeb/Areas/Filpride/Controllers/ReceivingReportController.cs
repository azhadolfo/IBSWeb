using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
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

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            ReceivingReportViewModel viewModel = new()
            {
                DeliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken),
                Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken),
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
                    var deliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                        .GetAsync(po => po.DeliveryReceiptId == viewModel.DeliveryReceiptId, cancellationToken);

                    FilprideReceivingReport model = new()
                    {
                        ReceivingReportNo = await _unitOfWork.FilprideReceivingReport.GenerateCodeAsync(cancellationToken),
                        Date = viewModel.Date,
                        DueDate = _unitOfWork.FilprideReceivingReport.CalculateDueDate(deliveryReceipt.CustomerOrderSlip.PurchaseOrder.Terms, viewModel.Date, cancellationToken),
                        DeliveryReceiptId = viewModel.DeliveryReceiptId,
                        CustomerId = viewModel.CustomerId,
                        SupplierSiNo = viewModel.SupplierSiNo,
                        SupplierSiDate = viewModel.SupplierSiDate,
                        SupplierDrNo = viewModel.SupplierDrNo,
                        SupplierDrDate = viewModel.SupplierDrDate,
                        WithdrawalCertificate = viewModel.WithdrawalCertificate,
                        OtherReference = viewModel.OtherReference,
                        QuantityDelivered = viewModel.QuantityDelivered,
                        QuantityReceived = viewModel.QuantityReceived,
                        GainOrLoss = viewModel.QuantityReceived - viewModel.QuantityDelivered,
                        TotalAmount = viewModel.QuantityReceived * deliveryReceipt.CustomerOrderSlip.PurchaseOrder.UnitCost,
                        TotalFreight = viewModel.TotalFreight,
                        Remarks = viewModel.Remarks,
                        CreatedBy = _userManager.GetUserName(User)
                    };

                    if (deliveryReceipt.CustomerOrderSlip.PurchaseOrder.Supplier.VatType == SD.VatType_Vatable)
                    {
                        model.NetOfVatAmount = _unitOfWork.FilprideReceivingReport.ComputeNetOfVat(model.TotalAmount);
                        model.VatAmount = _unitOfWork.FilprideReceivingReport.ComputeVatAmount(model.TotalAmount);
                    }
                    else
                    {
                        model.NetOfVatAmount = model.TotalAmount;
                        model.VatAmount = 0;
                    }

                    if (deliveryReceipt.CustomerOrderSlip.PurchaseOrder.Supplier.TaxType == SD.TaxType_WithTax)
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
                    viewModel.DeliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken);
                    viewModel.Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.DeliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken);
            viewModel.Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken);
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
                var existingRecord = await _unitOfWork.FilprideReceivingReport
                    .GetAsync(rr => rr.ReceivingReportNo == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                ReceivingReportViewModel viewModel = new()
                {
                    ReceivingReportId = existingRecord.ReceivingReportId,
                    Date = existingRecord.Date,
                    DeliveryReceiptId = existingRecord.DeliveryReceiptId,
                    DeliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken),
                    Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken),
                    CustomerId = existingRecord.CustomerId,
                    SupplierSiNo = existingRecord.SupplierSiNo,
                    SupplierSiDate = existingRecord.SupplierSiDate,
                    SupplierDrNo = existingRecord.SupplierDrNo,
                    SupplierDrDate = existingRecord.SupplierDrDate,
                    WithdrawalCertificate = existingRecord.WithdrawalCertificate,
                    OtherReference = existingRecord.OtherReference,
                    QuantityDelivered = existingRecord.QuantityDelivered,
                    QuantityReceived = existingRecord.QuantityReceived,
                    Freight = existingRecord.DeliveryReceipt.Freight,
                    TotalFreight = existingRecord.TotalFreight,
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
        public async Task<IActionResult> Edit(ReceivingReportViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    viewModel.CurrentUser = _userManager.GetUserName(User);
                    await _unitOfWork.FilprideReceivingReport.UpdateAsync(viewModel, cancellationToken);

                    TempData["success"] = "Receiving report updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.DeliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken);
                    viewModel.Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.DeliveryReceipts = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsync(cancellationToken);
            viewModel.Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken);
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
                var existingRecord = await _unitOfWork.FilprideReceivingReport
                    .GetAsync(po => po.ReceivingReportNo == id, cancellationToken);

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

        public async Task<IActionResult> Print(string? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideReceivingReport
                    .GetAsync(po => po.ReceivingReportNo == id, cancellationToken);

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

        public async Task<IActionResult> Post(string? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideReceivingReport
                    .GetAsync(po => po.ReceivingReportNo == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.PostedBy == null)
                {
                    existingRecord.PostedBy = _userManager.GetUserName(User);
                    existingRecord.PostedDate = DateTime.Now;
                    await _unitOfWork.FilprideReceivingReport.PostAsync(existingRecord, cancellationToken);
                }

                TempData["success"] = "Receiving report approved successfully.";
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        public async Task<IActionResult> GetDeliveryReceiptsByCustomer(int customerId)
        {
            var drList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListByCustomerAsync(customerId);

            return Json(drList);
        }

        public async Task<IActionResult> GetDeliveryReceiptDetails(int drId)
        {
            var deliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                .GetAsync(dr => dr.DeliveryReceiptId == drId);

            return Json(new
            {
                deliveryReceipt.Quantity,
                deliveryReceipt.Freight
            });
        }
    }
}