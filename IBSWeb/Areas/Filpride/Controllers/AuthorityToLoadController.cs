﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Integrated;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_RCD, SD.Department_TradeAndSupply)]
    public class AuthorityToLoadController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        public AuthorityToLoadController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
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
        public async Task<IActionResult> GetAuthorityToLoads([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var atlList = await _unitOfWork.FilprideAuthorityToLoad
                    .GetAllAsync(null, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    atlList = atlList
                    .Where(s =>
                        s.AuthorityToLoadNo.ToLower().Contains(searchValue) ||
                        s.DateBooked.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                        s.ValidUntil.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                        s.UppiAtlNo?.ToLower().Contains(searchValue) == true ||
                        s.CustomerOrderSlip.CustomerOrderSlipNo.ToLower().Contains(searchValue) == true ||
                        s.Remarks.ToLower().Contains(searchValue) ||
                        s.DeliveryReceipt?.DeliveryReceiptNo?.ToLower().Contains(searchValue) == true
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    atlList = atlList
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = atlList.Count();

                var pagedData = atlList
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
            var companyClaims = await GetCompanyClaimAsync();

            BookATLViewModel viewModel = new()
            {
                SupplierList = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken),
                Date = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()),
                CurrentUser = _userManager.GetUserName(User)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(BookATLViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                viewModel.SupplierList = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                FilprideAuthorityToLoad model = new()
                {
                    AuthorityToLoadNo = await _unitOfWork.FilprideAuthorityToLoad.GenerateAtlNo(cancellationToken),
                    CustomerOrderSlipId = viewModel.CosIds.FirstOrDefault(),
                    DateBooked = viewModel.Date,
                    ValidUntil = viewModel.Date.AddDays(4),
                    UppiAtlNo = viewModel.UPPIAtlNo,
                    Remarks = "Please secure delivery documents. FILPRIDE DR / SUPPLIER DR / WITHDRAWAL CERTIFICATE",
                    CreatedBy = _userManager.GetUserName(User),
                    CreatedDate = DateTime.Now,
                };

                await _unitOfWork.FilprideAuthorityToLoad.AddAsync(model, cancellationToken);

                var bookDetails = new List<FilprideBookAtlDetail>();

                foreach (var cos in viewModel.CosIds)
                {
                    var existingCos = await _dbContext.FilprideCustomerOrderSlips
                        .FirstOrDefaultAsync(c => c.CustomerOrderSlipId ==cos, cancellationToken);

                    existingCos.AuthorityToLoadNo = model.AuthorityToLoadNo;
                    existingCos.Status = nameof(CosStatus.ForApprovalOfOM);


                    bookDetails.Add(new FilprideBookAtlDetail
                    {
                        AuthorityToLoadId = model.AuthorityToLoadId,
                        CustomerOrderSlipId = cos,
                    });
                }

                await _dbContext.FilprideBookAtlDetails.AddRangeAsync(bookDetails, cancellationToken);

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new atl# {model.AuthorityToLoadNo}", "Authority To Load", "", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                TempData["success"] = "ATL booked successfully";
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                viewModel.SupplierList = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }

        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideAuthorityToLoad
                .GetAsync(atl => atl.AuthorityToLoadId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                return View(existingRecord);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierCOS(int supplierId)
        {
            // Query your database to get COS list for the supplier
            var cosList = await _dbContext.FilprideCustomerOrderSlips
                .Where(cos => cos.SupplierId == supplierId && cos.Status == nameof(CosStatus.ForAtlBooking))
                .Select(cos => new SelectListItem
                {
                    Value = cos.CustomerOrderSlipId.ToString(),
                    Text = cos.CustomerOrderSlipNo
                })
                .ToListAsync();;

            return Json(new { cosList });
        }

        [HttpGet]
        public async Task<IActionResult> GetHaulerDetails(int cosId)
        {
            // Query your database to get hauler details for the COS
            var existingCos = await _unitOfWork.FilprideCustomerOrderSlip  // Replace with your actual context and model
                .GetAsync(c => c.CustomerOrderSlipId == cosId);

            var haulerDetails = new
            {
                Hauler = existingCos.Hauler?.SupplierName,
                existingCos.Driver,
                existingCos.PlateNo,
                existingCos.Freight,
                LoadPort = existingCos.PickUpPoint?.Depot
            };

            return Json(haulerDetails);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateValidityDate(int id, DateOnly newValidUntil, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingAtl = await _unitOfWork.FilprideAuthorityToLoad
                    .GetAsync(atl => atl.AuthorityToLoadId == id, cancellationToken);

                existingAtl.ValidUntil = newValidUntil;

                FilprideAuditTrail auditTrailBook = new(existingAtl.CreatedBy, $"Update validity date of atl# {existingAtl.AuthorityToLoadNo}", "Authority To Load", "", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Json(new { success = true });

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Json(new { success = false });
            }
        }
    }
}
