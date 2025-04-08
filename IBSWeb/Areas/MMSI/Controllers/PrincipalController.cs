using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.MMSI
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class PrincipalController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _unitOfWork;

        public PrincipalController(ApplicationDbContext db, IUnitOfWork unitOfWork)
        {
            _db = db;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var principals = await _db.MMSIPrincipals
                .Include(p => p.Customer)
                .ToListAsync(cancellationToken);

            return View(principals);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            MMSIPrincipal model = new MMSIPrincipal();
            model.CustomerSelectList = await _unitOfWork.Msap.GetMMSICustomersById(cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSIPrincipal model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["error"] = "Invalid entry, please try again.";

                    return View(model);
                }

                var customer = await _db.MMSICustomers.FindAsync(model.CustomerId, cancellationToken);
                model.CustomerId = customer.MMSICustomerId;
                customer.HasPrincipal = true;

                await _db.MMSIPrincipals.AddAsync(model, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Creation Succeed!";

                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            // id of principal
            try
            {
                // model of principal to delete
                var model = await _db.MMSIPrincipals.FindAsync(id, cancellationToken);
                // id of customer where the principal is
                var idOfCustomer = model.CustomerId;

                // delete the model(principal)
                _db.MMSIPrincipals.Remove(model);
                await _db.SaveChangesAsync(cancellationToken);

                // get the customer of the principal
                var principalsOfTheCustomer = await _db.MMSIPrincipals.Where(p => p.CustomerId == idOfCustomer).ToListAsync(cancellationToken);

                // check if the customer still has principals, if 0, hasprincipal = false
                if (principalsOfTheCustomer.Count == 0)
                {
                    var customer = _db.MMSICustomers.Find(idOfCustomer);
                    customer.HasPrincipal = false;

                    await _db.SaveChangesAsync(cancellationToken);
                }

                TempData["success"] = "Entry deleted successfully";

                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIPrincipals.FindAsync(id, cancellationToken);
            model.CustomerSelectList = await _unitOfWork.Msap.GetMMSICustomersById(cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIPrincipal model, CancellationToken cancellationToken = default)
        {
            // model of new principal
            try
            {
                // model of old principal
                var currentModel = await _db.MMSIPrincipals.FindAsync(model.PrincipalId, cancellationToken);
                // id of old principal's customer
                var previousCustomerId = currentModel.CustomerId;

                if (currentModel != null)
                {
                    currentModel.Address = model.Address;
                    currentModel.PrincipalName = model.PrincipalName;
                    currentModel.TIN = model.TIN;
                    currentModel.BusinessType = model.BusinessType;
                    currentModel.PrincipalName = model.PrincipalName;
                    currentModel.Terms = model.Terms;
                    currentModel.Mobile1 = model.Mobile1;
                    currentModel.Mobile2 = model.Mobile2;
                    currentModel.Landline1 = model.Landline1;
                    currentModel.Landline2 = model.Landline2;
                    currentModel.IsVatable = model.IsVatable;
                    currentModel.IsActive = model.IsActive;
                    currentModel.CustomerId = model.CustomerId;

                    // current chosen customer
                    var newCustomer = await _db.MMSICustomers.FindAsync(model.CustomerId, cancellationToken);
                    newCustomer.HasPrincipal = true;
                }
                else
                {
                    TempData["error"] = "Principal not found.";
                    return View(model);
                }

                await _db.SaveChangesAsync(cancellationToken);

                // see if old customer still has principals, if not, has principal = false
                var previousCustomerPrincipals = await _db.MMSIPrincipals.Where(p => p.CustomerId == previousCustomerId).ToListAsync(cancellationToken);
                if (previousCustomerPrincipals.Count == 0)
                {
                    var customer = await _db.MMSICustomers.FindAsync(previousCustomerId, cancellationToken);
                    customer.HasPrincipal = false;

                    await _db.SaveChangesAsync(cancellationToken);
                }

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
