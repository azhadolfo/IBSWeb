using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Models.Mobility.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class StationEmployeeController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<StationEmployeeController> _logger;

        public StationEmployeeController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, ILogger<StationEmployeeController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<string?> GetStationCodeClaimAsync()
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
            var getStationEmployeeModel = _dbContext.MobilityStationEmployees
                .Where(emp => emp.IsActive)
                .ToList();
            return View(getStationEmployeeModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MobilityStationEmployee model, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.StationCode = stationCodeClaims;
                await _dbContext.MobilityStationEmployees.AddAsync(model, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Station employee created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create station employee. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var existingStationEmployee = await _dbContext.MobilityStationEmployees.FindAsync(id, cancellationToken);
            return View(existingStationEmployee);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MobilityStationEmployee model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var existingModel = await _dbContext.MobilityStationEmployees.FindAsync(model.EmployeeId, cancellationToken);

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
                TempData["success"] = "Station employee edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit station employee. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(model);
            }
        }
    }
}
