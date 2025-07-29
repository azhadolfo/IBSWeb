using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.MasterFile;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class CustomerBranchController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<CustomerController> _logger;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        public CustomerBranchController(IUnitOfWork unitOfWork, ILogger<CustomerController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public IActionResult Index(string? view, CancellationToken cancellationToken)
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var model = new FilprideCustomerBranch
            {
                CustomerSelectList = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FilprideCustomerBranch model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Make sure to fill all the required details.");
                model.CustomerSelectList =
                    await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _unitOfWork.FilprideCustomerBranch.AddAsync(model, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer branch created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create customer branch master file. Created by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                model.CustomerSelectList = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var branch = await _unitOfWork.FilprideCustomerBranch.GetAsync(b => b.Id == id, cancellationToken);

            if (branch == null)
            {
                return NotFound();
            }

            branch.CustomerSelectList = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
            return View(branch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FilprideCustomerBranch model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Make sure to fill all the required details.");
                model.CustomerSelectList =
                    await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _unitOfWork.FilprideCustomerBranch.UpdateAsync(model, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer branch updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to edit customer branch. Created by: {UserName}", _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                model.CustomerSelectList =
                    await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
                return View(model);
            }
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

        [HttpPost]
        public async Task<IActionResult> GetCustomerBranchesList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var queried = await _unitOfWork.FilprideCustomerBranch
                    .GetAllAsync(null, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                    .Where(b =>
                        b.BranchName.ToLower().Contains(searchValue) == true ||
                        b.BranchAddress.ToLower().Contains(searchValue) == true ||
                        b.BranchTin.ToLower().Contains(searchValue) == true ||
                        b.Customer!.CustomerName.ToLower().Contains(searchValue) == true
                        ).ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Name;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    queried = queried
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalRecords = queried.Count();
                var pagedData = queried
                    .Select(b  => new
                    {
                        b.Id,
                        b.Customer!.CustomerName,
                        b.BranchName,
                        b.BranchAddress,
                        b.BranchTin,
                    })
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
                _logger.LogError(ex, "Failed to get customer branches.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetCustomerDetails(int customerId, CancellationToken cancellationToken)
        {
            try
            {
                var customer = await _unitOfWork.FilprideCustomer
                    .GetAsync(c => c.CustomerId == customerId, cancellationToken);

                if (customer == null)
                {
                    TempData["error"] = "Customer not found";
                }

                return Json(new
                {
                    address = customer!.CustomerAddress,
                    tin = customer.CustomerTin,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get dispatch tickets.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
