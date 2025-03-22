using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.MMSI
{
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
            var collections = await _dbContext.MMSICollections.ToListAsync(cancellationToken);

            return View(collections);
        }

        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            var model = new MMSICollection();
            model.Billings = await _dispatchTicketRepository.GetMMSIUncollectedBillingsById(cancellationToken);
            return View(model);
        }

        public async Task<IActionResult> GetCollections(CancellationToken cancellationToken = default)
        {
            var collections = await _dbContext.MMSICollections.ToListAsync(cancellationToken);

            return Json(collections);
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
