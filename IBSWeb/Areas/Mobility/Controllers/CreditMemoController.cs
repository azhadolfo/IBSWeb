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

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    [DepartmentAuthorize(SD.Department_CreditAndCollection, SD.Department_RCD)]
    public class CreditMemoController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ILogger<CreditMemoController> _logger;

        public CreditMemoController(IUnitOfWork unitOfWork, ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ILogger<CreditMemoController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<string> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetCreditMemos([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var stationCodeClaims = await GetStationCodeClaimAsync();


                var creditMemos = await _unitOfWork.MobilityCreditMemo
                    .GetAllAsync(cm => cm.StationCode == stationCodeClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    creditMemos = creditMemos
                    .Where(s =>
                        s.CreditMemoNo.ToLower().Contains(searchValue) ||
                        (s.ServiceInvoice?.ServiceInvoiceNo.ToLower().Contains(searchValue) == true) ||
                        s.TransactionDate.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.CreditAmount.ToString().Contains(searchValue) ||
                        s.Remarks?.ToLower().Contains(searchValue) == true ||
                        s.Description.ToLower().Contains(searchValue) ||
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

                    creditMemos = creditMemos
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = creditMemos.Count();

                var pagedData = creditMemos
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
                _logger.LogError(ex, "Failed to get credit memos. Error: {ErrorMessage}, Stack: {StackTrace}. Get by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new CreditMemoViewModel();

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
        public async Task<IActionResult> Create(CreditMemoViewModel viewModel, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            var companyClaims = await GetCompanyClaimAsync();

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
                #region -- check for unposted DM or CM

                     var existingSOADMs = _dbContext.MobilityDebitMemos
                                   .Where(si => si.ServiceInvoiceId == viewModel.ServiceInvoiceId && si.Status == "Pending")
                                   .OrderBy(s => s.ServiceInvoiceId)
                                   .ToList();
                     if (existingSOADMs.Count > 0)
                     {
                         viewModel.ServiceInvoices = await _dbContext.MobilityServiceInvoices
                             .Where(sv => sv.StationCode == stationCodeClaims && sv.PostedBy != null)
                             .Select(sv => new SelectListItem
                             {
                                 Value = sv.ServiceInvoiceId.ToString(),
                                 Text = sv.ServiceInvoiceNo
                             })
                             .ToListAsync(cancellationToken);

                         ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSOADMs.First().DebitMemoNo}");
                         return View(viewModel);
                     }

                    var existingSOACMs = _dbContext.MobilityCreditMemos
                                      .Where(si => si.ServiceInvoiceId == viewModel.ServiceInvoiceId && si.Status == "Pending")
                                      .OrderBy(s => s.ServiceInvoiceId)
                                      .ToList();
                    if (existingSOACMs.Count > 0)
                    {
                        viewModel.ServiceInvoices = await _dbContext.MobilityServiceInvoices
                            .Where(sv => sv.StationCode == stationCodeClaims && sv.PostedBy != null)
                            .Select(sv => new SelectListItem
                            {
                                Value = sv.ServiceInvoiceId.ToString(),
                                Text = sv.ServiceInvoiceNo
                            })
                            .ToListAsync(cancellationToken);

                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSOACMs.First().CreditMemoNo}");
                        return View(viewModel);
                    }

                #endregion -- check for unposted DM or CM

                MobilityCreditMemo model = new()
                {
                    CreditAmount = -viewModel.Amount ?? 0,
                    Type = existingSv.Type,
                    CreditMemoNo = await _unitOfWork.MobilityCreditMemo.GenerateCodeAsync(stationCodeClaims, existingSv.Type, cancellationToken),
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

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(viewModel.CreatedBy, $"Create new credit memo# {viewModel.CreditMemoNo}", "Credit Memo", ipAddress, companyClaims);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Credit memo created successfully. Series Number: {model.CreditMemoNo}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stationCodeClaims = await GetStationCodeClaimAsync();

            var existingRecord = await _unitOfWork.MobilityCreditMemo.GetAsync(c => c.CreditMemoId == id, cancellationToken);

            if (existingRecord == null)
            {
                return BadRequest();
            }

            CreditMemoViewModel viewModel = new()
            {
                ServiceInvoices = await _dbContext.MobilityServiceInvoices
                    .Where(sv => sv.StationCode == stationCodeClaims && sv.PostedBy != null)
                    .Select(sv => new SelectListItem
                    {
                        Value = sv.ServiceInvoiceId.ToString(),
                        Text = sv.ServiceInvoiceNo
                    })
                    .ToListAsync(cancellationToken),
                CreditAmount = -existingRecord.Amount ?? 0,
                Type = existingRecord.Type,
                StationCode = existingRecord.StationCode,
                TransactionDate = existingRecord.TransactionDate,
                ServiceInvoiceId = existingRecord.ServiceInvoiceId,
                Period = existingRecord.Period,
                Amount = existingRecord.Amount,
                Description = existingRecord.Description,
                Remarks = existingRecord.Remarks,
                CreditMemoId = existingRecord.CreditMemoId,
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CreditMemoViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingCM = await _unitOfWork
                    .MobilityCreditMemo
                    .GetAsync(cm => cm.CreditMemoId == viewModel.CreditMemoId);

                #region -- Saving Default Enries --

                existingCM.TransactionDate = viewModel.TransactionDate;
                existingCM.ServiceInvoiceId = viewModel.ServiceInvoiceId;
                existingCM.Period = viewModel.Period;
                existingCM.Amount = viewModel.Amount;
                existingCM.Description = viewModel.Description;
                existingCM.Remarks = viewModel.Remarks;
                existingCM.CreditAmount = -viewModel.Amount ?? 0;
                existingCM.EditedBy = User.Identity!.Name;
                existingCM.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #endregion -- Saving Default Enries --

                #region --Audit Trail Recording

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(existingCM.EditedBy, $"Edited credit memo# {existingCM.CreditMemoNo}", "Credit Memo", ipAddress, companyClaims);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Credit Memo edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
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

            var creditMemo = await _unitOfWork.MobilityCreditMemo.GetAsync(c => c.CreditMemoId == id, cancellationToken);

            if (creditMemo == null)
            {
                return NotFound();
            }

            return View(creditMemo);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken, ViewModelDMCM viewModelDMCM)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var model = await _unitOfWork.MobilityCreditMemo.GetAsync(c => c.CreditMemoId == id, cancellationToken);

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

                            if (existingSv.Customer.VatType == "Vatable")
                            {
                                viewModelDMCM.Total = -model.Amount ?? 0;
                                viewModelDMCM.NetAmount = (model.Amount ?? 0 - existingSv.Discount) / 1.12m;
                                viewModelDMCM.VatAmount = (model.Amount ?? 0 - existingSv.Discount) - viewModelDMCM.NetAmount;
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (existingSv.Service.Percent / 100m);
                                if (existingSv.Customer.WithHoldingVat)
                                {
                                    viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                }
                            }
                            else
                            {
                                viewModelDMCM.NetAmount = model.Amount ?? 0 - existingSv.Discount;
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (existingSv.Service.Percent / 100m);
                                if (existingSv.Customer.WithHoldingVat)
                                {
                                    viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                }
                            }

                            if (existingSv.Customer.VatType == "Vatable")
                            {
                                var total = Math.Round(model.Amount ?? 0 / 1.12m, 4);

                                var roundedNetAmount = Math.Round(viewModelDMCM.NetAmount, 4);

                                if (roundedNetAmount > total)
                                {
                                    var shortAmount = viewModelDMCM.NetAmount - total;

                                    viewModelDMCM.Amount += shortAmount;
                                }
                            }

                            #endregion --SV Computation--

                            /////TODO: waiting for ma'am LSA journal entries
                            #region --Sales Book Recording(SV)--

                                // var sales = new FilprideSalesBook();
                                //
                                // if (model.ServiceInvoice.Customer.VatType == "Vatable")
                                // {
                                //     sales.TransactionDate = viewModelDMCM.Period;
                                //     sales.SerialNo = model.CreditMemoNo;
                                //     sales.SoldTo = model.ServiceInvoice.Customer.CustomerName;
                                //     sales.TinNo = model.ServiceInvoice.Customer.CustomerTin;
                                //     sales.Address = model.ServiceInvoice.Customer.CustomerAddress;
                                //     sales.Description = model.ServiceInvoice.Service.Name;
                                //     sales.Amount = viewModelDMCM.Total;
                                //     sales.VatableSales = (_unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(sales.Amount))) * -1;
                                //     sales.VatAmount = (_unitOfWork.FilprideCreditMemo.ComputeVatAmount(Math.Abs(sales.VatableSales))) * -1;
                                //     //sales.Discount = model.Discount;
                                //     sales.NetSales = viewModelDMCM.NetAmount;
                                //     sales.CreatedBy = model.CreatedBy;
                                //     sales.CreatedDate = model.CreatedDate;
                                //     sales.DueDate = existingSv.DueDate;
                                //     sales.DocumentId = model.ServiceInvoiceId;
                                //     sales.Company = model.Company;
                                // }
                                // else if (model.ServiceInvoice.Customer.VatType == "Exempt")
                                // {
                                //     sales.TransactionDate = viewModelDMCM.Period;
                                //     sales.SerialNo = model.CreditMemoNo;
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
                                //     sales.DocumentId = model.ServiceInvoiceId;
                                //     sales.Company = model.Company;
                                // }
                                // else
                                // {
                                //     sales.TransactionDate = viewModelDMCM.Period;
                                //     sales.SerialNo = model.CreditMemoNo;
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
                                //     sales.DocumentId = model.ServiceInvoiceId;
                                //     sales.Company = model.Company;
                                // }
                                // await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SV)--

                            ///TODO: waiting for ma'am LSA journal entries
                            #region --General Ledger Book Recording(SV)--

                                // decimal withHoldingTaxAmount = 0;
                                // decimal withHoldingVatAmount = 0;
                                // decimal netOfVatAmount = 0;
                                // decimal vatAmount = 0;
                                //
                                // if (model.ServiceInvoice.Customer.VatType == SD.VatType_Vatable)
                                // {
                                //     netOfVatAmount = (_unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(model.CreditAmount))) * -1;
                                //     vatAmount = (_unitOfWork.FilprideCreditMemo.ComputeVatAmount(Math.Abs(netOfVatAmount))) * -1;
                                // }
                                // else
                                // {
                                //     netOfVatAmount = model.CreditAmount;
                                // }
                                //
                                // if (model.ServiceInvoice.Customer.WithHoldingTax)
                                // {
                                //     withHoldingTaxAmount = (_unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.01m)) * -1;
                                // }
                                //
                                // if (model.ServiceInvoice.Customer.WithHoldingVat)
                                // {
                                //     withHoldingVatAmount = (_unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.05m)) * -1;
                                // }
                                //
                                // var ledgers = new List<FilprideGeneralLedgerBook>();
                                //
                                // ledgers.Add(
                                //         new FilprideGeneralLedgerBook
                                //         {
                                //             Date = model.TransactionDate,
                                //             Reference = model.CreditMemoNo,
                                //             Description = model.ServiceInvoice.Service.Name,
                                //             AccountId = arNonTradeTitle.AccountId,
                                //             AccountNo = arNonTradeTitle.AccountNumber,
                                //             AccountTitle = arNonTradeTitle.AccountName,
                                //             Debit = 0,
                                //             Credit = Math.Abs(model.CreditAmount - (withHoldingTaxAmount + withHoldingVatAmount)),
                                //             Company = model.Company,
                                //             CreatedBy = model.CreatedBy,
                                //             CreatedDate = model.CreatedDate,
                                //             CustomerId = model.ServiceInvoice.CustomerId,
                                //         }
                                //     );
                                // if (withHoldingTaxAmount < 0)
                                // {
                                //     ledgers.Add(
                                //         new FilprideGeneralLedgerBook
                                //         {
                                //             Date = model.TransactionDate,
                                //             Reference = model.CreditMemoNo,
                                //             Description = model.ServiceInvoice.Service.Name,
                                //             AccountId = arTradeCwt.AccountId,
                                //             AccountNo = arTradeCwt.AccountNumber,
                                //             AccountTitle = arTradeCwt.AccountName,
                                //             Debit = 0,
                                //             Credit = Math.Abs(withHoldingTaxAmount),
                                //             Company = model.Company,
                                //             CreatedBy = model.CreatedBy,
                                //             CreatedDate = model.CreatedDate
                                //         }
                                //     );
                                // }
                                // if (withHoldingVatAmount < 0)
                                // {
                                //     ledgers.Add(
                                //         new FilprideGeneralLedgerBook
                                //         {
                                //             Date = model.TransactionDate,
                                //             Reference = model.CreditMemoNo,
                                //             Description = model.ServiceInvoice.Service.Name,
                                //             AccountId = arTradeCwv.AccountId,
                                //             AccountNo = arTradeCwv.AccountNumber,
                                //             AccountTitle = arTradeCwv.AccountName,
                                //             Debit = 0,
                                //             Credit = Math.Abs(withHoldingVatAmount),
                                //             Company = model.Company,
                                //             CreatedBy = model.CreatedBy,
                                //             CreatedDate = model.CreatedDate
                                //         }
                                //     );
                                // }
                                //
                                // ledgers.Add(new FilprideGeneralLedgerBook
                                // {
                                //     Date = model.TransactionDate,
                                //     Reference = model.CreditMemoNo,
                                //     Description = model.ServiceInvoice.Service.Name,
                                //     AccountNo = model.ServiceInvoice.Service.CurrentAndPreviousNo,
                                //     AccountTitle = model.ServiceInvoice.Service.CurrentAndPreviousTitle,
                                //     Debit = viewModelDMCM.NetAmount,
                                //     Credit = 0,
                                //     Company = model.Company,
                                //     CreatedBy = model.CreatedBy,
                                //     CreatedDate = model.CreatedDate
                                // });
                                //
                                // if (vatAmount < 0)
                                // {
                                //     ledgers.Add(
                                //         new FilprideGeneralLedgerBook
                                //         {
                                //             Date = model.TransactionDate,
                                //             Reference = model.CreditMemoNo,
                                //             Description = model.ServiceInvoice.Service.Name,
                                //             AccountId = vatOutputTitle.AccountId,
                                //             AccountNo = vatOutputTitle.AccountNumber,
                                //             AccountTitle = vatOutputTitle.AccountName,
                                //             Debit = Math.Abs(vatAmount),
                                //             Credit = 0,
                                //             Company = model.Company,
                                //             CreatedBy = model.CreatedBy,
                                //             CreatedDate = model.CreatedDate
                                //         }
                                //     );
                                // }
                                //
                                // if (!_unitOfWork.FilprideCreditMemo.IsJournalEntriesBalanced(ledgers))
                                // {
                                //     throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                                // }
                                //
                                // await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                            #endregion --General Ledger Book Recording(SV)--
                        }

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.PostedBy, $"Posted credit memo# {model.CreditMemoNo}", "Credit Memo", ipAddress, companyClaims);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Credit Memo has been Posted.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to post credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                        ex.Message, ex.StackTrace, User.Identity!.Name);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
                return RedirectToAction(nameof(Print), new { id });
            }

            return NotFound();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var model = await _unitOfWork.MobilityCreditMemo.GetAsync(c => c.CreditMemoId == id, cancellationToken);

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

                        //TODO: uncomment this if the journal entries are provided
                        //await _unitOfWork.FilprideCreditMemo.RemoveRecords<FilprideSalesBook>(crb => crb.SerialNo == model.CreditMemoNo, cancellationToken);
                        //await _unitOfWork.FilprideCreditMemo.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.CreditMemoNo, cancellationToken);

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided credit memo# {model.CreditMemoNo}", "Credit Memo", ipAddress, companyClaims);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Credit Memo has been Voided.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to void credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                        ex.Message, ex.StackTrace, User.Identity!.Name);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var model = await _unitOfWork.MobilityCreditMemo.GetAsync(c => c.CreditMemoId == id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    if (model.CanceledBy == null)
                    {
                        model.CanceledBy = _userManager.GetUserName(this.User);
                        model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Canceled);
                        model.CancellationRemarks = cancellationRemarks;

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled credit memo# {model.CreditMemoNo}", "Credit Memo", ipAddress, companyClaims);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Credit Memo has been Cancelled.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
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

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var cm = await _unitOfWork.MobilityCreditMemo.GetAsync(x => x.CreditMemoId == id, cancellationToken);
            if (!cm.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of credit memo# {cm.CreditMemoNo}", "Credit Memo", ipAddress, companyClaims);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                cm.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }
    }
}
