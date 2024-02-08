using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CustomerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CustomerController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Customer> customer = await _unitOfWork.Customer
                .GetAllAsync();
            return View(customer);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Customer model)
        {
            if (ModelState.IsValid)
            {
                bool IsTinExist = await _unitOfWork.Customer.IsTinNoExistAsync(model.CustomerTin);

                if (IsTinExist)
                {
                    ModelState.AddModelError("CustomerTin", "Tin No already exist.");
                    return View(model);
                }

                model.CustomerCode = await _unitOfWork.Customer.GenerateCodeAsync(model.CustomerType);
                await _unitOfWork.Customer.AddAsync(model);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Customer created successfully";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", "");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Customer customer = await _unitOfWork.Customer.GetAsync(c => c.CustomerId == id);

            if (customer != null)
            {
                return View(customer);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Customer model)
        {
            if (ModelState.IsValid)
            {
                await _unitOfWork.Customer.UpdateAsync(model);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Customer updated successfully";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Activate(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Customer customer = await _unitOfWork
                .Customer
                .GetAsync(c => c.CustomerId == id);

            if (customer != null)
            {
                return View(customer);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Activate")]
        public async Task<IActionResult> ActivatePost(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Customer customer = await _unitOfWork
                .Customer
                .GetAsync(c => c.CustomerId == id);

            if (customer != null)
            {
                customer.IsActive = true;
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Customer has been activated";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Customer customer = await _unitOfWork
                .Customer
                .GetAsync(c => c.CustomerId == id);

            if (customer != null)
            {
                return View(customer);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Deactivate")]
        public async Task<IActionResult> DeactivatePost(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Customer customer = await _unitOfWork
                .Customer
                .GetAsync(c => c.CustomerId == id);

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