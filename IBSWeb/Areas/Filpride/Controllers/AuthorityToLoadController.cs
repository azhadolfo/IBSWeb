using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Integrated;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_RCD, SD.Department_TradeAndSupply)]
    public class AuthorityToLoadController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        private readonly ILogger<AuthorityToLoadController> _logger;

        public AuthorityToLoadController(IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILogger<AuthorityToLoadController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
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

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetAuthorityToLoads([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();


                var atlList = await _dbContext.FilprideAuthorityToLoads
                    .Include(a => a.Details).ThenInclude(d => d.CustomerOrderSlip)
                    .Where(a => a.Company == companyClaims)
                    .ToListAsync(cancellationToken);

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
                        s.Details.Any(d =>
                            d.CustomerOrderSlip!.CustomerOrderSlipNo.ToLower().Contains(searchValue)) ||
                        s.Remarks.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Name;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    atlList = atlList
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = atlList.Count;

                var pagedData = atlList
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .Select(atl => new
                    {
                        atl.AuthorityToLoadId,
                        atl.AuthorityToLoadNo,
                        atl.DateBooked,
                        atl.ValidUntil,
                        atl.CustomerOrderSlip,
                        CosNos = atl.Details
                            .Select(a => a.CustomerOrderSlip!.CustomerOrderSlipNo)
                            .ToList(),
                        atl.Remarks
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
                TempData["warning"] = "The submitted information is invalid.";
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
                    CreatedBy = GetUserFullName(),
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
                        AppointedId = appointedSupplier.SequenceId,
                    });

                    if (appointedSuppliers.All(x => x.UnreservedQuantity == 0))
                    {
                        appointedSupplier.CustomerOrderSlip!.IsCosAtlFinalized = true;
                    }

                    appointedSupplier.CustomerOrderSlip!.Status = nameof(CosStatus.ForApprovalOfOM);
                }

                await _dbContext.FilprideBookAtlDetails.AddRangeAsync(bookDetails, cancellationToken);

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new atl# {model.AuthorityToLoadNo}", "Authority To Load", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                TempData["success"] = $"ATL# {model.AuthorityToLoadNo} booked successfully.";
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
                var companyClaims = await GetCompanyClaimAsync();

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Preview authority to load# {existingRecord.AuthorityToLoadNo}", "Authority to Load", companyClaims!);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

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

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var atl = await _unitOfWork.FilprideAuthorityToLoad
                .GetAsync(x => x.AuthorityToLoadId == id, cancellationToken);

            if (atl == null)
            {
                return NotFound();
            }

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrail = new(User.Identity!.Name!, $"Printed copy of authority to load# {atl.AuthorityToLoadNo}", "Authority to Load", atl.Company);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrail, cancellationToken);

            #endregion --Audit Trail Recording

            return RedirectToAction(nameof(Print), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierCOS(int supplierId, int loadPortId)
        {
            var cosList = await _dbContext.FilprideCOSAppointedSuppliers
                .Include(a => a.CustomerOrderSlip)
                .Include(a => a.PurchaseOrder)
                .Where(a => a.SupplierId == supplierId
                            && !a.CustomerOrderSlip!.IsCosAtlFinalized
                            && a.CustomerOrderSlip.Status != nameof(CosStatus.Closed)
                            && a.CustomerOrderSlip.Status != nameof(CosStatus.Expired)
                            && a.CustomerOrderSlip.Status != nameof(CosStatus.Disapproved)
                            && a.CustomerOrderSlip.PickUpPointId == loadPortId
                            && a.UnreservedQuantity > 0)
                .Select(g => new
                {
                    appointedId = g.SequenceId,
                    cosId = g.CustomerOrderSlipId,
                    cosNo = g.CustomerOrderSlip!.CustomerOrderSlipNo,
                    volume = g.UnreservedQuantity,
                    poNo = g.PurchaseOrder!.PurchaseOrderNo,
                })
                .ToListAsync();

            return Json(cosList);
        }

        public async Task<IActionResult> GetSupplierCOSForEdit(int supplierId, int loadPortId, int atlId)
        {
            // Get the COS IDs and appointed IDs that are currently in this ATL with their quantities
            var existingBookDetails = await _dbContext.FilprideBookAtlDetails
                .Where(b => b.AuthorityToLoadId == atlId)
                .Select(b => new
                {
                    b.CustomerOrderSlipId,
                    b.AppointedId,
                    b.Quantity
                })
                .ToListAsync();

            var cosList = await _dbContext.FilprideCOSAppointedSuppliers
                .Include(a => a.CustomerOrderSlip)
                .Include(a => a.PurchaseOrder)
                .Where(a => a.SupplierId == supplierId
                            && a.CustomerOrderSlip!.Status != nameof(CosStatus.Closed)
                            && a.CustomerOrderSlip.Status != nameof(CosStatus.Expired)
                            && a.CustomerOrderSlip.Status != nameof(CosStatus.Disapproved)
                            && a.CustomerOrderSlip.PickUpPointId == loadPortId)
                .Select(g => new
                {
                    appointedId = g.SequenceId,
                    cosId = g.CustomerOrderSlipId,
                    cosNo = g.CustomerOrderSlip!.CustomerOrderSlipNo,
                    volume = g.UnreservedQuantity,
                    poNo = g.PurchaseOrder!.PurchaseOrderNo,
                })
                .ToListAsync();

            // Add back the quantities that are currently reserved in this ATL
            var result = cosList.Select(cos =>
            {
                var existingDetail = existingBookDetails
                    .FirstOrDefault(b => b.CustomerOrderSlipId == cos.cosId
                                      && b.AppointedId == cos.appointedId);

                return new
                {
                    cos.appointedId,
                    cos.cosId,
                    cos.cosNo,
                    volume = existingDetail != null ? cos.volume + existingDetail.Quantity : cos.volume,
                    cos.poNo
                };
            })
            .Where(x => x.volume > 0)
            .ToList();

            return Json(result);
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

            var atl = await _dbContext.FilprideAuthorityToLoads
                .Include(a => a.Details)
                .ThenInclude(b => b.CustomerOrderSlip)
                .ThenInclude(c => c!.AppointedSuppliers)
                .FirstOrDefaultAsync(a => a.AuthorityToLoadId == id, cancellationToken);

            if (atl == null)
            {
                return NotFound();
            }

            var hasBeenUsed = atl.Details.Any(b => b.UnservedQuantity < b.Quantity);

            if (hasBeenUsed)
            {
                TempData["error"] = $"Cannot edit ATL# {atl.AuthorityToLoadNo}. This ATL has already been partially or fully served and cannot be modified.";
                return RedirectToAction(nameof(Index));
            }

            var selectedCosDetails = atl.Details.Select(b => new CosAppointedDetails
            {
                CosId = b.CustomerOrderSlipId,
                AppointedId = b.AppointedId!.Value,
                Volume = b.Quantity
            }).ToList();

            BookATLViewModel viewModel = new()
            {
                AtlId = atl.AuthorityToLoadId,
                Date = atl.DateBooked,
                LoadPortId = atl.LoadPortId,
                SupplierId = atl.SupplierId,
                UPPIAtlNo = atl.UppiAtlNo,
                SelectedCosDetails = selectedCosDetails,
                SupplierList = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken),
                LoadPorts = await _unitOfWork.GetDistinctFilpridePickupPointListById(companyClaims, cancellationToken),
                CurrentUser = _userManager.GetUserName(User)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BookATLViewModel viewModel, CancellationToken cancellationToken)
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
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var atl = await _dbContext.FilprideAuthorityToLoads
                    .Include(a => a.Details)
                    .ThenInclude(b => b.CustomerOrderSlip)
                    .ThenInclude(c => c!.AppointedSuppliers)
                    .FirstOrDefaultAsync(a => a.AuthorityToLoadId == viewModel.AtlId, cancellationToken);

                if (atl == null)
                {
                    return NotFound();
                }

                var hasBeenUsed = atl.Details.Any(b => b.UnservedQuantity < b.Quantity);

                if (hasBeenUsed)
                {
                    TempData["error"] = $"Cannot edit ATL# {atl.AuthorityToLoadNo}. This ATL has already been partially or fully served and cannot be modified.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var oldDetail in atl.Details)
                {
                    var appointedSupplier = await _dbContext.FilprideCOSAppointedSuppliers
                        .Include(c => c.CustomerOrderSlip)
                        .FirstOrDefaultAsync(x => x.CustomerOrderSlipId == oldDetail.CustomerOrderSlipId
                            && x.SequenceId == oldDetail.AppointedId, cancellationToken);

                    if (appointedSupplier != null)
                    {
                        appointedSupplier.UnreservedQuantity += oldDetail.Quantity;
                        appointedSupplier.CustomerOrderSlip!.IsCosAtlFinalized = false;
                    }
                }

                _dbContext.FilprideBookAtlDetails.RemoveRange(atl.Details);

                var firstCosId = viewModel.SelectedCosDetails.FirstOrDefault()!.CosId;
                var cosRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(x => x.CustomerOrderSlipId == firstCosId, cancellationToken);

                if (cosRecord == null)
                {
                    return BadRequest();
                }

                atl.CustomerOrderSlipId = cosRecord.CustomerOrderSlipId;
                atl.LoadPortId = cosRecord.PickUpPointId ?? 0;
                atl.Depot = cosRecord.PickUpPoint!.Depot;
                atl.Freight = cosRecord.Freight ?? 0m;
                atl.DateBooked = viewModel.Date;
                atl.ValidUntil = viewModel.Date.AddDays(4);
                atl.UppiAtlNo = viewModel.UPPIAtlNo;
                atl.SupplierId = viewModel.SupplierId;
                viewModel.CurrentUser = GetUserFullName();

                var bookDetails = new List<FilprideBookAtlDetail>();

                foreach (var details in viewModel.SelectedCosDetails)
                {
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

                    bookDetails.Add(new FilprideBookAtlDetail
                    {
                        AuthorityToLoadId = atl.AuthorityToLoadId,
                        CustomerOrderSlipId = appointedSupplier.CustomerOrderSlipId,
                        Quantity = details.Volume,
                        UnservedQuantity = details.Volume,
                        AppointedId = appointedSupplier.SequenceId,
                    });

                    if (appointedSuppliers.All(x => x.UnreservedQuantity == 0))
                    {
                        appointedSupplier.CustomerOrderSlip!.IsCosAtlFinalized = true;
                    }

                    appointedSupplier.CustomerOrderSlip!.Status = nameof(CosStatus.ForApprovalOfOM);
                }

                await _dbContext.FilprideBookAtlDetails.AddRangeAsync(bookDetails, cancellationToken);

                FilprideAuditTrail auditTrailBook = new(viewModel.CurrentUser, $"Edited atl# {atl.AuthorityToLoadNo}", "Authority To Load", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                TempData["success"] = $"ATL# {atl.AuthorityToLoadNo} updated successfully.";
                await transaction.CommitAsync(cancellationToken);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                viewModel.SupplierList = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to update ATL. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
        }
    }
}
