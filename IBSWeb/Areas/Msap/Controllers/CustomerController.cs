using IBS.Models.Msap.MasterFile;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace IBSWeb.Areas.Msap.Controllers
{
    [Area("Msap")]
    public class CustomerController(
        IUnitOfWork unitOfWork,
        ILogger<CustomerController> logger,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
        : Controller
    {
        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            try
            {
                if (view == nameof(DynamicView.Customer))
                {
                    return Task.FromResult<IActionResult>(View("ExportIndex"));
                }

                return Task.FromResult<IActionResult>(View(Enumerable.Empty<Customer>()));
            }
            catch (Exception exception)
            {
                return Task.FromException<IActionResult>(exception);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }
            var model = new CustomerViewModel()
            {

                PaymentTerms = await unitOfWork.Terms
                    .GetTermsListAsyncByCode(cancellationToken),
                Commissionees = await unitOfWork.GetCommissioneeListAsyncById(companyClaims, cancellationToken),
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerViewModel model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            model.PaymentTerms = await unitOfWork.Terms
                .GetTermsListAsyncByCode(cancellationToken);

            var isTinExist = await unitOfWork.Customer.IsTinNoExistAsync(model.CustomerTin, companyClaims, cancellationToken);

            if (isTinExist)
            {
                ModelState.AddModelError("CustomerTin", "Tin No already exist.");
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.Company = companyClaims;
                model.CustomerCode = await unitOfWork.Customer.GenerateCodeAsync(model.CustomerType, cancellationToken);
                model.CreatedBy = GetUserFullName();
                await unitOfWork.Customer.AddAsync(model, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                #region -- Audit Trail Recording

                AuditTrail auditTrailBook = new(model.CreatedBy!,
                    $"Created new Customer #{model.CustomerCode}", "Customer");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create customer master file. Created by: {UserName}", userManager.GetUserName(User));
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

            var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == id, cancellationToken);
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (customer != null)
            {
                var model = new CustomerViewModel(customer);
                model.PaymentTerms = await unitOfWork.Terms
                    .GetTermsListAsyncByCode(cancellationToken);
                model.Commissionees = await unitOfWork.GetCommissioneeListAsyncById(companyClaims, cancellationToken);
                return View(model);
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerViewModel model, CancellationToken cancellationToken)
        {
            model.PaymentTerms = await unitOfWork.Terms
                .GetTermsListAsyncByCode(cancellationToken);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.EditedBy = GetUserFullName();
                await unitOfWork.Customer.UpdateAsync(model, cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new (model.EditedBy,
                    $"Edited Customer #{model.CustomerCode}", "Customer" );
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to edit customer master file. Created by: {UserName}", userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetCustomersList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var query = await unitOfWork.Customer
                    .GetAllAsync(null, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    query = query
                    .Where(c =>
                        c.CustomerCode.ToLower().Contains(searchValue) ||
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
                logger.LogError(ex, "Failed to get customer.");
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

            var customer = await unitOfWork
                .Customer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer == null)
            {
                return NotFound();
            }

            customer.PaymentTerms = await unitOfWork.Terms
                .GetTermsListAsyncByCode(cancellationToken);

            return View(customer);
        }

        [HttpPost, ActionName("Activate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var customer = await unitOfWork
                .Customer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer == null)
            {
                return NotFound();
            }

            customer.PaymentTerms = await unitOfWork.Terms
                .GetTermsListAsyncByCode(cancellationToken);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                customer.IsActive = true;
                await unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                var user = GetUserFullName();
                AuditTrail auditTrailBook = new(
                    user, $"Activated Customer #{customer.CustomerCode}",
                    "Customer");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer has been activated";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to activate customer master file. Activated by: {UserName}", userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Activate), new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var customer = await unitOfWork
                .Customer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                customer.PaymentTerms = await unitOfWork.Terms
                    .GetTermsListAsyncByCode(cancellationToken);

                return View(customer);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Deactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var customer = await unitOfWork
                .Customer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer == null)
            {
                return NotFound();
            }

            customer.PaymentTerms = await unitOfWork.Terms
                .GetTermsListAsyncByCode(cancellationToken);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                customer.IsActive = false;
                await unitOfWork.SaveAsync(cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(GetUserFullName(),
                    $"Deactivated Customer #{customer.CustomerCode}", "Customer");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer has been deactivated";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deactivate customer master file. Deactivated by: {UserName}", userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Deactivate), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetCustomerList(
            [FromForm] DataTablesParameters parameters,
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken cancellationToken)
        {
            try
            {
                var customers = await unitOfWork.Customer
                    .GetAllAsync(null, cancellationToken);

                // Apply date range filter if provided (using CreatedDate)
                if (dateFrom.HasValue)
                {
                    customers = customers
                        .Where(s => s.CreatedDate >= dateFrom.Value)
                        .ToList();
                }

                if (dateTo.HasValue)
                {
                    // Add one day to include the entire end date
                    var dateToInclusive = dateTo.Value.AddDays(1);
                    customers = customers
                        .Where(s => s.CreatedDate < dateToInclusive)
                        .ToList();
                }

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    customers = customers
                        .Where(s =>
                            (s.CustomerCode.ToLower().Contains(searchValue)) ||
                            (s.CustomerName.ToLower().Contains(searchValue)) ||
                            (s.CustomerTin.ToLower().Contains(searchValue)) ||
                            (s.BusinessStyle != null && s.BusinessStyle.ToLower().Contains(searchValue)) ||
                            (s.CustomerTerms.ToLower().Contains(searchValue)) ||
                            (s.CustomerType.ToLower().Contains(searchValue)) ||
                            s.CreatedDate.ToString("MMM dd, yyyy").ToLower().Contains(searchValue)
                        )
                        .ToList();
                }

                // Apply sorting if provided
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    // Map frontend column names to actual entity property names
                    var columnMapping = new Dictionary<string, string>
                    {
                        { "customerCode", "CustomerCode" },
                        { "customerName", "CustomerName" },
                        { "customerTin", "CustomerTin" },
                        { "businessStyle", "BusinessStyle" },
                        { "customerTerms", "CustomerTerms" },
                        { "customerType", "CustomerType" },
                        { "createdDate", "CreatedDate" }
                    };

                    // Get the actual property name
                    var actualColumnName = columnMapping.TryGetValue(columnName, out string? value)
                        ? value : columnName;

                    customers = customers
                        .AsQueryable()
                        .OrderBy($"{actualColumnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = customers.Count();

                // Apply pagination - HANDLE -1 FOR "ALL"
                IEnumerable<Customer> pagedCustomers;

                if (parameters.Length == -1)
                {
                    // "All" selected - return all records
                    pagedCustomers = customers;
                }
                else
                {
                    // Normal pagination
                    pagedCustomers = customers
                        .Skip(parameters.Start)
                        .Take(parameters.Length);
                }

                var pagedData = pagedCustomers
                    .Select(x => new
                    {
                        x.CustomerId,
                        x.CustomerCode,
                        x.CustomerName,
                        x.CustomerTin,
                        x.BusinessStyle,
                        x.CustomerTerms,
                        x.CustomerType,
                        x.CreatedDate
                    })
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
                logger.LogError(ex, "Failed to get customers. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        //Download as .xlsx file.(Export)

        #region -- export xlsx record --

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await unitOfWork.Customer
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
                worksheet.Cells[row, 11].Value = item.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                worksheet.Cells[row, 12].Value = item.CustomerId;
                worksheet.Cells[row, 13].Value = item.CustomerCode;

                row++;
            }

            //Ser password in Excel
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.SetPassword("mis123");

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CustomerList_IBS_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllCustomerIds()
        {
            var customerIds = dbContext.Customers
                .Select(c => c.CustomerId)
                .ToList();

            return Json(customerIds);
        }
    }
}
