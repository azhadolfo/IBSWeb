using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.MMSI;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class CollectionController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<CollectionController> _logger;

        public CollectionController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager, ILogger<CollectionController> logger)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            var model = new MMSICollection();
            model.Customers = await _unitOfWork.Msap.GetMMSICustomersById(cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSICollection model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var dateNow = DateTime.Now;
                    model.CreatedBy = await GetUserNameAsync();
                    model.CreatedDate = dateNow;

                    if (model.IsUndocumented)
                    {
                        model.MMSICollectionNumber = await _unitOfWork.Msap.GenerateCollectionNumber(cancellationToken);
                    }

                    await _dbContext.MMSICollections.AddAsync(model, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // save first then refetch again so it has auto generated id
                    var refetchModel = await _dbContext.MMSICollections
                        .Where(c => c.CreatedDate == dateNow)
                        .FirstOrDefaultAsync(cancellationToken);

                    int id = refetchModel.MMSICollectionId;


                    #region -- Audit Trail

                    var audit = new FilprideAuditTrail
                    {
                        Date = DateTimeHelper.GetCurrentPhilippineTime(),
                        Username = await GetUserNameAsync(),
                        MachineName = Environment.MachineName,
                        Activity = $"Create collection #{model.MMSICollectionNumber} for billings #{string.Join(", #", model.ToCollectBillings)}",
                        DocumentType = "Collection",
                        Company = await GetCompanyClaimAsync()
                    };

                    await _dbContext.FilprideAuditTrails.AddAsync(audit, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Audit Trail

                    //
                    foreach(var collectBills in model.ToCollectBillings)
                    {
                        // find the billings that was collected and mark them as collected
                        var billingChosen = await _dbContext.MMSIBillings.FindAsync(int.Parse(collectBills));
                        billingChosen.Status = "Collected";
                        billingChosen.CollectionId = refetchModel.MMSICollectionId;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }


                    if (model.IsUndocumented)
                    {
                        TempData["success"] = $"Collection was successfully created. Control Number: {model.MMSICollectionNumber}";
                    }
                    else
                    {
                        TempData["success"] = $"Collection was successfully created.";
                    }

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["error"] = "There was an error creating the collection.";

                    model.Billings = await _unitOfWork.Msap.GetMMSIUncollectedBillingsById(cancellationToken);
                    model.Customers = await _unitOfWork.Msap.GetMMSICustomersById(cancellationToken);

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                model.Billings = await _unitOfWork.Msap.GetMMSIUncollectedBillingsById(cancellationToken);
                model.Customers = await _unitOfWork.Msap.GetMMSICustomersById(cancellationToken);

                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetCollectionList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var queried = await _dbContext.MMSICollections
                    .Include(b => b.Customer)
                    .ToListAsync(cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                    .Where(dt =>
                        dt.Customer?.CustomerName.ToString().Contains(searchValue) == true ||
                        dt.Status.Contains(searchValue) == true
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    queried = queried
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = queried.Count();

                var pagedData = queried
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedData
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get disbursements.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var model = _dbContext.MMSICollections.Find(id);

            // default contents of billing field from previous create
            model.ToCollectBillings = await _dbContext.MMSIBillings
                .Where(b => b.CollectionId == model.MMSICollectionId)
                .Select(b => b.MMSIBillingId.ToString())
                .ToListAsync(cancellationToken);

            // selection of customers
            model.Customers = await _unitOfWork.Msap.GetMMSICustomersById(cancellationToken);

            // get bills with same customer
            model.Billings = await GetEditBillings(model.CustomerId, model.MMSICollectionId, cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSICollection model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    var currentModel = await _dbContext.MMSICollections.FindAsync(model.MMSICollectionId, cancellationToken);

                    #region -- Changes

                    var changes = new List<string>();

                    if (currentModel.CheckNumber != model.CheckNumber) { changes.Add($"CheckNumber: {currentModel.CheckNumber} -> {model.CheckNumber}"); }
                    if (currentModel.Date != model.Date) { changes.Add($"Date: {currentModel.Date} -> {model.Date}"); }
                    if (currentModel.CustomerId != model.CustomerId) { changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}"); }
                    if (currentModel.Amount != model.Amount) { changes.Add($"Amount: {currentModel.Amount} -> {model.Amount}"); }
                    if (currentModel.EWT != model.EWT) { changes.Add($"EWT: {currentModel.EWT} -> {model.EWT}"); }
                    if (currentModel.CheckDate != model.CheckDate) { changes.Add($"CheckDate: {currentModel.CheckDate} -> {model.CheckDate}"); }
                    if (currentModel.DepositDate != model.DepositDate) { changes.Add($"DepositDate: {currentModel.DepositDate} -> {model.DepositDate}"); }

                    #endregion -- Changes

                    #region -- Audit Trail

                    var audit = new FilprideAuditTrail
                    {
                        Date = DateTimeHelper.GetCurrentPhilippineTime(),
                        Username = await GetUserNameAsync(),
                        MachineName = Environment.MachineName,
                        Activity = changes.Any()
                            ? $"Edit collection #{currentModel.MMSICollectionNumber} {string.Join(", ", changes)}"
                            : $"No changes detected for collection #{currentModel.MMSICollectionId}",
                        DocumentType = "Collection",
                        Company = await GetCompanyClaimAsync()
                    };

                    await _dbContext.FilprideAuditTrails.AddAsync(audit, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Audit Trail

                    currentModel.Date = model.Date;
                    currentModel.CustomerId = model.CustomerId;
                    currentModel.CheckNumber = model.CheckNumber;
                    currentModel.CheckDate = model.CheckDate;
                    currentModel.DepositDate = model.DepositDate;
                    currentModel.Amount = model.Amount;
                    currentModel.EWT = model.EWT;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Collection modified successfully";

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["error"] = "There was an error updating the collection.";

                    return View(model);
                }

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return View(model);
            }
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken = default)
        {
            var collection = await _dbContext.MMSICollections.FindAsync(id, cancellationToken);

            if (collection != null)
            {
                // list of dispatch tickets
                collection.PaidBills = await _dbContext.MMSIBillings
                    .Where(b => b.CollectionId == collection.MMSICollectionId)
                    .Include(b => b.Customer)
                    .ToListAsync (cancellationToken);

                return View(collection);
            }
            else
            {
                TempData["Error"] = "Error: collection record not found.";

                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> GetCollections(CancellationToken cancellationToken = default)
        {
            var collections = await _dbContext.MMSICollections
                .Include(c => c.Customer)
                .ToListAsync(cancellationToken);

            return Json(collections);
        }

        [HttpPost]
        public async Task<IActionResult> GetSelectedBillings(List<string> billingIds, CancellationToken cancellationToken = default)
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

        [HttpGet]
        public async Task<IActionResult> IsCustomerVatable(int customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var customer = await _dbContext.FilprideCustomers
                    .FindAsync(customerId, cancellationToken);

                if (customer != null)
                {
                    return Json(customer.VatType == SD.VatType_Vatable);
                }
                else
                {
                    throw new NullReferenceException("Customer not found.");
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Customer not found" });
            }
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var claims = await _userManager.GetClaimsAsync(await _userManager.GetUserAsync(User));
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        private async Task<string> GetUserNameAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user.UserName;
        }

        public async Task<List<SelectListItem>?> GetEditBillings(int? customerId, int collectionId, CancellationToken cancellationToken = default)
        {
            // bills uncollected but with the same customers
            var list = await _unitOfWork.Msap.GetMMSIUncollectedBillingsByCustomer(customerId, cancellationToken);

            // get the current model
            var model = await _dbContext.MMSICollections.FindAsync(collectionId, cancellationToken);

            // if the model WAS having previous customer, fetch it previous bills as well
            if (model?.CustomerId == customerId)
            {
                list?.AddRange(await _unitOfWork.Msap.GetMMSICollectedBillsById(collectionId, cancellationToken));
            }

            return list;
        }

        public async Task<List<SelectListItem>?> GetUncollectedBillings(int? customerId, CancellationToken cancellationToken = default)
        {
            // bills uncollected by customer
            var list = await _unitOfWork.Msap.GetMMSIUncollectedBillingsByCustomer(customerId, cancellationToken);

            return list;
        }
    }
}
