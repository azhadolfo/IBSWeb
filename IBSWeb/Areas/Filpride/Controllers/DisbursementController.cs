using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
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

                var disbursements = _unitOfWork.FilprideCheckVoucher
                    .GetAllQuery(x=> x.CvType != nameof(CVType.Invoicing) &&
                                     x.PostedBy != null &&
                                     true) ;

                var totalRecords = await disbursements.CountAsync(cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    var hasDate = DateOnly.TryParse(searchValue, out var date);
                    var hasDcpDate = DateOnly.TryParse(searchValue, out var dcpDate);
                    var hasDcrDate = DateOnly.TryParse(searchValue, out var dcrDate);

                    disbursements = disbursements
                    .Where(s =>
                        s.CheckVoucherHeaderNo!.ToLower().Contains(searchValue) == true ||
                        s.Payee!.ToLower().Contains(searchValue) == true ||
                        s.Total.ToString().Contains(searchValue) ||
                        s.CheckVoucherHeaderId.ToString().Contains(searchValue) ||
                        s.Reference!.ToLower().Contains(searchValue) == true ||
                        s.CheckNo!.ToLower().Contains(searchValue) == true ||
                        (hasDate && s.Date == date) ||
                        (hasDcpDate && s.DcpDate == dcpDate) == true ||
                        (hasDcrDate && s.DcrDate == dcrDate) == true
                        );
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
                                    ? disbursements.Where(s => s.DcpDate != null)
                                    : disbursements.Where(s => s.DcpDate == null);
                                break;

                            case "dcrDate":
                                disbursements = searchValue == "not-null"
                                    ? disbursements.Where(s => s.DcrDate != null)
                                    : disbursements.Where(s => s.DcrDate == null);
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
                        .OrderBy($"{columnName} {sortDirection}") ;
                }

                var totalFilteredRecords = await disbursements.CountAsync(cancellationToken);

                var pagedData = await disbursements
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
                _logger.LogError(ex, "Failed to get disbursements.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Policy = nameof(Disbursement.DisbursementUpdateDcpDate))]
        [HttpPost]
        public async Task<IActionResult> UpdateDCPDate(int cvId, DateOnly dcpDate, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cv => cv.CheckVoucherHeaderId == cvId, cancellationToken);

            if (cv == null)
            {
                return Json(new { success = false, message = "Record not found" });
            }

            ///TODO: Uncomment this code later when we need to validate the DCP date based on the posted periods. For now, we will allow any DCP date since the period validation is not yet implemented for DCP date.
            //var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

            //if (dcpDate < DateOnly.FromDateTime(minDate))
            //{
            //    return Json(new { success = false, message = $"DCP date cannot be earlier than {minDate:MMM dd, yyyy} based on the posted periods." });
            //}

            //var isPeriodClosed = await _unitOfWork.IsPeriodPostedAsync(Module.CheckVoucher, cv.Date, cancellationToken);

            //if (isPeriodClosed && cv.DcpDate != null)
            //{
            //    return Json(new { success = false, message = $"Cannot update DCP date this record because the period {cv.Date:MMM yyyy} is already closed." });
            //}

            cv.DcpDate = dcpDate;
            cv.DcrDate = null;

            var connectedInvoices = await GetConnectedInvoicesAsync(cv.CheckVoucherHeaderId, string.Empty, cancellationToken);
            foreach (var invoice in connectedInvoices)
            {
                invoice.DcpDate = dcpDate;
                invoice.DcrDate = null;
            }

            var auditMessage = connectedInvoices.Count == 0
                ? $"Update DCP date of CV# {cv.CheckVoucherHeaderNo}"
                : $"Update DCP date of CV# {cv.CheckVoucherHeaderNo} and {connectedInvoices.Count} connected invoice(s)";

            FilprideAuditTrail auditTrailBook = new(GetUserFullName(), auditMessage, "Disbursement");
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            await _unitOfWork.SaveAsync(cancellationToken);

            return Json(new { success = true });
        }

        [Authorize(Policy = nameof(Disbursement.DisbursementUpdateDcrDate))]
        [HttpPost]
        public async Task<IActionResult> UpdateDCRDate(int cvId, DateOnly dcrDate, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cv => cv.CheckVoucherHeaderId == cvId, cancellationToken);

            if (cv == null)
            {
                return Json(new { success = false, message = "Record not found" });
            }

            if (cv.DcpDate == null)
            {
                return Json(new { success = false, message = "Cannot update DCR date because DCP date is not set." });
            }

            if (dcrDate < cv.DcpDate)
            {
                return Json(new { success = false, message = "DCR date cannot be earlier than DCP date." });
            }

            ///TODO: Uncomment the code below if we want to validate the DCR date based on the posted periods. But for now, we will just validate the DCR date based on the DCP date.
            //var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

            //if (dcrDate < DateOnly.FromDateTime(minDate))
            //{
            //    return Json(new { success = false, message = $"DCR date cannot be earlier than {minDate:MMM dd, yyyy} based on the posted periods." });
            //}

            //var isPeriodClosed = await _unitOfWork.IsPeriodPostedAsync(Module.CheckVoucher, cv.Date, cancellationToken);

            //if (isPeriodClosed && cv.DcrDate != null)
            //{
            //    return Json(new { success = false, message = $"Cannot update DCR date this record because the period {cv.Date:MMM yyyy} is already closed." });
            //}

            cv.DcrDate = dcrDate;

            var connectedInvoices = await GetConnectedInvoicesAsync(cv.CheckVoucherHeaderId, string.Empty, cancellationToken);
            foreach (var invoice in connectedInvoices)
            {
                invoice.DcrDate = dcrDate;
            }

            var auditMessage = connectedInvoices.Count == 0
                ? $"Update DCR date of CV# {cv.CheckVoucherHeaderNo}"
                : $"Update DCR date of CV# {cv.CheckVoucherHeaderNo} and {connectedInvoices.Count} connected invoice(s)";

            FilprideAuditTrail auditTrailBook = new(GetUserFullName(), auditMessage, "Disbursement");
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            await _unitOfWork.SaveAsync(cancellationToken);

            return Json(new { success = true });
        }

        private async Task<List<FilprideCheckVoucherHeader>> GetConnectedInvoicesAsync(
            int paymentCvId,
            string company,
            CancellationToken cancellationToken)
        {
            return await _dbContext.FilprideCheckVoucherHeaders
                .Where(invoice => 
                                  invoice.PostedBy != null &&
                                  _dbContext.FilprideMultipleCheckVoucherPayments
                                      .Any(payment => payment.CheckVoucherHeaderPaymentId == paymentCvId &&
                                                      payment.CheckVoucherHeaderInvoiceId == invoice.CheckVoucherHeaderId))
                .ToListAsync(cancellationToken);
        }
    }
}
