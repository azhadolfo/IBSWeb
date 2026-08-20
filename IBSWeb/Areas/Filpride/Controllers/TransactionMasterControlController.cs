using IBS.Models;
using IBS.Models.Filpride.ViewModels;
using IBS.Services;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [Authorize(Roles = "Admin")]
    public class TransactionMasterControlController(
        ITransactionMasterControlService transactionMasterControlService,
        UserManager<ApplicationUser> userManager,
        ILogger<TransactionMasterControlController> logger)
        : Controller
    {
        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name
                   ?? "Unknown";
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

        public IActionResult Index()
        {
            return View(new TransactionMasterControlViewModel());
        }

        [Authorize(Roles = "Admin")]
        public IActionResult BatchReJournal()
        {
            ViewBag.ReJournalTypeOptions = GetReJournalTypeOptions();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(TransactionMasterControlViewModel searchModel, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(searchModel.ReferenceNo))
            {
                ModelState.AddModelError(nameof(searchModel.ReferenceNo), "Reference number is required.");
                return View(searchModel);
            }

            var company = await GetCompanyClaimAsync();
            var result = await transactionMasterControlService.FindTransactionAsync(searchModel.ReferenceNo, company, cancellationToken);

            if (result != null)
            {
                return RedirectToAction(nameof(Edit), new { referenceNo = result.Value.ReferenceNo, type = result.Value.Type });
            }

            TempData["error"] = "No transaction found with that reference number.";
            return View(searchModel);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string referenceNo, string type, CancellationToken cancellationToken)
        {
            var company = await GetCompanyClaimAsync();
            var model = await transactionMasterControlService.GetTransactionDetailsAsync(referenceNo, type, company, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TransactionMasterControlViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var company = await GetCompanyClaimAsync();
                if (company == null)
                {
                    throw new InvalidOperationException("Company claim is missing for the current user.");
                }

                await transactionMasterControlService.UpdateTransactionAsync(model, company, GetUserFullName(), cancellationToken);

                TempData["success"] = "Transaction updated successfully across all records.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                var safeRefNo = model.ReferenceNo.Replace("\r", string.Empty).Replace("\n", string.Empty);
                logger.LogError(ex, "Error updating transaction via Master Control. Ref: {Ref}", safeRefNo);
                TempData["error"] = "An error occurred while updating the transaction. Please contact support.";
                return View(model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReJournalAll(int? month, int? year, string? transactionType, CancellationToken cancellationToken)
        {
            if (!month.HasValue || !year.HasValue)
            {
                return BadRequest(new { success = false, error = "Month and year are required." });
            }

            transactionType = string.IsNullOrWhiteSpace(transactionType)
                ? TransactionMasterControlService.ReJournalTypeAll
                : transactionType.Trim();

            if (!TransactionMasterControlService.ReJournalTypes.Contains(transactionType, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, error = "Invalid rejournal type selected." });
            }

            var company = await GetCompanyClaimAsync();

            if (company == null)
            {
                return BadRequest(new { success = false, error = "Company claim is missing for the current user." });
            }

            try
            {
                var result = await transactionMasterControlService.ReJournalAllAsync(
                    month.Value,
                    year.Value,
                    company,
                    GetUserFullName(),
                    transactionType,
                    cancellationToken);

                return Json(new
                {
                    month,
                    year,
                    transactionType,
                    purchaseCount = result.PurchaseCount,
                    salesCount = result.SalesCount,
                    serviceCount = result.ServiceCount,
                    collectionCount = result.CollectionCount,
                    provisionalReceiptCount = result.ProvisionalReceiptCount,
                    debitMemoCount = result.DebitMemoCount,
                    creditMemoCount = result.CreditMemoCount,
                    paymentCount = result.PaymentCount,
                    jvCount = result.JvCount
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running batch rejournal for {Month}/{Year}.", month, year);
                return Json(new { success = false, error = ex.Message });
            }
        }

        private static List<SelectListItem> GetReJournalTypeOptions()
        {
            return
            [
                new() { Value = TransactionMasterControlService.ReJournalTypeAll, Text = "All" },
                new() { Value = TransactionMasterControlService.ReJournalTypePurchase, Text = "Purchase" },
                new() { Value = TransactionMasterControlService.ReJournalTypeSales, Text = "Sales" },
                new() { Value = TransactionMasterControlService.ReJournalTypeService, Text = "Service" },
                new() { Value = TransactionMasterControlService.ReJournalTypeCollection, Text = "Collection" },
                new() { Value = TransactionMasterControlService.ReJournalTypeProvisionalReceipt, Text = "Provisional Receipt" },
                new() { Value = TransactionMasterControlService.ReJournalTypeDebitMemo, Text = "Debit Memo" },
                new() { Value = TransactionMasterControlService.ReJournalTypeCreditMemo, Text = "Credit Memo" },
                new() { Value = TransactionMasterControlService.ReJournalTypePayment, Text = "Payment" },
                new() { Value = TransactionMasterControlService.ReJournalTypeJv, Text = "JV" }
            ];
        }
    }
}
