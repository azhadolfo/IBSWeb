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
                var exisitingRecords = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipNo == id, cancellationToken);

                if (exisitingRecords == null)
                {
                    return BadRequest();
                }

                CustomerOrderSlipViewModel viewModel = new()
                {
                    CustomerOrderSlipId = exisitingRecords.CustomerOrderSlipId,
                    Date = exisitingRecords.Date,
                    DeliveryDateAndTime = exisitingRecords.DeliveryDateAndTime,
                    CustomerId = exisitingRecords.CustomerId,
                    CustomerAddress = exisitingRecords.Customer.CustomerAddress,
                    TinNo = exisitingRecords.Customer.CustomerTin,
                    Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken),
                    PoNo = exisitingRecords.PoNo,
                    ProductId = exisitingRecords.ProductId,
                    Products = await _unitOfWork.GetProductListAsyncById(cancellationToken),
                    Quantity = exisitingRecords.Quantity,
                    DeliveredPrice = exisitingRecords.DeliveredPrice,
                    Vat = exisitingRecords.Vat,
                    TotalAmount = exisitingRecords.TotalAmount,
                    Remarks = exisitingRecords.Remarks,
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        //PENDING Create print/preview of COS

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