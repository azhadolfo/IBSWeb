using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<ProductController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        public ProductController(IUnitOfWork unitOfWork, ILogger<ProductController> logger, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;

        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Product> products = await _unitOfWork
                .Product
                .GetAllAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                bool IsProductExist = await _unitOfWork
                    .Product
                    .IsProductExist(model.ProductName, cancellationToken);

                if (!IsProductExist)
                {
                    model.CreatedBy = _userManager.GetUserName(User);
                    await _unitOfWork.Product.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);
                    TempData["success"] = "Product created successfully";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("ProductName", "Product name already exist.");
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

            Product product = await _unitOfWork.Product.GetAsync(c => c.ProductId == id, cancellationToken);

            if (product != null)
            {
                return View(product);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.EditedBy = _userManager.GetUserName(User);
                    await _unitOfWork.Product.UpdateAsync(model, cancellationToken);
                    TempData["success"] = "Product updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in updating product.");
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

            Product product = await _unitOfWork
                .Product
                .GetAsync(c => c.ProductId == id, cancellationToken);

            if (product != null)
            {
                return View(product);
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

            Product product = await _unitOfWork
                .Product
                .GetAsync(c => c.ProductId == id, cancellationToken);

            if (product != null)
            {
                product.IsActive = true;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Product activated successfully";
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

            Product product = await _unitOfWork
                .Product
                .GetAsync(c => c.ProductId == id, cancellationToken);

            if (product != null)
            {
                return View(product);
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

            Product product = await _unitOfWork
                .Product
                .GetAsync(c => c.ProductId == id, cancellationToken);

            if (product != null)
            {
                product.IsActive = false;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Product deactivated successfully";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }
    }
}