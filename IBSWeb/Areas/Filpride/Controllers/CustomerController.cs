using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class CustomerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<CustomerController> _logger;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        public CustomerController(IUnitOfWork unitOfWork, ILogger<CustomerController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            IEnumerable<FilprideCustomer> customer = await _unitOfWork.FilprideCustomer
                .GetAllAsync(null, cancellationToken);

            if (view == nameof(DynamicView.Customer))
            {
                return View("ExportIndex", customer);
            }

            return View(customer);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FilprideCustomer model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Make sure to fill all the required details.");
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                //bool IsTinExist = await _unitOfWork.FilprideCustomer.IsTinNoExistAsync(model.CustomerTin, companyClaims, cancellationToken);

                bool isTinExist = false;

                if (!isTinExist)
                {
                    model.Company = companyClaims;
                    model.CustomerCode = await _unitOfWork.FilprideCustomer.GenerateCodeAsync(model.CustomerType, cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(User);
                    await _unitOfWork.FilprideCustomer.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);

                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new customer {model.CustomerCode}", "Customer", model.Company);
                    await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Customer created successfully";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("CustomerTin", "Tin No already exist.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create customer master file. Created by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var customer = await _unitOfWork.FilprideCustomer.GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                return View(customer);
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FilprideCustomer model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.EditedBy = _userManager.GetUserName(User);
                await _unitOfWork.FilprideCustomer.UpdateAsync(model, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to edit customer master file. Created by: {UserName}", _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetCustomersList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var query = await _unitOfWork.FilprideCustomer
                    .GetAllAsync(null, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    query = query
                    .Where(c =>
                        c.CustomerCode!.ToLower().Contains(searchValue) ||
                        c.CustomerName.ToLower().Contains(searchValue) ||
                        c.CustomerTerms.ToLower().Contains(searchValue) ||
                        c.VatType.ToLower().Contains(searchValue)
                        ).ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    query = query
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalRecords = query.Count();
                var pagedData = query
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
                _logger.LogError(ex, "Failed to get customer.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Activate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var customer = await _unitOfWork
                .FilprideCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost, ActionName("Activate")]
        public async Task<IActionResult> ActivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var customer = await _unitOfWork
                .FilprideCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer == null)
            {
                return NotFound();
            }

            customer.IsActive = true;
            await _unitOfWork.SaveAsync(cancellationToken);
            TempData["success"] = "Customer has been activated";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var customer = await _unitOfWork
                .FilprideCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                return View(customer);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Deactivate")]
        public async Task<IActionResult> DeactivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var customer = await _unitOfWork
                .FilprideCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer == null)
            {
                return NotFound();
            }

            customer.IsActive = false;
            await _unitOfWork.SaveAsync(cancellationToken);
            TempData["success"] = "Customer has been deactivated";
            return RedirectToAction(nameof(Index));
        }

        //Download as .xlsx file.(Export)

        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await _unitOfWork.FilprideCustomer
                .GetAllAsync(c => recordIds.Contains(c.CustomerId));

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("Customers");

            worksheet.Cells["A1"].Value = "CustomerName";
            worksheet.Cells["B1"].Value = "CustomerAddress";
            worksheet.Cells["C1"].Value = "CustomerZipCode";
            worksheet.Cells["D1"].Value = "CustomerTinNumber";
            worksheet.Cells["E1"].Value = "BusinessStyle";
            worksheet.Cells["F1"].Value = "Terms";
            worksheet.Cells["G1"].Value = "CustomerType";
            worksheet.Cells["H1"].Value = "WithHoldingVat";
            worksheet.Cells["I1"].Value = "WithHoldingTax";
            worksheet.Cells["J1"].Value = "CreatedBy";
            worksheet.Cells["K1"].Value = "CreatedDate";
            worksheet.Cells["L1"].Value = "OriginalCustomerId";
            worksheet.Cells["M1"].Value = "OriginalCustomerNumber";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.CustomerName;
                worksheet.Cells[row, 2].Value = item.CustomerAddress;
                worksheet.Cells[row, 3].Value = item.ZipCode;
                worksheet.Cells[row, 4].Value = item.CustomerTin;
                worksheet.Cells[row, 5].Value = item.BusinessStyle;
                worksheet.Cells[row, 6].Value = item.CustomerTerms;
                worksheet.Cells[row, 7].Value = item.VatType;
                worksheet.Cells[row, 8].Value = item.WithHoldingVat;
                worksheet.Cells[row, 9].Value = item.WithHoldingTax;
                worksheet.Cells[row, 10].Value = item.CreatedBy;
                worksheet.Cells[row, 11].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 12].Value = item.CustomerId;
                worksheet.Cells[row, 13].Value = item.CustomerCode;

                row++;
            }

            //Ser password in Excel
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.SetPassword("mis123");

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CustomerList_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllCustomerIds()
        {
            var customerIds = _dbContext.FilprideCustomers
                .Select(c => c.CustomerId)
                .ToList();

            return Json(customerIds);
        }
    }
}
