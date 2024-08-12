using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class CustomerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<CustomerController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        public CustomerController(IUnitOfWork unitOfWork, ILogger<CustomerController> logger, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            IEnumerable<FilprideCustomer> customer = await _unitOfWork.FilprideCustomer
                .GetAllAsync(c => c.Company == companyClaims, cancellationToken);
            return View(customer);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(FilprideCustomer model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                var companyClaims = await GetCompanyClaimAsync();

                //bool IsTinExist = await _unitOfWork.FilprideCustomer.IsTinNoExistAsync(model.CustomerTin, companyClaims, cancellationToken);
                bool IsTinExist = false;
                if (!IsTinExist)
                {
                    model.Company = companyClaims;
                    model.CustomerCode = await _unitOfWork.FilprideCustomer.GenerateCodeAsync(model.CustomerType, companyClaims, cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(User);
                    await _unitOfWork.FilprideCustomer.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);
                    TempData["success"] = "Customer created successfully";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("CustomerTin", "Tin No already exist.");
                return View(model);
            }
            ModelState.AddModelError("", "Make sure to fill all the required details.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            FilprideCustomer customer = await _unitOfWork.FilprideCustomer.GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                return View(customer);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilprideCustomer model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.EditedBy = _userManager.GetUserName(User);
                    await _unitOfWork.FilprideCustomer.UpdateAsync(model, cancellationToken);
                    TempData["success"] = "Customer updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in updating customer.");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Activate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            FilprideCustomer customer = await _unitOfWork
                .FilprideCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                return View(customer);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Activate")]
        public async Task<IActionResult> ActivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            FilprideCustomer customer = await _unitOfWork
                .FilprideCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                customer.IsActive = true;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Customer has been activated";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            FilprideCustomer customer = await _unitOfWork
                .FilprideCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                return View(customer);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Deactivate")]
        public async Task<IActionResult> DeactivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            FilprideCustomer customer = await _unitOfWork
                .FilprideCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                customer.IsActive = false;
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Customer has been activated";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }
    }
}