using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class DepositController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<DepositController> _logger;
        private readonly IUnitOfWork _unitOfWork;


        public DepositController(ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            ILogger<DepositController> logger,
            IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        private async Task<string?> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetDeposits([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var query = await _unitOfWork.MobilityDeposit
                    .GetAllAsync(cancellationToken:cancellationToken);

                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    query = query
                        .Where(s =>
                            s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                            s.ShiftDate.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                            s.ShiftNumber.ToString().ToLower().Contains(searchValue) ||
                            s.PageNumber.ToString().ToLower().Contains(searchValue) ||
                            s.AccountNumber.ToLower().Contains(searchValue) ||
                            s.Amount.ToString().ToLower().Contains(searchValue)
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
                _logger.LogError(ex, "Failed to get deposits. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }
        public async Task<IActionResult> Approved(Guid id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.MobilityDeposit
                .GetAsync(s => s.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.ApprovedBy = User.Identity!.Name;
                model.ApprovedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.ApprovedBy!, $"Approved deposit for shift date: {model.ShiftDate} shift#: {model.ShiftNumber}", "Deposit", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Deposit has been posted.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve deposit. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

        }
    }
}
