using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class PrincipalController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;

        public PrincipalController(ApplicationDbContext db, IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager)
        {
            _db = db;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
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

        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var principals = await _db.MMSIPrincipals
                .Include(p => p.Customer)
                .ToListAsync(cancellationToken);

            return View(principals);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            MMSIPrincipal model = new MMSIPrincipal();
            model.CustomerSelectList = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSIPrincipal model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["error"] = "Invalid entry, please try again.";

                    return View(model);
                }

                var customer = await _db.FilprideCustomers
                    .FindAsync(model.CustomerId, cancellationToken) ?? throw new InvalidOperationException();

                model.CustomerId = customer.CustomerId;

                await _db.MMSIPrincipals.AddAsync(model, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Creation Succeed!";

                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            // id of principal
            try
            {
                // model of principal to delete
                var model = await _db.MMSIPrincipals
                    .FindAsync(id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                // delete the model(principal)
                _db.MMSIPrincipals.Remove(model);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Entry deleted successfully";

                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var model = await _db.MMSIPrincipals.FindAsync(id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            model.CustomerSelectList = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIPrincipal model, CancellationToken cancellationToken = default)
        {
            // model of new principal
            try
            {
                // model of old principal
                var currentModel = await _db.MMSIPrincipals.FindAsync(model.PrincipalId, cancellationToken);

                if (currentModel != null)
                {
                    currentModel.Address = model.Address;
                    currentModel.PrincipalName = model.PrincipalName;
                    currentModel.TIN = model.TIN;
                    currentModel.BusinessType = model.BusinessType;
                    currentModel.PrincipalName = model.PrincipalName;
                    currentModel.Terms = model.Terms;
                    currentModel.Mobile1 = model.Mobile1;
                    currentModel.Mobile2 = model.Mobile2;
                    currentModel.Landline1 = model.Landline1;
                    currentModel.Landline2 = model.Landline2;
                    currentModel.IsVatable = model.IsVatable;
                    currentModel.IsActive = model.IsActive;
                    currentModel.CustomerId = model.CustomerId;
                }
                else
                {
                    TempData["error"] = "Principal not found.";
                    return View(model);
                }

                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }

            catch (Exception)
            {
                TempData["error"] = "An error occurred while editing the entry. Please try again.";
                return View(model);
            }
        }
    }
}
