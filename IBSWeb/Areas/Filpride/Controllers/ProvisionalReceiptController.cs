using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class ProvisionalReceiptController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProvisionalReceiptController> _logger;

        public ProvisionalReceiptController(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            ILogger<ProvisionalReceiptController> logger)
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

        private async Task PopulateFormDependenciesAsync(ProvisionalReceiptViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                viewModel.Suppliers = [];
            }
            else
            {
                viewModel.Suppliers = await _unitOfWork.GetFilprideEmployeeSupplierListAsyncById(companyClaims, cancellationToken);
            }

            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.ProvisionalReceipt, cancellationToken);
        }

        private static PREditViewModel MapToEditViewModel(FilprideProvisionalReceipt model)
        {
            return new PREditViewModel
            {
                Id = model.Id,
                TransactionDate = model.TransactionDate,
                SupplierId = model.SupplierId,
                ReferenceNo = model.ReferenceNo,
                Remarks = model.Remarks,
                CashAmount = model.CashAmount,
                CheckAmount = model.CheckAmount,
                CheckDate = model.CheckDate,
                CheckNo = model.CheckNo,
                CheckBank = model.CheckBank,
                CheckBranch = model.CheckBranch,
                ManagersCheckAmount = model.ManagersCheckAmount,
                ManagersCheckDate = model.ManagersCheckDate,
                ManagersCheckNo = model.ManagersCheckNo,
                ManagersCheckBank = model.ManagersCheckBank,
                ManagersCheckBranch = model.ManagersCheckBranch,
                EWT = model.EWT,
                WVAT = model.WVAT,
                Total = model.Total,
                BatchNumber = model.BatchNumber
            };
        }

        private static void MapFormToEntity(ProvisionalReceiptViewModel viewModel, FilprideProvisionalReceipt model)
        {
            model.TransactionDate = viewModel.TransactionDate;
            model.SupplierId = viewModel.SupplierId;
            model.ReferenceNo = viewModel.ReferenceNo.Trim();
            model.Remarks = viewModel.Remarks?.Trim() ?? string.Empty;
            model.CashAmount = viewModel.CashAmount;
            model.CheckAmount = viewModel.CheckAmount;
            model.CheckDate = viewModel.CheckDate;
            model.CheckNo = viewModel.CheckNo?.Trim();
            model.CheckBank = viewModel.CheckBank?.Trim();
            model.CheckBranch = viewModel.CheckBranch?.Trim();
            model.ManagersCheckAmount = viewModel.ManagersCheckAmount;
            model.ManagersCheckDate = viewModel.ManagersCheckDate;
            model.ManagersCheckNo = viewModel.ManagersCheckNo?.Trim();
            model.ManagersCheckBank = viewModel.ManagersCheckBank?.Trim();
            model.ManagersCheckBranch = viewModel.ManagersCheckBranch?.Trim();
            model.EWT = viewModel.EWT;
            model.WVAT = viewModel.WVAT;
            model.Total = viewModel.CashAmount
                          + viewModel.CheckAmount
                          + viewModel.ManagersCheckAmount
                          + viewModel.EWT
                          + viewModel.WVAT;
            model.BatchNumber = viewModel.BatchNumber;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            ViewBag.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.ProvisionalReceipt, cancellationToken);
            return View();
        }

        public async Task<IActionResult> GetBanks(CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            return Json(await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetProvisionalReceipts([FromForm] DataTablesParameters parameters, DateOnly filterDate, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var query = _unitOfWork.ProvisionalReceipt
                    .GetAllQuery(pr => true);

                var totalRecords = await query.CountAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    var hasTransactionDate = DateOnly.TryParse(searchValue, out var transactionDate);

                    query = query.Where(pr =>
                        pr.SeriesNumber.ToLower().Contains(searchValue) ||
                        pr.ReferenceNo.ToLower().Contains(searchValue) ||
                        (pr.Remarks ?? string.Empty).ToLower().Contains(searchValue) ||
                        pr.Supplier.SupplierName.ToLower().Contains(searchValue) ||
                        (pr.CreatedBy ?? string.Empty).ToLower().Contains(searchValue) ||
                        pr.Status.ToLower().Contains(searchValue) ||
                        (hasTransactionDate && pr.TransactionDate == transactionDate));
                }

                if (filterDate != DateOnly.MinValue && filterDate != default)
                {
                    query = query.Where(pr => pr.TransactionDate == filterDate);
                }

                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var ascending = orderColumn.Dir.ToLower() == "asc";
                    query = columnName switch
                    {
                        "seriesNumber" => ascending
                            ? query.OrderBy(pr => pr.SeriesNumber)
                            : query.OrderByDescending(pr => pr.SeriesNumber),
                        "transactionDate" => ascending
                            ? query.OrderBy(pr => pr.TransactionDate)
                            : query.OrderByDescending(pr => pr.TransactionDate),
                        "referenceNo" => ascending
                            ? query.OrderBy(pr => pr.ReferenceNo)
                            : query.OrderByDescending(pr => pr.ReferenceNo),
                        "total" => ascending
                            ? query.OrderBy(pr => pr.Total)
                            : query.OrderByDescending(pr => pr.Total),
                        "createdBy" => ascending
                            ? query.OrderBy(pr => pr.CreatedBy)
                            : query.OrderByDescending(pr => pr.CreatedBy),
                        "status" => ascending
                            ? query.OrderBy(pr => pr.Status)
                            : query.OrderByDescending(pr => pr.Status),
                        "employeeName" => ascending
                            ? query.OrderBy(pr => pr.Supplier.SupplierName)
                            : query.OrderByDescending(pr => pr.Supplier.SupplierName),
                        _ => query.OrderByDescending(pr => pr.Id)
                    };
                }
                else
                {
                    query = query.OrderByDescending(pr => pr.Id);
                }

                var totalFilteredRecords = await query.CountAsync(cancellationToken);

                var pagedData = await query
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .Select(pr => new
                    {
                        pr.Id,
                        pr.SeriesNumber,
                        pr.TransactionDate,
                        EmployeeName = pr.Supplier.SupplierName,
                        pr.ReferenceNo,
                        pr.Total,
                        pr.DepositedDate,
                        pr.ClearedDate,
                        pr.CreatedBy,
                        pr.Status,
                        pr.PostedBy,
                        pr.VoidedBy,
                        pr.CanceledBy
                    })
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
                _logger.LogError(ex, "Failed to get provisional receipts. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Policy = nameof(ProvisionalReceipt.ProvisionalReceiptCreate))]
        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var viewModel = new PRCreateViewModel
            {
                TransactionDate = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()),
                Type = nameof(DocumentType.Undocumented)
            };

            await PopulateFormDependenciesAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        [Authorize(Policy = nameof(ProvisionalReceipt.ProvisionalReceiptCreate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PRCreateViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await PopulateFormDependenciesAsync(viewModel, cancellationToken);
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            var total = viewModel.CashAmount + viewModel.CheckAmount + viewModel.ManagersCheckAmount + viewModel.EWT + viewModel.WVAT;

            if (total <= 0)
            {
                await PopulateFormDependenciesAsync(viewModel, cancellationToken);
                TempData["warning"] = "Please input at least one form of payment.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var userFullName = GetUserFullName();
                var model = new FilprideProvisionalReceipt
                {
                    SeriesNumber = await _unitOfWork.ProvisionalReceipt
                        .GenerateSeriesNumberAsync(companyClaims, viewModel.Type, cancellationToken),
                    CreatedBy = userFullName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    Status = nameof(CollectionReceiptStatus.Pending),
                };

                MapFormToEntity(viewModel, model);
                model.Type = viewModel.Type;

                await _dbContext.FilprideProvisionalReceipts.AddAsync(model, cancellationToken);

                var auditTrail = new FilprideAuditTrail(userFullName,
                    $"Create new provisional receipt# {model.SeriesNumber}", "Provisional Receipt");
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrail, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Provisional receipt #{model.SeriesNumber} created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                await PopulateFormDependenciesAsync(viewModel, cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create provisional receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, GetUserFullName());
                return View(viewModel);
            }
        }

        [Authorize(Policy = nameof(ProvisionalReceipt.ProvisionalReceiptEdit))]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var model = await _unitOfWork.ProvisionalReceipt
                .GetAsync(pr => pr.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            if (await _unitOfWork.IsPeriodPostedAsync(Module.ProvisionalReceipt, model.TransactionDate, cancellationToken))
            {
                TempData["error"] = $"Cannot edit this record because the period {model.TransactionDate:MMM yyyy} is already closed.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = MapToEditViewModel(model);
            await PopulateFormDependenciesAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        [Authorize(Policy = nameof(ProvisionalReceipt.ProvisionalReceiptEdit))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PREditViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await PopulateFormDependenciesAsync(viewModel, cancellationToken);
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            var model = await _unitOfWork.ProvisionalReceipt
                .GetAsync(pr => pr.Id == viewModel.Id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var total = viewModel.CashAmount + viewModel.CheckAmount + viewModel.ManagersCheckAmount + viewModel.EWT + viewModel.WVAT;

            if (total <= 0)
            {
                await PopulateFormDependenciesAsync(viewModel, cancellationToken);
                TempData["warning"] = "Please input at least one form of payment.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                MapFormToEntity(viewModel, model);
                model.EditedBy = GetUserFullName();
                model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                var auditTrail = new FilprideAuditTrail(GetUserFullName(),
                    $"Edited provisional receipt# {model.SeriesNumber}", "Provisional Receipt");
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrail, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Provisional receipt updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                await PopulateFormDependenciesAsync(viewModel, cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit provisional receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, GetUserFullName());
                return View(viewModel);
            }
        }

        [Authorize(Policy = nameof(ProvisionalReceipt.ProvisionalReceiptPreview))]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var model = await _unitOfWork.ProvisionalReceipt
                .GetAsync(pr => pr.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [Authorize(Policy = nameof(ProvisionalReceipt.ProvisionalReceiptPreview))]
        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var model = await _unitOfWork.ProvisionalReceipt
                .GetAsync(pr => pr.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (!model.IsPrinted)
                {
                    model.IsPrinted = true;

                    var printedBy = GetUserFullName();
                    var auditTrail = new FilprideAuditTrail(printedBy,
                        $"Printed original copy of provisional receipt# {model.SeriesNumber}", "Provisional Receipt");
                    await _dbContext.FilprideAuditTrails.AddAsync(auditTrail, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to tag provisional receipt as printed. Error: {ErrorMessage}, Stack: {StackTrace}. Printed by: {UserName}",
                    ex.Message, ex.StackTrace, GetUserFullName());
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Print), new { id });
        }

        [Authorize(Policy = nameof(ProvisionalReceipt.ProvisionalReceiptPost))]
        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var model = await _unitOfWork.ProvisionalReceipt
                .GetAsync(pr => pr.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            if (model.PostedBy != null)
            {
                TempData["info"] = "Provisional receipt has already been posted.";
                return RedirectToAction(nameof(Print), new { id });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.PostedBy = GetUserFullName();
                model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(CollectionReceiptStatus.Posted);

                var auditTrail = new FilprideAuditTrail(model.PostedBy,
                    $"Posted provisional receipt# {model.SeriesNumber}", "Provisional Receipt");
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrail, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Provisional receipt has been posted.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to post provisional receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, GetUserFullName());
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Print), new { id });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var model = await _unitOfWork.ProvisionalReceipt
                .GetAsync(pr => pr.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.PostedBy = null;
                model.PostedDate = null;
                model.VoidedBy = GetUserFullName();
                model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(CollectionReceiptStatus.Voided);

                await _unitOfWork.GeneralLedger.ReverseEntries(model.SeriesNumber, cancellationToken);

                var auditTrail = new FilprideAuditTrail(model.VoidedBy,
                    $"Voided provisional receipt# {model.SeriesNumber}", "Provisional Receipt");
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrail, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Provisional receipt has been voided.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to void provisional receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                    ex.Message, ex.StackTrace, GetUserFullName());
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = nameof(ProvisionalReceipt.ProvisionalReceiptCancel))]
        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var model = await _unitOfWork.ProvisionalReceipt
                .GetAsync(pr => pr.Id == id, cancellationToken);

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
                model.Status = nameof(CollectionReceiptStatus.Canceled);

                var auditTrail = new FilprideAuditTrail(model.CanceledBy,
                    $"Canceled provisional receipt# {model.SeriesNumber}", "Provisional Receipt");
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrail, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Provisional receipt has been canceled.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to cancel provisional receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, GetUserFullName());
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = nameof(ProvisionalReceipt.ProvisionalReceiptAddDepositInfo))]
        [HttpGet]
        public async Task<IActionResult> Deposit(int id, int bankId, DateOnly depositDate, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var bank = await _unitOfWork.FilprideBankAccount.GetAsync(b => b.BankAccountId == bankId, cancellationToken);
            var model = await _unitOfWork.ProvisionalReceipt
                .GetAsync(pr => pr.Id == id, cancellationToken);

            if (bank == null || model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.BankId = bank.BankAccountId;
                model.BankAccountName = bank.AccountName;
                model.BankAccountNo = bank.AccountNo;
                model.DepositedDate = depositDate;
                model.ClearedDate = null;
                model.Status = nameof(CollectionReceiptStatus.Deposited);

                var auditTrail = new FilprideAuditTrail(GetUserFullName(),
                    $"Record deposit date of provisional receipt#{model.SeriesNumber}", "Provisional Receipt");
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrail, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Provisional receipt deposit date has been recorded successfully.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to record provisional receipt deposit date. Error: {ErrorMessage}, Stack: {StackTrace}. Recorded by: {UserName}",
                    ex.Message, ex.StackTrace, GetUserFullName());
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = nameof(ProvisionalReceipt.ProvisionalReceiptReturnCheck))]
        [HttpGet]
        public async Task<IActionResult> Return(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var model = await _unitOfWork.ProvisionalReceipt
                .GetAsync(pr => pr.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.DepositedDate = null;
                model.ClearedDate = null;
                model.Status = nameof(CollectionReceiptStatus.Returned);

                var auditTrail = new FilprideAuditTrail(GetUserFullName(),
                    $"Return checks of provisional receipt#{model.SeriesNumber}", "Provisional Receipt");
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrail, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Provisional receipt has been returned successfully.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to return provisional receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Returned by: {UserName}",
                    ex.Message, ex.StackTrace, GetUserFullName());
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = nameof(ProvisionalReceipt.ProvisionalReceiptRedeposit))]
        [HttpGet]
        public async Task<IActionResult> Redeposit(int id, DateOnly redepositDate, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var model = await _unitOfWork.ProvisionalReceipt
                .GetAsync(pr => pr.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.DepositedDate = redepositDate;
                model.ClearedDate = null;
                model.Status = nameof(CollectionReceiptStatus.Redeposited);

                var auditTrail = new FilprideAuditTrail(GetUserFullName(),
                    $"Redeposit provisional receipt#{model.SeriesNumber}", "Provisional Receipt");
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrail, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Provisional receipt has been redeposited successfully.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to redeposit provisional receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Redeposited by: {UserName}",
                    ex.Message, ex.StackTrace, GetUserFullName());
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = nameof(ProvisionalReceipt.ProvisionalReceiptApplyClearingDate))]
        [HttpGet]
        public async Task<IActionResult> ApplyClearingDate(int id, DateOnly clearingDate, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var model = await _unitOfWork.ProvisionalReceipt
                .GetAsync(pr => pr.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model.DepositedDate == null)
                {
                    throw new InvalidOperationException("Deposited date cannot be null.");
                }

                model.ClearedDate = clearingDate;
                model.Status = nameof(CollectionReceiptStatus.Cleared);
                await _unitOfWork.ProvisionalReceipt.DepositAsync(model, cancellationToken);

                var auditTrail = new FilprideAuditTrail(GetUserFullName(),
                    $"Apply clearing date for provisional receipt#{model.SeriesNumber}", "Provisional Receipt");
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrail, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Provisional receipt clearing date has been applied successfully.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to apply provisional receipt clearing date. Error: {ErrorMessage}, Stack: {StackTrace}. Recorded by: {UserName}",
                    ex.Message, ex.StackTrace, GetUserFullName());
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = nameof(ProvisionalReceipt.ProvisionalReceiptUnpost))]
        public async Task<IActionResult> Unpost(int id, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var provisionalReceipt = await _unitOfWork.ProvisionalReceipt
                                                          .GetAsync(x => x.Id == id, cancellationToken)
                                                      ?? throw new NullReferenceException("Provisional receipt id not found.");

                if (await _unitOfWork.IsPeriodPostedAsync(Module.ProvisionalReceipt, provisionalReceipt.TransactionDate, cancellationToken))
                {
                    TempData["error"] = $"Cannot unpost this record because the period {provisionalReceipt.TransactionDate:MMM yyyy} is already closed.";
                    return RedirectToAction(nameof(Print), new { id });
                }

                provisionalReceipt.PostedBy = null;
                provisionalReceipt.PostedDate = null;
                provisionalReceipt.Status = nameof(Status.Pending);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Unposted provisional receipt# {provisionalReceipt.SeriesNumber}", "Provisional Receipt");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Provisional receipt has been Unposted.";

                return RedirectToAction(nameof(Print), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unpost provisional receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Unposted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
