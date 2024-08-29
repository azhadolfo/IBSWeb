﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_Logistics, SD.Department_TradeAndSupply, SD.Department_Marketing, SD.Department_RCD, SD.Department_CreditAndCollection)]
    public class ReceivingReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        public ReceivingReportController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
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

        [HttpPost]
        public async Task<IActionResult> GetReceivingReports([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var receivingReports = await _unitOfWork.FilprideReceivingReportRepository
                    .GetAllAsync(rr => rr.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    receivingReports = receivingReports
                    .Where(s =>
                        s.ReceivingReportNo.ToLower().Contains(searchValue) ||
                        s.PurchaseOrder.PurchaseOrderNo.ToLower().Contains(searchValue) ||
                        s.SupplierInvoiceNumber?.ToLower().Contains(searchValue) == true ||
                        s.SupplierInvoiceDate?.ToString().Contains(searchValue) == true ||
                        s.SupplierDrNo?.ToLower().Contains(searchValue) == true ||
                        s.WithdrawalCertificate?.ToLower().Contains(searchValue) == true ||
                        s.TruckOrVessels.ToLower().Contains(searchValue) ||
                        s.Date.ToString().Contains(searchValue) ||
                        s.QuantityReceived.ToString().Contains(searchValue) ||
                        s.QuantityDelivered.ToString().Contains(searchValue) ||
                        s.Amount.ToString().Contains(searchValue) ||
                        s.OtherRef?.ToLower().Contains(searchValue) == null ||
                        s.Remarks.ToLower().Contains(searchValue) ||
                        s.CreatedBy.ToLower().Contains(searchValue)
                        )
                    .ToList();

                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    receivingReports = receivingReports
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = receivingReports.Count();

                var pagedData = receivingReports
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
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideReceivingReport();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.PurchaseOrders = await _dbContext.FilpridePurchaseOrders
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
        public async Task<IActionResult> Create(FilprideReceivingReport model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            model.PurchaseOrders = await _dbContext.FilpridePurchaseOrders
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
                            .FilpridePurchaseOrders
                            .Include(po => po.Supplier)
                            .Include(po => po.Product)
                            .FirstOrDefaultAsync(po => po.PurchaseOrderId == model.POId, cancellationToken);

                #endregion --Retrieve PO

                var rr = _dbContext.FilprideReceivingReports
                .Where(rr => rr.Company == companyClaims && rr.PONo == po.PurchaseOrderNo)
                .ToList();

                var totalAmountRR = po.Quantity - po.QuantityReceived;

                if (model.QuantityDelivered > totalAmountRR)
                {
                    TempData["error"] = "Input is exceed to remaining quantity delivered";
                    return View(model);
                }

                model.ReceivingReportNo = await _unitOfWork.FilprideReceivingReportRepository.GenerateCodeAsync(companyClaims, cancellationToken);
                model.CreatedBy = _userManager.GetUserName(this.User);
                model.GainOrLoss = model.QuantityReceived - model.QuantityDelivered;

                var existingPo = await _unitOfWork.FilpridePurchaseOrderRepository.GetAsync(po => po.PurchaseOrderId == model.POId, cancellationToken);

                model.PONo = existingPo.PurchaseOrderNo;

                model.DueDate = await _unitOfWork.FilprideReceivingReportRepository.ComputeDueDateAsync(model.POId, model.Date, cancellationToken);

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
            if (id == null || _dbContext.FilprideReceivingReports == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var receivingReport = await _dbContext.FilprideReceivingReports.FindAsync(id, cancellationToken);
            if (receivingReport == null)
            {
                return NotFound();
            }

            receivingReport.PurchaseOrders = await _dbContext.FilpridePurchaseOrders
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
        public async Task<IActionResult> Edit(FilprideReceivingReport model, CancellationToken cancellationToken)
        {
            var existingModel = await _dbContext.FilprideReceivingReports.FindAsync(model.ReceivingReportId, cancellationToken);
            var companyClaims = await GetCompanyClaimAsync();

            existingModel.PurchaseOrders = await _dbContext.FilpridePurchaseOrders
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
                            .FilpridePurchaseOrders
                            .Include(po => po.Supplier)
                            .Include(po => po.Product)
                            .FirstOrDefaultAsync(po => po.PurchaseOrderId == model.POId, cancellationToken);

                #endregion --Retrieve PO

                var rr = _dbContext.FilprideReceivingReports
                .Where(rr => rr.Company == companyClaims && rr.PONo == po.PurchaseOrderNo)
                .ToList();

                var totalAmountRR = po.Quantity - po.QuantityReceived;

                if (model.QuantityDelivered > totalAmountRR && existingModel.PostedBy == null)
                {
                    TempData["error"] = "Input is exceed to remaining quantity delivered";
                    return View(model);
                }

                existingModel.Date = model.Date;
                existingModel.POId = model.POId;

                var existingPo = await _unitOfWork.FilpridePurchaseOrderRepository.GetAsync(po => po.PurchaseOrderId == model.POId);

                existingModel.PONo = existingPo.PurchaseOrderNo;

                existingModel.DueDate = await _unitOfWork.FilprideReceivingReportRepository.ComputeDueDateAsync(model.POId, model.Date, cancellationToken);
                existingModel.SupplierInvoiceNumber = model.SupplierInvoiceNumber;
                existingModel.SupplierInvoiceDate = model.SupplierInvoiceDate;
                existingModel.SupplierDrNo = model.SupplierDrNo;
                existingModel.WithdrawalCertificate = model.WithdrawalCertificate;
                existingModel.TruckOrVessels = model.TruckOrVessels;
                existingModel.QuantityDelivered = model.QuantityDelivered;
                existingModel.QuantityReceived = model.QuantityReceived;
                existingModel.GainOrLoss = model.QuantityReceived - model.QuantityDelivered;
                existingModel.OtherRef = model.OtherRef;
                existingModel.Remarks = model.Remarks;
                existingModel.ReceivedDate = model.ReceivedDate;

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
            if (id == null || _dbContext.FilprideReceivingReports == null)
            {
                return NotFound();
            }

            var receivingReport = await _unitOfWork.FilprideReceivingReportRepository.GetAsync(rr => rr.ReceivingReportId == id, cancellationToken);

            if (receivingReport == null)
            {
                return NotFound();
            }

            return View(receivingReport);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideReceivingReportRepository.GetAsync(rr => rr.ReceivingReportId == id, cancellationToken);

            if (model != null)
            {
                try
                {

                    if (model.ReceivedDate == null)
                    {
                        TempData["error"] = "Please indicate the received date.";
                        return RedirectToAction(nameof(Index));
                    }

                    if (model.PostedBy == null)
                    {
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;
                        model.Status = nameof(Status.Posted);

                        #region --General Ledger Recording

                        var ledgers = new List<FilprideGeneralLedgerBook>();

                        if (model.PurchaseOrder.Product.ProductName == "BIODIESEL")
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
                        else if (model.PurchaseOrder.Product.ProductName == "ECONOGAS")
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

                        if (!_unitOfWork.FilprideReceivingReportRepository.IsJournalEntriesBalanced(ledgers))
                        {
                            throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                        }

                        await _dbContext.AddRangeAsync(ledgers, cancellationToken);

                        #endregion --General Ledger Recording

                        #region--Inventory Recording

                        await _unitOfWork.FilprideInventory.AddPurchaseToInventoryAsync(model, cancellationToken);

                        #endregion

                        await _unitOfWork.FilprideReceivingReportRepository.UpdatePOAsync(model.PurchaseOrder.PurchaseOrderId, model.QuantityReceived, cancellationToken);

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
                        return RedirectToAction(nameof(Print), new { id });
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
            var model = await _dbContext.FilprideReceivingReports
                .FindAsync(id, cancellationToken);

            var existingInventory = await _dbContext.FilprideInventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Reference == model.ReceivingReportNo && i.Company == model.Company);

            if (model != null && existingInventory != null)
            {
                if (model.VoidedBy == null)
                {
                    if (model.PostedBy != null)
                    {
                        model.PostedBy = null;
                    }

                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;
                    model.Status = nameof(Status.Voided);

                    await _unitOfWork.FilprideReceivingReportRepository.RemoveRecords<FilpridePurchaseBook>(pb => pb.DocumentNo == model.ReceivingReportNo, cancellationToken);
                    await _unitOfWork.FilprideReceivingReportRepository.RemoveRecords<FilprideGeneralLedgerBook>(pb => pb.Reference == model.ReceivingReportNo, cancellationToken);
                    await _unitOfWork.FilprideInventory.VoidInventory(existingInventory, cancellationToken);
                    await _unitOfWork.FilprideReceivingReportRepository.RemoveQuantityReceived(model.POId, model.QuantityReceived, cancellationToken);
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
            var model = await _dbContext.FilprideReceivingReports.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.CanceledQuantity = model.QuantityDelivered < model.QuantityReceived ? model.QuantityDelivered : model.QuantityReceived;
                    model.QuantityDelivered = 0;
                    model.QuantityReceived = 0;
                    model.Status = nameof(Status.Canceled);

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
            var po = await _unitOfWork.FilpridePurchaseOrderRepository.GetAsync(po => po.PurchaseOrderId == id, cancellationToken);

            var rrPostedOnly = await _dbContext
                .FilprideReceivingReports
                .Where(rr => rr.Company == po.Company && rr.PONo == po.PurchaseOrderNo && rr.PostedBy != null)
                .ToListAsync(cancellationToken);

            var rr = await _dbContext
                .FilprideReceivingReports
                .Where(rr => rr.Company == po.Company && rr.PONo == po.PurchaseOrderNo)
                .ToListAsync(cancellationToken);

            var rrNotPosted = await _dbContext
                .FilprideReceivingReports
                .Where(rr => rr.Company == po.Company && rr.PONo == po.PurchaseOrderNo && rr.PostedBy == null && rr.CanceledBy == null)
                .ToListAsync(cancellationToken);

            var rrCanceled = await _dbContext
                .FilprideReceivingReports
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

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.FilprideReceivingReportRepository.GetAsync(x => x.ReceivingReportId == id, cancellationToken);
            if (cv?.IsPrinted == false)
            {
                #region --Audit Trail Recording

                //var printedBy = _userManager.GetUserName(this.User);
                //AuditTrail auditTrail = new(printedBy, $"Printed original copy of cv# {cv.CVNo}", "Check Vouchers");
                //await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }
    }
}