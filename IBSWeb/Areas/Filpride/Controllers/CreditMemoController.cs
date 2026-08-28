using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class CreditMemoController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ILogger<CreditMemoController> _logger;

        public CreditMemoController(IUnitOfWork unitOfWork, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, ILogger<CreditMemoController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        private static (int? SupplierId, string? SupplierName) ResolveLockedPeriodSupplier(FilprideSalesInvoice salesInvoice)
        {
            var deliveryReceipt = salesInvoice.DeliveryReceipt;
            if (deliveryReceipt == null)
            {
                return (null, null);
            }

            var suppliers = deliveryReceipt.Details
                .Where(detail => detail.PurchaseOrder != null)
                .GroupBy(detail => detail.PurchaseOrder!.SupplierId)
                .Select(group => new
                {
                    SupplierId = group.Key,
                    SupplierName = group.First().PurchaseOrder!.SupplierName
                })
                .ToList();

            if (suppliers.Count == 1)
            {
                return (suppliers[0].SupplierId, suppliers[0].SupplierName);
            }

            if (suppliers.Count > 1)
            {
                return (null, "Multiple Suppliers");
            }

            return (deliveryReceipt.PurchaseOrder?.SupplierId, deliveryReceipt.PurchaseOrder?.SupplierName);
        }

        private const string FilterTypeClaimType = "CreditMemo_FilterType";

        private async Task UpdateFilterTypeClaim(string filterType)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var claims = await _userManager.GetClaimsAsync(user);
                var existingClaim = claims.FirstOrDefault(c => c.Type == FilterTypeClaimType);
                if (existingClaim != null)
                {
                    await _userManager.RemoveClaimAsync(user, existingClaim);
                }

                if (!string.IsNullOrEmpty(filterType))
                {
                    await _userManager.AddClaimAsync(user, new Claim(FilterTypeClaimType, filterType));
                }
            }
        }

        private async Task<string?> GetCurrentFilterType()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == FilterTypeClaimType)?.Value;
        }

        public async Task<IActionResult> Index(string? filterType, string? view)
        {
            if (view == nameof(DynamicView.CreditMemo))
            {
                return View("ExportIndex");
            }

            await UpdateFilterTypeClaim(filterType!);
            ViewBag.FilterType = await GetCurrentFilterType();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetCreditMemos([FromForm] DataTablesParameters parameters, DateOnly filterDate, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();
                var filterTypeClaim = await GetCurrentFilterType();

                var creditMemos = _unitOfWork.FilprideCreditMemo
                    .GetAllQuery(x => true);

                if (!string.IsNullOrEmpty(filterTypeClaim))
                {
                    switch (filterTypeClaim)
                    {
                        case "ForFMApproval":
                            creditMemos = creditMemos.Where(cm => cm.Status == nameof(DmCmStatus.ForApprovalOfFM));
                            break;
                    }
                }

                var totalRecords = await creditMemos.CountAsync(cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    var hasTransactionDate = DateOnly.TryParse(searchValue, out var transactionDate);

                    creditMemos = creditMemos
                    .Where(s =>
                        s.CreditMemoNo!.ToLower().Contains(searchValue) ||
                        s.SalesInvoice!.SalesInvoiceNo!.ToLower().Contains(searchValue) == true ||
                        s.ServiceInvoice!.ServiceInvoiceNo.ToLower().Contains(searchValue) == true ||
                        (hasTransactionDate && s.TransactionDate == transactionDate) ||
                        s.CreditAmount.ToString().Contains(searchValue) ||
                        s.Remarks!.ToLower().Contains(searchValue) == true ||
                        s.Description.ToLower().Contains(searchValue) ||
                        s.CreatedBy!.ToLower().Contains(searchValue)
                        );
                }
                if (filterDate != DateOnly.MinValue && filterDate != default)
                {
                    creditMemos = creditMemos.Where(s => s.TransactionDate == filterDate);
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    creditMemos = creditMemos
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalFilteredRecords = await creditMemos.CountAsync(cancellationToken);

                var pagedData = await creditMemos
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToListAsync(cancellationToken);

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalFilteredRecords,
                    data = pagedData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get credit memos. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task IncludeSelectLists(CreditMemoViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.SalesInvoices = (await _unitOfWork.FilprideSalesInvoice
                    .GetAllAsync(si => si.PostedBy != null, cancellationToken))
                .OrderBy(si => si.SalesInvoiceNo)
                .Select(si => new SelectListItem
                {
                    Value = si.SalesInvoiceId.ToString(),
                    Text = si.SalesInvoiceNo
                })
                .ToList();

            viewModel.ServiceInvoices = (await _unitOfWork.FilprideServiceInvoice
                    .GetAllAsync(sv => sv.PostedBy != null, cancellationToken))
                .OrderBy(sv => sv.ServiceInvoiceNo)
                .Select(sv => new SelectListItem
                {
                    Value = sv.ServiceInvoiceId.ToString(),
                    Text = sv.ServiceInvoiceNo
                })
                .ToList();

            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CreditMemo, cancellationToken);
        }

        [Authorize(Policy = nameof(CreditMemo.CreditMemoCreate))]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new CreditMemoViewModel();
            await IncludeSelectLists(viewModel, cancellationToken);
            return View(viewModel);
        }

        [Authorize(Policy = nameof(CreditMemo.CreditMemoCreate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreditMemoViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await IncludeSelectLists(viewModel, cancellationToken);
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(viewModel);
            }

            var model = new FilprideCreditMemo
            {
                Source = viewModel.Source,
                TransactionDate = viewModel.TransactionDate,
                SalesInvoiceId = viewModel.SalesInvoiceId,
                Quantity = viewModel.Quantity,
                AdjustedPrice = viewModel.AdjustedPrice,
                ServiceInvoiceId = viewModel.ServiceInvoiceId,
                Period = viewModel.Period,
                Amount = viewModel.Amount,
                Remarks = viewModel.Remarks,
                Description = viewModel.Description,
            };

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var existingSalesInvoice = await _unitOfWork.FilprideSalesInvoice
                        .GetAsync(invoice => invoice.SalesInvoiceId == model.SalesInvoiceId, cancellationToken);

            var existingSv = await _unitOfWork.FilprideServiceInvoice
                        .GetAsync(sv => sv.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- check for unposted DM or CM

                if (model.SalesInvoiceId != null)
                {
                    var existingSidMs = (await _unitOfWork.FilprideDebitMemo
                                  .GetAllAsync(si => si.SalesInvoiceId == model.SalesInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null, cancellationToken))
                                  .OrderBy(s => s.SalesInvoiceId)
                                  .ToList();
                    if (existingSidMs.Count > 0)
                    {
                        await IncludeSelectLists(viewModel, cancellationToken);
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSidMs.First().DebitMemoNo}");
                        return View(viewModel);
                    }

                    var existingSicMs = (await _unitOfWork.FilprideCreditMemo
                                      .GetAllAsync(si => si.SalesInvoiceId == model.SalesInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null, cancellationToken))
                                      .OrderBy(s => s.SalesInvoiceId)
                                      .ToList();
                    if (existingSicMs.Count > 0)
                    {
                        await IncludeSelectLists(viewModel, cancellationToken);
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSicMs.First().CreditMemoNo}");
                        return View(viewModel);
                    }
                }
                else
                {
                    var existingSOADMs = (await _unitOfWork.FilprideDebitMemo
                                  .GetAllAsync(si => si.ServiceInvoiceId == model.ServiceInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null, cancellationToken))
                                  .OrderBy(s => s.ServiceInvoiceId)
                                  .ToList();
                    if (existingSOADMs.Count > 0)
                    {
                        await IncludeSelectLists(viewModel, cancellationToken);
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSOADMs.First().DebitMemoNo}");
                        return View(viewModel);
                    }

                    var existingSOACMs = (await _unitOfWork.FilprideCreditMemo
                                      .GetAllAsync(si => si.ServiceInvoiceId == model.ServiceInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null, cancellationToken))
                                      .OrderBy(s => s.SalesInvoiceId)
                                      .ToList();
                    if (existingSOACMs.Count > 0)
                    {
                        await IncludeSelectLists(viewModel, cancellationToken);
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSOACMs.First().CreditMemoNo}");
                        return View(viewModel);
                    }
                }

                #endregion -- check for unposted DM or CM

                model.CreatedBy = GetUserFullName();
                if (model.Source == "Sales Invoice")
                {
                    model.ServiceInvoiceId = null;
                    model.CreditMemoNo = await _unitOfWork.FilprideCreditMemo.GenerateCodeAsync(companyClaims, existingSalesInvoice!.Type, cancellationToken);
                    model.Type = existingSalesInvoice.Type;
                    model.CreditAmount = DecimalRoundingHelper.ComputeAmountFromUnitPrice(model.Quantity ?? 0m, -(model.AdjustedPrice ?? 0m));
                }
                else if (model.Source == "Service Invoice")
                {
                    model.SalesInvoiceId = null;

                    model.CreditMemoNo = await _unitOfWork.FilprideCreditMemo.GenerateCodeAsync(companyClaims, existingSv!.Type, cancellationToken);
                    model.Type = existingSv.Type;
                    model.CreditAmount = -model.Amount ?? 0;
                }

                await _unitOfWork.FilprideCreditMemo.AddAsync(model, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new credit memo# {model.CreditMemoNo}", "Credit Memo");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Credit memo #{model.CreditMemoNo} created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await IncludeSelectLists(viewModel, cancellationToken);
                _logger.LogError(ex, "Failed to create credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [Authorize(Policy = nameof(CreditMemo.CreditMemoEdit))]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var creditMemo =
                    await _unitOfWork.FilprideCreditMemo.GetAsync(c => c.CreditMemoId == id, cancellationToken);

                if (creditMemo == null)
                {
                    return NotFound();
                }

                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CreditMemo, cancellationToken);
                if (await _unitOfWork.IsPeriodPostedAsync(Module.CreditMemo, creditMemo.TransactionDate, cancellationToken))
                {
                    throw new ArgumentException(
                        $"Cannot edit this record because the period {creditMemo.TransactionDate:MMM yyyy} is already closed.");
                }

                var viewModel = new CreditMemoViewModel
                {
                    CreditMemoId = creditMemo.CreditMemoId,
                    Source = creditMemo.Source,
                    TransactionDate = creditMemo.TransactionDate,
                    SalesInvoiceId = creditMemo.SalesInvoiceId,
                    Quantity = creditMemo.Quantity,
                    AdjustedPrice = creditMemo.AdjustedPrice,
                    ServiceInvoiceId = creditMemo.ServiceInvoiceId,
                    Period = creditMemo.Period,
                    Amount = creditMemo.Amount,
                    Remarks = creditMemo.Remarks,
                    Description = creditMemo.Description,
                    MinDate = minDate,
                };

                await IncludeSelectLists(viewModel, cancellationToken);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch credit memo. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Policy = nameof(CreditMemo.CreditMemoEdit))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CreditMemoViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await IncludeSelectLists(viewModel, cancellationToken);
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(viewModel);
            }

            var model = new FilprideCreditMemo
            {
                CreditMemoId = viewModel.CreditMemoId,
                Source = viewModel.Source,
                TransactionDate = viewModel.TransactionDate,
                SalesInvoiceId = viewModel.SalesInvoiceId,
                Quantity = viewModel.Quantity,
                AdjustedPrice = viewModel.AdjustedPrice,
                ServiceInvoiceId = viewModel.ServiceInvoiceId,
                Period = viewModel.Period,
                Amount = viewModel.Amount,
                Remarks = viewModel.Remarks,
                Description = viewModel.Description,
                CreditAmount = DecimalRoundingHelper.ComputeAmountFromUnitPrice(viewModel.Quantity ?? 0m, viewModel.AdjustedPrice ?? 0m)
            };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingCm = await _unitOfWork.FilprideCreditMemo
                                .GetAsync(cm => cm.CreditMemoId == model.CreditMemoId, cancellationToken);

                if (existingCm == null)
                {
                    return NotFound();
                }

                model.EditedBy = GetUserFullName();
                model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                switch (model.Source)
                {
                    case "Sales Invoice":
                        model.ServiceInvoiceId = null;

                        #region -- Saving Default Enries --

                        existingCm.TransactionDate = model.TransactionDate;
                        existingCm.SalesInvoiceId = model.SalesInvoiceId;
                        existingCm.Quantity = model.Quantity;
                        existingCm.AdjustedPrice = model.AdjustedPrice;
                        existingCm.Description = model.Description;
                        existingCm.Remarks = model.Remarks;
                        if (existingCm.Status == nameof(DmCmStatus.ForPosting))
                        {
                            existingCm.SalesInvoice!.Balance += Math.Abs(existingCm.CreditAmount);
                            existingCm.SalesInvoice!.CreditAmount -= Math.Abs(existingCm.CreditAmount);
                        }

                        #endregion -- Saving Default Enries --

                        existingCm.CreditAmount = DecimalRoundingHelper.ComputeAmountFromUnitPrice(model.Quantity ?? 0m, -(model.AdjustedPrice ?? 0m));
                        break;

                    case "Service Invoice":
                        model.SalesInvoiceId = null;

                        #region -- Saving Default Enries --

                        existingCm.TransactionDate = model.TransactionDate;
                        existingCm.ServiceInvoiceId = model.ServiceInvoiceId;
                        existingCm.Period = model.Period;
                        existingCm.Amount = model.Amount;
                        existingCm.Description = model.Description;
                        existingCm.Remarks = model.Remarks;

                        #endregion -- Saving Default Enries --

                        existingCm.CreditAmount = -model.Amount ?? 0;
                        break;
                }

                if (existingCm.Status == nameof(DmCmStatus.ForPosting))
                {
                    existingCm.Status = nameof(DmCmStatus.ForApprovalOfFM);
                }

                existingCm.EditedBy = GetUserFullName();
                existingCm.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingCm.EditedBy!, $"Edited credit memo# {existingCm.CreditMemoNo}", "Credit Memo");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Credit Memo edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await IncludeSelectLists(viewModel, cancellationToken);
                _logger.LogError(ex, "Failed to edit credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [Authorize(Policy = nameof(CreditMemo.CreditMemoPreview))]
        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var creditMemo = await _unitOfWork.FilprideCreditMemo.GetAsync(c => c.CreditMemoId == id, cancellationToken);

            if (creditMemo == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return NotFound();
            }

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Preview credit memo# {creditMemo.CreditMemoNo}", "Credit Memo");
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            return View(creditMemo);
        }

        [Authorize(Policy = nameof(CreditMemo.CreditMemoPost))]
        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken, ViewModelDMCM viewModelDmcm)
        {
            var model = await _unitOfWork.FilprideCreditMemo.GetAsync(c => c.CreditMemoId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            if (model.PostedBy != null)
            {
                TempData["info"] = "Credit Memo has already been posted.";
                return RedirectToAction(nameof(Print), new { id });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await PostCreditMemoAsync(model, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Credit Memo has been Posted.";
                return RedirectToAction(nameof(Print), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideCreditMemo.GetAsync(cm => cm.CreditMemoId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var dateToday = DateTimeHelper.GetCurrentPhilippineTime();
                model.PostedBy = null;
                model.VoidedBy = GetUserFullName();
                model.VoidedDate = dateToday;
                model.Status = nameof(DmCmStatus.Voided);
                if (model.SalesInvoice != null)
                {
                    var creditAmount = Math.Abs(model.CreditAmount);
                    var oldBalance = model.SalesInvoice.Balance;
                    var (supplierId, supplierName) = ResolveLockedPeriodSupplier(model.SalesInvoice);
                    model.SalesInvoice.Balance += creditAmount;
                    model.SalesInvoice.CreditAmount -= creditAmount;

                    await _unitOfWork.LockedPeriodAdjustment.AddIfPeriodPostedAsync(new()
                    {
                        Module = Module.SalesInvoice,
                        TransactionDate = DateOnly.FromDateTime(dateToday),
                        EntityType = Module.CreditMemo,
                        EntityNo = model.CreditMemoNo ?? string.Empty,
                        CustomerId = model.SalesInvoice.CustomerOrderSlip?.CustomerId ?? model.SalesInvoice.CustomerId,
                        CustomerName = model.SalesInvoice.CustomerOrderSlip?.CustomerName ?? model.SalesInvoice.Customer?.CustomerName,
                        SupplierId = supplierId,
                        SupplierName = supplierName,
                        AdjustmentType = LockedPeriodAdjustmentType.CreditMemo,
                        OldValue = oldBalance,
                        NewValue = model.SalesInvoice.Balance,
                        AdjustmentValue = creditAmount,
                        AffectedQuantity = model.Quantity ?? 0m,
                        Reason = "Voided credit memo reversal",
                        CreatedBy = model.VoidedBy
                    }, cancellationToken);
                }

                await _unitOfWork.GeneralLedger.ReverseEntries(model.CreditMemoNo, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided credit memo# {model.CreditMemoNo}", "Credit Memo");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Json(new { success = true, message = $"Credit Memo #{model.CreditMemoNo} has been voided successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to void credit memo. Voided by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Policy = nameof(CreditMemo.CreditMemoCancel))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideCreditMemo
                .GetAsync(cm => cm.CreditMemoId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.CanceledBy = GetUserFullName();
                model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.CancellationRemarks = cancellationRemarks;
                if (model.Status == nameof(DmCmStatus.ForPosting) && model.SalesInvoice != null)
                {
                    model.SalesInvoice.Balance += Math.Abs(model.CreditAmount);
                    model.SalesInvoice.CreditAmount -= Math.Abs(model.CreditAmount);
                }
                model.Status = nameof(DmCmStatus.Canceled);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled credit memo# {model.CreditMemoNo}", "Credit Memo");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Json(new { success = true, message = $"Collection Receipt #{model.CreditMemoNo} has been cancelled successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSVDetails(int svId, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideServiceInvoice.GetAsync(sv => sv.ServiceInvoiceId == svId, cancellationToken);
            if (model == null)
            {
                return Json(null);
            }

            return Json(new
            {
                model.Period,
                model.Total
            });
        }

        [Authorize(Policy = nameof(CreditMemo.CreditMemoPreview))]
        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cm = await _unitOfWork.FilprideCreditMemo
                .GetAsync(x => x.CreditMemoId == id, cancellationToken);

            if (cm == null)
            {
                return NotFound();
            }

            if (!cm.IsPrinted)
            {
                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Printed original copy of credit memo# {cm.CreditMemoNo}", "Credit Memo");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                cm.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            else
            {
                #region --Audit Trail Recording

                var printedBy = GetUserFullName();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed re-printed copy of credit memo# {cm.CreditMemoNo}", "Credit Memo");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording
            }

            return RedirectToAction(nameof(Print), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetCreditMemoList(
            [FromForm] DataTablesParameters parameters,
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var creditMemos = await _unitOfWork.FilprideCreditMemo
                    .GetAllAsync(cm => true, cancellationToken);

                // Apply date range filter if provided
                if (dateFrom.HasValue)
                {
                    creditMemos = creditMemos
                        .Where(s => s.TransactionDate >= DateOnly.FromDateTime(dateFrom.Value))
                        .ToList();
                }

                if (dateTo.HasValue)
                {
                    creditMemos = creditMemos
                        .Where(s => s.TransactionDate <= DateOnly.FromDateTime(dateTo.Value))
                        .ToList();
                }

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    creditMemos = creditMemos
                        .Where(s =>
                            s.CreditMemoNo!.ToLower().Contains(searchValue) ||
                            s.TransactionDate.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                            s.SalesInvoice?.SalesInvoiceNo?.ToLower().Contains(searchValue) == true ||
                            s.ServiceInvoice?.ServiceInvoiceNo.ToLower().Contains(searchValue) == true ||
                            s.Source.ToLower().Contains(searchValue) ||
                            s.CreditAmount.ToString().Contains(searchValue) ||
                            s.CreatedBy!.ToLower().Contains(searchValue) ||
                            s.Status.ToLower().Contains(searchValue)
                        )
                        .ToList();
                }

                // Apply sorting if provided
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Name;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    creditMemos = creditMemos
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = creditMemos.Count();

                // Apply pagination - HANDLE -1 FOR "ALL"
                IEnumerable<FilprideCreditMemo> pagedCreditMemos;

                if (parameters.Length == -1)
                {
                    // "All" selected - return all records
                    pagedCreditMemos = creditMemos;
                }
                else
                {
                    // Normal pagination
                    pagedCreditMemos = creditMemos
                        .Skip(parameters.Start)
                        .Take(parameters.Length);
                }

                var pagedData = pagedCreditMemos
                    .Select(x => new
                    {
                        x.CreditMemoId,
                        x.CreditMemoNo,
                        x.TransactionDate,
                        salesInvoiceNo = x.SalesInvoice?.SalesInvoiceNo,
                        serviceInvoiceNo = x.ServiceInvoice?.ServiceInvoiceNo,
                        x.Source,
                        x.CreditAmount,
                        x.CreatedBy,
                        x.Status,
                        x.SalesInvoiceId,
                        // Include status flags for badge rendering
                        isPosted = x.PostedBy != null,
                        isVoided = x.VoidedBy != null,
                        isCanceled = x.CanceledBy != null
                    })
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
                _logger.LogError(ex, "Failed to get credit memos. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        //Download as .xlsx file.(Export)

        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = (await _unitOfWork.FilprideCreditMemo
                .GetAllAsync(cm => recordIds.Contains(cm.CreditMemoId)))
                .OrderBy(cm => cm.CreditMemoNo)
                .ToList();

            // Create the Excel package
            using (var package = new ExcelPackage())
            {
                // Add a new worksheet to the Excel package

                #region -- Sales Invoice Table Header --

                var worksheet2 = package.Workbook.Worksheets.Add("SalesInvoice");

                worksheet2.Cells["A1"].Value = "OtherRefNo";
                worksheet2.Cells["B1"].Value = "Quantity";
                worksheet2.Cells["C1"].Value = "UnitPrice";
                worksheet2.Cells["D1"].Value = "Amount";
                worksheet2.Cells["E1"].Value = "Remarks";
                worksheet2.Cells["F1"].Value = "Status";
                worksheet2.Cells["G1"].Value = "TransactionDate";
                worksheet2.Cells["H1"].Value = "Discount";
                worksheet2.Cells["I1"].Value = "AmountPaid";
                worksheet2.Cells["J1"].Value = "Balance";
                worksheet2.Cells["K1"].Value = "IsPaid";
                worksheet2.Cells["L1"].Value = "IsTaxAndVatPaid";
                worksheet2.Cells["M1"].Value = "DueDate";
                worksheet2.Cells["N1"].Value = "CreatedBy";
                worksheet2.Cells["O1"].Value = "CreatedDate";
                worksheet2.Cells["P1"].Value = "CancellationRemarks";
                worksheet2.Cells["Q1"].Value = "OriginalReceivingReportId";
                worksheet2.Cells["R1"].Value = "OriginalCustomerId";
                worksheet2.Cells["S1"].Value = "OriginalPOId";
                worksheet2.Cells["T1"].Value = "OriginalProductId";
                worksheet2.Cells["U1"].Value = "OriginalSeriesNumber";
                worksheet2.Cells["V1"].Value = "OriginalDocumentId";
                worksheet2.Cells["W1"].Value = "PostedBy";
                worksheet2.Cells["X1"].Value = "PostedDate";

                #endregion -- Sales Invoice Table Header --

                #region -- Service Invoice Table Header --

                var worksheet3 = package.Workbook.Worksheets.Add("ServiceInvoice");

                worksheet3.Cells["A1"].Value = "DueDate";
                worksheet3.Cells["B1"].Value = "Period";
                worksheet3.Cells["C1"].Value = "Amount";
                worksheet3.Cells["D1"].Value = "Total";
                worksheet3.Cells["E1"].Value = "Discount";
                worksheet3.Cells["F1"].Value = "CurrentAndPreviousMonth";
                worksheet3.Cells["G1"].Value = "UnearnedAmount";
                worksheet3.Cells["H1"].Value = "Status";
                worksheet3.Cells["I1"].Value = "AmountPaid";
                worksheet3.Cells["J1"].Value = "Balance";
                worksheet3.Cells["K1"].Value = "Instructions";
                worksheet3.Cells["L1"].Value = "IsPaid";
                worksheet3.Cells["M1"].Value = "CreatedBy";
                worksheet3.Cells["N1"].Value = "CreatedDate";
                worksheet3.Cells["O1"].Value = "CancellationRemarks";
                worksheet3.Cells["P1"].Value = "OriginalCustomerId";
                worksheet3.Cells["Q1"].Value = "OriginalSeriesNumber";
                worksheet3.Cells["R1"].Value = "OriginalServicesId";
                worksheet3.Cells["S1"].Value = "OriginalDocumentId";
                worksheet3.Cells["T1"].Value = "PostedBy";
                worksheet3.Cells["U1"].Value = "PostedDate";

                #endregion -- Service Invoice Table Header --

                #region -- Credit Memo Table Header --

                var worksheet = package.Workbook.Worksheets.Add("CreditMemo");

                worksheet.Cells["A1"].Value = "TransactionDate";
                worksheet.Cells["B1"].Value = "DebitAmount";
                worksheet.Cells["C1"].Value = "Description";
                worksheet.Cells["D1"].Value = "AdjustedPrice";
                worksheet.Cells["E1"].Value = "Quantity";
                worksheet.Cells["F1"].Value = "Source";
                worksheet.Cells["G1"].Value = "Remarks";
                worksheet.Cells["H1"].Value = "Period";
                worksheet.Cells["I1"].Value = "Amount";
                worksheet.Cells["J1"].Value = "CurrentAndPreviousAmount";
                worksheet.Cells["K1"].Value = "UnearnedAmount";
                worksheet.Cells["L1"].Value = "ServicesId";
                worksheet.Cells["M1"].Value = "CreatedBy";
                worksheet.Cells["N1"].Value = "CreatedDate";
                worksheet.Cells["O1"].Value = "CancellationRemarks";
                worksheet.Cells["P1"].Value = "OriginalSalesInvoiceId";
                worksheet.Cells["Q1"].Value = "OriginalSeriesNumber";
                worksheet.Cells["R1"].Value = "OriginalServiceInvoiceId";
                worksheet.Cells["S1"].Value = "OriginalDocumentId";
                worksheet.Cells["T1"].Value = "PostedBy";
                worksheet.Cells["U1"].Value = "PostedDate";

                #endregion -- Credit Memo Table Header --

                #region -- Credit Memo Export --

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 2].Value = item.CreditAmount;
                    worksheet.Cells[row, 3].Value = item.Description;
                    worksheet.Cells[row, 4].Value = item.AdjustedPrice;
                    worksheet.Cells[row, 5].Value = item.Quantity;
                    worksheet.Cells[row, 6].Value = item.Source;
                    worksheet.Cells[row, 7].Value = item.Remarks;
                    worksheet.Cells[row, 8].Value = item.Period;
                    worksheet.Cells[row, 9].Value = item.Amount;
                    worksheet.Cells[row, 10].Value = item.CurrentAndPreviousAmount;
                    worksheet.Cells[row, 11].Value = item.UnearnedAmount;
                    worksheet.Cells[row, 12].Value = item.ServiceInvoice?.ServiceId;
                    worksheet.Cells[row, 13].Value = item.CreatedBy;
                    worksheet.Cells[row, 14].Value = item.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                    worksheet.Cells[row, 15].Value = item.CancellationRemarks;
                    worksheet.Cells[row, 16].Value = item.SalesInvoiceId;
                    worksheet.Cells[row, 17].Value = item.CreditMemoNo;
                    worksheet.Cells[row, 18].Value = item.ServiceInvoiceId;
                    worksheet.Cells[row, 19].Value = item.CreditMemoId;
                    worksheet.Cells[row, 20].Value = item.PostedBy;
                    worksheet.Cells[row, 21].Value = item.PostedDate?.ToString("yyyy-MM-dd HH:mm:ss.ffffff") ?? null;

                    row++;
                }

                #endregion -- Credit Memo Export --

                #region -- Sales Invoice Export --

                int siRow = 2;
                var currentSi = "";

                foreach (var item in selectedList)
                {
                    if (item.SalesInvoice == null)
                    {
                        continue;
                    }
                    if (item.SalesInvoice.SalesInvoiceNo == currentSi)
                    {
                        continue;
                    }

                    currentSi = item.SalesInvoice.SalesInvoiceNo;
                    worksheet2.Cells[siRow, 1].Value = item.SalesInvoice.OtherRefNo;
                    worksheet2.Cells[siRow, 2].Value = item.SalesInvoice.Quantity;
                    worksheet2.Cells[siRow, 3].Value = item.SalesInvoice.UnitPrice;
                    worksheet2.Cells[siRow, 4].Value = item.SalesInvoice.Amount;
                    worksheet2.Cells[siRow, 5].Value = item.SalesInvoice.Remarks;
                    worksheet2.Cells[siRow, 6].Value = item.SalesInvoice.Status;
                    worksheet2.Cells[siRow, 7].Value = item.SalesInvoice.TransactionDate.ToString("yyyy-MM-dd");
                    worksheet2.Cells[siRow, 8].Value = item.SalesInvoice.Discount;
                    worksheet2.Cells[siRow, 9].Value = item.SalesInvoice.AmountPaid;
                    worksheet2.Cells[siRow, 10].Value = item.SalesInvoice.Balance;
                    worksheet2.Cells[siRow, 11].Value = item.SalesInvoice.IsPaid;
                    worksheet2.Cells[siRow, 12].Value = item.SalesInvoice.IsTaxAndVatPaid;
                    worksheet2.Cells[siRow, 13].Value = item.SalesInvoice.DueDate.ToString("yyyy-MM-dd");
                    worksheet2.Cells[siRow, 14].Value = item.SalesInvoice.CreatedBy;
                    worksheet2.Cells[siRow, 15].Value = item.SalesInvoice.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                    worksheet2.Cells[siRow, 16].Value = item.SalesInvoice.CancellationRemarks;
                    worksheet2.Cells[siRow, 17].Value = item.SalesInvoice.ReceivingReportId;
                    worksheet2.Cells[siRow, 18].Value = item.SalesInvoice.CustomerId;
                    worksheet2.Cells[siRow, 19].Value = item.SalesInvoice.PurchaseOrderId;
                    worksheet2.Cells[siRow, 20].Value = item.SalesInvoice.ProductId;
                    worksheet2.Cells[siRow, 21].Value = item.SalesInvoice.SalesInvoiceNo;
                    worksheet2.Cells[siRow, 22].Value = item.SalesInvoice.SalesInvoiceId;
                    worksheet2.Cells[siRow, 23].Value = item.SalesInvoice.PostedBy;
                    worksheet2.Cells[siRow, 24].Value = item.SalesInvoice.PostedDate?.ToString("yyyy-MM-dd HH:mm:ss.ffffff") ?? null;

                    siRow++;
                }

                #endregion -- Sales Invoice Export --

                #region -- Service Invoice Export --

                int svRow = 2;
                var currentSv = "";

                foreach (var item in selectedList)
                {
                    if (item.ServiceInvoice == null)
                    {
                        continue;
                    }
                    if (item.ServiceInvoice.ServiceInvoiceNo == currentSv)
                    {
                        continue;
                    }

                    currentSv = item.ServiceInvoice.ServiceInvoiceNo;
                    worksheet3.Cells[svRow, 1].Value = item.ServiceInvoice.DueDate.ToString("yyyy-MM-dd");
                    worksheet3.Cells[svRow, 2].Value = item.ServiceInvoice.Period.ToString("yyyy-MM-dd");
                    worksheet3.Cells[svRow, 3].Value = item.ServiceInvoice.Total;
                    worksheet3.Cells[svRow, 4].Value = item.ServiceInvoice.Total;
                    worksheet3.Cells[svRow, 5].Value = item.ServiceInvoice.Discount;
                    worksheet3.Cells[svRow, 6].Value = item.ServiceInvoice.CurrentAndPreviousAmount;
                    worksheet3.Cells[svRow, 7].Value = item.ServiceInvoice.UnearnedAmount;
                    worksheet3.Cells[svRow, 8].Value = item.ServiceInvoice.Status;
                    worksheet3.Cells[svRow, 9].Value = item.ServiceInvoice.AmountPaid;
                    worksheet3.Cells[svRow, 10].Value = item.ServiceInvoice.Balance;
                    worksheet3.Cells[svRow, 11].Value = item.ServiceInvoice.Instructions;
                    worksheet3.Cells[svRow, 12].Value = item.ServiceInvoice.IsPaid;
                    worksheet3.Cells[svRow, 13].Value = item.ServiceInvoice.CreatedBy;
                    worksheet3.Cells[svRow, 14].Value = item.ServiceInvoice.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                    worksheet3.Cells[svRow, 15].Value = item.ServiceInvoice.CancellationRemarks;
                    worksheet3.Cells[svRow, 16].Value = item.ServiceInvoice.CustomerId;
                    worksheet3.Cells[svRow, 17].Value = item.ServiceInvoice.ServiceInvoiceNo;
                    worksheet3.Cells[svRow, 18].Value = item.ServiceInvoice.ServiceId;
                    worksheet3.Cells[svRow, 19].Value = item.ServiceInvoice.ServiceInvoiceId;
                    worksheet3.Cells[svRow, 20].Value = item.ServiceInvoice.PostedBy;
                    worksheet3.Cells[svRow, 21].Value = item.ServiceInvoice.PostedDate?.ToString("yyyy-MM-dd HH:mm:ss.ffffff") ?? null;

                    svRow++;
                }

                #endregion -- Service Invoice Export --

                //Set password in Excel
                foreach (var excelWorkSheet in package.Workbook.Worksheets)
                {
                    excelWorkSheet.Protection.SetPassword("mis123");
                }

                package.Workbook.Protection.SetPassword("mis123");

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"CreditMemoList_IBS_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public async Task<IActionResult> GetAllCreditMemoIds()
        {
            var cmIds = (await _unitOfWork.FilprideCreditMemo
                 .GetAllAsync(cm => cm.Type == nameof(DocumentType.Documented)))
                 .Select(cm => cm.CreditMemoId)
                 .ToList();

            return Json(cmIds);
        }

        [Authorize(Policy = nameof(CreditMemo.CreditMemoUnpost))]
        public async Task<IActionResult> Unpost(int id, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var creditMemo = await _unitOfWork.FilprideCreditMemo
                                                          .GetAsync(x => x.CreditMemoId == id, cancellationToken)
                                                      ?? throw new NullReferenceException("Credit memo id not found.");

                if (await _unitOfWork.IsPeriodPostedAsync(Module.CreditMemo, creditMemo.TransactionDate, cancellationToken))
                {
                    TempData["error"] = $"Cannot unpost this record because the period {creditMemo.TransactionDate:MMM yyyy} is already closed.";
                    return RedirectToAction(nameof(Print), new { id });
                }

                if (creditMemo.SalesInvoice != null)
                {
                    var creditAmount = Math.Abs(creditMemo.CreditAmount);
                    creditMemo.SalesInvoice.Balance += creditAmount;
                    creditMemo.SalesInvoice.CreditAmount -= creditAmount;
                }

                creditMemo.PostedBy = null;
                creditMemo.PostedDate = null;
                creditMemo.Status = nameof(DmCmStatus.ForApprovalOfFM);

                await _unitOfWork.FilprideCreditMemo.RemoveRecords<FilprideGeneralLedgerBook>(x => x.Reference == creditMemo.CreditMemoNo, cancellationToken);
                await _unitOfWork.FilprideCreditMemo.RemoveRecords<LockedPeriodAdjustment>(
                    x => x.EntityType == Module.CreditMemo && x.EntityTypeNo == creditMemo.CreditMemoNo,
                    cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Unposted credit memo# {creditMemo.CreditMemoNo}", "Credit Memo");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Credit Memo has been Unposted.";

                return RedirectToAction(nameof(Print), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unpost credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Unposted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "Admin,FinanceManager")]
        public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideCreditMemo
                .GetAsync(x => x.CreditMemoId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            if (model.Status != nameof(DmCmStatus.ForApprovalOfFM))
            {
                TempData["error"] = "This record is not pending for approval.";
                return RedirectToAction(nameof(Print), new { id });
            }

            var isApprover =
                User.IsInRole("Admin") ||
                User.IsInRole("FinanceManager");

            if (!isApprover)
            {
                TempData["error"] = "You have no access to do this action.";
                return RedirectToAction(nameof(Print), new { id });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model.SalesInvoice != null)
                {
                    var oldBalance = model.SalesInvoice.Balance;
                    var creditAmount = Math.Abs(model.CreditAmount);
                    var (supplierId, supplierName) = ResolveLockedPeriodSupplier(model.SalesInvoice);
                    model.SalesInvoice.Balance -= Math.Abs(model.CreditAmount);
                    model.SalesInvoice.CreditAmount += creditAmount;

                    await _unitOfWork.LockedPeriodAdjustment.AddIfPeriodPostedAsync(new()
                    {
                        Module = Module.SalesInvoice,
                        TransactionDate = model.SalesInvoice.TransactionDate,
                        EntityType = Module.CreditMemo,
                        EntityNo = model.CreditMemoNo ?? string.Empty,
                        CustomerId = model.SalesInvoice.CustomerOrderSlip?.CustomerId ?? model.SalesInvoice.CustomerId,
                        CustomerName = model.SalesInvoice.CustomerOrderSlip?.CustomerName ?? model.SalesInvoice.Customer?.CustomerName,
                        SupplierId = supplierId,
                        SupplierName = supplierName,
                        AdjustmentType = LockedPeriodAdjustmentType.CreditMemo,
                        OldValue = oldBalance,
                        NewValue = model.SalesInvoice.Balance,
                        AdjustmentValue = -creditAmount,
                        AffectedQuantity = model.Quantity ?? 0m,
                        Reason = "Approved credit memo affecting previous sales",
                        CreatedBy = GetUserFullName()
                    }, cancellationToken);
                }

                model.ApprovedBy = GetUserFullName();
                model.ApprovedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Approved credit memo# {model.CreditMemoNo}", "Credit Memo");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await PostCreditMemoAsync(model, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Credit Memo has been approved and posted.";
                return RedirectToAction(nameof(Print), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Approved by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                //return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });  --implement this if need to show in dashboard the approval of cnc
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task PostCreditMemoAsync(FilprideCreditMemo model, CancellationToken cancellationToken)
        {
            if (await _unitOfWork.IsPeriodPostedAsync(Module.CreditMemo, model.TransactionDate, cancellationToken))
            {
                throw new ArgumentException($"Cannot post this record because the period {model.TransactionDate:MMM yyyy} is already closed.");
            }

            model.PostedBy = GetUserFullName();
            model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
            model.Status = nameof(DmCmStatus.Posted);

            var accountTitlesDto = await _unitOfWork.FilprideServiceInvoice.GetListOfAccountTitleDto(cancellationToken);
            var arTradeReceivableTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020100") ?? throw new ArgumentException("Account title '101020100' not found.");
            var arNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020500") ?? throw new ArgumentException("Account title '101020500' not found.");
            var arTradeCwt = accountTitlesDto.Find(c => c.AccountNumber == "101020200") ?? throw new ArgumentException("Account title '101020200' not found.");
            var arTradeCwv = accountTitlesDto.Find(c => c.AccountNumber == "101020300") ?? throw new ArgumentException("Account title '101020300' not found.");
            var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account title '201030100' not found.");

            if (model.SalesInvoiceId != null)
            {
                var (salesAcctNo, _) = _unitOfWork.FilprideSalesInvoice.GetSalesAccountTitle(model.SalesInvoice!.Product!.ProductCode);
                var salesTitle = accountTitlesDto.Find(c => c.AccountNumber == salesAcctNo) ?? throw new ArgumentException($"Account title '{salesAcctNo}' not found.");

                decimal withHoldingTaxAmount = 0;
                decimal withHoldingVatAmount = 0;
                decimal netOfVatAmount;
                decimal vatAmount = 0;

                if (model.SalesInvoice!.CustomerOrderSlip!.VatType == SD.VatType_Vatable)
                {
                    netOfVatAmount = _unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(model.CreditAmount)) * -1;
                    vatAmount = _unitOfWork.FilprideCreditMemo.ComputeVatAmount(Math.Abs(netOfVatAmount)) * -1;
                }
                else
                {
                    netOfVatAmount = model.CreditAmount;
                }

                if (model.SalesInvoice.CustomerOrderSlip.HasEWT)
                {
                    withHoldingTaxAmount = _unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), model.SalesInvoice.DeliveryReceipt!.CwtPercent) * -1;
                }

                if (model.SalesInvoice.CustomerOrderSlip.HasWVAT)
                {
                    withHoldingVatAmount = _unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), model.SalesInvoice.DeliveryReceipt!.CwvPercent) * -1;
                }

                var ledgers = new List<FilprideGeneralLedgerBook>
                {
                    new()
                    {
                        Date = model.TransactionDate,
                        Reference = model.CreditMemoNo!,
                        Description = model.SalesInvoice.CustomerOrderSlip.ProductName,
                        AccountId = arTradeReceivableTitle.AccountId,
                        AccountNo = arTradeReceivableTitle.AccountNumber,
                        AccountTitle = arTradeReceivableTitle.AccountName,
                        Debit = 0,
                        Credit = Math.Abs(model.CreditAmount - (withHoldingTaxAmount + withHoldingVatAmount)),
                        CreatedBy = model.PostedBy,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        SubAccountType = SubAccountType.Customer,
                        SubAccountId = model.SalesInvoice.CustomerOrderSlip.CustomerId,
                        SubAccountName = model.SalesInvoice.CustomerOrderSlip.CustomerName,
                        ModuleType = nameof(ModuleType.CreditMemo)
                    }
                };

                if (withHoldingTaxAmount < 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = model.TransactionDate,
                        Reference = model.CreditMemoNo!,
                        Description = model.SalesInvoice.CustomerOrderSlip.ProductName,
                        AccountId = arTradeCwt.AccountId,
                        AccountNo = arTradeCwt.AccountNumber,
                        AccountTitle = arTradeCwt.AccountName,
                        Debit = 0,
                        Credit = Math.Abs(withHoldingTaxAmount),
                        CreatedBy = model.PostedBy,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.CreditMemo)
                    });
                }

                if (withHoldingVatAmount < 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = model.TransactionDate,
                        Reference = model.CreditMemoNo!,
                        Description = model.SalesInvoice.CustomerOrderSlip.ProductName,
                        AccountId = arTradeCwv.AccountId,
                        AccountNo = arTradeCwv.AccountNumber,
                        AccountTitle = arTradeCwv.AccountName,
                        Debit = 0,
                        Credit = Math.Abs(withHoldingVatAmount),
                        CreatedBy = model.PostedBy,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.CreditMemo)
                    });
                }

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = model.TransactionDate,
                    Reference = model.CreditMemoNo!,
                    Description = model.SalesInvoice.CustomerOrderSlip.ProductName,
                    AccountId = salesTitle.AccountId,
                    AccountNo = salesTitle.AccountNumber,
                    AccountTitle = salesTitle.AccountName,
                    Debit = Math.Abs(netOfVatAmount),
                    Credit = 0,
                    CreatedBy = model.PostedBy,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModuleType = nameof(ModuleType.CreditMemo)
                });

                if (vatAmount < 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = model.TransactionDate,
                        Reference = model.CreditMemoNo!,
                        Description = model.SalesInvoice.CustomerOrderSlip.ProductName,
                        AccountId = vatOutputTitle.AccountId,
                        AccountNo = vatOutputTitle.AccountNumber,
                        AccountTitle = vatOutputTitle.AccountName,
                        Debit = Math.Abs(vatAmount),
                        Credit = 0,
                        CreatedBy = model.PostedBy,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.CreditMemo)
                    });
                }

                if (!_unitOfWork.FilprideCreditMemo.IsJournalEntriesBalanced(ledgers))
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

                await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
            }

            if (model.ServiceInvoiceId != null)
            {
                var existingSv = await _unitOfWork.FilprideServiceInvoice
                    .GetAsync(sv => sv.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

                var serviceAccountNo = existingSv!.Service!.CurrentAndPreviousNo;
                var serviceTitle = accountTitlesDto.Find(c => c.AccountNumber == serviceAccountNo) ?? throw new ArgumentException($"Account title '{serviceAccountNo}' not found.");

                decimal netAmount;
                if (model.ServiceInvoice!.VatType == SD.VatType_Vatable)
                {
                    netAmount = DecimalRoundingHelper.ComputeNetOfVat((model.Amount ?? 0m) - existingSv.Discount);
                    var total = DecimalRoundingHelper.ComputeNetOfVat(model.Amount ?? 0m);
                    var roundedNetAmount = DecimalRoundingHelper.RoundToFour(netAmount);

                    if (roundedNetAmount > total)
                    {
                        var shortAmount = netAmount - total;
                        netAmount -= shortAmount;
                    }
                }
                else
                {
                    netAmount = (model.Amount ?? 0) - existingSv.Discount;
                }

                decimal withHoldingTaxAmount = 0;
                decimal withHoldingVatAmount = 0;
                decimal netOfVatAmount = 0;
                decimal vatAmount = 0;

                if (model.ServiceInvoice.VatType == SD.VatType_Vatable)
                {
                    netOfVatAmount = _unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(model.CreditAmount)) * -1;
                    vatAmount = _unitOfWork.FilprideCreditMemo.ComputeVatAmount(Math.Abs(netOfVatAmount)) * -1;
                }
                else
                {
                    netOfVatAmount = model.CreditAmount;
                }

                if (model.ServiceInvoice.HasEwt)
                {
                    withHoldingTaxAmount = _unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), existingSv.ServicePercent / 100m) * -1;
                }

                if (model.ServiceInvoice.HasWvat)
                {
                    withHoldingVatAmount = _unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.05m) * -1;
                }

                var ledgers = new List<FilprideGeneralLedgerBook>
                {
                    new()
                    {
                        Date = model.TransactionDate,
                        Reference = model.CreditMemoNo!,
                        Description = model.ServiceInvoice.ServiceName,
                        AccountId = arNonTradeTitle.AccountId,
                        AccountNo = arNonTradeTitle.AccountNumber,
                        AccountTitle = arNonTradeTitle.AccountName,
                        Debit = 0,
                        Credit = Math.Abs(model.CreditAmount - (withHoldingTaxAmount + withHoldingVatAmount)),
                        CreatedBy = model.PostedBy,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        SubAccountType = SubAccountType.Customer,
                        SubAccountId = model.ServiceInvoice.CustomerId,
                        SubAccountName = model.ServiceInvoice.CustomerName,
                        ModuleType = nameof(ModuleType.CreditMemo)
                    }
                };

                if (withHoldingTaxAmount < 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = model.TransactionDate,
                        Reference = model.CreditMemoNo!,
                        Description = model.ServiceInvoice.ServiceName,
                        AccountId = arTradeCwt.AccountId,
                        AccountNo = arTradeCwt.AccountNumber,
                        AccountTitle = arTradeCwt.AccountName,
                        Debit = 0,
                        Credit = Math.Abs(withHoldingTaxAmount),
                        CreatedBy = model.PostedBy,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.CreditMemo)
                    });
                }

                if (withHoldingVatAmount < 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = model.TransactionDate,
                        Reference = model.CreditMemoNo!,
                        Description = model.ServiceInvoice.ServiceName,
                        AccountId = arTradeCwv.AccountId,
                        AccountNo = arTradeCwv.AccountNumber,
                        AccountTitle = arTradeCwv.AccountName,
                        Debit = 0,
                        Credit = Math.Abs(withHoldingVatAmount),
                        CreatedBy = model.PostedBy,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.CreditMemo)
                    });
                }

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = model.TransactionDate,
                    Reference = model.CreditMemoNo!,
                    Description = model.ServiceInvoice.ServiceName,
                    AccountId = serviceTitle.AccountId,
                    AccountNo = serviceTitle.AccountNumber,
                    AccountTitle = serviceTitle.AccountName,
                    Debit = DecimalRoundingHelper.RoundToFour(netAmount),
                    Credit = 0,
                    CreatedBy = model.PostedBy,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModuleType = nameof(ModuleType.CreditMemo)
                });

                if (vatAmount < 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = model.TransactionDate,
                        Reference = model.CreditMemoNo!,
                        Description = model.ServiceInvoice.ServiceName,
                        AccountId = vatOutputTitle.AccountId,
                        AccountNo = vatOutputTitle.AccountNumber,
                        AccountTitle = vatOutputTitle.AccountName,
                        Debit = Math.Abs(vatAmount),
                        Credit = 0,
                        CreatedBy = model.PostedBy,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.CreditMemo)
                    });
                }

                if (!_unitOfWork.FilprideCreditMemo.IsJournalEntriesBalanced(ledgers))
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

                await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
            }

            FilprideAuditTrail auditTrailBook = new(model.PostedBy!, $"Posted credit memo# {model.CreditMemoNo}", "Credit Memo");
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);
        }
    }
}
