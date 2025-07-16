using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
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

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        public CustomerBranchController(IUnitOfWork unitOfWork, ILogger<CustomerController> logger, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            IEnumerable<FilprideCustomerBranch> customer = await _dbContext.FilprideCustomerBranches
                .Include(b => b.Customer)
                .ToListAsync(cancellationToken);

            return View(customer);
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

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    await _dbContext.FilprideCustomerBranches.AddAsync(model, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
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

            ModelState.AddModelError("", "Make sure to fill all the required details.");
            model.CustomerSelectList = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var customer = await _dbContext.FilprideCustomerBranches.FindAsync(id, cancellationToken);

            if (customer == null)
            {
                return NotFound();
            }

            customer.CustomerSelectList = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FilprideCustomerBranch model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    await UpdateAsync(model, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Customer branch updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Failed to edit customer branch. Created by: {UserName}", _userManager.GetUserName(User));
                    TempData["error"] = $"Error: '{ex.Message}'";
                    model.CustomerSelectList = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken)
                    return View(model);
                }
            }

            ModelState.AddModelError("", "Make sure to fill all the required details.");
            model.CustomerSelectList =
                await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
            return View(model);
        }

        public async Task UpdateAsync(FilprideCustomerBranch model, CancellationToken cancellationToken)
        {
            var currentModel = await _dbContext.FilprideCustomerBranches.FindAsync(model.Id, cancellationToken);

            if (currentModel == null)
            {
                throw new NullReferenceException("Customer branch not found");
            }

            currentModel.CustomerId = model.CustomerId;
            currentModel.BranchName = model.BranchName;
            currentModel.BranchAddress = model.BranchAddress;
            currentModel.BranchTin = model.BranchTin;

            await _dbContext.SaveChangesAsync(cancellationToken);
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
    }
}
