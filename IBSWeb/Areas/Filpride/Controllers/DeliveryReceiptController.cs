﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
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
    public class DeliveryReceiptController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        public DeliveryReceiptController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetDeliveryReceipts([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var drList = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAllAsync(cos => cos.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    drList = drList
                    .Where(s =>
                        s.DeliveryReceiptNo.ToLower().Contains(searchValue) ||
                        s.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                        s.Customer.CustomerName?.ToLower().Contains(searchValue) == true ||
                        s.Quantity.ToString().Contains(searchValue) == true ||
                        s.TotalAmount.ToString().Contains(searchValue) == true ||
                        s.Remarks?.ToLower().Contains(searchValue) == true
                        )
                    .ToList();

                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    drList = drList
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = drList.Count();

                var pagedData = drList
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

            DeliveryReceiptViewModel viewModel = new()
            {
                Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken),
                CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                try
                {
                    FilprideDeliveryReceipt model = new()
                    {
                        DeliveryReceiptNo = await _unitOfWork.FilprideDeliveryReceipt.GenerateCodeAsync(cancellationToken),
                        Date = viewModel.Date,
                        EstimatedTimeOfArrival = viewModel.ETA,
                        CustomerOrderSlipId = viewModel.CustomerOrderSlipId,
                        CustomerId = viewModel.CustomerId,
                        Remarks = viewModel.Remarks,
                        Quantity = viewModel.Volume,
                        TotalAmount = viewModel.TotalAmount,
                        Company = companyClaims,
                        CreatedBy = _userManager.GetUserName(User),
                        ManualDrNo = viewModel.ManualDrNo,
                        Demuragge = viewModel.Demuragge
                    };

                    await _unitOfWork.FilprideDeliveryReceipt.AddAsync(model, cancellationToken);

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    await _unitOfWork.SaveAsync(cancellationToken);

                    TempData["success"] = "Delivery receipt created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
                    viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
            viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            try
            {
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                DeliveryReceiptViewModel viewModel = new()
                {
                    DeliverReceiptId = existingRecord.DeliveryReceiptId,
                    Date = existingRecord.Date,
                    ETA = existingRecord.EstimatedTimeOfArrival,
                    CustomerId = existingRecord.Customer.CustomerId,
                    Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken),
                    CustomerAddress = existingRecord.Customer.CustomerAddress,
                    CustomerTin = existingRecord.Customer.CustomerTin,
                    CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                    CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken),
                    Product = existingRecord.CustomerOrderSlip.PurchaseOrder.Product.ProductName,
                    CosVolume = existingRecord.CustomerOrderSlip.Quantity,
                    RemainingVolume = existingRecord.CustomerOrderSlip.BalanceQuantity,
                    Price = existingRecord.CustomerOrderSlip.DeliveredPrice,
                    Volume = existingRecord.Quantity,
                    TotalAmount = existingRecord.TotalAmount,
                    Remarks = existingRecord.Remarks,
                    ManualDrNo = existingRecord.ManualDrNo,
                    Demuragge = existingRecord.Demuragge
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
            var companyClaims = await GetCompanyClaimAsync();

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
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
                    viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
            viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken);
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
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

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

        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(cos => cos.DeliveryReceiptId == id, cancellationToken);

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

        public async Task<IActionResult> Post(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(cos => cos.DeliveryReceiptId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.PostedBy == null)
                {
                    existingRecord.PostedBy = _userManager.GetUserName(User);
                    existingRecord.PostedDate = DateTime.Now;
                    existingRecord.Status = nameof(Status.Posted);

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingRecord.PostedBy, $"Posted delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, existingRecord.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

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
            var orderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListPerCustomerNotDeliveredAsync(customerId);

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
                cos.Quantity,
                RemainingVolume = cos.BalanceQuantity,
                Price = cos.DeliveredPrice
            });
        }

        public async Task<IActionResult> Delivered(int? id, string deliveredDate, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(cos => cos.DeliveryReceiptId == id, cancellationToken);

            if (existingRecord == null)
            {
                return BadRequest();
            }

            try
            {
                existingRecord.DeliveredDate = DateOnly.Parse(deliveredDate);
                existingRecord.Status = "Delivered";

                await _unitOfWork.FilprideReceivingReport.AutoGenerateReceivingReport(existingRecord, cancellationToken);

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User), $"Mark as delivered the delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, existingRecord.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = "Product has been delivered";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.Status = nameof(Status.Canceled);
                    model.CancellationRemarks = cancellationRemarks;

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _unitOfWork.SaveAsync(cancellationToken);
                    TempData["success"] = "Delivery Receipt has been canceled.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

            if (model != null)
            {
                try
                {
                    if (model.VoidedBy == null)
                    {
                        if (model.PostedBy != null)
                        {
                            model.PostedBy = null;
                        }

                        model.VoidedBy = _userManager.GetUserName(this.User);
                        model.VoidedDate = DateTime.Now;
                        model.Status = nameof(Status.Voided);

                        await _unitOfWork.FilprideDeliveryReceipt.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.DeliveryReceiptNo, cancellationToken);
                        await _unitOfWork.FilprideDeliveryReceipt.DeductTheVolumeToCos(model.CustomerOrderSlipId, model.Quantity, cancellationToken);

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Delivery receipt has been Voided.";
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return BadRequest();
        }

    }
}