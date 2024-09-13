using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class CustomerOrderSlipController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        public CustomerOrderSlipController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
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

            var cosList = await _unitOfWork.FilprideCustomerOrderSlip
                .GetAllAsync(cos => cos.Company == companyClaims, cancellationToken);

            return View(cosList);
        }

        [HttpPost]
        public async Task<IActionResult> GetCustomerOrderSlips([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var cosList = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAllAsync(cos => cos.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    cosList = cosList
                    .Where(s =>
                        s.CustomerOrderSlipNo.ToLower().Contains(searchValue) ||
                        s.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                        s.Customer.CustomerName?.ToLower().Contains(searchValue) == true ||
                        s.Quantity.ToString().Contains(searchValue) == true ||
                        s.TotalAmount.ToString().Contains(searchValue) == true ||
                        s.Remarks?.ToLower().Contains(searchValue) == true ||
                        s.Status.ToLower().Contains(searchValue)
                        )
                    .ToList();

                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    cosList = cosList
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = cosList.Count();

                var pagedData = cosList
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedData
                });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }



        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            CustomerOrderSlipViewModel viewModel = new()
            {
                Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken),
                Commissioners = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken),
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CustomerOrderSlipViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

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
                        CustomerPoNo = viewModel.CustomerPoNo,
                        Quantity = viewModel.Quantity,
                        BalanceQuantity = viewModel.Quantity,
                        DeliveredPrice = viewModel.DeliveredPrice,
                        TotalAmount = viewModel.TotalAmount,
                        Remarks = viewModel.Remarks,
                        Company = companyClaims,
                        CreatedBy = _userManager.GetUserName(User),
                        Status = nameof(CosStatus.Created)
                    };

                    if (viewModel.HasCommission)
                    {
                        model.HasCommission = viewModel.HasCommission;
                        model.CommissionerId = viewModel.CommissionerId;
                        model.CommissionRate = viewModel.CommissionerRate;
                    }

                    await _unitOfWork.FilprideCustomerOrderSlip.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);

                    TempData["success"] = "Customer order slip created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
                    viewModel.Commissioners = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
            viewModel.Commissioners = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditCos(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var exisitingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

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
                    Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken),
                    HasCommission = exisitingRecord.HasCommission,
                    CommissionerId = exisitingRecord.CommissionerId,
                    Commissioners = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken),
                    CommissionerRate = exisitingRecord.CommissionRate,
                    CustomerPoNo = exisitingRecord.CustomerPoNo,
                    Quantity = exisitingRecord.Quantity,
                    DeliveredPrice = exisitingRecord.DeliveredPrice,
                    Vat = _unitOfWork.FilprideCustomerOrderSlip.ComputeVatAmount((exisitingRecord.TotalAmount / 1.12m)),
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
        public async Task<IActionResult> EditCos(CustomerOrderSlipViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (ModelState.IsValid)
            {
                try
                {
                    viewModel.CurrentUser = _userManager.GetUserName(User);
                    await _unitOfWork.FilprideCustomerOrderSlip.UpdateAsync(viewModel, cancellationToken);

                    TempData["success"] = "Customer order slip updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> Preview(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                CustomerOrderSlipForApprovalViewModel model = new()
                {
                    CustomerOrderSlip = existingRecord,
                    NetOfVatProductCost = _unitOfWork.FilprideCustomerOrderSlip.ComputeNetOfVat(existingRecord.PurchaseOrder.Price),
                    NetOfVatCosPrice = _unitOfWork.FilprideCustomerOrderSlip.ComputeNetOfVat(existingRecord.DeliveredPrice),
                    NetOfVatFreightCharge = _unitOfWork.FilprideCustomerOrderSlip.ComputeNetOfVat((decimal)existingRecord.Freight),
                    VatAmount = _unitOfWork.FilprideCustomerOrderSlip.ComputeVatAmount(_unitOfWork.FilprideCustomerOrderSlip.ComputeNetOfVat(existingRecord.TotalAmount))
                };

                model.GrossMargin = model.NetOfVatCosPrice - model.NetOfVatProductCost - model.NetOfVatFreightCharge - (decimal)existingRecord.CommissionRate;

                if (existingRecord.FirstApprovedBy == null)
                {
                    return View("PreviewByOperationManager", model);
                }

                model.CreditBalance = await _unitOfWork.FilprideCustomerOrderSlip.GetCustomerCreditBalance(existingRecord.CustomerId, cancellationToken);
                model.Total = model.CreditBalance - existingRecord.TotalAmount;

                return View("PreviewByFinance", model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> ApproveByOperationManager(int? id, decimal grossMargin, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (grossMargin <= 0)
            {
                TempData["error"] = "Gross margin cannot be negative and zero";
                return RedirectToAction(nameof(Preview), new { id });
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.FirstApprovedBy == null)
                {
                    existingRecord.FirstApprovedBy = _userManager.GetUserName(User);
                    existingRecord.FirstApprovedDate = DateTime.Now;
                    await _unitOfWork.FilprideCustomerOrderSlip.OperationManagerApproved(existingRecord, grossMargin, cancellationToken);
                }

                TempData["success"] = "Customer order slip approved by operation manager successfully.";
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        public async Task<IActionResult> ApproveByFinance(int? id, string? terms, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.SecondApprovedBy == null)
                {
                    existingRecord.SecondApprovedBy = _userManager.GetUserName(User);
                    existingRecord.SecondApprovedDate = DateTime.Now;
                    existingRecord.Terms = terms;
                    await _unitOfWork.FilprideCustomerOrderSlip.FinanceApproved(existingRecord, cancellationToken);
                }

                TempData["success"] = "Customer order slip approved by finance successfully.";
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        public async Task<IActionResult> Disapprove(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                //PENDING DisapproveAsync repo

                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.DisapprovedBy == null)
                {
                    existingRecord.DisapprovedBy = _userManager.GetUserName(User);
                    existingRecord.DisapprovedDate = DateTime.Now;
                    existingRecord.Status = nameof(CosStatus.Disapproved);
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

        [HttpGet]
        public async Task<IActionResult> AppointSupplier(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

            var viewModel = new CustomerOrderSlipAppointingSupplierViewModel
            {
                CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken),
                PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsyncById(companyClaims, cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AppointSupplier(CustomerOrderSlipAppointingSupplierViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            viewModel.CurrentUser = _userManager.GetUserName(User);

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCos = await _unitOfWork.FilprideCustomerOrderSlip
                        .GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

                    if (existingCos == null)
                    {
                        return BadRequest();
                    }

                    existingCos.PurchaseOrderId = viewModel.PurchaseOrderId;
                    existingCos.DeliveryOption = viewModel.DeliveryOption;
                    existingCos.PickUpPointId = viewModel.PickUpPointId;
                    existingCos.Status = nameof(CosStatus.SupplierAppointed);

                    if (existingCos.DeliveryOption == SD.DeliveryOption_DirectDelivery)
                    {
                        existingCos.Freight = viewModel.Freight;

                        var existingPo = await _unitOfWork.FilpridePurchaseOrder
                            .GetAsync(po => po.PurchaseOrderId == viewModel.PurchaseOrderId, cancellationToken);

                        if (existingPo == null)
                        {
                            return BadRequest();
                        }

                        var subPoModel = new FilpridePurchaseOrder
                        {
                            PurchaseOrderNo = await _unitOfWork.FilpridePurchaseOrder.GenerateCodeAsync(companyClaims, cancellationToken),
                            Date = DateOnly.FromDateTime(DateTime.Now),
                            SupplierId = viewModel.SupplierId,
                            ProductId = existingPo.ProductId,
                            Terms = existingPo.Terms,
                            Quantity = existingCos.Quantity,
                            Price = viewModel.Freight,
                            Remarks = viewModel.SubPoRemarks,
                            Company = existingPo.Company,
                            IsSubPo = true,
                            CustomerId = existingCos.CustomerId,
                            SubPoSeries = await _unitOfWork.FilpridePurchaseOrder.GenerateCodeForSubPoAsync(existingPo.PurchaseOrderNo, existingPo.Company, cancellationToken),
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTime.Now,
                            PostedBy = viewModel.CurrentUser,
                            PostedDate = DateTime.Now
                        };

                        subPoModel.Amount = subPoModel.Quantity * subPoModel.Price;
                        await _unitOfWork.FilpridePurchaseOrder.AddAsync(subPoModel, cancellationToken);
                    }
                    else
                    {
                        var highestFreight = await _unitOfWork.FilprideFreight
                            .GetAsync(f => f.ClusterCode == existingCos.Customer.ClusterCode && f.PickUpPointId == existingCos.PickUpPointId) ?? throw new ArgumentNullException("No freight reference found!");

                        existingCos.Freight = highestFreight.Freight;
                    }

                    TempData["success"] = "Appointed supplier successfully.";
                    await _unitOfWork.SaveAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));

                }
                catch (Exception ex)
                {
                    viewModel.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);
                    viewModel.PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsyncById(companyClaims, cancellationToken);
                    viewModel.PickUpPoints = await _unitOfWork.FilpridePickUpPoint.GetPickUpPointListBasedOnSupplier(viewModel.SupplierId, cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }
            viewModel.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);
            viewModel.PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsyncById(companyClaims, cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> GetPurchaseOrders(int? supplierId, CancellationToken cancellationToken)
        {
            if (supplierId == null)
            {
                return NotFound();
            }

            var purchaseOrderList = await _unitOfWork.FilpridePurchaseOrder
                .GetPurchaseOrderListAsyncBySupplier((int)supplierId, cancellationToken);

            return Json(purchaseOrderList);
        }

        public async Task<IActionResult> GetPickUpPoints(int? supplierId, CancellationToken cancellationToken)
        {
            if (supplierId == null)
            {
                return NotFound();
            }

            var pickUpPoints = await _unitOfWork.FilpridePickUpPoint
                .GetPickUpPointListBasedOnSupplier((int)supplierId, cancellationToken);

            return Json(pickUpPoints);
        }

        public async Task<IActionResult> CheckCustomerBalance(int? customerId, CancellationToken cancellationToken)
        {
            if (customerId == null)
            {
                return NotFound();
            }

            var balance = await _unitOfWork.FilprideCustomerOrderSlip
                .GetCustomerCreditBalance((int)customerId, cancellationToken);

            return Json(balance);
        }

        [HttpGet]
        public async Task<IActionResult> AppointHauler(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

            var viewModel = new CustomerOrderSlipAppointingHauler
            {
                CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AppointHauler(CustomerOrderSlipAppointingHauler viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                try
                {
                    viewModel.CurrentUser = _userManager.GetUserName(User);

                    var existingCos = await _unitOfWork.FilprideCustomerOrderSlip
                        .GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

                    if (existingCos == null)
                    {
                        return BadRequest();
                    }

                    existingCos.HaulerId = viewModel.HaulerId;
                    existingCos.Freight = viewModel.Freight;
                    existingCos.Status = nameof(CosStatus.HaulerAppointed);

                    TempData["success"] = "Appointed hauler successfully.";
                    await _unitOfWork.SaveAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));

                }
                catch (Exception ex)
                {
                    viewModel.Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }
    }
}