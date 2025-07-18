using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MasterFile;
using IBS.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OfficeOpenXml;
using System.Threading;
using IBS.Models;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<ProductController> _logger;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        public ProductController(IUnitOfWork unitOfWork, ILogger<ProductController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(string? view)
        {
            IEnumerable<Product> products = await _unitOfWork
                .Product
                .GetAllAsync();

            if (view == nameof(DynamicView.Product))
            {
                IEnumerable<Product> exportProducts = await _unitOfWork
                .Product
                .GetAllAsync();

                return View("ExportIndex", exportProducts);
            }

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

            var product = await _unitOfWork.Product.GetAsync(c => c.ProductId == id, cancellationToken);

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

            var product = await _unitOfWork
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

            var product = await _unitOfWork
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

            var product = await _unitOfWork
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

            var product = await _unitOfWork
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

        [HttpGet]
        public IActionResult GetAllProductIds()
        {
            var productIds = _dbContext.Products
                                     .Select(p => p.ProductId) // Assuming Id is the primary key
                                     .ToList();

            return Json(productIds);
        }

        //Download as .xlsx file.(Export)
        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            var selectedList = await _unitOfWork.Product
                .GetAllAsync(p => recordIds.Contains(p.ProductId));

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("Product");

            worksheet.Cells["A1"].Value = "ProductCode";
            worksheet.Cells["B1"].Value = "ProductName";
            worksheet.Cells["C1"].Value = "ProductUnit";
            worksheet.Cells["D1"].Value = "CreatedBy";
            worksheet.Cells["E1"].Value = "CreatedDate";
            worksheet.Cells["F1"].Value = "OriginalProductId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.ProductCode;
                worksheet.Cells[row, 2].Value = item.ProductName;
                worksheet.Cells[row, 3].Value = item.ProductUnit;
                worksheet.Cells[row, 4].Value = item.CreatedBy;
                worksheet.Cells[row, 5].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 6].Value = item.ProductId;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ProductList_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- export xlsx record --
    }
}
