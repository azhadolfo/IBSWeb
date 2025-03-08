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
    public class ReceivingReportController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        public ReceivingReportController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<string> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value ?? "ALL";
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var receivingReports = await _unitOfWork.MobilityReceivingReport
                .GetAllAsync(null, cancellationToken);

            if (GetStationCodeClaimAsync().Result != "ALL")
            {
                receivingReports = receivingReports.Where(po => po.StationCode == GetStationCodeClaimAsync().Result);
            }
            ViewData["StationCode"] = GetStationCodeClaimAsync().Result;

            return View(receivingReports);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            ReceivingReportViewModel viewModel = new()
            {
                DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken),
                Stations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken)
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
                    var stationCodeClaim = await GetStationCodeClaimAsync();

                    MobilityReceivingReport model = new()
                    {
                        ReceivingReportNo = await _unitOfWork.MobilityReceivingReport.GenerateCodeAsync(stationCodeClaim, cancellationToken),
                        Date = viewModel.Date,
                        Driver = viewModel.Driver,
                        PlateNo = viewModel.PlateNo,
                        Remarks = viewModel.Remarks,
                        DeliveryReceiptId = viewModel.DeliveryReceiptId,
                        ReceivedQuantity = viewModel.ReceivedQuantity,
                        StationCode = stationCodeClaim,
                        CreatedBy = _userManager.GetUserName(User)
                    };

                    await _unitOfWork.MobilityReceivingReport.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);

                    TempData["success"] = "Receiving report created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken);
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
                var existingRecord = await _unitOfWork.MobilityReceivingReport
                    .GetAsync(rr => rr.ReceivingReportNo == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                ReceivingReportViewModel viewModel = new()
                {
                    ReceivingReportId = existingRecord.ReceivingReportId,
                    Date = existingRecord.Date,
                    Driver = existingRecord.Driver,
                    PlateNo = existingRecord.PlateNo,
                    Remarks = existingRecord.Remarks,
                    DeliveryReceiptId = existingRecord.DeliveryReceiptId,
                    DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken),
                    InvoiceQuantity = existingRecord.FilprideDeliveryReceipt.Quantity,
                    Product = existingRecord.FilprideDeliveryReceipt.CustomerOrderSlip.PurchaseOrder.Product.ProductName,
                    ReceivedQuantity = existingRecord.ReceivedQuantity
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
                    await _unitOfWork.MobilityReceivingReport.UpdateAsync(viewModel, cancellationToken);

                    TempData["success"] = "Receiving report updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }
            viewModel.DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken);
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
                var existingRecord = await _unitOfWork.MobilityReceivingReport
                    .GetAsync(rr => rr.ReceivingReportNo == id, cancellationToken);

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
                var existingRecord = await _unitOfWork.MobilityReceivingReport
                    .GetAsync(rr => rr.ReceivingReportNo == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.PostedBy == null)
                {
                    existingRecord.PostedBy = _userManager.GetUserName(User);
                    existingRecord.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    await _unitOfWork.MobilityReceivingReport.PostAsync(existingRecord, cancellationToken);
                }

                TempData["success"] = "Receiving report approved successfully.";
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDrDetails(int drId, CancellationToken cancellationToken)
        {
            var drDetails = await _unitOfWork.FilprideDeliveryReceipt
                .GetAsync(dr => dr.DeliveryReceiptId == drId, cancellationToken);
            if (drDetails == null)
            {
                return NotFound();
            }
            return Json(new
            {
                Product = drDetails.CustomerOrderSlip.PurchaseOrder.Product.ProductName,
                drDetails.Quantity
            });
        }
    }
}
