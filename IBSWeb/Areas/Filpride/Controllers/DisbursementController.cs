using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_Finance, SD.Department_RCD)]
    public class DisbursementController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<DisbursementController> _logger;

        public DisbursementController(ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            ILogger<DisbursementController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
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

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetDisbursements([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var disbursements = await _unitOfWork.FilprideCheckVoucher
                    .GetAllAsync(d =>
                        d.CvType != nameof(CVType.Invoicing) &&
                        d.PostedBy != null &&
                        d.Company == companyClaims, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    disbursements = disbursements
                    .Where(s =>
                        s.CheckVoucherHeaderNo?.ToLower().Contains(searchValue) == true ||
                        s.Payee?.ToLower().Contains(searchValue) == true ||
                        s.Total.ToString().Contains(searchValue) ||
                        s.CheckVoucherHeaderId.ToString().Contains(searchValue) ||
                        s.Reference?.ToLower().Contains(searchValue) == true ||
                        s.Date.ToString().Contains(searchValue) ||
                        s.DcpDate?.ToString().Contains(searchValue) == true ||
                        s.DcrDate?.ToString().Contains(searchValue) == true
                        )
                    .Where(cv => cv.Company == companyClaims)
                    .ToList();
                }

                // Column-specific search
                foreach (var column in parameters.Columns)
                {
                    if (!string.IsNullOrEmpty(column.Search.Value))
                    {
                        var searchValue = column.Search.Value.ToLower();
                        switch (column.Data)
                        {
                            case "dcpDate":
                                disbursements = searchValue == "not-null"
                                    ? disbursements.Where(s => s.DcpDate != null).ToList()
                                    : disbursements.Where(s => s.DcpDate == null).ToList();
                                break;
                            case "dcrDate":
                                disbursements = searchValue == "not-null"
                                    ? disbursements.Where(s => s.DcrDate != null).ToList()
                                    : disbursements.Where(s => s.DcrDate == null).ToList();
                                break;
                        }
                    }
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    disbursements = disbursements
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = disbursements.Count();

                var pagedData = disbursements
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
                _logger.LogError(ex, "Failed to get disbursements.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDCPDate(int cvId, DateOnly dcpDate, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cv => cv.CheckVoucherHeaderId == cvId, cancellationToken);

            if (cv == null)
            {
                return Json(new { success = false, message = "Record not found" });
            }

            var isPeriodClosed = await _unitOfWork.IsPeriodPostedAsync(cv.Date, cancellationToken);

            if (isPeriodClosed)
            {
                return Json(new { success = false, message = "Book already closed." });
            }

            cv.DcpDate = dcpDate;

            FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Update DCP date of CV# {cv.CheckVoucherHeaderNo}", "Disbursement", cv.Company);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            await _unitOfWork.SaveAsync(cancellationToken);

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDCRDate(int cvId, DateOnly dcrDate, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cv => cv.CheckVoucherHeaderId == cvId, cancellationToken);

            if (cv == null)
            {
                return Json(new { success = false, message = "Record not found" });
            }

            var isPeriodClosed = await _unitOfWork.IsPeriodPostedAsync(cv.Date, cancellationToken);

            if (isPeriodClosed)
            {
                return Json(new { success = false, message = "Book already closed." });
            }

            cv.DcrDate = dcrDate;

            FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Update DCR date of CV# {cv.CheckVoucherHeaderNo}", "Disbursement", cv.Company);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            await _unitOfWork.SaveAsync(cancellationToken);

            return Json(new { success = true });
        }
    }
}
