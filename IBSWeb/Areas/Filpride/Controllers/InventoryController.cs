using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        public InventoryController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> BeginningInventory(CancellationToken cancellationToken)
        {
            BeginningInventoryViewModel? viewModel = new();

            viewModel.ProductList = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            var companyClaims = await GetCompanyClaimAsync();

            viewModel.PO = await _dbContext.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(p => p.Company == companyClaims)
                .Select(p => new SelectListItem
                {
                    Value = p.PurchaseOrderId.ToString(),
                    Text = p.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> BeginningInventory(BeginningInventoryViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (ModelState.IsValid)
            {
                try
                {
                    var hasBeginningInventory = await _unitOfWork.FilprideInventory.HasAlreadyBeginningInventory(viewModel.ProductId, viewModel.POId, companyClaims, cancellationToken);

                    if (hasBeginningInventory)
                    {
                        viewModel.ProductList = await _unitOfWork.GetProductListAsyncById(cancellationToken);

                        TempData["error"] = "Beginning Inventory for this product already exists. Please contact MIS if you think this was a mistake.";
                        return View(viewModel);
                    }

                    viewModel.CurrentUser = _userManager.GetUserName(User);
                    await _unitOfWork.FilprideInventory.AddBeginningInventory(viewModel, companyClaims, cancellationToken);
                    TempData["success"] = "Beginning balance created successfully";
                    return RedirectToAction(nameof(BeginningInventory));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.ToString();
                    return View();
                }
            }

            viewModel.ProductList = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            viewModel.PO = await _dbContext.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(p => p.Company == companyClaims)
                .Select(p => new SelectListItem
                {
                    Value = p.PurchaseOrderId.ToString(),
                    Text = p.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            TempData["error"] = "The information you submitted is not valid!";
            return View(viewModel);
        }

        public async Task<IActionResult> InventoryReport(CancellationToken cancellationToken)
        {
            InventoryReportViewModel viewModel = new InventoryReportViewModel();

            var companyClaims = await GetCompanyClaimAsync();

            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            viewModel.PO = await _dbContext.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(p => p.Company == companyClaims)
                .Select(p => new SelectListItem
                {
                    Value = p.PurchaseOrderId.ToString(),
                    Text = p.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        public async Task<IActionResult> DisplayInventory(InventoryReportViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                var companyClaims = await GetCompanyClaimAsync();

                var dateFrom = viewModel.DateTo.AddDays(-viewModel.DateTo.Day + 1);

                var previousMonth = viewModel.DateTo.Month - 1;

                var endingBalance = await _dbContext.FilprideInventories
                    .OrderBy(e => e.Date)
                    .ThenBy(e => e.InventoryId)
                    .Where(e => e.Company == companyClaims)
                    .LastOrDefaultAsync(e => viewModel.POId == null || e.POId == viewModel.POId && e.Date.Month == previousMonth, cancellationToken);

                List<FilprideInventory> inventories = new List<FilprideInventory>();
                if (endingBalance != null)
                {
                    inventories = await _dbContext.FilprideInventories
                       .Where(i => i.Date >= dateFrom && i.Date <= viewModel.DateTo && i.Company == companyClaims && i.ProductId == viewModel.ProductId && (viewModel.POId == null || i.POId == viewModel.POId) || i.InventoryId == endingBalance.InventoryId)
                       .ToListAsync(cancellationToken);
                }
                else
                {
                    inventories = await _dbContext.FilprideInventories
                       .Where(i => i.Date >= dateFrom && i.Date <= viewModel.DateTo && i.Company == companyClaims && i.ProductId == viewModel.ProductId && (viewModel.POId == null || i.POId == viewModel.POId))
                       .ToListAsync(cancellationToken);
                }

                var product = await _dbContext.Products
                    .FindAsync(viewModel.ProductId, cancellationToken);

                ViewData["Product"] = product.ProductName;
                ViewBag.ProductId = viewModel.ProductId;
                ViewBag.POId = viewModel.POId;

                return View(inventories);
            }

            return View(viewModel);
        }

        public async Task<IActionResult> ConsolidatedPO(InventoryReportViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                var companyClaims = await GetCompanyClaimAsync();
                var dateFrom = viewModel.DateTo.AddDays(-viewModel.DateTo.Day + 1);
                List<FilprideInventory> inventories = new List<FilprideInventory>();
                if (viewModel.POId == null)
                {
                    inventories = await _dbContext.FilprideInventories
                        .Include(i => i.PurchaseOrder)
                        .Where(i => i.Company == companyClaims && i.Date >= dateFrom && i.Date <= viewModel.DateTo && i.ProductId == viewModel.ProductId)
                        .OrderBy(i => i.Date)
                        .ThenBy(i => i.InventoryId)
                        .ToListAsync(cancellationToken);
                }
                else
                {
                    inventories = await _dbContext.FilprideInventories
                        .Include(i => i.PurchaseOrder)
                        .Where(i => i.Company == companyClaims && i.Date >= dateFrom && i.Date <= viewModel.DateTo && i.ProductId == viewModel.ProductId && i.POId == viewModel.POId)
                        .OrderBy(i => i.Date)
                        .ThenBy(i => i.InventoryId)
                        .ToListAsync(cancellationToken);
                }

                var product = await _dbContext.Products
                    .FindAsync(viewModel.ProductId, cancellationToken);

                ViewData["Product"] = product.ProductName;
                ViewBag.ProductId = viewModel.ProductId;
                ViewBag.POId = viewModel.POId;

                return View(inventories);
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ActualInventory(CancellationToken cancellationToken)
        {
            ActualInventoryViewModel? viewModel = new();

            viewModel.ProductList = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.COA = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 && (coa.AccountName.StartsWith("AR-Non Trade Receivable") || coa.AccountName.StartsWith("Cost of Goods Sold") || coa.AccountNumber.StartsWith("6010103")))
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
            viewModel.PO = await _dbContext.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(p => p.Company == companyClaims)
                .Select(p => new SelectListItem
                {
                    Value = p.PurchaseOrderId.ToString(),
                    Text = p.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        public IActionResult GetProducts(int poId, int id, DateOnly dateTo)
        {
            if (id != 0)
            {
                var dateFrom = dateTo.AddDays(-dateTo.Day + 1);

                var getPerBook = _dbContext.FilprideInventories
                    .Where(i => i.Date >= dateFrom && i.Date <= dateTo && i.ProductId == id && i.POId == poId)
                    .OrderByDescending(model => model.InventoryId)
                    .FirstOrDefault();

                if (getPerBook != null)
                {
                    return Json(new { InventoryBalance = getPerBook.InventoryBalance, AverageCost = getPerBook.AverageCost, TotalBalance = getPerBook.TotalBalance });
                }
            }
            return Json(new { InventoryBalance = 0.00, AverageCost = 0.00, TotalBalance = 0.00 });
        }

        [HttpPost]
        public async Task<IActionResult> ActualInventory(ActualInventoryViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (ModelState.IsValid)
            {
                try
                {
                    viewModel.CurrentUser = _userManager.GetUserName(User);
                    await _unitOfWork.FilprideInventory.AddActualInventory(viewModel, companyClaims, cancellationToken);
                    TempData["success"] = "Actual inventory created successfully";
                    return RedirectToAction(nameof(ActualInventory));
                }
                catch (Exception ex)
                {
                    viewModel.ProductList = await _unitOfWork.GetProductListAsyncById(cancellationToken);

                    viewModel.PO = await _dbContext.FilpridePurchaseOrders
                        .OrderBy(p => p.PurchaseOrderNo)
                        .Where(p => p.Company == companyClaims)
                        .Select(p => new SelectListItem
                        {
                            Value = p.PurchaseOrderId.ToString(),
                            Text = p.PurchaseOrderNo
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.COA = await _dbContext.ChartOfAccounts
                        .Where(coa => coa.Level == 4 && (coa.AccountName.StartsWith("AR-Non Trade Receivable") || coa.AccountName.StartsWith("Cost of Goods Sold") || coa.AccountNumber.StartsWith("6010103")))
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.ProductList = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            viewModel.COA = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 && (coa.AccountName.StartsWith("AR-Non Trade Receivable") || coa.AccountName.StartsWith("Cost of Goods Sold") || coa.AccountNumber.StartsWith("6010103")))
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> ValidateInventory(int? id, CancellationToken cancellationToken)
        {
            try
            {
                if (id == null || id == 0)
                {
                    return NotFound();
                }

                FilprideInventory? inventory = await _dbContext.FilprideInventories
                    .FirstOrDefaultAsync(i => i.InventoryId == id, cancellationToken);

                IEnumerable<FilprideGeneralLedgerBook>? ledgerEntries = await _dbContext.FilprideGeneralLedgerBooks
                    .Where(l => l.Reference == inventory.InventoryId.ToString())
                    .ToListAsync(cancellationToken);

                if (inventory != null || ledgerEntries != null)
                {
                    #region -- Journal Voucher entry --

                    var header = new FilprideJournalVoucherHeader
                    {
                        JournalVoucherHeaderNo = await _unitOfWork.FilprideJournalVoucher.GenerateCodeAsync(inventory.Company, cancellationToken),
                        JVReason = "Actual Inventory",
                        Particulars = inventory.Particular,
                        Date = inventory.Date,
                        CreatedBy = _userManager.GetUserName(this.User),
                        CreatedDate = DateTime.Now,
                        PostedBy = _userManager.GetUserName(this.User),
                        PostedDate = DateTime.Now,
                        Company = inventory.Company
                    };

                    await _dbContext.FilprideJournalVoucherHeaders.AddAsync(header, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    var details = new List<FilprideJournalVoucherDetail>();

                    foreach (var entry in ledgerEntries)
                    {
                        entry.IsPosted = true;
                        entry.Reference = header.JournalVoucherHeaderNo;

                        details.Add(new FilprideJournalVoucherDetail
                        {
                            AccountNo = entry.AccountNo,
                            AccountName = entry.AccountTitle,
                            TransactionNo = header.JournalVoucherHeaderNo,
                            JournalVoucherHeaderId = header.JournalVoucherHeaderId,
                            Debit = entry.Debit,
                            Credit = entry.Credit
                        });
                    }

                    inventory.IsValidated = true;
                    inventory.ValidatedBy = _userManager.GetUserName(this.User);
                    inventory.ValidatedDate = DateTime.Now;

                    await _dbContext.FilprideJournalVoucherDetails.AddRangeAsync(details, cancellationToken);

                    #endregion -- Journal Voucher entry --

                    #region -- Journal Book Entry --

                    var journalBook = new List<FilprideJournalBook>();

                    foreach (var entry in ledgerEntries)
                    {
                        journalBook.Add(new FilprideJournalBook
                        {
                            Date = entry.Date,
                            Reference = header.JournalVoucherHeaderNo,
                            Description = "Actual Inventory",
                            AccountTitle = entry.AccountNo + " " + entry.AccountTitle,
                            Debit = Math.Abs(entry.Debit),
                            Credit = Math.Abs(entry.Credit),
                            CreatedBy = _userManager.GetUserName(this.User),
                            CreatedDate = DateTime.Now,
                            Company = entry.Company,
                        });
                    }
                    await _dbContext.AddRangeAsync(journalBook, cancellationToken);

                    #endregion -- Journal Book Entry --

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return BadRequest();
            }
        }
    }
}