using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
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
    public class ReceivingReportsController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        public ReceivingReportsController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
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

            var rr = await _unitOfWork.FilprideReceivingReportRepo
                .GetAllAsync(rr => rr.Company == companyClaims, cancellationToken);

            return View(rr);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new ReceivingReport();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.PurchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => po.Company == companyClaims && !po.IsReceived && po.PostedBy != null && !po.IsClosed)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReceivingReport model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            model.PurchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => po.Company == companyClaims && !po.IsReceived && po.PostedBy != null)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
            if (ModelState.IsValid)
            {
                #region --Retrieve PO

                var po = await _dbContext
                            .PurchaseOrders
                            .Include(po => po.Supplier)
                            .Include(po => po.Product)
                            .FirstOrDefaultAsync(po => po.PurchaseOrderId == model.POId, cancellationToken);

                #endregion --Retrieve PO

                var rr = _dbContext.ReceivingReports
                .Where(rr => rr.Company == companyClaims && rr.PONo == po.PurchaseOrderNo)
                .ToList();

                var totalAmountRR = po.Quantity - po.QuantityReceived;

                if (model.QuantityDelivered > totalAmountRR)
                {
                    TempData["error"] = "Input is exceed to remaining quantity delivered";
                    return View(model);
                }

                model.ReceivingReportNo = await _unitOfWork.FilprideReceivingReportRepo.GenerateCodeAsync(companyClaims, cancellationToken);
                model.CreatedBy = _userManager.GetUserName(this.User);
                model.GainOrLoss = model.QuantityReceived - model.QuantityDelivered;

                var existingPo = await _unitOfWork.FilpridePurchaseOrderRepo.GetAsync(po => po.PurchaseOrderId == model.POId, cancellationToken);

                model.PONo = existingPo.PurchaseOrderNo;

                model.DueDate = await _unitOfWork.FilprideReceivingReportRepo.ComputeDueDateAsync(model.POId, model.Date, cancellationToken);

                if (po.Supplier.VatType == "Vatable")
                {
                    model.Amount = model.QuantityDelivered < model.QuantityReceived ? model.QuantityDelivered * po.Price : model.QuantityReceived * po.Price;
                    model.NetAmount = model.Amount / 1.12m;
                    model.VatAmount = model.NetAmount * .12m;
                }
                else
                {
                    model.Amount = model.QuantityDelivered < model.QuantityReceived ? model.QuantityDelivered * po.Price : model.QuantityReceived * po.Price;
                    model.NetAmount = model.Amount;
                }

                if (po.Supplier.TaxType == "Withholding Tax")
                {
                    model.EwtAmount = model.NetAmount * .01m;
                    model.NetAmountOfEWT = model.Amount - model.EwtAmount;
                }

                model.Company = companyClaims;

                await _dbContext.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Receiving Report created successfully";

                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.ReceivingReports == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var receivingReport = await _dbContext.ReceivingReports.FindAsync(id, cancellationToken);
            if (receivingReport == null)
            {
                return NotFound();
            }

            receivingReport.PurchaseOrders = await _dbContext.PurchaseOrders
                .Where(rr => rr.Company == companyClaims)
                .Select(s => new SelectListItem
                {
                    Value = s.PurchaseOrderId.ToString(),
                    Text = s.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            return View(receivingReport);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ReceivingReport model, CancellationToken cancellationToken)
        {
            var existingModel = await _dbContext.ReceivingReports.FindAsync(model.ReceivingReportId, cancellationToken);
            var companyClaims = await GetCompanyClaimAsync();

            existingModel.PurchaseOrders = await _dbContext.PurchaseOrders
                .Where(s => s.Company == companyClaims)
                .Select(s => new SelectListItem
                {
                    Value = s.PurchaseOrderId.ToString(),
                    Text = s.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                if (existingModel == null)
                {
                    return NotFound();
                }

                #region --Retrieve PO

                var po = await _dbContext
                            .PurchaseOrders
                            .Include(po => po.Supplier)
                            .Include(po => po.Product)
                            .FirstOrDefaultAsync(po => po.PurchaseOrderId == model.POId, cancellationToken);

                #endregion --Retrieve PO

                var rr = _dbContext.ReceivingReports
                .Where(rr => rr.Company == companyClaims && rr.PONo == po.PurchaseOrderNo)
                .ToList();

                var totalAmountRR = po.Quantity - po.QuantityReceived;

                if (model.QuantityDelivered > totalAmountRR)
                {
                    TempData["error"] = "Input is exceed to remaining quantity delivered";
                    return View(model);
                }

                existingModel.Date = model.Date;
                existingModel.POId = model.POId;

                var existingPo = await _unitOfWork.FilpridePurchaseOrderRepo.GetAsync(po => po.PurchaseOrderId == model.POId);

                existingModel.PONo = existingPo.PurchaseOrderNo;

                existingModel.DueDate = await _unitOfWork.FilprideReceivingReportRepo.ComputeDueDateAsync(model.POId, model.Date, cancellationToken);
                existingModel.SupplierInvoiceNumber = model.SupplierInvoiceNumber;
                existingModel.SupplierInvoiceDate = model.SupplierInvoiceDate;
                existingModel.TruckOrVessels = model.TruckOrVessels;
                existingModel.QuantityDelivered = model.QuantityDelivered;
                existingModel.QuantityReceived = model.QuantityReceived;
                existingModel.GainOrLoss = model.QuantityReceived - model.QuantityDelivered;
                existingModel.OtherRef = model.OtherRef;
                existingModel.Remarks = model.Remarks;

                if (po.Supplier.VatType == "Vatable")
                {
                    existingModel.Amount = model.QuantityReceived * po.Price;
                    existingModel.NetAmount = existingModel.Amount / 1.12m;
                    existingModel.VatAmount = existingModel.NetAmount * .12m;
                }
                else
                {
                    existingModel.Amount = model.QuantityReceived * po.Price;
                    existingModel.NetAmount = existingModel.Amount;
                }

                if (po.Supplier.TaxType == "Withholding Tax")
                {
                    existingModel.EwtAmount = existingModel.NetAmount * .01m;
                }

                existingModel.EditedBy = _userManager.GetUserName(User);
                existingModel.EditedDate = DateTime.Now;

                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Receiving Report updated successfully";
                return RedirectToAction("Index");
            }

            return View(existingModel);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.ReceivingReports == null)
            {
                return NotFound();
            }

            var receivingReport = await _unitOfWork.FilprideReceivingReportRepo.GetAsync(rr => rr.ReceivingReportId == id, cancellationToken);

            if (receivingReport == null)
            {
                return NotFound();
            }

            return View(receivingReport);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideReceivingReportRepo.GetAsync(rr => rr.ReceivingReportId == id, cancellationToken);

            if (model != null)
            {
                try
                {
                    if (model.PostedBy == null)
                    {
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;

                        #region --General Ledger Recording

                        var ledgers = new List<FilprideGeneralLedgerBook>();

                        if (model.PurchaseOrder.Product.ProductName == "Biodiesel")
                        {
                            ledgers.Add(new FilprideGeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.ReceivingReportNo,
                                Description = "Receipt of Goods",
                                AccountNo = "1010401",
                                AccountTitle = "Inventory - Biodiesel",
                                Debit = model.NetAmount,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate,
                                Company = model.Company
                            });
                        }
                        else if (model.PurchaseOrder.Product.ProductName == "Econogas")
                        {
                            ledgers.Add(new FilprideGeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.ReceivingReportNo,
                                Description = "Receipt of Goods",
                                AccountNo = "1010402",
                                AccountTitle = "Inventory - Econogas",
                                Debit = model.NetAmount,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate,
                                Company = model.Company
                            });
                        }
                        else
                        {
                            ledgers.Add(new FilprideGeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.ReceivingReportNo,
                                Description = "Receipt of Goods",
                                AccountNo = "1010403",
                                AccountTitle = "Inventory - Envirogas",
                                Debit = model.NetAmount,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate,
                                Company = model.Company
                            });
                        }

                        if (model.VatAmount > 0)
                        {
                            ledgers.Add(new FilprideGeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.ReceivingReportNo,
                                Description = "Receipt of Goods",
                                AccountNo = "1010602",
                                AccountTitle = "Vat Input",
                                Debit = model.VatAmount,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate,
                                Company = model.Company
                            });
                        }

                        if (model.EwtAmount > 0)
                        {
                            ledgers.Add(new FilprideGeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.ReceivingReportNo,
                                Description = "Receipt of Goods",
                                AccountNo = "2010302",
                                AccountTitle = "Expanded Withholding Tax 1%",
                                Debit = 0,
                                Credit = model.EwtAmount,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate,
                                Company = model.Company
                            });
                        }

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = model.Date,
                            Reference = model.ReceivingReportNo,
                            Description = "Receipt of Goods",
                            AccountNo = "2010101",
                            AccountTitle = "AP-Trade Payable",
                            Debit = 0,
                            Credit = model.Amount - model.EwtAmount,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate,
                            Company = model.Company
                        });

                        if (!_unitOfWork.FilprideReceivingReportRepo.IsJournalEntriesBalanced(ledgers))
                        {
                            throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                        }

                        await _dbContext.AddRangeAsync(ledgers, cancellationToken);

                        #endregion --General Ledger Recording

                        #region--Inventory Recording

                        await _unitOfWork.FilprideInventory.AddPurchaseToInventoryAsync(model, cancellationToken);

                        #endregion

                        await _unitOfWork.FilprideReceivingReportRepo.UpdatePOAsync(model.PurchaseOrder.PurchaseOrderId, model.QuantityReceived, cancellationToken);

                        #region --Purchase Book Recording

                        var purchaseBook = new List<FilpridePurchaseBook>();

                        purchaseBook.Add(new FilpridePurchaseBook
                        {
                            Date = model.Date,
                            SupplierName = model.PurchaseOrder.Supplier.SupplierName,
                            SupplierTin = model.PurchaseOrder.Supplier.SupplierTin,
                            SupplierAddress = model.PurchaseOrder.Supplier.SupplierAddress,
                            DocumentNo = model.ReceivingReportNo,
                            Description = model.PurchaseOrder.Product.ProductName,
                            Amount = model.Amount,
                            VatAmount = model.VatAmount,
                            WhtAmount = model.EwtAmount,
                            NetPurchases = model.NetAmount,
                            CreatedBy = model.CreatedBy,
                            PONo = model.PurchaseOrder.PurchaseOrderNo,
                            DueDate = model.DueDate,
                            Company = model.Company
                        });

                        await _dbContext.AddRangeAsync(purchaseBook, cancellationToken);
                        #endregion --Purchase Book Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Receiving Report has been Posted.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ReceivingReports
                .FindAsync(id, cancellationToken);

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

                    ///PENDING - leo
                    //await _generalRepo.RemoveRecords<PurchaseJournalBook>(pb => pb.DocumentNo == model.RRNo, cancellationToken);
                    //await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.RRNo, cancellationToken);
                    //await _generalRepo.RemoveRecords<Inventory>(i => i.Reference == model.RRNo, cancellationToken);
                    await _unitOfWork.FilprideReceivingReportRepo.RemoveQuantityReceived(model.POId, model.QuantityReceived, cancellationToken);
                    model.QuantityReceived = 0;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Receiving Report has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ReceivingReports.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.CanceledQuantity = model.QuantityDelivered < model.QuantityReceived ? model.QuantityDelivered : model.QuantityReceived;
                    model.QuantityDelivered = 0;
                    model.QuantityReceived = 0;

                    ///PENDING - leo
                    //model.CancellationRemarks = cancellationRemarks;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Receiving Report has been Cancelled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetLiquidations(int id, CancellationToken cancellationToken)
        {
            var po = await _unitOfWork.FilpridePurchaseOrderRepo.GetAsync(po => po.PurchaseOrderId == id, cancellationToken);

            var rrPostedOnly = await _dbContext
                .ReceivingReports
                .Where(rr => rr.Company == po.Company && rr.PONo == po.PurchaseOrderNo && rr.PostedBy != null)
                .ToListAsync(cancellationToken);

            var rr = await _dbContext
                .ReceivingReports
                .Where(rr => rr.Company == po.Company && rr.PONo == po.PurchaseOrderNo)
                .ToListAsync(cancellationToken);

            var rrNotPosted = await _dbContext
                .ReceivingReports
                .Where(rr => rr.Company == po.Company && rr.PONo == po.PurchaseOrderNo && rr.PostedBy == null && rr.CanceledBy == null)
                .ToListAsync(cancellationToken);

            var rrCanceled = await _dbContext
                .ReceivingReports
                .Where(rr => rr.Company == po.Company && rr.PONo == po.PurchaseOrderNo && rr.CanceledBy != null)
                .ToListAsync(cancellationToken);

            if (po != null)
            {
                return Json(new
                {
                    poNo = po.PurchaseOrderNo,
                    poQuantity = po.Quantity.ToString("N4"),
                    rrList = rr,
                    rrListPostedOnly = rrPostedOnly,
                    rrListNotPosted = rrNotPosted,
                    rrListCanceled = rrCanceled
                });
            }
            else
            {
                return Json(null);
            }
        }
    }
}