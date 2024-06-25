using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area("Filpride")]
    public class CustomerOrderSlipController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        public CustomerOrderSlipController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var cosList = await _unitOfWork.FilprideCustomerOrderSlip
                .GetAllAsync(null, cancellationToken);

            return View(cosList);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            CustomerOrderSlipViewModel viewModel = new()
            {
                Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken),
                Products = await _unitOfWork.GetProductListAsyncById(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CustomerOrderSlipViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    FilprideCustomerOrderSlip model = new()
                    {
                        CustomerOrderSlipNo = await _unitOfWork.FilprideCustomerOrderSlip.GenerateCodeAsync(cancellationToken),
                        Date = viewModel.Date,
                        DeliveryDateAndTime = viewModel.DeliveryDateAndTime,
                        CustomerId = viewModel.CustomerId,
                        PoNo = viewModel.PoNo,
                        ProductId = viewModel.ProductId,
                        Quantity = viewModel.Quantity,
                        BalanceQuantity = viewModel.Quantity,
                        DeliveredPrice = viewModel.DeliveredPrice,
                        Vat = viewModel.Vat,
                        TotalAmount = viewModel.TotalAmount,
                        Remarks = viewModel.Remarks,
                        CreatedBy = _userManager.GetUserName(User)
                    };

                    await _unitOfWork.FilprideCustomerOrderSlip.AddAsync(model);
                    await _unitOfWork.SaveAsync(cancellationToken);

                    TempData["success"] = "Customer order slip created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken);
                    viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken);
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
                var exisitingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipNo == id, cancellationToken);

                if (exisitingRecord == null)
                {
                    return BadRequest();
                }

                CustomerOrderSlipViewModel viewModel = new()
                {
                    CustomerOrderSlipId = exisitingRecord.CustomerOrderSlipId,
                    Date = exisitingRecord.Date,
                    DeliveryDateAndTime = exisitingRecord.DeliveryDateAndTime,
                    CustomerId = exisitingRecord.CustomerId,
                    CustomerAddress = exisitingRecord.Customer.CustomerAddress,
                    TinNo = exisitingRecord.Customer.CustomerTin,
                    Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken),
                    PoNo = exisitingRecord.PoNo,
                    ProductId = exisitingRecord.ProductId,
                    Products = await _unitOfWork.GetProductListAsyncById(cancellationToken),
                    Quantity = exisitingRecord.Quantity,
                    DeliveredPrice = exisitingRecord.DeliveredPrice,
                    Vat = exisitingRecord.Vat,
                    TotalAmount = exisitingRecord.TotalAmount,
                    Remarks = exisitingRecord.Remarks,
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
        public async Task<IActionResult> Edit(CustomerOrderSlipViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _unitOfWork.FilprideCustomerOrderSlip.UpdateAsync(viewModel, _userManager.GetUserName(User), cancellationToken);

                    TempData["success"] = "Customer order slip updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken);
                    viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken);
            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> Preview(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipNo == id, cancellationToken);

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
                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipNo == id, cancellationToken);

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

        public async Task<IActionResult> Approve(string? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                //PENDING ApproveAsync repo

                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipNo == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.ApprovedBy == null)
                {
                    existingRecord.ApprovedBy = _userManager.GetUserName(User);
                    existingRecord.ApprovedDate = DateTime.Now;
                    existingRecord.ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7));
                    await _unitOfWork.SaveAsync(cancellationToken);
                }

                TempData["success"] = "Customer order slip approved successfully.";
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        public async Task<IActionResult> Disapprove(string? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                //PENDING DisapproveAsync repo

                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipNo == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.DisapprovedBy == null)
                {
                    existingRecord.DisapprovedBy = _userManager.GetUserName(User);
                    existingRecord.DisapprovedDate = DateTime.Now;
                    await _unitOfWork.SaveAsync(cancellationToken);
                }

                TempData["success"] = "Customer order slip disapproved successfully.";
                return RedirectToAction(nameof(Index));
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

            var customerDto = await _unitOfWork.FilprideCustomerOrderSlip.MapCustomerToDTO(id, null);

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
    }
}