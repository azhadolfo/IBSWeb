using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.MasterFile;
using IBS.Models.Mobility;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
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
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            IEnumerable<MobilityProduct> products = await _unitOfWork
                .MobilityProduct
                .GetAllAsync();

            return View(products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MobilityProduct model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Make sure to fill all the required details.");
                return View(model);
            }

            bool IsProductExist = await _unitOfWork
                .MobilityProduct
                .IsProductExist(model.ProductName, cancellationToken);

            if (IsProductExist)
            {
                ModelState.AddModelError("ProductName", "Product name already exist.");
                return View(model);
            }

            model.CreatedBy = _userManager.GetUserName(User);
            await _unitOfWork.MobilityProduct.AddAsync(model, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            #region -- Audit Trail Recording --

            FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                $"Created new Product #{model.ProductCode}", "Product", nameof(Mobility));
            await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

            #endregion -- Audit Trail Recording --

            TempData["success"] = "Product created successfully";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var product = await _unitOfWork.MobilityProduct.GetAsync(c => c.ProductId == id, cancellationToken);

            if (product != null)
            {
                return View(product);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MobilityProduct model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model.EditedBy = _userManager.GetUserName(User);
                await _unitOfWork.MobilityProduct.UpdateAsync(model, cancellationToken);

                #region -- Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Edited Product #{model.ProductCode}", "Product", nameof(Mobility));
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

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

        [HttpGet]
        public async Task<IActionResult> Activate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var product = await _unitOfWork
                .MobilityProduct
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
                .MobilityProduct
                .GetAsync(c => c.ProductId == id, cancellationToken);

            if (product == null)
            {
                return NotFound();
            }

            product.IsActive = true;
            await _unitOfWork.SaveAsync(cancellationToken);

            #region -- Audit Trail Recording --

            FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                $"Activated Product #{product.ProductCode}", "Product", nameof(Mobility));
            await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

            #endregion -- Audit Trail Recording --

            TempData["success"] = "Product activated successfully";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var product = await _unitOfWork
                .MobilityProduct
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
                .MobilityProduct
                .GetAsync(c => c.ProductId == id, cancellationToken);

            if (product == null)
            {
                return NotFound();
            }

            product.IsActive = false;
            await _unitOfWork.SaveAsync(cancellationToken);

            #region -- Audit Trail Recording --

            FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                $"Deactivated Product #{product.ProductCode}", "Product", nameof(Mobility));
            await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

            #endregion -- Audit Trail Recording --

            TempData["success"] = "Product deactivated successfully";
            return RedirectToAction(nameof(Index));
        }
    }
}
