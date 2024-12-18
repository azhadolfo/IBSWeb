using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Integrated;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using IBS.Services.Attributes;
using IBS.Utility.Constants;

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

        public AuthorityToLoadController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
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
        public async Task<IActionResult> GetAuthorityToLoads([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var atlList = await _unitOfWork.FilprideAuthorityToLoad
                    .GetAllAsync(null, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    atlList = atlList
                    .Where(s =>
                        s.AuthorityToLoadNo.ToLower().Contains(searchValue) ||
                        s.DateBooked.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                        s.ValidUntil.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                        s.UppiAtlNo?.ToLower().Contains(searchValue) == true ||
                        s.Remarks.ToLower().Contains(searchValue) ||
                        s.DeliveryReceipt?.DeliveryReceiptNo?.ToLower().Contains(searchValue) == true
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
                return RedirectToAction(nameof(Index));
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

                ViewData["PurchaseOrders"] = new List<FilprideCOSAppointedSupplier>();

                if (existingRecord.CustomerOrderSlip.HasMultiplePO)
                {
                    ViewData["PurchaseOrders"] = await _dbContext.FilprideCOSAppointedSuppliers
                        .Include(a => a.PurchaseOrder)
                        .Where(a => a.CustomerOrderSlipId == existingRecord.CustomerOrderSlipId)
                        .ToListAsync(cancellationToken);
                }

                return View(existingRecord);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
