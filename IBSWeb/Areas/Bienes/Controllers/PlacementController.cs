using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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


    }
}
