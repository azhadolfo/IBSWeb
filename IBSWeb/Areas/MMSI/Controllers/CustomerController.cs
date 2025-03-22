using IBS.DataAccess.Data;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.MMSI
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CustomerController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var customers = _db.MMSICustomers.ToList();

            return View(customers);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSICustomer model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["error"] = "Invalid entry, please try again.";

                    return View(model);
                }

                await _db.MMSICustomers.AddAsync(model, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Creation Succeed!";

                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException?.Message))
                {
                    TempData["error"] = ex.InnerException.Message;
                }
                else
                {
                    TempData["error"] = ex.Message;
                }

                return View(model);
            }
        }

        public IActionResult Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = _db.MMSICustomers.Where(i => i.MMSICustomerId == id).FirstOrDefault();

                _db.MMSICustomers.Remove(model);
                _db.SaveChanges();

                TempData["success"] = "Entry deleted successfully";

                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                TempData["error"] = ex.InnerException.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = _db.MMSICustomers.Where(a => a.MMSICustomerId == id).FirstOrDefault();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSICustomer model, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentModel = await _db.MMSICustomers.FindAsync(model.MMSICustomerId, cancellationToken);

                if (currentModel == null)
                {
                    currentModel.CustomerAddress = model.CustomerAddress;
                    currentModel.CustomerName = model.CustomerName;
                    currentModel.CustomerTIN = model.CustomerTIN;
                    currentModel.CustomerBusinessStyle = model.CustomerBusinessStyle;
                    currentModel.CustomerTerms = model.CustomerName;
                    currentModel.CustomerTerms = model.CustomerTerms;
                    currentModel.Mobile1 = model.Mobile1;
                    currentModel.Mobile2 = model.Mobile2;
                    currentModel.Landline1 = model.Landline1;
                    currentModel.Landline2 = model.Landline2;
                }
                else
                {
                    TempData["error"] = "Customer not found.";
                    return View(model);
                }

                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                TempData["error"] = "An error occurred while editing the entry. Please try again.";
                return View(model);
            }
        }
    }
}
