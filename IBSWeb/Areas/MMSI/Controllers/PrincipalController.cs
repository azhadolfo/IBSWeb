using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI;
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
        private readonly DispatchTicketRepository _dispatchTicketRepository;

        public PrincipalController(ApplicationDbContext db, DispatchTicketRepository dispatchTicketRepository)
        {
            _db = db;
            _dispatchTicketRepository = dispatchTicketRepository;
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
            model.CustomerSelectList = await _dispatchTicketRepository.GetMMSICustomersById(cancellationToken);

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

                var customer = await _db.MMSICustomers.Where(c => c.MMSICustomerId == model.CustomerId)
                    .FirstOrDefaultAsync(cancellationToken);
                model.PrincipalNumber = customer.CustomerNumber;

                await _db.MMSIPrincipals.AddAsync(model, cancellationToken);
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

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIPrincipals.FindAsync(id, cancellationToken);

                _db.MMSIPrincipals.Remove(model);
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
            var model = await _db.MMSIPrincipals.FindAsync(id, cancellationToken);
            model.CustomerSelectList = await _dispatchTicketRepository.GetMMSICustomersById(cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIPrincipal model, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentModel = await _db.MMSIPrincipals.FindAsync(model.PrincipalId, cancellationToken);

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
                }
                else
                {
                    TempData["error"] = "Principal not found.";
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
