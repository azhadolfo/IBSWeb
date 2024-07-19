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

        public async Task<IActionResult> Index()
        {
            IEnumerable<FilprideCustomer> customer = await _unitOfWork.Customer
                .GetAllAsync();
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
                bool IsTinExist = await _unitOfWork.Customer.IsTinNoExistAsync(model.CustomerTin, cancellationToken);

                if (!IsTinExist)
                {
                    model.CustomerCode = await _unitOfWork.Customer.GenerateCodeAsync(model.CustomerType, cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(User);
                    await _unitOfWork.Customer.AddAsync(model, cancellationToken);
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

            FilprideCustomer customer = await _unitOfWork.Customer.GetAsync(c => c.CustomerId == id, cancellationToken);

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
                    await _unitOfWork.Customer.UpdateAsync(model, cancellationToken);
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
                .Customer
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
                .Customer
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
                .Customer
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
                .Customer
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