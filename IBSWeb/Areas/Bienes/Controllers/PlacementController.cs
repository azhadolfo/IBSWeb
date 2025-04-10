using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Bienes;
using IBS.Models.Bienes.ViewModels;
using IBS.Models.Filpride.Books;
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

        private async Task<List<SelectListItem>> GetCompanies(CancellationToken cancellationToken)
        {
            return await _dbContext.Companies
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.CompanyId.ToString(),
                    Text = c.CompanyName
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<List<SelectListItem>> GetBanks(CancellationToken cancellationToken)
        {
            return await _dbContext.BienesBankAccounts
                .Select(c => new SelectListItem
                {
                    Value = c.BankAccountId.ToString(),
                    Text = c.Bank
                })
                .ToListAsync(cancellationToken);
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
                            s.Company.CompanyName.ToLower().Contains(searchValue) == true ||
                            s.BankAccount.Bank.ToLower().Contains(searchValue) == true ||
                            s.TDAccountNumber.ToLower().Contains(searchValue) ||
                            s.PrincipalAmount.ToString().Contains(searchValue)
                        )
                        .ToList();
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
            PlacementViewModel viewModel = new()
            {
                Companies = await GetCompanies(cancellationToken),
                BankAccounts = await GetBanks(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PlacementViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                viewModel.Companies = await GetCompanies(cancellationToken);
                viewModel.BankAccounts = await GetBanks(cancellationToken);
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
                    SettlementAccountNumber = viewModel.SettlementAccountNumber,
                    DateFrom = viewModel.FromDate,
                    DateTo = viewModel.ToDate,
                    Remarks = viewModel.Remarks,
                    ChequeNumber = viewModel.ChequeNumber,
                    CVNo = viewModel.CVNo,
                    PrincipalAmount = viewModel.PrincipalAmount,
                    PrincipalDisposition = viewModel.PrincipalDisposition,
                    PlacementType = viewModel.PlacementType,
                    InterestRate = viewModel.InterestRate / 100,
                    HasEWT = viewModel.HasEwt,
                    EWTRate = viewModel.EWTRate / 100,
                    HasTrustFee = viewModel.HasTrustFee,
                    TrustFeeRate = viewModel.TrustFeeRate / 100,
                    CreatedBy = User.Identity.Name,
                    LockedDate = viewModel.ToDate.AddDays(2).ToDateTime(TimeOnly.MinValue),
                };

                if (model.PlacementType == PlacementType.LongTerm)
                {
                    model.NumberOfYears = viewModel.NumberOfYears;
                    model.FrequencyOfPayment = viewModel.FrequencyOfPayment;
                }

                await _unitOfWork.BienesPlacement.AddAsync(model, cancellationToken);

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new placement# {model.ControlNumber}", "Placement", ipAddress, nameof(Bienes));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = "Placement created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create placement. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                await transaction.RollbackAsync(cancellationToken);
                viewModel.Companies = await GetCompanies(cancellationToken);
                viewModel.BankAccounts = await GetBanks(cancellationToken);
                return View(viewModel);
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetBankBranchById(int bankId)
        {
            var bank = await _unitOfWork.BienesBankAccount
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
    }
}
