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

        public ProductController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
        public async Task<IActionResult> Create(Product model)
        {
            if (ModelState.IsValid)
            {
                bool IsProductExist = await _unitOfWork
                    .Product
                    .IsProductExist(model.ProductName);

                if (!IsProductExist)
                {
                    await _unitOfWork.Product.AddAsync(model);
                    await _unitOfWork.SaveAsync();
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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Product product = await _unitOfWork.Product.GetAsync(c => c.ProductId == id);

            if (product != null)
            {
                return View(product);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product model)
        {
            if (ModelState.IsValid)
            {
                await _unitOfWork.Product.UpdateAsync(model);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Product updated successfully";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Product product = await _unitOfWork
                .Product
                .GetAsync(u => u.ProductId == id);

            if (product != null)
            {
                return View(product);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeletePost(int? id)
        {
            Product? product = await _unitOfWork
                .Product
                .GetAsync(u => u.ProductId == id);

            if (product != null)
            {
                await _unitOfWork.Product.RemoveAsync(product);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Product deleted successfully";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }
    }
}