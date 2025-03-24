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
    public class CollectionController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly DispatchTicketRepository _dispatchTicketRepository;

        public CollectionController(ApplicationDbContext dbContext, DispatchTicketRepository dispatchTicketRepository)
        {
            _dbContext = dbContext;
            _dispatchTicketRepository = dispatchTicketRepository;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            return View();
        }

        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            var model = new MMSICollection();
            model.Billings = await _dispatchTicketRepository.GetMMSIUncollectedBillingsById(cancellationToken);
            model.Customers = await _dispatchTicketRepository.GetMMSICustomersById(cancellationToken);
            return View(model);
        }

        public async Task<IActionResult> GetCollections(CancellationToken cancellationToken = default)
        {
            var collections = await _dbContext.MMSICollections.ToListAsync(cancellationToken);

            return Json(collections);
        }

        [HttpPost]
        public async Task<IActionResult> GetBillings(List<string> billingIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var intBillingIds = billingIds.Select(int.Parse).ToList();
                var billings = await _dbContext.MMSIBillings
                    .Where(t => intBillingIds.Contains(t.MMSIBillingId)) // Assuming Id is the primary key
                    .ToListAsync(cancellationToken);

                return Json(new
                {
                    success = true,
                    data = billings
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

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken = default)
        {
            var collection = await _dbContext.MMSICollections.FindAsync(id, cancellationToken);

            if (collection == null)
            {
                TempData["Error"] = "Error: collection record not found.";

                return RedirectToAction("Index");
            }
            else
            {
                return View(collection);
            }
        }
    }
}
