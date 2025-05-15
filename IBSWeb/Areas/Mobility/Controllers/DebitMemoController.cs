using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    [DepartmentAuthorize(SD.Department_CreditAndCollection, SD.Department_RCD)]
    public class DebitMemoController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<DebitMemoController> _logger;

        public DebitMemoController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, ILogger<DebitMemoController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<string?> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetDebitMemos([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var stationCodeClaims = await GetStationCodeClaimAsync();

                var debitMemos = await _unitOfWork.MobilityDebitMemo
                    .GetAllAsync(dm => dm.StationCode == stationCodeClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    debitMemos = debitMemos
                        .Where(s =>
                            s.DebitMemoNo!.ToLower().Contains(searchValue) ||
                            (s.ServiceInvoice?.ServiceInvoiceNo.ToLower().Contains(searchValue) == true) ||
                            s.TransactionDate.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                            s.DebitAmount.ToString().Contains(searchValue) ||
                            s.Remarks?.ToLower().Contains(searchValue) == true ||
                            s.Description.ToLower().Contains(searchValue) ||
                            s.CreatedBy!.ToLower().Contains(searchValue)
                            )
                        .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    debitMemos = debitMemos
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = debitMemos.Count();

                var pagedData = debitMemos
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
                _logger.LogError(ex, "Failed to get debit memo. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new DebitMemoViewModel();
            var stationCodeClaims = await GetStationCodeClaimAsync();

            viewModel.ServiceInvoices = await _dbContext.MobilityServiceInvoices
                .Where(sv => sv.StationCode == stationCodeClaims && sv.PostedBy != null)
                .Select(sv => new SelectListItem
                {
                    Value = sv.ServiceInvoiceId.ToString(),
                    Text = sv.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DebitMemoViewModel viewModel, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            var existingSv = await _unitOfWork.MobilityServiceInvoice
                        .GetAsync(sv => sv.ServiceInvoiceId == viewModel.ServiceInvoiceId, cancellationToken);

            if (!ModelState.IsValid)
            {
                viewModel.ServiceInvoices = await _dbContext.MobilityServiceInvoices
                    .Where(sv => sv.StationCode == stationCodeClaims && sv.PostedBy != null)
                    .Select(sv => new SelectListItem
                    {
                        Value = sv.ServiceInvoiceId.ToString(),
                        Text = sv.ServiceInvoiceNo
                    })
                    .ToListAsync(cancellationToken);

                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- checking for unposted DM or CM

                    var existingSVDMs = _dbContext.MobilityDebitMemos
                                  .Where(si => si.ServiceInvoiceId == viewModel.ServiceInvoiceId && si.Status == "Pending")
                                  .OrderBy(s => s.DebitMemoId)
                                  .ToList();
                    if (existingSVDMs.Count > 0)
                    {
                        viewModel.ServiceInvoices = await _dbContext.MobilityServiceInvoices
                            .Where(sv => sv.StationCode == stationCodeClaims && sv.PostedBy != null)
                            .Select(sv => new SelectListItem
                            {
                                Value = sv.ServiceInvoiceId.ToString(),
                                Text = sv.ServiceInvoiceNo
                            })
                            .ToListAsync(cancellationToken);

                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSVDMs.First().DebitMemoNo}");
                        return View(viewModel);
                    }

                    var existingSVCMs = _dbContext.MobilityCreditMemos
                                      .Where(si => si.ServiceInvoiceId == viewModel.ServiceInvoiceId && si.Status == "Pending")
                                      .OrderBy(s => s.CreditMemoId)
                                      .ToList();
                    if (existingSVCMs.Count > 0)
                    {
                        viewModel.ServiceInvoices = await _dbContext.MobilityServiceInvoices
                            .Where(sv => sv.StationCode == stationCodeClaims && sv.PostedBy != null)
                            .Select(sv => new SelectListItem
                            {
                                Value = sv.ServiceInvoiceId.ToString(),
                                Text = sv.ServiceInvoiceNo
                            })
                            .ToListAsync(cancellationToken);

                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSVCMs.First().CreditMemoNo}");
                        return View(viewModel);
                    }

                #endregion -- checking for unposted DM or CM

                MobilityDebitMemo model = new()
                {
                    DebitAmount = -viewModel.Amount ?? 0,
                    Type = existingSv.Type,
                    DebitMemoNo = await _unitOfWork.MobilityDebitMemo.GenerateCodeAsync(stationCodeClaims, existingSv.Type, cancellationToken),
                    StationCode = existingSv.StationCode,
                    CreatedBy = User.Identity!.Name,
                    TransactionDate = viewModel.TransactionDate,
                    ServiceInvoiceId = viewModel.ServiceInvoiceId,
                    Period = viewModel.Period,
                    Amount = viewModel.Amount,
                    Description = viewModel.Description,
                    Remarks = viewModel.Remarks,
                };
                await _dbContext.AddAsync(model, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(viewModel.CreatedBy, $"Create new debit memo# {viewModel.DebitMemoNo}", "Debit Memo", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Debit memo created successfully. Series Number: {model.DebitMemoNo}";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create debit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var debitMemo = await _unitOfWork.MobilityDebitMemo.GetAsync(dm => dm.DebitMemoId == id, cancellationToken);
            if (debitMemo == null)
            {
                return BadRequest();
            }
            return View(debitMemo);
        }

        public async Task<IActionResult> Post(int id, ViewModelDMCM viewModelDMCM, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.MobilityDebitMemo.GetAsync(dm => dm.DebitMemoId == id, cancellationToken);

            if (model != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (model.PostedBy == null)
                    {
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Posted);

                        //var accountTitlesDto = await _unitOfWork.FilprideServiceInvoice.GetListOfAccountTitleDto(cancellationToken);
                        //var arTradeReceivableTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020100") ?? throw new ArgumentException("Account title '101020100' not found.");
                        //var arNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020500") ?? throw new ArgumentException("Account title '101020500' not found.");
                        //var arTradeCwt = accountTitlesDto.Find(c => c.AccountNumber == "101020200") ?? throw new ArgumentException("Account title '101020200' not found.");
                        //var arTradeCwv = accountTitlesDto.Find(c => c.AccountNumber == "101020300") ?? throw new ArgumentException("Account title '101020300' not found.");
                        //var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account title '201030100' not found.");

                        if (model.ServiceInvoiceId != null)
                        {
                            var existingSv = await _unitOfWork.MobilityServiceInvoice
                                .GetAsync(sv => sv.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

                            #region --SV Computation--

                                viewModelDMCM.Period = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                                if (existingSv.Customer!.VatType == "Vatable")
                                {
                                    viewModelDMCM.Total = model.Amount ?? 0 - existingSv.Discount;
                                    viewModelDMCM.NetAmount = _unitOfWork.MobilityServiceInvoice.ComputeNetOfVat(viewModelDMCM.Total);
                                    viewModelDMCM.VatAmount = _unitOfWork.MobilityServiceInvoice.ComputeVatAmount(viewModelDMCM.NetAmount);
                                    viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (existingSv.Customer.WithHoldingTax ? existingSv.Service!.Percent / 100m : 0);
                                    if (existingSv.Customer.WithHoldingVat)
                                    {
                                        viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                    }
                                }
                                else
                                {
                                    viewModelDMCM.NetAmount = model.Amount ?? 0 - existingSv.Discount;
                                    viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (existingSv.Customer.WithHoldingTax ? existingSv.Service!.Percent / 100m : 0);
                                    if (existingSv.Customer.WithHoldingVat)
                                    {
                                        viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                    }
                                }

                            #endregion --SV Computation--

                            ///TODO: waiting for ma'am LSA journal entries
                            #region --Sales Book Recording(SV)--

                                // var sales = new FilprideSalesBook();
                                //
                                // if (model.ServiceInvoice.Customer.VatType == "Vatable")
                                // {
                                //     sales.TransactionDate = viewModelDMCM.Period;
                                //     sales.SerialNo = model.DebitMemoNo;
                                //     sales.SoldTo = model.ServiceInvoice.Customer.CustomerName;
                                //     sales.TinNo = model.ServiceInvoice.Customer.CustomerTin;
                                //     sales.Address = model.ServiceInvoice.Customer.CustomerAddress;
                                //     sales.Description = model.ServiceInvoice.Service.Name;
                                //     sales.Amount = viewModelDMCM.Total;
                                //     sales.VatAmount = viewModelDMCM.VatAmount;
                                //     sales.VatableSales = viewModelDMCM.Total / 1.12m;
                                //     //sales.Discount = model.Discount;
                                //     sales.NetSales = viewModelDMCM.NetAmount;
                                //     sales.CreatedBy = model.CreatedBy;
                                //     sales.CreatedDate = model.CreatedDate;
                                //     sales.DueDate = existingSv.DueDate;
                                //     sales.DocumentId = existingSv.ServiceInvoiceId;
                                //     sales.Company = model.Company;
                                // }
                                // else if (model.ServiceInvoice.Customer.VatType == "Exempt")
                                // {
                                //     sales.TransactionDate = viewModelDMCM.Period;
                                //     sales.SerialNo = model.DebitMemoNo;
                                //     sales.SoldTo = model.ServiceInvoice.Customer.CustomerName;
                                //     sales.TinNo = model.ServiceInvoice.Customer.CustomerTin;
                                //     sales.Address = model.ServiceInvoice.Customer.CustomerAddress;
                                //     sales.Description = model.ServiceInvoice.Service.Name;
                                //     sales.Amount = viewModelDMCM.Total;
                                //     sales.VatExemptSales = viewModelDMCM.Total;
                                //     //sales.Discount = model.Discount;
                                //     sales.NetSales = viewModelDMCM.NetAmount;
                                //     sales.CreatedBy = model.CreatedBy;
                                //     sales.CreatedDate = model.CreatedDate;
                                //     sales.DueDate = existingSv.DueDate;
                                //     sales.DocumentId = existingSv.ServiceInvoiceId;
                                //     sales.Company = model.Company;
                                // }
                                // else
                                // {
                                //     sales.TransactionDate = viewModelDMCM.Period;
                                //     sales.SerialNo = model.DebitMemoNo;
                                //     sales.SoldTo = model.ServiceInvoice.Customer.CustomerName;
                                //     sales.TinNo = model.ServiceInvoice.Customer.CustomerTin;
                                //     sales.Address = model.ServiceInvoice.Customer.CustomerAddress;
                                //     sales.Description = model.ServiceInvoice.Service.Name;
                                //     sales.Amount = viewModelDMCM.Total;
                                //     sales.ZeroRated = viewModelDMCM.Total;
                                //     //sales.Discount = model.Discount;
                                //     sales.NetSales = viewModelDMCM.NetAmount;
                                //     sales.CreatedBy = model.CreatedBy;
                                //     sales.CreatedDate = model.CreatedDate;
                                //     sales.DueDate = existingSv.DueDate;
                                //     sales.DocumentId = existingSv.ServiceInvoiceId;
                                //     sales.Company = model.Company;
                                // }
                                // await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SV)--

                            ///TODO: waiting for ma'am LSA journal entries
                            #region --General Ledger Book Recording(SV)--

                                // var ledgers = new List<FilprideGeneralLedgerBook>();
                                //
                                // ledgers.Add(
                                //         new FilprideGeneralLedgerBook
                                //         {
                                //             Date = model.TransactionDate,
                                //             Reference = model.DebitMemoNo,
                                //             Description = model.ServiceInvoice.Service.Name,
                                //             AccountId = arNonTradeTitle.AccountId,
                                //             AccountNo = arNonTradeTitle.AccountNumber,
                                //             AccountTitle = arNonTradeTitle.AccountName,
                                //             Debit = viewModelDMCM.Total - (viewModelDMCM.WithholdingTaxAmount + viewModelDMCM.WithholdingVatAmount),
                                //             Credit = 0,
                                //             Company = model.Company,
                                //             CreatedBy = model.CreatedBy,
                                //             CreatedDate = model.CreatedDate,
                                //             CustomerId = model.ServiceInvoice.CustomerId
                                //         }
                                //     );
                                // if (viewModelDMCM.WithholdingTaxAmount > 0)
                                // {
                                //     ledgers.Add(
                                //         new FilprideGeneralLedgerBook
                                //         {
                                //             Date = model.TransactionDate,
                                //             Reference = model.DebitMemoNo,
                                //             Description = model.ServiceInvoice.Service.Name,
                                //             AccountId = arTradeCwt.AccountId,
                                //             AccountNo = arTradeCwt.AccountNumber,
                                //             AccountTitle = arTradeCwt.AccountName,
                                //             Debit = viewModelDMCM.WithholdingTaxAmount,
                                //             Credit = 0,
                                //             Company = model.Company,
                                //             CreatedBy = model.CreatedBy,
                                //             CreatedDate = model.CreatedDate
                                //         }
                                //     );
                                // }
                                // if (viewModelDMCM.WithholdingVatAmount > 0)
                                // {
                                //     ledgers.Add(
                                //         new FilprideGeneralLedgerBook
                                //         {
                                //             Date = model.TransactionDate,
                                //             Reference = model.DebitMemoNo,
                                //             Description = model.ServiceInvoice.Service.Name,
                                //             AccountId = arTradeCwv.AccountId,
                                //             AccountNo = arTradeCwv.AccountNumber,
                                //             AccountTitle = arTradeCwv.AccountName,
                                //             Debit = viewModelDMCM.WithholdingVatAmount,
                                //             Credit = 0,
                                //             Company = model.Company,
                                //             CreatedBy = model.CreatedBy,
                                //             CreatedDate = model.CreatedDate
                                //         }
                                //     );
                                // }
                                //
                                // if (viewModelDMCM.Total > 0)
                                // {
                                //     ledgers.Add(new FilprideGeneralLedgerBook
                                //     {
                                //         Date = model.TransactionDate,
                                //         Reference = model.DebitMemoNo,
                                //         Description = model.ServiceInvoice.Service.Name,
                                //         AccountNo = model.ServiceInvoice.Service.CurrentAndPreviousNo,
                                //         AccountTitle = model.ServiceInvoice.Service.CurrentAndPreviousTitle,
                                //         Debit = 0,
                                //         Credit = viewModelDMCM.NetAmount,
                                //         Company = model.Company,
                                //         CreatedBy = model.CreatedBy,
                                //         CreatedDate = model.CreatedDate
                                //     });
                                // }
                                //
                                // if (viewModelDMCM.VatAmount > 0)
                                // {
                                //     ledgers.Add(
                                //         new FilprideGeneralLedgerBook
                                //         {
                                //             Date = model.TransactionDate,
                                //             Reference = model.DebitMemoNo,
                                //             Description = model.ServiceInvoice.Service.Name,
                                //             AccountId = vatOutputTitle.AccountId,
                                //             AccountNo = vatOutputTitle.AccountNumber,
                                //             AccountTitle = vatOutputTitle.AccountName,
                                //             Debit = 0,
                                //             Credit = viewModelDMCM.VatAmount,
                                //             Company = model.Company,
                                //             CreatedBy = model.CreatedBy,
                                //             CreatedDate = model.CreatedDate
                                //         }
                                //     );
                                // }
                                //
                                // if (!_unitOfWork.FilprideDebitMemo.IsJournalEntriesBalanced(ledgers))
                                // {
                                //     throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                                // }
                                //
                                // await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                            #endregion --General Ledger Book Recording(SV)--
                        }

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.PostedBy!, $"Posted debit memo# {model.DebitMemoNo}", "Debit Memo", nameof(Mobility));
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Debit Memo has been Posted.";
                    }
                    return RedirectToAction(nameof(Print), new { id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to post debit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                        ex.Message, ex.StackTrace, User.Identity!.Name);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.MobilityDebitMemo.GetAsync(dm => dm.DebitMemoId == id, cancellationToken);

            if (model != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (model.VoidedBy == null)
                    {
                        if (model.PostedBy != null)
                        {
                            model.PostedBy = null;
                        }

                        model.VoidedBy = _userManager.GetUserName(this.User);
                        model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Voided);

                        ///TODO: woiting for ma;am LSA journal entries
                        //await _unitOfWork.FilprideDebitMemo.RemoveRecords<FilprideSalesBook>(crb => crb.SerialNo == model.DebitMemoNo);
                        //await _unitOfWork.FilprideDebitMemo.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.DebitMemoNo);

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided debit memo# {model.DebitMemoNo}", "Debit Memo", nameof(Mobility));
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Debit Memo has been Voided.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to void debit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.MobilityDebitMemo.GetAsync(dm => dm.DebitMemoId == id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    if (model.CanceledBy == null)
                    {
                        model.CanceledBy = _userManager.GetUserName(this.User);
                        model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.CancellationRemarks = cancellationRemarks;
                        model.Status = nameof(Status.Canceled);

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled debit memo# {model.DebitMemoNo}", "Debit Memo", nameof(Mobility));
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Debit Memo has been Cancelled.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel debit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<JsonResult> GetSVDetails(int svId, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.MobilityServiceInvoice.GetAsync(sv => sv.ServiceInvoiceId == svId, cancellationToken);
            if (model != null)
            {
                return Json(new
                {
                    model.Period,
                    model.Amount
                });
            }

            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var stationCodeClaims = await GetStationCodeClaimAsync();

            var existingRecord = await _unitOfWork.MobilityDebitMemo.GetAsync(dm => dm.DebitMemoId == id, cancellationToken);

            if (existingRecord == null)
            {
                return BadRequest();
            }

            DebitMemoViewModel viewModel = new()
            {
                ServiceInvoices = await _dbContext.MobilityServiceInvoices
                    .Where(sv => sv.StationCode == stationCodeClaims && sv.PostedBy != null)
                    .Select(sv => new SelectListItem
                    {
                        Value = sv.ServiceInvoiceId.ToString(),
                        Text = sv.ServiceInvoiceNo
                    })
                    .ToListAsync(cancellationToken),
                DebitAmount = -existingRecord.Amount ?? 0,
                Type = existingRecord.Type,
                StationCode = existingRecord.StationCode,
                TransactionDate = existingRecord.TransactionDate,
                ServiceInvoiceId = existingRecord.ServiceInvoiceId,
                Period = existingRecord.Period,
                Amount = existingRecord.Amount,
                Description = existingRecord.Description,
                Remarks = existingRecord.Remarks,
                DebitMemoId = existingRecord.DebitMemoId,
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DebitMemoViewModel viewModel, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingDM = await _unitOfWork
                    .MobilityDebitMemo
                    .GetAsync(dm => dm.DebitMemoId == viewModel.DebitMemoId);

                #region -- Saving Default Enries --

                existingDM.TransactionDate = viewModel.TransactionDate;
                existingDM.ServiceInvoiceId = viewModel.ServiceInvoiceId;
                existingDM.Period = viewModel.Period;
                existingDM.Amount = viewModel.Amount;
                existingDM.Description = viewModel.Description;
                existingDM.Remarks = viewModel.Remarks;
                existingDM.DebitAmount = viewModel.Amount ?? 0;
                existingDM.EditedBy = _userManager.GetUserName(User);
                existingDM.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #endregion -- Saving Default Enries --

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingDM.EditedBy!, $"Edited debit memo# {existingDM.DebitMemoNo}", "Debit Memo", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Debit Memo edited successfully. Series Number: {existingDM.DebitMemoNo}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to terminate placement. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var dm = await _unitOfWork.MobilityDebitMemo.GetAsync(x => x.DebitMemoId == id, cancellationToken);
            if (!dm.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User)!;
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of debit memo# {dm.DebitMemoNo}", "Debit Memo", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                dm.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }
    }
}
