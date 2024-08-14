using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
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
    public class ServiceInvoiceController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        public ServiceInvoiceController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
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

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var results = await _unitOfWork.FilprideServiceInvoice
                .GetAllAsync(s => s.Company == companyClaims, cancellationToken);

            return View(results);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideServiceInvoice();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.Customers = await _dbContext.FilprideCustomers
                .Where(c => c.Company == companyClaims)
                .OrderBy(c => c.CustomerId)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName
                })
                .ToListAsync(cancellationToken);
            viewModel.Services = await _dbContext.FilprideServices
                .Where(s => s.Company == companyClaims)
                .OrderBy(s => s.ServiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(FilprideServiceInvoice model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            model.Customers = await _dbContext.FilprideCustomers
                .Where(c => c.Company == companyClaims)
                .OrderBy(c => c.CustomerId)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName
                })
                .ToListAsync(cancellationToken);
            model.Services = await _dbContext.FilprideServices
                .Where(s => s.Company == companyClaims)
                .OrderBy(s => s.ServiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);
            if (ModelState.IsValid)
            {
                #region --Retrieval of Services

                var services = await _dbContext.FilprideServices.FindAsync(model.ServiceId, cancellationToken);

                #endregion --Retrieval of Services

                #region --Retrieval of Customer

                var customer = await _unitOfWork.FilprideCustomer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

                #endregion --Retrieval of Customer

                #region --Saving the default properties

                model.ServiceInvoiceNo = await _unitOfWork.FilprideServiceInvoice.GenerateCodeAsync(companyClaims, cancellationToken);

                model.CreatedBy = _userManager.GetUserName(this.User);

                model.Total = model.Amount;

                model.Company = companyClaims;

                if (customer.CustomerType == "Vatable")
                {
                    model.NetAmount = (model.Total - model.Discount) / 1.12m;
                    model.VatAmount = (model.Total - model.Discount) - model.NetAmount;
                    model.WithholdingTaxAmount = model.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        model.WithholdingVatAmount = model.NetAmount * 0.05m;
                    }
                }
                else
                {
                    model.NetAmount = model.Total - model.Discount;
                    model.WithholdingTaxAmount = model.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        model.WithholdingVatAmount = model.NetAmount * 0.05m;
                    }
                }

                if (DateOnly.FromDateTime(model.CreatedDate) < model.Period)
                {
                    model.UnearnedAmount += model.Amount;
                }
                else
                {
                    model.CurrentAndPreviousAmount += model.Amount;
                }

                if (customer.CustomerType == "Vatable")
                {
                    model.CurrentAndPreviousAmount = Math.Round(model.CurrentAndPreviousAmount / 1.12m, 4);
                    model.UnearnedAmount = Math.Round(model.UnearnedAmount / 1.12m, 4);

                    var total = model.CurrentAndPreviousAmount + model.UnearnedAmount;

                    var roundedNetAmount = Math.Round(model.NetAmount, 4);

                    if (roundedNetAmount > total)
                    {
                        var shortAmount = model.NetAmount - total;

                        model.CurrentAndPreviousAmount += shortAmount;
                    }
                }

                _dbContext.Add(model);

                #endregion --Saving the default properties

                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(model);
        }

        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var soa = await _unitOfWork.FilprideServiceInvoice
                .GetAsync(s => s.ServiceInvoiceId == id, cancellationToken);

            return View(soa);
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var soa = await _unitOfWork.FilprideServiceInvoice
                .GetAsync(s => s.ServiceInvoiceId == id, cancellationToken);

            return PartialView("_PreviewPartialView", soa);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideServiceInvoice
                .GetAsync(s => s.ServiceInvoiceId == id, cancellationToken);

            try
            {
                if (model != null)
                {
                    if (model.PostedBy == null)
                    {
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;

                        #region --Retrieval of Services

                        var services = await _dbContext.FilprideServices.FindAsync(model.ServiceId, cancellationToken);

                        #endregion --Retrieval of Services

                        #region --Retrieval of Customer

                        var customer = await _unitOfWork.FilprideCustomer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

                        #endregion --Retrieval of Customer

                        #region --SV Computation--

                        var postedDate = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                        if (customer.VatType == "Vatable")
                        {
                            model.Total = model.Amount - model.Discount;
                            model.NetAmount = _unitOfWork.FilprideServiceInvoice.ComputeNetOfVat(model.Total);
                            model.VatAmount = _unitOfWork.FilprideServiceInvoice.ComputeVatAmount(model.Total);
                            model.WithholdingTaxAmount = Math.Round(model.NetAmount * (customer.WithHoldingTax ? services.Percent / 100m : 0), 4);
                            if (customer.WithHoldingVat)
                            {
                                model.WithholdingVatAmount = Math.Round(model.NetAmount * 0.05m, 4);
                            }
                        }
                        else
                        {
                            model.NetAmount = model.Amount - model.Discount;
                            model.WithholdingTaxAmount = model.NetAmount * (customer.WithHoldingTax ? services.Percent / 100m : 0);
                            if (customer.WithHoldingVat)
                            {
                                model.WithholdingVatAmount = model.NetAmount * 0.05m;
                            }
                        }

                        #endregion --SV Computation--

                        #region --Sales Book Recording

                        var sales = new FilprideSalesBook();

                        if (model.Customer.VatType == "Vatable")
                        {
                            sales.TransactionDate = postedDate;
                            sales.SerialNo = model.ServiceInvoiceNo;
                            sales.SoldTo = model.Customer.CustomerName;
                            sales.TinNo = model.Customer.CustomerTin;
                            sales.Address = model.Customer.CustomerAddress;
                            sales.Description = model.Service.Name;
                            sales.Amount = model.Total;
                            sales.VatAmount = model.VatAmount;
                            sales.VatableSales = model.Total / 1.12m;
                            sales.Discount = model.Discount;
                            sales.NetSales = model.NetAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.ServiceInvoiceId;
                            sales.Company = model.Company;
                        }
                        else if (model.Customer.VatType == "Exempt")
                        {
                            sales.TransactionDate = postedDate;
                            sales.SerialNo = model.ServiceInvoiceNo;
                            sales.SoldTo = model.Customer.CustomerName;
                            sales.TinNo = model.Customer.CustomerTin;
                            sales.Address = model.Customer.CustomerAddress;
                            sales.Description = model.Service.Name;
                            sales.Amount = model.Total;
                            sales.VatExemptSales = model.Total;
                            sales.Discount = model.Discount;
                            sales.NetSales = model.NetAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.ServiceInvoiceId;
                            sales.Company = model.Company;
                        }
                        else
                        {
                            sales.TransactionDate = postedDate;
                            sales.SerialNo = model.ServiceInvoiceNo;
                            sales.SoldTo = model.Customer.CustomerName;
                            sales.TinNo = model.Customer.CustomerTin;
                            sales.Address = model.Customer.CustomerAddress;
                            sales.Description = model.Service.Name;
                            sales.Amount = model.Total;
                            sales.ZeroRated = model.Total;
                            sales.Discount = model.Discount;
                            sales.NetSales = model.NetAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.ServiceInvoiceId;
                            sales.Company = model.Company;
                        }

                        await _dbContext.AddAsync(sales, cancellationToken);

                        #endregion --Sales Book Recording

                        #region --General Ledger Book Recording

                        var ledgers = new List<FilprideGeneralLedgerBook>();

                        ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = postedDate,
                                    Reference = model.ServiceInvoiceNo,
                                    Description = model.Service.Name,
                                    AccountNo = "1010204",
                                    AccountTitle = "AR-Non Trade Receivable",
                                    Debit = Math.Round(model.Total - (model.WithholdingTaxAmount + model.WithholdingVatAmount), 4),
                                    Credit = 0,
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        if (model.WithholdingTaxAmount > 0)
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = postedDate,
                                    Reference = model.ServiceInvoiceNo,
                                    Description = model.Service.Name,
                                    AccountNo = "1010202",
                                    AccountTitle = "Deferred Creditable Withholding Tax",
                                    Debit = model.WithholdingTaxAmount,
                                    Credit = 0,
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (model.WithholdingVatAmount > 0)
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = postedDate,
                                    Reference = model.ServiceInvoiceNo,
                                    Description = model.Service.Name,
                                    AccountNo = "1010203",
                                    AccountTitle = "Deferred Creditable Withholding Vat",
                                    Debit = model.WithholdingVatAmount,
                                    Credit = 0,
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        ledgers.Add(
                               new FilprideGeneralLedgerBook
                               {
                                   Date = postedDate,
                                   Reference = model.ServiceInvoiceNo,
                                   Description = model.Service.Name,
                                   AccountNo = model.Service.CurrentAndPreviousNo,
                                   AccountTitle = model.Service.CurrentAndPreviousTitle,
                                   Debit = 0,
                                   Credit = Math.Round((model.NetAmount), 4),
                                   Company = model.Company,
                                   CreatedBy = model.CreatedBy,
                                   CreatedDate = model.CreatedDate
                               }
                           );

                        if (model.VatAmount > 0)
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = postedDate,
                                    Reference = model.ServiceInvoiceNo,
                                    Description = model.Service.Name,
                                    AccountNo = "2010301",
                                    AccountTitle = "Vat Output",
                                    Debit = 0,
                                    Credit = Math.Round((model.VatAmount), 4),
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (!_unitOfWork.FilprideServiceInvoice.IsJournalEntriesBalanced(ledgers))
                        {
                            throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                        }

                        await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                        #endregion --General Ledger Book Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Service invoice has been posted.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Index");
            }

            return null;
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideServiceInvoices.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    ///PENDING
                    //model.CancellationRemarks = cancellationRemarks;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Service invoice has been Cancelled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideServiceInvoices.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.VoidedBy == null)
                {
                    if (model.PostedBy != null)
                    {
                        model.PostedBy = null;
                    }

                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;

                    ///PENDING - further discussion
                    //await _generalRepo.RemoveRecords<SalesBook>(gl => gl.SerialNo == model.SVNo, cancellationToken);
                    //await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.SVNo, cancellationToken);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Service invoice has been voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }
            var existingModel = await _unitOfWork.FilprideServiceInvoice.GetAsync(sv => sv.ServiceInvoiceId == id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            existingModel.Customers = await _dbContext.FilprideCustomers
                .OrderBy(c => c.CustomerId)
                .Where(c => c.Company == existingModel.Company)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName
                })
                .ToListAsync(cancellationToken);
            existingModel.Services = await _dbContext.FilprideServices
                .OrderBy(s => s.ServiceId)
                .Where(s => s.Company == existingModel.Company)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);

            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilprideServiceInvoice model, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.FilprideServiceInvoice.GetAsync(s => s.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                #region --Retrieval of Services

                var services = await _dbContext.FilprideServices.FindAsync(model.ServiceId, cancellationToken);

                #endregion --Retrieval of Services

                #region --Retrieval of Customer

                var customer = await _dbContext.FilprideCustomers.FindAsync(model.CustomerId, cancellationToken);

                #endregion --Retrieval of Customer

                #region --Saving the default properties

                existingModel.Discount = model.Discount;
                existingModel.Amount = model.Amount;
                existingModel.Period = model.Period;
                existingModel.DueDate = model.DueDate;
                existingModel.Instructions = model.Instructions;

                decimal total = 0;
                total += model.Amount;
                existingModel.Total = total;

                if (customer.CustomerType == "Vatable")
                {
                    existingModel.NetAmount = (existingModel.Total - existingModel.Discount) / 1.12m;
                    existingModel.VatAmount = (existingModel.Total - existingModel.Discount) - existingModel.NetAmount;
                    existingModel.WithholdingTaxAmount = existingModel.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        existingModel.WithholdingVatAmount = existingModel.NetAmount * 0.05m;
                    }
                }
                else
                {
                    existingModel.NetAmount = existingModel.Total - existingModel.Discount;
                    existingModel.WithholdingTaxAmount = existingModel.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        existingModel.WithholdingVatAmount = existingModel.NetAmount * 0.05m;
                    }
                }

                #endregion --Saving the default properties

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("Index");
            }

            return View(existingModel);
        }
    }
}