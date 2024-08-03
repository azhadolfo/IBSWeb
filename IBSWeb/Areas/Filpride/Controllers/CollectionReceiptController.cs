using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class CollectionReceiptController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public CollectionReceiptController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var viewData = await _unitOfWork.FilprideCollectionReceipt.GetAllAsync(null, cancellationToken);

            return View(viewData);
        }

        [HttpGet]
        public async Task<IActionResult> SingleCollectionCreateForSales(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideCollectionReceipt();

            viewModel.Customers = await _dbContext.FilprideCustomers
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerId.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SingleCollectionCreateForSales(FilprideCollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            model.Customers = await _dbContext.FilprideCustomers
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerCode.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            model.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => !si.IsPaid && si.CustomerId == model.CustomerId && si.PostedBy != null)
                .OrderBy(si => si.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);

            model.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                #region --Saving default value

                var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                if (computeTotalInModelIfZero == 0)
                {
                    TempData["error"] = "Please input atleast one type form of payment";
                    return View(model);
                }
                var existingSalesInvoice = await _dbContext.FilprideSalesInvoices
                                               .FirstOrDefaultAsync(si => si.SalesInvoiceId == model.SalesInvoiceId, cancellationToken);
                var generateCRNo = await _unitOfWork.FilprideCollectionReceipt.GenerateCodeAsync(cancellationToken);

                model.SINo = existingSalesInvoice.SalesInvoiceNo;
                model.CollectionReceiptNo = generateCRNo;
                model.CreatedBy = _userManager.GetUserName(this.User);
                model.Total = computeTotalInModelIfZero;

                try
                {
                    if (bir2306 != null && bir2306.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2306");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2306.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2306.CopyToAsync(stream);
                        }

                        model.F2306FilePath = fileSavePath;
                        model.IsCertificateUpload = true;
                    }

                    if (bir2307 != null && bir2307.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2307");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2307.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2307.CopyToAsync(stream);
                        }

                        model.F2307FilePath = fileSavePath;
                        model.IsCertificateUpload = true;
                    }
                }
                catch (Exception ex)
                {
                }

                await _dbContext.AddAsync(model, cancellationToken);

                decimal offsetAmount = 0;

                #endregion --Saving default value

                #region --Offsetting function

                var offsettings = new List<FilprideOffsettings>();

                for (int i = 0; i < accountTitle.Length; i++)
                {
                    var currentAccountTitle = accountTitleText[i];
                    var currentAccountAmount = accountAmount[i];
                    offsetAmount += accountAmount[i];

                    var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);

                    offsettings.Add(
                        new FilprideOffsettings
                        {
                            AccountNo = accountTitle[i],
                            AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                            Source = model.CollectionReceiptNo,
                            Reference = model.SINo,
                            Amount = currentAccountAmount,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        }
                    );
                }

                await _dbContext.AddRangeAsync(offsettings, cancellationToken);

                #endregion --Offsetting function

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("CollectionIndex");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MultipleCollectionCreateForSales(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideCollectionReceipt();

            viewModel.Customers = await _dbContext.FilprideCustomers
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerId.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MultipleCollectionCreateForSales(FilprideCollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            model.Customers = await _dbContext.FilprideCustomers
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerName.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            model.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => !si.IsPaid && si.CustomerId == model.CustomerId && si.PostedBy != null)
                .OrderBy(si => si.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);

            model.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                #region --Saving default value

                var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                if (computeTotalInModelIfZero == 0)
                {
                    TempData["error"] = "Please input atleast one type form of payment";
                    return View(model);
                }
                var existingSalesInvoice = await _dbContext.FilprideSalesInvoices
                                               .Where(si => model.MultipleSIId.Contains(si.SalesInvoiceId))
                                               .ToListAsync(cancellationToken);

                model.MultipleSI = new string[model.MultipleSIId.Length];
                var salesInvoice = new FilprideSalesInvoice();
                for (int i = 0; i < model.MultipleSIId.Length; i++)
                {
                    var siId = model.MultipleSIId[i];
                    salesInvoice = await _dbContext.FilprideSalesInvoices
                                .FirstOrDefaultAsync(si => si.SalesInvoiceId == siId);

                    if (salesInvoice != null)
                    {
                        model.MultipleSI[i] = salesInvoice.SalesInvoiceNo;
                    }
                }

                var generateCRNo = await _unitOfWork.FilprideCollectionReceipt.GenerateCodeAsync(cancellationToken);

                model.CollectionReceiptNo = generateCRNo;
                model.CreatedBy = _userManager.GetUserName(this.User);
                model.Total = computeTotalInModelIfZero;

                try
                {
                    if (bir2306 != null && bir2306.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2306");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2306.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2306.CopyToAsync(stream);
                        }

                        model.F2306FilePath = fileSavePath;
                        model.IsCertificateUpload = true;
                    }

                    if (bir2307 != null && bir2307.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2307");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2307.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2307.CopyToAsync(stream);
                        }

                        model.F2307FilePath = fileSavePath;
                        model.IsCertificateUpload = true;
                    }
                }
                catch (Exception ex)
                {
                }

                await _dbContext.AddAsync(model, cancellationToken);

                decimal offsetAmount = 0;

                #endregion --Saving default value

                #region --Offsetting function

                var offsettings = new List<FilprideOffsettings>();

                for (int i = 0; i < accountTitle.Length; i++)
                {
                    var currentAccountTitle = accountTitleText[i];
                    var currentAccountAmount = accountAmount[i];
                    offsetAmount += accountAmount[i];

                    var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);

                    offsettings.Add(
                        new FilprideOffsettings
                        {
                            AccountNo = accountTitle[i],
                            AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                            Source = model.CollectionReceiptNo,
                            Reference = model.SINo,
                            Amount = currentAccountAmount,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        }
                    );
                }

                await _dbContext.AddRangeAsync(offsettings, cancellationToken);

                #endregion --Offsetting function

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("CollectionIndex");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateForService(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideCollectionReceipt();

            viewModel.Customers = await _dbContext.FilprideCustomers
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerId.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateForService(FilprideCollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            model.Customers = await _dbContext.FilprideCustomers
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerId.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            model.SalesInvoices = await _dbContext.FilprideServiceInvoices
                .Where(si => !si.IsPaid && si.CustomerId == model.CustomerId && si.PostedBy != null)
                .OrderBy(si => si.ServiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.CustomerId.ToString(),
                    Text = s.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);

            model.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                #region --Saving default value

                var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                if (computeTotalInModelIfZero == 0)
                {
                    TempData["error"] = "Please input atleast one type form of payment";
                    return View(model);
                }
                var existingServiceInvoice = await _dbContext.FilprideServiceInvoices
                                               .FirstOrDefaultAsync(si => si.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);
                var generateCRNo = await _unitOfWork.FilprideCollectionReceipt.GenerateCodeAsync(cancellationToken);

                model.SVNo = existingServiceInvoice.ServiceInvoiceNo;
                model.CollectionReceiptNo = generateCRNo;
                model.CreatedBy = _userManager.GetUserName(this.User);
                model.Total = computeTotalInModelIfZero;

                try
                {
                    if (bir2306 != null && bir2306.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2306");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2306.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2306.CopyToAsync(stream);
                        }

                        model.F2306FilePath = fileSavePath;
                        model.IsCertificateUpload = true;
                    }

                    if (bir2307 != null && bir2307.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2307");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2307.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2307.CopyToAsync(stream);
                        }

                        model.F2307FilePath = fileSavePath;
                        model.IsCertificateUpload = true;
                    }
                }
                catch (Exception ex)
                {
                }

                await _dbContext.AddAsync(model, cancellationToken);

                decimal offsetAmount = 0;

                #endregion --Saving default value

                #region --Offsetting function

                var offsettings = new List<FilprideOffsettings>();

                for (int i = 0; i < accountTitle.Length; i++)
                {
                    var currentAccountTitle = accountTitleText[i];
                    var currentAccountAmount = accountAmount[i];
                    offsetAmount += accountAmount[i];

                    var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);

                    offsettings.Add(
                        new FilprideOffsettings
                        {
                            AccountNo = accountTitle[i],
                            AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                            Source = model.CollectionReceiptNo,
                            Reference = model.SVNo,
                            Amount = currentAccountAmount,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        }
                    );
                }

                await _dbContext.AddRangeAsync(offsettings, cancellationToken);

                #endregion --Offsetting function

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("CollectionIndex");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var cr = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);
            return View(cr);
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var cr = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);
            return PartialView("_CollectionPreviewPartialView", cr);
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesInvoices(int customerNo, CancellationToken cancellationToken)
        {
            var invoices = await _dbContext
                .FilprideSalesInvoices
                .Where(si => si.CustomerId == customerNo && !si.IsPaid && si.PostedBy != null)
                .OrderBy(si => si.SalesInvoiceId)
                .ToListAsync(cancellationToken);

            var invoiceList = invoices.Select(si => new SelectListItem
            {
                Value = si.SalesInvoiceId.ToString(),   // Replace with your actual ID property
                Text = si.SalesInvoiceNo              // Replace with your actual property for display text
            }).ToList();

            return Json(invoiceList);
        }

        [HttpGet]
        public async Task<IActionResult> GetServiceInvoices(int customerNo, CancellationToken cancellationToken)
        {
            var invoices = await _dbContext
                .FilprideServiceInvoices
                .Where(si => si.CustomerId == customerNo && !si.IsPaid && si.PostedBy != null)
                .OrderBy(si => si.ServiceInvoiceId)
                .ToListAsync(cancellationToken);

            var invoiceList = invoices.Select(si => new SelectListItem
            {
                Value = si.ServiceInvoiceId.ToString(),   // Replace with your actual ID property
                Text = si.ServiceInvoiceNo              // Replace with your actual property for display text
            }).ToList();

            return Json(invoiceList);
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetails(int invoiceNo, bool isSales, bool isServices, CancellationToken cancellationToken)
        {
            if (isSales && !isServices)
            {
                var si = await _dbContext
                .FilprideSalesInvoices
                .FirstOrDefaultAsync(si => si.SalesInvoiceId == invoiceNo, cancellationToken);

                return Json(new
                {
                    Amount = si.NetDiscount.ToString("N2"),
                    AmountPaid = si.AmountPaid.ToString("N2"),
                    Balance = si.Balance.ToString("N2"),
                    Ewt = si.WithHoldingTaxAmount.ToString("N2"),
                    Wvat = si.WithHoldingVatAmount.ToString("N2"),
                    Total = (si.NetDiscount - (si.WithHoldingTaxAmount + si.WithHoldingVatAmount)).ToString("N2")
                });
            }
            else if (isServices && !isSales)
            {
                var sv = await _dbContext
                .FilprideServiceInvoices
                .FirstOrDefaultAsync(si => si.ServiceInvoiceId == invoiceNo, cancellationToken);

                return Json(new
                {
                    Amount = sv.Total.ToString("N2"),
                    AmountPaid = sv.AmountPaid.ToString("N2"),
                    Balance = sv.Balance.ToString("N2"),
                    Ewt = sv.WithholdingTaxAmount.ToString("N2"),
                    Wvat = sv.WithholdingVatAmount.ToString("N2"),
                    Total = (sv.Total - (sv.WithholdingTaxAmount + sv.WithholdingVatAmount)).ToString("N2")
                });
            }
            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> GetMultipleInvoiceDetails(int[] siNo, bool isSales, CancellationToken cancellationToken)
        {
            if (isSales)
            {
                var si = await _dbContext
                .FilprideServiceInvoices
                .FirstOrDefaultAsync(si => siNo.Contains(si.ServiceInvoiceId), cancellationToken);

                //var amountPaid = 0m;
                //var amount = 0m;
                //foreach (var item in si)
                //{
                //    amountPaid = item.AmountPaid;
                //    amount = item.NetDiscount;
                //}

                return Json(new
                {
                    Amount = si.Amount,
                    AmountPaid = si.AmountPaid
                    //Balance = si.Balance.ToString("N2"),
                    //Ewt = si.WithHoldingTaxAmount.ToString("N2"),
                    //Wvat = si.WithHoldingVatAmount.ToString("N2"),
                    //Total = (si.NetDiscount - (si.WithHoldingTaxAmount + si.WithHoldingVatAmount)).ToString("N2")
                });
            }
            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }
            var existingModel = await _dbContext.FilprideCollectionReceipts.FindAsync(id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            existingModel.Customers = await _dbContext.FilprideCustomers
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerId.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            existingModel.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId)
                .OrderBy(si => si.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);

            existingModel.ServiceInvoices = await _dbContext.FilprideServiceInvoices
                .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId)
                .OrderBy(si => si.ServiceInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceInvoiceId.ToString(),
                    Text = s.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);

            existingModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            var findCustomers = await _dbContext.FilprideCustomers
                .FirstOrDefaultAsync(c => c.CustomerId == existingModel.CustomerId, cancellationToken);

            var offsettings = await _dbContext.FilprideOffsettings
                .Where(offset => offset.Source == existingModel.CollectionReceiptNo)
                .ToListAsync(cancellationToken);

            ViewBag.CustomerName = findCustomers?.CustomerName;
            ViewBag.Offsettings = offsettings;

            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilprideCollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == model.CollectionReceiptId, cancellationToken);

            if (ModelState.IsValid)
            {
                #region --Saving default value

                var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                if (computeTotalInModelIfZero == 0)
                {
                    TempData["error"] = "Please input atleast one type form of payment";
                    return View(model);
                }

                existingModel.TransactionDate = model.TransactionDate;
                existingModel.ReferenceNo = model.ReferenceNo;
                existingModel.Remarks = model.Remarks;
                existingModel.CheckDate = model.CheckDate;
                existingModel.CheckNo = model.CheckNo;
                existingModel.CheckBank = model.CheckBank;
                existingModel.CheckBranch = model.CheckBranch;
                existingModel.CashAmount = model.CashAmount;
                existingModel.CheckAmount = model.CheckAmount;
                existingModel.ManagerCheckAmount = model.ManagerCheckAmount;
                existingModel.EWT = model.EWT;
                existingModel.WVAT = model.WVAT;
                existingModel.Total = computeTotalInModelIfZero;

                try
                {
                    if (bir2306 != null && bir2306.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2306");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2306.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2306.CopyToAsync(stream);
                        }

                        existingModel.F2306FilePath = fileSavePath;
                        existingModel.IsCertificateUpload = true;
                    }

                    if (bir2307 != null && bir2307.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2307");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2307.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2307.CopyToAsync(stream);
                        }

                        existingModel.F2307FilePath = fileSavePath;
                        existingModel.IsCertificateUpload = true;
                    }
                }
                catch (Exception ex)
                {
                }

                decimal offsetAmount = 0;

                #endregion --Saving default value

                #region --Offsetting function

                var findOffsettings = await _dbContext.FilprideOffsettings
                .Where(offset => offset.Source == existingModel.CollectionReceiptNo)
                .ToListAsync(cancellationToken);

                var accountTitleSet = new HashSet<string>(accountTitle);

                // Remove records not in accountTitle
                foreach (var offsetting in findOffsettings)
                {
                    if (!accountTitleSet.Contains(offsetting.AccountNo))
                    {
                        _dbContext.FilprideOffsettings.Remove(offsetting);
                    }
                }

                // Dictionary to keep track of AccountNo and their ids for comparison
                var accountTitleDict = new Dictionary<string, List<int>>();
                foreach (var offsetting in findOffsettings)
                {
                    if (!accountTitleDict.ContainsKey(offsetting.AccountNo))
                    {
                        accountTitleDict[offsetting.AccountNo] = new List<int>();
                    }
                    accountTitleDict[offsetting.AccountNo].Add(offsetting.OffSettingId);
                }

                // Add or update records
                for (int i = 0; i < accountTitle.Length; i++)
                {
                    var accountNo = accountTitle[i];
                    var currentAccountTitle = accountTitleText[i];
                    var currentAccountAmount = accountAmount[i];
                    offsetAmount += accountAmount[i];

                    var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);

                    if (accountTitleDict.TryGetValue(accountNo, out var ids))
                    {
                        // Update the first matching record and remove it from the list
                        var offsettingId = ids.First();
                        ids.RemoveAt(0);
                        var offsetting = findOffsettings.First(o => o.OffSettingId == offsettingId);

                        offsetting.AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0];
                        offsetting.Amount = currentAccountAmount;
                        offsetting.CreatedBy = _userManager.GetUserName(this.User);
                        offsetting.CreatedDate = DateTime.Now;

                        if (ids.Count == 0)
                        {
                            accountTitleDict.Remove(accountNo);
                        }
                    }
                    else
                    {
                        // Add new record
                        var newOffsetting = new FilprideOffsettings
                        {
                            AccountNo = accountNo,
                            AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                            Source = existingModel.CollectionReceiptNo,
                            Reference = existingModel.SINo != null ? existingModel.SINo : existingModel.SVNo,
                            Amount = currentAccountAmount,
                            CreatedBy = _userManager.GetUserName(this.User),
                            CreatedDate = DateTime.Now
                        };
                        _dbContext.FilprideOffsettings.Add(newOffsetting);
                    }
                }

                // Remove remaining records that were duplicates
                foreach (var ids in accountTitleDict.Values)
                {
                    foreach (var id in ids)
                    {
                        var offsetting = findOffsettings.First(o => o.OffSettingId == id);
                        _dbContext.FilprideOffsettings.Remove(offsetting);
                    }
                }

                #endregion --Offsetting function

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("CollectionIndex");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        public async Task<IActionResult> Post(int itemId, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == itemId, cancellationToken);

            if (model != null)
            {
                try
                {
                    if (model.PostedBy == null)
                    {
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;

                        List<FilprideOffsettings>? offset = new List<FilprideOffsettings>();

                        if (model.SalesInvoiceId != null)
                        {
                            offset = await _unitOfWork.FilprideCollectionReceipt.GetOffsettings(model.CollectionReceiptNo, model.SINo, cancellationToken);
                        }
                        else
                        {
                            offset = await _unitOfWork.FilprideCollectionReceipt.GetOffsettings(model.CollectionReceiptNo, model.SVNo, cancellationToken);
                        }

                        decimal offsetAmount = 0;

                        #region --General Ledger Book Recording

                        var ledgers = new List<FilprideGeneralLedgerBook>();

                        if (model.CashAmount > 0 || model.CheckAmount > 0 || model.ManagerCheckAmount > 0)
                        {
                            ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CollectionReceiptNo,
                                        Description = "Collection for Receivable",
                                        AccountNo = "1010101",
                                        AccountTitle = "Cash in Bank",
                                        Debit = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount,
                                        Credit = 0,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                        }

                        if (model.EWT > 0)
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CollectionReceiptNo,
                                    Description = "Collection for Receivable",
                                    AccountNo = "1010604",
                                    AccountTitle = "Creditable Withholding Tax",
                                    Debit = model.EWT,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (model.WVAT > 0)
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CollectionReceiptNo,
                                    Description = "Collection for Receivable",
                                    AccountNo = "1010605",
                                    AccountTitle = "Creditable Withholding Vat",
                                    Debit = model.WVAT,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (offset != null)
                        {
                            foreach (var item in offset)
                            {
                                ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CollectionReceiptNo,
                                    Description = "Collection for Receivable",
                                    AccountNo = item.AccountNo,
                                    AccountTitle = item.AccountTitle,
                                    Debit = item.Amount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                                );

                                offsetAmount += item.Amount;
                            }
                        }

                        if (model.CashAmount > 0 || model.CheckAmount > 0 || model.ManagerCheckAmount > 0 || offsetAmount > 0)
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CollectionReceiptNo,
                                    Description = "Collection for Receivable",
                                    AccountNo = "1010201",
                                    AccountTitle = "AR-Trade Receivable",
                                    Debit = 0,
                                    Credit = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + offsetAmount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (model.EWT > 0)
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CollectionReceiptNo,
                                    Description = "Collection for Receivable",
                                    AccountNo = "1010202",
                                    AccountTitle = "Deferred Creditable Withholding Tax",
                                    Debit = 0,
                                    Credit = model.EWT,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (model.WVAT > 0)
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CollectionReceiptNo,
                                    Description = "Collection for Receivable",
                                    AccountNo = "1010203",
                                    AccountTitle = "Deferred Creditable Withholding Vat",
                                    Debit = 0,
                                    Credit = model.WVAT,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (!_unitOfWork.FilprideCollectionReceipt.IsJournalEntriesBalanced(ledgers))
                        {
                            throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                        }

                        await _dbContext.AddRangeAsync(ledgers, cancellationToken);

                        #endregion --General Ledger Book Recording

                        #region --Cash Receipt Book Recording

                        var crb = new List<FilprideCashReceiptBook>();

                        crb.Add(
                            new FilprideCashReceiptBook
                            {
                                Date = model.TransactionDate,
                                RefNo = model.CollectionReceiptNo,
                                CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.CustomerName : model.MultipleSIId != null ? model.Customer.CustomerName : model.ServiceInvoice.Customer.CustomerName,
                                Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                COA = "1010101 Cash in Bank",
                                Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SalesInvoiceNo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.ServiceInvoiceNo,
                                Debit = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }

                        );

                        if (model.EWT > 0)
                        {
                            crb.Add(
                                new FilprideCashReceiptBook
                                {
                                    Date = model.TransactionDate,
                                    RefNo = model.CollectionReceiptNo,
                                    CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.CustomerName : model.MultipleSIId != null ? model.Customer.CustomerName : model.ServiceInvoice.Customer.CustomerName,
                                    Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                    CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                    COA = "1010604 Creditable Withholding Tax",
                                    Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SalesInvoiceNo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.ServiceInvoiceNo,
                                    Debit = model.EWT,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (model.WVAT > 0)
                        {
                            crb.Add(
                                new FilprideCashReceiptBook
                                {
                                    Date = model.TransactionDate,
                                    RefNo = model.CollectionReceiptNo,
                                    CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.CustomerName : model.MultipleSIId != null ? model.Customer.CustomerName : model.ServiceInvoice.Customer.CustomerName,
                                    Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                    CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                    COA = "1010605 Creditable Withholding Vat",
                                    Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SalesInvoiceNo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.ServiceInvoiceNo,
                                    Debit = model.WVAT,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (offset != null)
                        {
                            foreach (var item in offset)
                            {
                                crb.Add(
                                    new FilprideCashReceiptBook
                                    {
                                        Date = model.TransactionDate,
                                        RefNo = model.CollectionReceiptNo,
                                        CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.CustomerName : model.MultipleSIId != null ? model.Customer.CustomerName : model.ServiceInvoice.Customer.CustomerName,
                                        Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                        CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                        COA = item.AccountNo,
                                        Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SalesInvoiceNo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.ServiceInvoiceNo,
                                        Debit = item.Amount,
                                        Credit = 0,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                        }

                        crb.Add(
                        new FilprideCashReceiptBook
                        {
                            Date = model.TransactionDate,
                            RefNo = model.CollectionReceiptNo,
                            CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.CustomerName : model.MultipleSIId != null ? model.Customer.CustomerName : model.ServiceInvoice.Customer.CustomerName,
                            Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                            CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                            COA = "1010201 AR-Trade Receivable",
                            Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SalesInvoiceNo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.ServiceInvoiceNo,
                            Debit = 0,
                            Credit = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + offsetAmount,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        }
                        );

                        if (model.EWT > 0)
                        {
                            crb.Add(
                                new FilprideCashReceiptBook
                                {
                                    Date = model.TransactionDate,
                                    RefNo = model.CollectionReceiptNo,
                                    CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.CustomerName : model.MultipleSIId != null ? model.Customer.CustomerName : model.ServiceInvoice.Customer.CustomerName,
                                    Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                    CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                    COA = "1010202 Deferred Creditable Withholding Tax",
                                    Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SalesInvoiceNo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.ServiceInvoiceNo,
                                    Debit = 0,
                                    Credit = model.EWT,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (model.WVAT > 0)
                        {
                            crb.Add(
                                new FilprideCashReceiptBook
                                {
                                    Date = model.TransactionDate,
                                    RefNo = model.CollectionReceiptNo,
                                    CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.CustomerName : model.MultipleSIId != null ? model.Customer.CustomerName : model.ServiceInvoice.Customer.CustomerName,
                                    Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                    CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                    COA = "1010203 Deferred Creditable Withholding Vat",
                                    Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SalesInvoiceNo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.ServiceInvoiceNo,
                                    Debit = 0,
                                    Credit = model.WVAT,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        await _dbContext.AddRangeAsync(crb, cancellationToken);

                        #endregion --Cash Receipt Book Recording

                        if (model.SalesInvoiceId != null)
                        {
                            await _unitOfWork.FilprideCollectionReceipt.UpdateInvoice(model.SalesInvoice.SalesInvoiceId, model.Total, offsetAmount, cancellationToken);
                        }
                        else if (model.MultipleSIId != null)
                        {
                            await _unitOfWork.FilprideCollectionReceipt.UpdateMutipleInvoice(model.MultipleSI, model.SIMultipleAmount, offsetAmount, cancellationToken);
                        }
                        else
                        {
                            await _unitOfWork.FilprideCollectionReceipt.UpdateSV(model.ServiceInvoice.ServiceInvoiceId, model.Total, offsetAmount, cancellationToken);
                        }

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Collection Receipt has been Posted.";
                    }

                    return RedirectToAction("CollectionIndex");
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction("CollectionIndex");
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int itemId, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == itemId, cancellationToken);

            if (model != null)
            {
                if (model.VoidedBy == null)
                {
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        if (model.PostedBy != null)
                        {
                            model.PostedBy = null;
                        }

                        model.VoidedBy = _userManager.GetUserName(this.User);
                        model.VoidedDate = DateTime.Now;
                        var series = model.SINo != null ? model.SINo : model.SVNo;

                        var findOffsetting = await _dbContext.FilprideOffsettings.Where(offset => offset.Source == model.CollectionReceiptNo && offset.Reference == series).ToListAsync(cancellationToken);

                        ///PENDING - further discussion
                        //await _generalRepo.RemoveRecords<CashReceiptBook>(crb => crb.RefNo == model.CRNo, cancellationToken);
                        //await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.CRNo, cancellationToken);

                        ///PENDING - further discussion
                        //if (findOffsetting.Any())
                        //{
                        //    await _generalRepo.RemoveRecords<Offsetting>(offset => offset.Source == model.CRNo && offset.Reference == series, cancellationToken);
                        //}
                        if (series.Contains("SI"))
                        {
                            await _unitOfWork.FilprideCollectionReceipt.RemoveSIPayment(model.SalesInvoice.SalesInvoiceId, model.Total, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                        }
                        else
                        {
                            await _unitOfWork.FilprideCollectionReceipt.RemoveSVPayment(model.ServiceInvoice.ServiceInvoiceId, model.Total, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                        }

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync();
                        TempData["success"] = "Collection Receipt has been Voided.";
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        TempData["error"] = ex.Message;
                    }
                }
                return RedirectToAction("CollectionIndex");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int itemId, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCollectionReceipts.FindAsync(itemId, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    ///PENDING
                    //model.CancellationRemarks = cancellationRemarks;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Collection Receipt has been Cancelled.";
                }
                return RedirectToAction("CollectionIndex");
            }

            return NotFound();
        }
    }
}