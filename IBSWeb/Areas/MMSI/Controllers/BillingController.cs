using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI;
using IBS.Models.MMSI;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.MMSI
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class BillingController : Controller
    {
        private readonly DispatchTicketRepository _dispatchRepo;
        private readonly ApplicationDbContext _db;

        public BillingController(DispatchTicketRepository dispatchRepo, ApplicationDbContext db)
        {
            _dispatchRepo = dispatchRepo;
            _db = db;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var model = await _db.MMSIBillings
                .Include(a => a.Terminal)
                .Include(a => a.Vessel)
                .Include (a => a.Customer)
                .Include (a => a.Port)
                .ToListAsync();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            MMSIBilling model = new()
            {
                Customers = await _dispatchRepo.GetMMSICustomersById(cancellationToken),
                Vessels = await _dispatchRepo.GetMMSIVesselsById(cancellationToken),
                Ports = await _dispatchRepo.GetMMSIPortsById(cancellationToken),
                UnbilledDispatchTickets = await _dispatchRepo.GetMMSIUnbilledTicketsById(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSIBilling model, CancellationToken cancellationToken)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Status = "For Collection";

                    if (!model.IsDocumented)
                    {
                        model.MMSIBillingNumber = await _dispatchRepo.GenerateBillingNumber(cancellationToken);
                    }

                    decimal totalAmount = 0;

                    foreach(var billDispatchTicket in model.ToBillDispatchTickets)
                    {
                        var dtEntry = await _db.MMSIDispatchTickets.FindAsync(int.Parse(billDispatchTicket));
                        totalAmount = (totalAmount + dtEntry?.TotalNetRevenue) ?? 0m;
                        dtEntry.Status = "For Collection";
                        dtEntry.BillingId = model.MMSIBillingNumber;
                        await _db.SaveChangesAsync(cancellationToken);
                    }

                    model.Amount = totalAmount;
                    await _db.MMSIBillings.AddAsync(model, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    TempData["success"] = "Entry Created Successfully!";

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["error"] = "Can't create entry, please review your input.";
                    model = await _dispatchRepo.GetBillingLists(model, cancellationToken);

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(ex.InnerException.Message))
                {
                    TempData["error"] = ex.Message;
                }
                else
                {
                    TempData["error"] = $"{ex.InnerException.Message}";
                }

                model = await _dispatchRepo.GetBillingLists(model, cancellationToken);

                return View(model);
            }
        }

        public async Task<IActionResult> GetDispatchTickets(List<string> dispatchTicketIds)
        {
            try
            {
                var intDispatchTicketIds = dispatchTicketIds.Select(int.Parse).ToList();
                var dispatchTickets = await _db.MMSIDispatchTickets
                    .Include(t => t.Tugboat)
                    .Include(t => t.ActivityService)
                    .Where(t => intDispatchTicketIds.Contains(t.DispatchTicketId)) // Assuming Id is the primary key
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = dispatchTickets
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIBillings
                .Where(b => b.MMSIBillingId == id)
                .Include(b => b.Customer)
                .Include(b => b.Vessel)
                .Include (b => b.Port)
                .Include(b => b.Terminal)
                .FirstOrDefaultAsync();
            model = await _dispatchRepo.GetBillingLists(model, cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIBilling model, IFormFile? file, CancellationToken cancellationToken)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var currentModel = await _db.MMSIBillings.FindAsync(model.MMSIBillingId, cancellationToken);

                    if(model.IsDocumented != null)
                    {
                        if (model.IsDocumented)
                        {
                            currentModel.MMSIBillingNumber = model.MMSIBillingNumber;
                        }
                        if (!model.IsDocumented && !currentModel.IsDocumented)
                        {
                            // as is, use old generated
                        }
                        if (!model.IsDocumented && currentModel.IsDocumented)
                        {
                            currentModel.MMSIBillingNumber = await _dispatchRepo.GenerateBillingNumber(cancellationToken);
                        }
                    }

                    currentModel.Date = model.Date;
                    currentModel.Status = "Created";
                    currentModel.VoyageNumber = model.VoyageNumber;
                    currentModel.CustomerId = model.CustomerId;
                    currentModel.VesselId = model.VesselId;
                    currentModel.PortId = model.PortId;
                    currentModel.TerminalId = model.TerminalId;
                    currentModel.IsDocumented = model.IsDocumented;

                    await _db.SaveChangesAsync();
                    TempData["success"] = "Entry edited successfully!";

                    return RedirectToAction(nameof(Index));

                }
                else
                {
                    TempData["error"] = "Can't create entry, please review your input.";

                    model = await _db.MMSIBillings
                        .Include(b => b.Customer)
                        .Include(b => b.Vessel)
                        .Include(b => b.Port)
                        .Include(b => b.Terminal)// Include the Port navigation property of Terminal
                    .FirstOrDefaultAsync(dt => dt.MMSIBillingId == model.MMSIBillingId, cancellationToken);

                    model = await _dispatchRepo.GetBillingLists(model, cancellationToken);

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(ex.InnerException.Message))
                {
                    TempData["error"] = ex.Message;
                }
                else
                {
                    TempData["error"] = $"{ex.InnerException.Message}";
                }

                model = await _db.MMSIBillings
                        .Include(b => b.Customer)
                        .Include(b => b.Vessel)
                        .Include(b => b.Port)
                        .Include(b => b.Terminal)// Include the Port navigation property of Terminal
                    .FirstOrDefaultAsync(dt => dt.MMSIBillingId == model.MMSIBillingId, cancellationToken);

                model = await _dispatchRepo.GetBillingLists(model, cancellationToken);

                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIBillings.FindAsync(id, cancellationToken);

                if (model != null)
                {

                    _db.MMSIBillings.Remove(model);
                    _db.SaveChanges();

                    TempData["success"] = "Billing deleted successfully!";

                    return RedirectToAction(nameof(Index));
                }

                else
                {
                    TempData["error"] = "Can't find entry.";

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(ex.InnerException.Message))
                {
                    TempData["error"] = ex.Message;
                }
                else
                {
                    TempData["error"] = $"{ex.InnerException.Message}";
                }

                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Preview (int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIBillings
                .Where(a => a.MMSIBillingId == id)
                .Include(a => a.Terminal)
                .Include(a => a.Vessel)
                .Include(a => a.Customer)
                .Include(a => a.Port)
                .FirstOrDefaultAsync(cancellationToken);

            model.ToBillDispatchTickets = await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == model.MMSIBillingNumber)
                .Select(dt => dt.DispatchNumber.ToString())
                .ToListAsync(cancellationToken);

            model.PaidDispatchTickets = await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == model.MMSIBillingNumber)
                .Include(dt => dt.Tugboat)
                .Include(dt => dt.ActivityService)
                .ToListAsync (cancellationToken);

            return View(model);
        }

        public JsonResult GetCustomerDetails(int customerId)
        {
            var customerDetails = _db.MMSICustomers
                .Find(customerId);

            var customerDetailsJson = new
            {
                terms = customerDetails.CustomerTerms,
                address = customerDetails.CustomerAddress,
                tinNo = customerDetails.CustomerTIN,
                businessStyle = customerDetails.CustomerBusinessStyle
            };

            return Json(customerDetailsJson);
        }

    }
}
