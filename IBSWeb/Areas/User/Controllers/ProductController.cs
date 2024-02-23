using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<ProductController> _logger;

        public ProductController(IUnitOfWork unitOfWork, ILogger<ProductController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;

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
                    await _unitOfWork.Product.UpdateAsync(model, cancellationToken);
                    TempData["success"] = "Product updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in updating product.");
                    TempData["error"] = ex.Message;
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Product product = await _unitOfWork
                .Product
                .GetAsync(u => u.ProductId == id, cancellationToken);

            if (product != null)
            {
                return View(product);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeletePost(int? id, CancellationToken cancellationToken)
        {
            Product? product = await _unitOfWork
                .Product
                .GetAsync(u => u.ProductId == id, cancellationToken);

            if (product != null)
            {
                await _unitOfWork.Product.RemoveAsync(product, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Product deleted successfully";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }
    }
}