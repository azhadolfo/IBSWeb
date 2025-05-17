using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Bienes;
using IBS.Models.Bienes.ViewModels;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Bienes.Controllers
{
    [Area(nameof(Bienes))]
    [CompanyAuthorize(nameof(Bienes))]
    public class PlacementController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ILogger<PlacementController> _logger;

        public PlacementController(IUnitOfWork unitOfWork,
            ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            ILogger<PlacementController> logger)
        {
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
            _userManager = userManager;
            _logger = logger;
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

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPlacements([FromForm] DataTablesParameters parameters,
            CancellationToken cancellationToken)
        {
            try
            {
                var query = await _unitOfWork.BienesPlacement
                    .GetAllAsync(cancellationToken: cancellationToken);

                    if (!string.IsNullOrEmpty(parameters.Search?.Value))
                    {
                        var searchValue = parameters.Search.Value.ToLower();

                        query = query
                            .Where(s =>
                                s.ControlNumber.ToLower().Contains(searchValue) ||
                                s.CreatedDate.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                                s.Company.CompanyName.ToLower().Contains(searchValue) ||
                                s.BankAccount?.Bank.ToLower().Contains(searchValue) == true ||
                                s.TDAccountNumber.ToLower().Contains(searchValue) ||
                                s.PrincipalAmount.ToString().Contains(searchValue)
                        )
                        .ToList();
                }


                // Column-specific search
                foreach (var column in parameters.Columns)
                {
                    if (!string.IsNullOrEmpty(column.Search?.Value))
                    {
                        var searchValue = column.Search.Value.ToLower();
                        switch (column.Data)
                        {
                            case "status":
                                query = query.Where(p => p.Status.ToLower() == searchValue);
                                break;
                        }
                    }
                }

                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    query = query
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = query.Count();

                var pagedData = query
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
                _logger.LogError(ex, "Failed to get placements. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            PlacementViewModel viewModel = new()
            {
                Companies = await _unitOfWork.GetCompanyListAsyncById(cancellationToken),
                BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken),
                SettlementAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PlacementViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.Companies = await _unitOfWork.GetCompanyListAsyncById(cancellationToken);
                viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {

                BienesPlacement model = new()
                {
                    ControlNumber = await _unitOfWork.BienesPlacement.GenerateControlNumberAsync(viewModel.CompanyId, cancellationToken),
                    CompanyId = viewModel.CompanyId,
                    BankId = viewModel.BankId,
                    Bank = viewModel.Bank,
                    Branch = viewModel.Branch,
                    TDAccountNumber = viewModel.TDAccountNumber,
                    AccountName = viewModel.AccountName,
                    SettlementAccountId = viewModel.SettlementAccountId,
                    DateFrom = viewModel.FromDate,
                    DateTo = viewModel.ToDate,
                    Remarks = viewModel.Remarks,
                    ChequeNumber = viewModel.ChequeNumber,
                    CVNo = viewModel.CVNo,
                    BatchNumber = viewModel.BatchNumber,
                    PrincipalAmount = viewModel.PrincipalAmount,
                    PrincipalDisposition = viewModel.PrincipalDisposition,
                    PlacementType = viewModel.PlacementType,
                    InterestRate = viewModel.InterestRate / 100,
                    HasEWT = viewModel.HasEwt,
                    EWTRate = viewModel.EWTRate / 100,
                    HasTrustFee = viewModel.HasTrustFee,
                    TrustFeeRate = viewModel.TrustFeeRate / 100,
                    CreatedBy = User.Identity!.Name!,
                    LockedDate = viewModel.ToDate.AddDays(2).ToDateTime(TimeOnly.MinValue),

                };

                if (model.PlacementType == PlacementType.LongTerm)
                {
                    model.NumberOfYears = viewModel.NumberOfYears;
                    model.FrequencyOfPayment = viewModel.FrequencyOfPayment;
                }

                await _unitOfWork.BienesPlacement.AddAsync(model, cancellationToken);

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new placement# {model.ControlNumber}", "Placement", nameof(Bienes));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = $"Placement was successfully created. Control Number: {model.ControlNumber}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create placement. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                await transaction.RollbackAsync(cancellationToken);
                viewModel.Companies = await _unitOfWork.GetCompanyListAsyncById(cancellationToken);
                viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
                return View(viewModel);
            }
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var existingRecord = await _unitOfWork.BienesPlacement
                    .GetAsync(p => p.PlacementId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return NotFound();
                }

                PlacementViewModel viewModel = new()
                {
                    PlacementId = existingRecord.PlacementId,
                    Companies = await _unitOfWork.GetCompanyListAsyncById(cancellationToken),
                    BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken),
                    CompanyId = existingRecord.CompanyId,
                    BankId = existingRecord.BankId,
                    Bank = existingRecord.Bank,
                    Branch = existingRecord.Branch,
                    TDAccountNumber = existingRecord.TDAccountNumber,
                    AccountName = existingRecord.AccountName,
                    SettlementAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken),
                    SettlementAccountId = existingRecord.SettlementAccountId,
                    FromDate = existingRecord.DateFrom,
                    ToDate = existingRecord.DateTo,
                    Remarks = existingRecord.Remarks,
                    ChequeNumber = existingRecord.ChequeNumber,
                    CVNo = existingRecord.CVNo,
                    BatchNumber = existingRecord.BatchNumber,
                    PrincipalAmount = existingRecord.PrincipalAmount,
                    PrincipalDisposition = existingRecord.PrincipalDisposition,
                    PlacementType = existingRecord.PlacementType,
                    InterestRate = existingRecord.InterestRate * 100,
                    HasEwt = existingRecord.HasEWT,
                    EWTRate = existingRecord.EWTRate * 100,
                    HasTrustFee = existingRecord.HasTrustFee,
                    TrustFeeRate = existingRecord.TrustFeeRate * 100,
                    NumberOfYears = existingRecord.NumberOfYears,
                    FrequencyOfPayment = existingRecord.FrequencyOfPayment,
                };

                return View(viewModel);

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch placement. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PlacementViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                viewModel.Companies = await _unitOfWork.GetCompanyListAsyncById(cancellationToken);
                viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                viewModel.CurrentUser = User.Identity!.Name!;

                await _unitOfWork.BienesPlacement.UpdateAsync(viewModel, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = "Placement updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit placement. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                await transaction.RollbackAsync(cancellationToken);
                viewModel.Companies = await _unitOfWork.GetCompanyListAsyncById(cancellationToken);
                viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Preview(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.BienesPlacement
                    .GetAsync(p => p.PlacementId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                return View(existingRecord);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to preview. Error: {ErrorMessage}, Stack: {StackTrace}. Previewed by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Index));
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
                var existingRecord = await _unitOfWork.BienesPlacement
                    .GetAsync(p => p.PlacementId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.DateFrom == default || existingRecord.DateTo == default)
                {
                    TempData["error"] = "The system has detected that this is a newly rolled-over account. " +
                                        "Please ensure necessary modifications are made before proceeding with the posting.";
                    return RedirectToAction(nameof(Preview), new { id });
                }

                existingRecord.PostedBy = User.Identity!.Name!;
                existingRecord.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingRecord.Status = nameof(PlacementStatus.Posted);
                existingRecord.IsPosted = true;

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(existingRecord.PostedBy, $"Posted placement# {existingRecord.ControlNumber}", "Placement", nameof(Bienes));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                TempData["success"] = "Placement posted successfully.";
                return RedirectToAction(nameof(Preview), new { id });

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to post placement. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBankBranchById(int bankId)
        {
            var bank = await _unitOfWork.FilprideBankAccount
                .GetAsync(b => b.BankAccountId == bankId);

            if (bank == null)
            {
                return NotFound();
            }

            return Json(new
            {
                branch = bank.Branch,
                bank = bank.Bank,
            });
        }

        [HttpPost]
        public async Task<IActionResult> Terminate(TerminatePlacementViewModel viewModel, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingRecord = await _unitOfWork.BienesPlacement
                    .GetAsync(p => p.PlacementId == viewModel.PlacementId, cancellationToken);

                if (existingRecord == null)
                {
                    return NotFound();
                }

                existingRecord.Status = nameof(PlacementStatus.Terminated);
                existingRecord.TerminatedDate = viewModel.TerminatedDate;
                existingRecord.TerminatedBy = User.Identity!.Name!;
                existingRecord.InterestDeposited = viewModel.InterestDeposited;
                existingRecord.InterestDepositedDate = viewModel.InterestDepositedDate == default ? null : viewModel.InterestDepositedDate;
                existingRecord.InterestDepositedTo = viewModel.InterestDepositedTo;
                existingRecord.InterestStatus = viewModel.InterestStatus;
                existingRecord.TerminationRemarks = viewModel.TerminationRemarks;

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(existingRecord.TerminatedBy, $"Terminate placement# {existingRecord.ControlNumber}", "Placement", nameof(Bienes));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = "Terminated placement successfully.";
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Preview), new { id = viewModel.PlacementId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to terminate placement. Error: {ErrorMessage}, Stack: {StackTrace}. Terminated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Preview), new { id = viewModel.PlacementId });
            }

        }

        public async Task<IActionResult> Reactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingRecord = await _unitOfWork.BienesPlacement
                    .GetAsync(p => p.PlacementId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return NotFound();
                }

                existingRecord.Status = existingRecord.IsLocked ? nameof(PlacementStatus.Locked) : nameof(PlacementStatus.Posted);
                existingRecord.TerminatedDate = null;
                existingRecord.TerminatedBy = null;
                existingRecord.InterestDeposited = 0;
                existingRecord.InterestDepositedDate = null;
                existingRecord.InterestDepositedTo = null;
                existingRecord.InterestStatus = null;
                existingRecord.TerminationRemarks = null;

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Reactivate placement# {existingRecord.ControlNumber}", "Placement", nameof(Bienes));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = "Reactivate placement successfully.";
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to reactivate placement. Error: {ErrorMessage}, Stack: {StackTrace}. Reactived by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> RollOver(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingRecord = await _unitOfWork.BienesPlacement
                    .GetAsync(p => p.PlacementId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return NotFound();
                }

                var user = User.Identity!.Name;

                existingRecord.IsRolled = true;

                if (existingRecord.TerminatedBy == null)
                {
                    existingRecord.TerminatedBy = user;
                }

                await _unitOfWork.BienesPlacement.RollOverAsync(existingRecord, user!, cancellationToken);

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(user!, $"Rollover placement# {existingRecord.ControlNumber}", "Placement", nameof(Bienes));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = $"Rollover placement successfully. " +
                                      $"You can now modified the newly created placement with the same control#{existingRecord.ControlNumber}";
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to roll over placement. Error: {ErrorMessage}, Stack: {StackTrace}. Rollover by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RollOver(int? id, TerminatePlacementViewModel? terminateModel = null, CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingRecord = await _unitOfWork.BienesPlacement
                    .GetAsync(p => p.PlacementId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return NotFound();
                }

                var user = User.Identity!.Name;

                existingRecord.IsRolled = true;

                // Check if termination details were provided
                if (terminateModel?.PlacementId > 0)
                {
                    // Apply termination details
                    existingRecord.Status = nameof(PlacementStatus.Terminated);
                    existingRecord.TerminatedDate = terminateModel.TerminatedDate;
                    existingRecord.TerminatedBy = user;
                    existingRecord.InterestDeposited = terminateModel.InterestDeposited;
                    existingRecord.InterestDepositedDate = terminateModel.InterestDepositedDate == default ? null : terminateModel.InterestDepositedDate;
                    existingRecord.InterestDepositedTo = terminateModel.InterestDepositedTo;
                    existingRecord.InterestStatus = terminateModel.InterestStatus;
                    existingRecord.TerminationRemarks = terminateModel.TerminationRemarks;
                }

                await _unitOfWork.BienesPlacement.RollOverAsync(existingRecord, user!, cancellationToken);

                FilprideAuditTrail auditTrailBook = new(user!, $"Rollover placement# {existingRecord.ControlNumber}", "Placement", nameof(Bienes));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = $"Rollover placement successfully. " +
                                      $"You can now modified the newly created placement with the same control#{existingRecord.ControlNumber}";
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to roll over placement. Error: {ErrorMessage}, Stack: {StackTrace}. Rollover by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Swapping(int? id, int companyId, TerminatePlacementViewModel? terminateModel = null, CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingRecord = await _unitOfWork.BienesPlacement
                    .GetAsync(p => p.PlacementId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return NotFound();
                }

                var user = User.Identity!.Name;

                if (companyId == existingRecord.CompanyId)
                {
                    TempData["error"] = "The selected company matches the previously chosen company. Please select a different company.";
                    return RedirectToAction(nameof(Preview), new { id });
                }

                existingRecord.IsSwapped = true;

                // Check if termination details were provided
                if (terminateModel?.PlacementId > 0)
                {
                    // Apply termination details
                    existingRecord.Status = nameof(PlacementStatus.Terminated);
                    existingRecord.TerminatedDate = terminateModel.TerminatedDate;
                    existingRecord.TerminatedBy = user;
                    existingRecord.InterestDeposited = terminateModel.InterestDeposited;
                    existingRecord.InterestDepositedDate = terminateModel.InterestDepositedDate == default ? null : terminateModel.InterestDepositedDate;
                    existingRecord.InterestDepositedTo = terminateModel.InterestDepositedTo;
                    existingRecord.InterestStatus = terminateModel.InterestStatus;
                    existingRecord.TerminationRemarks = terminateModel.TerminationRemarks;
                }

                var newControlNumber = await _unitOfWork.BienesPlacement.SwappingAsync(existingRecord, companyId, user!, cancellationToken);

                FilprideAuditTrail auditTrailBook = new(user!, $"Swapped placement# {existingRecord.ControlNumber}", "Placement", nameof(Bienes));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = $"Swapped placement successfully. " +
                                      $"You can now modified the newly created placement with control#{newControlNumber}";
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to swap placement. Error: {ErrorMessage}, Stack: {StackTrace}. Swapped by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanies(int bankId, CancellationToken cancellationToken)
        {
            try
            {
                var companies = await _unitOfWork.GetCompanyListAsyncById(cancellationToken);

                if (companies.Count == 0)
                {
                    return NotFound();
                }

                return Ok(companies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
