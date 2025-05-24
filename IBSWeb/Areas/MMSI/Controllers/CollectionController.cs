using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using IBS.Services;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using IBS.Utility.Enums;
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
        private readonly IUserAccessService _userAccessService;

        public CollectionController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager, ILogger<CollectionController> logger,
            IUserAccessService userAccessService)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
            _userAccessService = userAccessService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User)!, ProcedureEnum.CreateCollection, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CreateCollectionViewModel
            {
                Customers = await _unitOfWork.Collection.GetMMSICustomersWithCollectiblesById(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCollectionViewModel viewModel, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "There was an error creating the collection.";

                viewModel.Customers = await _unitOfWork.Collection.GetMMSICustomersWithCollectiblesById(cancellationToken);

                return View(viewModel);
            }

            try
            {
                var model = CreateCollectionVmToCollectionModel(viewModel);

                var dateNow = DateTime.Now;
                model.CreatedBy = await GetUserNameAsync() ?? throw new InvalidOperationException();
                model.CreatedDate = dateNow;
                model.Status = "Create";

                if (model.IsUndocumented)
                {
                    model.MMSICollectionNumber = await _unitOfWork.Collection.GenerateCollectionNumber(cancellationToken);
                }
                else
                {
                    model.MMSICollectionNumber = viewModel.MMSICollectionNumber ?? string.Empty;
                }

                await _dbContext.MMSICollections.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // save first then refetch again so it has auto generates id
                var refetchModel = await _dbContext.MMSICollections
                    .Where(c => c.CreatedDate == dateNow)
                    .FirstOrDefaultAsync(cancellationToken);

                if (refetchModel == null)
                {
                    return BadRequest();
                }

                int id = refetchModel.MMSICollectionId;


                #region -- Audit Trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync() ??  throw new InvalidOperationException(),
                    MachineName = Environment.MachineName,
                    Activity = $"Create collection #{model.MMSICollectionNumber} for billings #{string.Join(", #", viewModel.ToCollectBillings!)}",
                    DocumentType = "Collection",
                    Company = await GetCompanyClaimAsync() ??  throw new InvalidOperationException()
                };

                await _dbContext.FilprideAuditTrails.AddAsync(audit, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion -- Audit Trail

                //
                foreach(var collectBills in viewModel.ToCollectBillings!)
                {
                    // find the billings that was collected and mark them as collected
                    var billingChosen = await _dbContext.MMSIBillings.FindAsync(int.Parse(collectBills));
                    billingChosen!.Status = "Collected";
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
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                viewModel.Customers = await _unitOfWork.Collection.GetMMSICustomersWithCollectiblesById(cancellationToken);

                return View(viewModel);
            }
        }

        public MMSICollection CreateCollectionVmToCollectionModel(CreateCollectionViewModel viewModel)
        {
            var model = new MMSICollection
            {
                IsUndocumented = viewModel.IsUndocumented,
                Date = viewModel.Date,
                CustomerId = viewModel.CustomerId,
                Amount = viewModel.Amount,
                EWT = viewModel.EWT,
                CheckNumber = viewModel.CheckNumber,
                CheckDate = viewModel.CheckDate,
                DepositDate = viewModel.DepositDate,
            };

            if (viewModel.MMSICollectionId != null)
            {
                model.MMSICollectionId = viewModel.MMSICollectionId ?? 0;
            }

            return model;
        }

        public CreateCollectionViewModel CollectionModelToCreateCollectionVm(MMSICollection model)
        {
            var viewModel = new CreateCollectionViewModel
            {
                MMSICollectionId = model.MMSICollectionId,
                MMSICollectionNumber = model.MMSICollectionNumber,
                IsUndocumented = model.IsUndocumented,
                Date = model.Date,
                CustomerId = model.CustomerId,
                Amount = model.Amount,
                EWT = model.EWT,
                CheckNumber = model.CheckNumber,
                CheckDate = model.CheckDate,
                DepositDate = model.DepositDate,
            };

            return viewModel;
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
                    .Where(c =>

                        c.Date.Day.ToString().Contains(searchValue) == true ||
                        c.Date.Month.ToString().Contains(searchValue) == true ||
                        c.Date.Year.ToString().Contains(searchValue) == true ||

                        c.CheckDate.Day.ToString().Contains(searchValue) == true ||
                        c.CheckDate.Month.ToString().Contains(searchValue) == true ||
                        c.CheckDate.Year.ToString().Contains(searchValue) == true ||

                        c.DepositDate.Day.ToString().Contains(searchValue) == true ||
                        c.DepositDate.Month.ToString().Contains(searchValue) == true ||
                        c.DepositDate.Year.ToString().Contains(searchValue) == true ||

                        c.Amount.ToString().Contains(searchValue) == true ||
                        c.MMSICollectionNumber.ToLower().Contains(searchValue) == true ||
                        c.Customer?.CustomerName.ToLower().Contains(searchValue) == true ||
                        c.Status.ToLower().Contains(searchValue) == true
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

            if (model == null)
            {
                return NotFound();
            }

            var viewModel = CollectionModelToCreateCollectionVm(model);

            // default contents of billing field from previous create
            viewModel.ToCollectBillings = await _dbContext.MMSIBillings
                .Where(b => b.CollectionId == model.MMSICollectionId)
                .Select(b => b.MMSIBillingId.ToString())
                .ToListAsync(cancellationToken);

            // selection of customers
            viewModel.Customers = await _unitOfWork.Collection.GetMMSICustomersById(cancellationToken);

            // get bills with same customer
            viewModel.Billings = await GetEditBillings(model.CustomerId, model.MMSICollectionId, cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CreateCollectionViewModel viewModel, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    var model = CreateCollectionVmToCollectionModel(viewModel);

                    //previous billings
                    var previousCollectedBills = await _dbContext.MMSIBillings
                        .Where(b => b.CollectionId == model.MMSICollectionId)
                        .ToListAsync(cancellationToken);

                    //previous billings
                    var previousCollectedBillsString = await _dbContext.MMSIBillings
                        .Where(b => b.CollectionId == model.MMSICollectionId)
                        .Select(b => b.MMSIBillingId.ToString())
                        .ToListAsync(cancellationToken);

                    //revert old billings
                    foreach (var previousBilling in previousCollectedBills)
                    {
                        var billing = await _dbContext.MMSIBillings
                            .FindAsync(previousBilling.MMSIBillingId, cancellationToken);

                        if (billing == null) throw new NullReferenceException("Billing not found.");

                        billing.Status = "For Collection";
                        billing.CollectionId = 0;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }

                    if (viewModel.ToCollectBillings == null) throw new NullReferenceException("No Billing was selected.");

                    //relate new billings to collection
                    foreach (var newBilling in viewModel.ToCollectBillings)
                    {
                        var billing = await _dbContext.MMSIBillings
                            .FindAsync(int.Parse(newBilling), cancellationToken);

                        if (billing == null) throw new NullReferenceException("Billing not found.");

                        billing.Status = "Collected";
                        billing.CollectionId = model.MMSICollectionId;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }

                    var currentModel = await _dbContext.MMSICollections.FindAsync(model.MMSICollectionId, cancellationToken);

                    if (currentModel == null)
                    {
                        throw new NullReferenceException("The collection does not exist.");
                    }

                    #region -- Changes

                    var changes = new List<string>();

                    if (currentModel.CheckNumber != model.CheckNumber) { changes.Add($"CheckNumber: {currentModel.CheckNumber} -> {model.CheckNumber}"); }
                    if (currentModel.Date != model.Date) { changes.Add($"Date: {currentModel.Date} -> {model.Date}"); }
                    if (currentModel.CustomerId != model.CustomerId) { changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}"); }
                    if (currentModel.Amount != model.Amount) { changes.Add($"Amount: {currentModel.Amount} -> {model.Amount}"); }
                    if (currentModel.EWT != model.EWT) { changes.Add($"EWT: {currentModel.EWT} -> {model.EWT}"); }
                    if (currentModel.CheckDate != model.CheckDate) { changes.Add($"CheckDate: {currentModel.CheckDate} -> {model.CheckDate}"); }
                    if (currentModel.DepositDate != model.DepositDate) { changes.Add($"DepositDate: {currentModel.DepositDate} -> {model.DepositDate}"); }
                    if (!previousCollectedBillsString.OrderBy(x => x).SequenceEqual(viewModel.ToCollectBillings.OrderBy(x => x)))
                    { changes.Add($"ToBillDispatchTickets: #{string.Join(", #", previousCollectedBillsString)} -> #{string.Join(", #", viewModel.ToCollectBillings)}"); }

                    #endregion -- Changes

                    #region -- Audit Trail

                    var audit = new FilprideAuditTrail
                    {
                        Date = DateTimeHelper.GetCurrentPhilippineTime(),
                        Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                        MachineName = Environment.MachineName,
                        Activity = changes.Any()
                            ? $"Edit collection #{currentModel.MMSICollectionNumber} {string.Join(", ", changes)}"
                            : $"No changes detected for collection #{currentModel.MMSICollectionId}",
                        DocumentType = "Collection",
                        Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException()
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

                    return View(viewModel);
                }

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return View(viewModel);
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

                throw new NullReferenceException("Customer not found.");
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Customer not found" });
            }
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        private async Task<string?> GetUserNameAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.UserName;
        }

        public async Task<List<SelectListItem>?> GetEditBillings(int? customerId, int collectionId, CancellationToken cancellationToken = default)
        {
            // bills uncollected but with the same customers
            var list = await _unitOfWork.Collection.GetMMSIUncollectedBillingsByCustomer(customerId, cancellationToken);

            // get the current model
            var model = await _dbContext.MMSICollections.FindAsync(collectionId, cancellationToken);

            // if the model WAS having previous customer, fetch it previous bills as well
            if (model?.CustomerId == customerId)
            {
                list?.AddRange(await _unitOfWork.Collection.GetMMSICollectedBillsById(collectionId, cancellationToken));
            }

            return list;
        }

        public async Task<List<SelectListItem>?> GetUncollectedBillings(int? customerId, CancellationToken cancellationToken = default)
        {
            // bills uncollected by customer
            var list = await _unitOfWork.Collection.GetMMSIUncollectedBillingsByCustomer(customerId, cancellationToken);

            return list;
        }
    }
}
