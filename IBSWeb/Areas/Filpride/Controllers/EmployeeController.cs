using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, ILogger<EmployeeController> logger)
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

        public IActionResult Index()
        {
            var getEmployeeModel = _dbContext.FilprideEmployees
                .Where(x => x.IsActive)
                .ToList();
            return View(getEmployeeModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(FilprideEmployee model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.Company = companyClaims;
                await _dbContext.FilprideEmployees.AddAsync(model, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Employee created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create employee. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var existingEmployee = await _dbContext.FilprideEmployees.FindAsync(id, cancellationToken);
            return View(existingEmployee);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilprideEmployee model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var existingModel = await _dbContext.FilprideEmployees.FindAsync(model.EmployeeId, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            try
            {
                #region -- Saving Default

                existingModel.EmployeeNumber = model.EmployeeNumber;
                existingModel.Initial = model.Initial;
                existingModel.FirstName = model.FirstName;
                existingModel.MiddleName = model.MiddleName;
                existingModel.LastName = model.LastName;
                existingModel.Suffix = model.Suffix;
                existingModel.BirthDate = model.BirthDate;
                existingModel.TelNo = model.TelNo;
                existingModel.SssNo = model.SssNo;
                existingModel.TinNo = model.TinNo;
                existingModel.PhilhealthNo = model.PhilhealthNo;
                existingModel.PagibigNo = model.PagibigNo;
                existingModel.Department = model.Department;
                existingModel.DateHired = model.DateHired;
                existingModel.DateResigned = model.DateResigned;
                existingModel.Position = model.Position;
                existingModel.IsManagerial = model.IsManagerial;
                existingModel.Supervisor = model.Supervisor;
                existingModel.Salary = model.Salary;
                existingModel.IsActive = model.IsActive;
                existingModel.Status = model.Status;
                existingModel.Address = model.Address;

                #endregion -- Saving Default

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Employee edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit employee. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(model);
            }
        }
    }
}
