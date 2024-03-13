using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class SupplierController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<SupplierController> _logger;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public SupplierController(IUnitOfWork unitOfWork, ILogger<SupplierController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;

        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Supplier> suppliers = await _unitOfWork.Supplier
                .GetAllAsync();

            return View(suppliers);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Supplier model, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                if (await _unitOfWork.Supplier.IsSupplierExistAsync(model.SupplierName, cancellationToken))
                {
                    ModelState.AddModelError("SupplierName", "Supplier already exist.");
                    return View(model);
                }

                if (await _unitOfWork.Supplier.IsTinNoExistAsync(model.SupplierTin, cancellationToken))
                {
                    ModelState.AddModelError("SupplierTin", "Tin number already exist.");
                    return View(model);
                }

                try
                {
                    if (file != null || file.Length > 0)
                    {
                        string localPath = Path.Combine(_webHostEnvironment.WebRootPath, "Proof of Registration", model.SupplierName);

                        model.ProofOfRegistrationFilePath = await _unitOfWork.Supplier.SaveProofOfRegistration(file, localPath, cancellationToken);
                    }

                    model.SupplierCode = await _unitOfWork.Supplier
                        .GenerateCodeAsync(cancellationToken);

                    await _unitOfWork.Supplier.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);
                    TempData["success"] = "Supplier created successfully";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on creating supplier.");
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("", "Make sure to fill all the required details.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Supplier supplier = await _unitOfWork.Supplier.GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier != null)
            {
                return View(supplier);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Supplier model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _unitOfWork.Supplier.UpdateAsync(model, cancellationToken);
                    TempData["success"] = "Supplier updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in updating supplier.");
                    TempData["error"] = ex.Message;
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

            Supplier supplier = await _unitOfWork
                .Supplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier != null)
            {
                return View(supplier);
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

            Supplier supplier = await _unitOfWork
                .Supplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier != null)
            {
                supplier.IsActive = true;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Supplier activated successfully";
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

            Supplier supplier = await _unitOfWork
                .Supplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier != null)
            {
                return View(supplier);
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

            Supplier supplier = await _unitOfWork
                .Supplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier != null)
            {
                supplier.IsActive = false;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Supplier deactivated successfully";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }
    }
}
