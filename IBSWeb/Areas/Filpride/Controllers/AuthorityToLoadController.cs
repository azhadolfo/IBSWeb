using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Integrated;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_RCD, SD.Department_TradeAndSupply)]
    public class AuthorityToLoadController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        private readonly ILogger<AuthorityToLoadController> _logger;

        public AuthorityToLoadController(IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext dbContext,
            ILogger<AuthorityToLoadController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
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
        public async Task<IActionResult> GetAuthorityToLoads([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();


                var atlList = await _unitOfWork.FilprideAuthorityToLoad
                    .GetAllAsync(a => a.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    atlList = atlList
                    .Where(s =>
                        s.AuthorityToLoadNo.ToLower().Contains(searchValue) ||
                        s.DateBooked.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.ValidUntil.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.UppiAtlNo?.ToLower().Contains(searchValue) == true ||
                        s.CustomerOrderSlip?.CustomerOrderSlipNo.ToLower().Contains(searchValue) == true ||
                        s.Remarks.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    atlList = atlList
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = atlList.Count();

                var pagedData = atlList
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
                _logger.LogError(ex, "Failed to get authority to loads. Error: {ErrorMessage}, Stack: {StackTrace}.",
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

            BookATLViewModel viewModel = new()
            {
                SupplierList = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken),
                LoadPorts = await _unitOfWork.GetDistinctFilpridePickupPointListById(companyClaims, cancellationToken),
                Date = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()),
                CurrentUser = _userManager.GetUserName(User)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookATLViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.SupplierList = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
                viewModel.LoadPorts = await _unitOfWork.GetDistinctFilpridePickupPointListById(companyClaims, cancellationToken);
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var firstCosId = viewModel.SelectedCosDetails.FirstOrDefault()!.CosId;

                var cosRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(x => x.CustomerOrderSlipId == firstCosId, cancellationToken);

                if (cosRecord == null)
                {
                    return BadRequest();
                }

                FilprideAuthorityToLoad model = new()
                {
                    AuthorityToLoadNo = await _unitOfWork.FilprideAuthorityToLoad.GenerateAtlNo(companyClaims, cancellationToken),
                    CustomerOrderSlipId = cosRecord.CustomerOrderSlipId,
                    LoadPortId = cosRecord.PickUpPointId ?? 0,
                    Depot = cosRecord.PickUpPoint!.Depot,
                    Freight = cosRecord.Freight ?? 0m,
                    DateBooked = viewModel.Date,
                    ValidUntil = viewModel.Date.AddDays(4),
                    UppiAtlNo = viewModel.UPPIAtlNo,
                    Remarks = "Please secure delivery documents. FILPRIDE DR / SUPPLIER DR / WITHDRAWAL CERTIFICATE",
                    CreatedBy = _userManager.GetUserName(User)!,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    SupplierId = viewModel.SupplierId,
                    Company = companyClaims
                };

                await _unitOfWork.FilprideAuthorityToLoad.AddAsync(model, cancellationToken);

                var bookDetails = new List<FilprideBookAtlDetail>();

                foreach (var details in viewModel.SelectedCosDetails)
                {
                    // Get all appointed suppliers for this COS in a single query
                    var appointedSuppliers = await _dbContext.FilprideCOSAppointedSuppliers
                        .Include(c => c.CustomerOrderSlip)
                        .Where(c => c.CustomerOrderSlipId == details.CosId)
                        .ToListAsync(cancellationToken);

                    var appointedSupplier = appointedSuppliers.FirstOrDefault(x => x.SequenceId == details.AppointedId);

                    if (appointedSupplier == null)
                    {
                        continue;
                    }

                    appointedSupplier.UnreservedQuantity -= details.Volume;

                    // Add new book details
                    bookDetails.Add(new FilprideBookAtlDetail
                    {
                        AuthorityToLoadId = model.AuthorityToLoadId,
                        CustomerOrderSlipId = appointedSupplier.CustomerOrderSlipId,
                        Quantity = details.Volume,
                        UnservedQuantity = details.Volume,
                    });

                    if (appointedSuppliers.All(x => x.UnreservedQuantity == 0))
                    {
                        appointedSupplier.CustomerOrderSlip!.Status = nameof(CosStatus.ForApprovalOfOM);
                    }
                }

                await _dbContext.FilprideBookAtlDetails.AddRangeAsync(bookDetails, cancellationToken);

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new atl# {model.AuthorityToLoadNo}", "Authority To Load", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                TempData["success"] = $"ATL# {model.AuthorityToLoadNo} booked successfully.";
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                viewModel.SupplierList = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to book ATL. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }

        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideAuthorityToLoad
                .GetAsync(atl => atl.AuthorityToLoadId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                return View(existingRecord);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to print ATL. Error: {ErrorMessage}, Stack: {StackTrace}. Printed by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierCOS(int supplierId, int loadPortId)
        {
            var cosList = await _dbContext.FilprideCOSAppointedSuppliers
                .Include(a => a.CustomerOrderSlip)
                .Where(a => a.SupplierId == supplierId
                            && a.CustomerOrderSlip!.Status == nameof(CosStatus.ForAtlBooking)
                            && a.CustomerOrderSlip!.PickUpPointId == loadPortId
                            && a.UnreservedQuantity > 0)
                .Select(g => new
                {
                    appointedId = g.SequenceId,
                    cosId = g.CustomerOrderSlipId,
                    cosNo = g.CustomerOrderSlip!.CustomerOrderSlipNo,
                    volume = g.UnreservedQuantity
                })
                .ToListAsync();

            return Json(cosList);
        }



        [HttpGet]
        public async Task<IActionResult> GetHaulerDetails(int cosId)
        {
            var companyClaims = await GetCompanyClaimAsync();
            // Query your database to get hauler details for the COS
            var existingCos = await _unitOfWork.FilprideCustomerOrderSlip
                .GetAsync(c => c.CustomerOrderSlipId == cosId && c.Company == companyClaims);

            if (existingCos == null)
            {
                return NotFound(null);
            }

            var haulerDetails = new
            {
                Hauler = existingCos.Hauler?.SupplierName,
                existingCos.Driver,
                existingCos.PlateNo,
                existingCos.Freight,
                LoadPort = existingCos.PickUpPoint?.Depot
            };

            return Json(haulerDetails);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateValidityDate(int id, DateOnly newValidUntil, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingAtl = await _unitOfWork.FilprideAuthorityToLoad
                    .GetAsync(atl => atl.AuthorityToLoadId == id, cancellationToken);

                if (existingAtl == null)
                {
                    return NotFound();
                }

                existingAtl.ValidUntil = newValidUntil;

                FilprideAuditTrail auditTrailBook = new(existingAtl.CreatedBy, $"Update validity date of atl# {existingAtl.AuthorityToLoadNo}", "Authority To Load", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Json(new { success = true });

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update the validity date of ATL. Error: {ErrorMessage}, Stack: {StackTrace}. Updated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return Json(new { success = false });
            }
        }
    }
}
