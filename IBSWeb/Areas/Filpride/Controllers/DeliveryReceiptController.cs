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
    public class DeliveryReceiptController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        public DeliveryReceiptController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var drList = await _unitOfWork.FilprideDeliveryReceipt
                .GetAllAsync(null, cancellationToken);

            return View(drList);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            DeliveryReceiptViewModel viewModel = new()
            {
                Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken),
                CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListAsync(cancellationToken),
                Haulers = await _unitOfWork.FilprideHauler.GetHaulerListAsync(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    FilprideDeliveryReceipt model = new()
                    {
                        DeliveryReceiptNo = await _unitOfWork.FilprideDeliveryReceipt.GenerateCodeAsync(cancellationToken),
                        Date = viewModel.Date,
                        CustomerOrderSlipId = viewModel.CustomerOrderSlipId,
                        DeliveryType = viewModel.DeliveryType,
                        HaulerId = viewModel.HaulerId,
                        CustomerId = viewModel.CustomerId,
                        Freight = viewModel.Freight,
                        AuthorityToLoadNo = viewModel.AuthorityToLoadNo,
                        Remarks = viewModel.Remarks,
                        Quantity = viewModel.Volume,
                        TotalAmount = viewModel.TotalAmount,
                        CreatedBy = _userManager.GetUserName(User)
                    };

                    model.InvoiceNo = viewModel.InvoiceNo ?? model.DeliveryReceiptNo;
                    model.NetOfVatAmount = _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(model.TotalAmount);
                    model.VatAmount = _unitOfWork.FilprideDeliveryReceipt.ComputeVatAmount(model.TotalAmount);

                    await _unitOfWork.FilprideDeliveryReceipt.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);

                    TempData["success"] = "Delivery receipt created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
                    viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListAsync(cancellationToken);
                    viewModel.Haulers = await _unitOfWork.FilprideHauler.GetHaulerListAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
            viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListAsync(cancellationToken);
            viewModel.Haulers = await _unitOfWork.FilprideHauler.GetHaulerListAsync(cancellationToken);
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
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptNo == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                DeliveryReceiptViewModel viewModel = new()
                {
                    DeliverReceiptId = existingRecord.DeliveryReceiptId,
                    Date = existingRecord.Date,
                    InvoiceNo = existingRecord.InvoiceNo,
                    CustomerId = existingRecord.Customer.CustomerId,
                    Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken),
                    CustomerAddress = existingRecord.Customer.CustomerAddress,
                    CustomerTin = existingRecord.Customer.CustomerTin,
                    CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                    CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListAsync(cancellationToken),
                    Product = existingRecord.CustomerOrderSlip.PurchaseOrder.Product.ProductName,
                    InitialVolume = existingRecord.CustomerOrderSlip.Quantity,
                    RemainingVolume = existingRecord.CustomerOrderSlip.BalanceQuantity,
                    Price = existingRecord.CustomerOrderSlip.DeliveredPrice,
                    Volume = existingRecord.Quantity,
                    TotalAmount = existingRecord.TotalAmount,
                    DeliveryType = existingRecord.DeliveryType,
                    HaulerId = existingRecord.HaulerId,
                    Haulers = await _unitOfWork.FilprideHauler.GetHaulerListAsync(cancellationToken),
                    Freight = existingRecord.Freight,
                    AuthorityToLoadNo = existingRecord.AuthorityToLoadNo,
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
        public async Task<IActionResult> Edit(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    viewModel.CurrentUser = _userManager.GetUserName(User);
                    await _unitOfWork.FilprideDeliveryReceipt.UpdateAsync(viewModel, cancellationToken);

                    TempData["success"] = "Delivery receipt updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
                    viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListAsync(cancellationToken);
                    viewModel.Haulers = await _unitOfWork.FilprideHauler.GetHaulerListAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
            viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListAsync(cancellationToken);
            viewModel.Haulers = await _unitOfWork.FilprideHauler.GetHaulerListAsync(cancellationToken);
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
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptNo == id, cancellationToken);

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
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(cos => cos.DeliveryReceiptNo == id, cancellationToken);

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
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(cos => cos.DeliveryReceiptNo == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.PostedBy == null)
                {
                    existingRecord.PostedBy = _userManager.GetUserName(User);
                    existingRecord.PostedDate = DateTime.Now;
                    await _unitOfWork.FilprideDeliveryReceipt.PostAsync(existingRecord, cancellationToken);
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
            var orderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListPerCustomerAsync(customerId);

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
                InitialVolume = cos.Quantity,
                RemainingVolume = cos.BalanceQuantity,
                Price = cos.DeliveredPrice
            });
        }
    }
}