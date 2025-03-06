using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using IBS.Services.Attributes;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class CashierReportController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<CashierReportController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        public CashierReportController(IUnitOfWork unitOfWork, ILogger<CashierReportController> logger, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode")?.Value ?? "ALL";

            ViewData["StationCodeClaim"] = stationCodeClaim;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetSalesHeaders([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var claims = await _userManager.GetClaimsAsync(user);
                var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode")?.Value ?? "ALL";

                Expression<Func<MobilitySalesHeader, bool>> filter = s => stationCodeClaim == "ALL" || s.StationCode == stationCodeClaim;

                var salesHeaders = await _unitOfWork.MobilitySalesHeader.GetAllAsync(filter, cancellationToken);

                var salesHeaderWithStationName = _unitOfWork.MobilitySalesHeader.GetSalesHeaderJoin(salesHeaders, cancellationToken).ToList();

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    salesHeaderWithStationName = salesHeaderWithStationName
                        .Where(s =>
                            s.StationCodeWithName.ToLower().Contains(searchValue) ||
                            s.SalesNo.ToLower().Contains(searchValue) ||
                            s.Date.ToString().Contains(searchValue) ||
                            s.Cashier.ToLower().Contains(searchValue) ||
                            s.Shift.ToString().Contains(searchValue) ||
                            s.TimeIn.ToString().Contains(searchValue) ||
                            s.TimeOut.ToString().Contains(searchValue))
                        .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    salesHeaderWithStationName = salesHeaderWithStationName
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = salesHeaderWithStationName.Count();

                var pagedData = salesHeaderWithStationName
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

        public async Task<IActionResult> Preview(string? id, string? stationCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stationCode))
            {
                return NotFound();
            }
            var station = await _unitOfWork.MobilityStation.MapStationToDTO(stationCode, cancellationToken);

            var sales = await _dbContext.MobilitySalesHeaders
                .Include(s => s.SalesDetails)
                .FirstOrDefaultAsync(s => s.SalesNo == id && s.StationCode == stationCode, cancellationToken);

            if (sales == null)
            {
                return BadRequest();
            }

            ViewData["Station"] = $"{station.StationCode} - {station.StationName}";
            return View(sales);
        }

        public async Task<IActionResult> Post(string? id, string? stationCode, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stationCode))
            {
                try
                {
                    var postedBy = _userManager.GetUserName(User);
                    await _unitOfWork.MobilitySalesHeader.PostAsync(id, postedBy, stationCode, cancellationToken);
                    TempData["success"] = "Cashier report approved successfully.";
                    return RedirectToAction(nameof(Preview), new { id, stationCode });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on posting cashier report.");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return RedirectToAction(nameof(Preview), new { id, stationCode });
                }
            }

            return BadRequest();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string? id, string? stationCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stationCode))
            {
                return NotFound();
            }

            var sales = await _unitOfWork.MobilitySalesHeader.GetAsync(s => s.SalesNo == id && s.StationCode == stationCode, cancellationToken);

            if (sales == null)
            {
                return BadRequest();
            }

            return View(sales);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MobilitySalesHeader model, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(model.Particular))
            {
                ModelState.AddModelError("Header.Particular", "Indicate the reason of this changes.");
                return View(model);
            }

            try
            {
                model.EditedBy = _userManager.GetUserName(User);
                await _unitOfWork.MobilitySalesHeader.UpdateAsync(model, cancellationToken);
                TempData["success"] = "Cashier report updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in updating cashier report.");
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AdjustReport(CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

            var model = new AdjustReportViewModel
            {
                OfflineList = await _unitOfWork.MobilityOffline.GetOfflineListAsync(stationCodeClaim, cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AdjustReport(AdjustReportViewModel model, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.MobilityOffline.InsertEntry(model, cancellationToken);

                TempData["success"] = "Adjusted report successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in inserting manual entry.");
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOfflineDetails(int offlineId, CancellationToken cancellationToken = default)
        {
            var offline = await _unitOfWork.MobilityOffline.GetOffline(offlineId, cancellationToken);

            if (offline == null)
            {
                return NotFound();
            }

            var formattedData = new
            {
                StartDate = offline.StartDate.ToString("MMM/dd/yyyy"),
                EndDate = offline.EndDate.ToString("MMM/dd/yyyy"),
                offline.Product,
                offline.Pump,
                FirstDsrOpeningBefore = offline.FirstDsrOpening,
                FirstDsrClosingBefore = offline.FirstDsrClosing,
                SecondDsrOpeningBefore = offline.SecondDsrOpening,
                SecondDsrClosingBefore = offline.SecondDsrClosing,
                Liters = offline.Liters.ToString("N4"),
                Balance = offline.Balance.ToString("N4"),
                offline.FirstDsrNo,
                offline.SecondDsrNo
            };

            return Json(formattedData);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerInvoicing(CancellationToken cancellationToken)
        {
            var model = new CustomerInvoicingViewModel
            {
                DsrList = await _unitOfWork.MobilitySalesHeader.GetUnpostedDsrList(cancellationToken),
                Customers = await _unitOfWork.GetMobilityCustomerListAsyncByCodeName(cancellationToken),
                Lubes = await _unitOfWork.GetProductListAsyncById(cancellationToken),
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CustomerInvoicing(CustomerInvoicingViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    viewModel.User = _userManager.GetUserName(User);
                    await _unitOfWork.MobilitySalesHeader.ProcessCustomerInvoicing(viewModel, cancellationToken);
                    TempData["success"] = "Customer invoicing successfully added";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.DsrList = await _unitOfWork.MobilitySalesHeader.GetUnpostedDsrList(cancellationToken);
                    viewModel.Customers = await _unitOfWork.GetMobilityCustomerListAsyncByCodeName(cancellationToken);
                    viewModel.Lubes = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.DsrList = await _unitOfWork.MobilitySalesHeader.GetUnpostedDsrList(cancellationToken);
            viewModel.Customers = await _unitOfWork.GetMobilityCustomerListAsyncByCodeName(cancellationToken);
            viewModel.Lubes = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }
    }
}
