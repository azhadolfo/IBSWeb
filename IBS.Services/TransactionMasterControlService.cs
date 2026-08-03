using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace IBS.Services
{
    public interface ITransactionMasterControlService
    {
        Task<(string Type, string ReferenceNo)?> FindTransactionAsync(string referenceNo, string? company, CancellationToken cancellationToken);
        Task<TransactionMasterControlViewModel?> GetTransactionDetailsAsync(string referenceNo, string type, string? company, CancellationToken cancellationToken);
        Task UpdateTransactionAsync(TransactionMasterControlViewModel model, string? company, string userFullName, CancellationToken cancellationToken);
        Task<ReJournalBatchResult> ReJournalAllAsync(int month, int year, string company, string userFullName, CancellationToken cancellationToken);
    }

    public sealed class ReJournalBatchResult
    {
        public int PurchaseCount { get; init; }
        public int SalesCount { get; init; }
        public int ServiceCount { get; init; }
        public int CollectionCount { get; init; }
        public int ProvisionalReceiptCount { get; init; }
        public int DebitMemoCount { get; init; }
        public int CreditMemoCount { get; init; }
        public int PaymentCount { get; init; }
        public int JvCount { get; init; }
    }

    public class TransactionMasterControlService(
        ApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<TransactionMasterControlService> logger)
        : ITransactionMasterControlService
    {
        private const string _paymentForSeparator = ". Payment for ";
        private const string _dateFormat = "MM/dd/yyyy";

        public async Task<(string Type, string ReferenceNo)?> FindTransactionAsync(string referenceNo, string? company, CancellationToken cancellationToken)
        {
            referenceNo = referenceNo.Trim();

            if (await dbContext.FilprideCheckVoucherHeaders.AnyAsync(x => x.CheckVoucherHeaderNo == referenceNo && x.Company == company, cancellationToken))
            {
                return ("CV", referenceNo);
            }

            if (await dbContext.FilprideJournalVoucherHeaders.AnyAsync(x => x.JournalVoucherHeaderNo == referenceNo && x.Company == company, cancellationToken))
            {
                return ("JV", referenceNo);
            }

            if (await dbContext.FilprideSalesInvoices.AnyAsync(x => x.SalesInvoiceNo == referenceNo && x.Company == company, cancellationToken))
            {
                return ("SI", referenceNo);
            }

            if (await dbContext.FilprideServiceInvoices.AnyAsync(x => x.ServiceInvoiceNo == referenceNo && x.Company == company, cancellationToken))
            {
                return ("SV", referenceNo);
            }

            if (await dbContext.FilprideCollectionReceipts.AnyAsync(x => x.CollectionReceiptNo == referenceNo && x.Company == company, cancellationToken))
            {
                return ("CR", referenceNo);
            }

            return null;
        }

        public async Task<TransactionMasterControlViewModel?> GetTransactionDetailsAsync(string referenceNo, string type, string? company, CancellationToken cancellationToken)
        {
            TransactionMasterControlViewModel model = new() { ReferenceNo = referenceNo, TransactionType = type };

            if (type == "CV")
            {
                var header = await dbContext.FilprideCheckVoucherHeaders
                    .FirstOrDefaultAsync(x => x.CheckVoucherHeaderNo == referenceNo && x.Company == company, cancellationToken);

                if (header == null)
                {
                    return null;
                }

                model.Date = header.Date;
                var particulars = header.Particulars ?? string.Empty;
                var index = particulars.IndexOf(_paymentForSeparator, StringComparison.Ordinal);
                if (index >= 0)
                {
                    model.Particulars = particulars.Substring(0, index).Trim();
                    model.PaymentFor = particulars.Substring(index + _paymentForSeparator.Length).Trim();
                }
                else
                {
                    model.Particulars = particulars;
                }

                model.Payee = header.Payee;
                model.CheckNo = header.CheckNo;
                model.CheckDate = header.CheckDate;
                model.IsFound = true;
            }
            else if (type == "JV")
            {
                var header = await dbContext.FilprideJournalVoucherHeaders
                    .FirstOrDefaultAsync(x => x.JournalVoucherHeaderNo == referenceNo && x.Company == company, cancellationToken);

                if (header == null)
                {
                    return null;
                }

                model.Date = header.Date;
                model.Particulars = header.Particulars;
                model.IsFound = true;
            }
            else if (type == "SI")
            {
                var header = await dbContext.FilprideSalesInvoices
                    .FirstOrDefaultAsync(x => x.SalesInvoiceNo == referenceNo && x.Company == company, cancellationToken);

                if (header == null)
                {
                    return null;
                }

                model.Date = header.TransactionDate;
                model.Particulars = header.Remarks;
                model.IsFound = true;
            }
            else if (type == "SV")
            {
                var header = await dbContext.FilprideServiceInvoices
                    .FirstOrDefaultAsync(x => x.ServiceInvoiceNo == referenceNo && x.Company == company, cancellationToken);

                if (header == null)
                {
                    return null;
                }

                model.Date = header.Period;
                model.Particulars = header.Instructions;
                model.IsFound = true;
            }
            else if (type == "CR")
            {
                var header = await dbContext.FilprideCollectionReceipts
                    .FirstOrDefaultAsync(x => x.CollectionReceiptNo == referenceNo && x.Company == company, cancellationToken);

                if (header == null)
                {
                    return null;
                }

                model.Date = header.TransactionDate;
                model.Particulars = header.Remarks ?? string.Empty;
                model.IsFound = true;
            }
            else
            {
                return null;
            }

            return model;
        }

        public async Task UpdateTransactionAsync(TransactionMasterControlViewModel model, string? company, string userFullName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(company))
            {
                throw new InvalidOperationException("Company claim is missing for the current user.");
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model.TransactionType == "CV")
                {
                    var header = await dbContext.FilprideCheckVoucherHeaders
                        .FirstOrDefaultAsync(x => x.CheckVoucherHeaderNo == model.ReferenceNo && x.Company == company, cancellationToken);

                    if (header == null)
                    {
                        throw new InvalidOperationException("CV Header not found.");
                    }

                    var finalParticulars = !string.IsNullOrWhiteSpace(model.PaymentFor)
                        ? $"{model.Particulars}{_paymentForSeparator}{model.PaymentFor}"
                        : model.Particulars;

                    header.Particulars = finalParticulars;
                    header.Payee = model.Payee;
                    header.CheckNo = model.CheckNo;
                    header.CheckDate = model.CheckDate;
                    StampEdited(header, userFullName);

                    await UpdateGeneralLedgerBooksAsync(model.ReferenceNo, finalParticulars, company, cancellationToken);

                    if (header.CvType == nameof(CVType.Invoicing))
                    {
                        var paymentCvIds = await dbContext.FilprideMultipleCheckVoucherPayments
                            .Where(x => x.CheckVoucherHeaderInvoiceId == header.CheckVoucherHeaderId)
                            .Select(x => x.CheckVoucherHeaderPaymentId)
                            .ToListAsync(cancellationToken);

                        var paymentHeaders = await dbContext.FilprideCheckVoucherHeaders
                            .Where(x => paymentCvIds.Contains(x.CheckVoucherHeaderId))
                            .ToDictionaryAsync(x => x.CheckVoucherHeaderId, cancellationToken);

                        foreach (var paymentId in paymentCvIds)
                        {
                            if (!paymentHeaders.TryGetValue(paymentId, out var paymentHeader))
                            {
                                continue;
                            }

                            var oldPaymentParticulars = paymentHeader.Particulars ?? "";
                            var paymentIndex = oldPaymentParticulars.IndexOf(_paymentForSeparator, StringComparison.Ordinal);

                            if (paymentIndex < 0)
                            {
                                continue;
                            }

                            var suffix = oldPaymentParticulars.Substring(paymentIndex);
                            var newPaymentParticulars = model.Particulars + suffix;

                            if (paymentHeader.Particulars == newPaymentParticulars)
                            {
                                continue;
                            }

                            paymentHeader.Particulars = newPaymentParticulars;
                            StampEdited(paymentHeader, userFullName);

                            await UpdateGeneralLedgerBooksAsync(paymentHeader.CheckVoucherHeaderNo!, newPaymentParticulars, company, cancellationToken);
                        }
                    }
                }
                else if (model.TransactionType == "JV")
                {
                    var header = await dbContext.FilprideJournalVoucherHeaders
                        .FirstOrDefaultAsync(x => x.JournalVoucherHeaderNo == model.ReferenceNo && x.Company == company, cancellationToken);
                    if (header == null)
                    {
                        throw new InvalidOperationException("JV Header not found.");
                    }

                    header.Particulars = model.Particulars;
                    StampEdited(header, userFullName);

                    await UpdateGeneralLedgerBooksAsync(model.ReferenceNo, model.Particulars, company, cancellationToken);
                }
                else if (model.TransactionType == "SI")
                {
                    var header = await dbContext.FilprideSalesInvoices
                        .FirstOrDefaultAsync(x => x.SalesInvoiceNo == model.ReferenceNo && x.Company == company, cancellationToken);
                    if (header == null)
                    {
                        throw new InvalidOperationException("SI Header not found.");
                    }

                    header.Remarks = model.Particulars;
                    StampEdited(header, userFullName);

                }
                else if (model.TransactionType == "SV")
                {
                    var header = await dbContext.FilprideServiceInvoices
                        .FirstOrDefaultAsync(x => x.ServiceInvoiceNo == model.ReferenceNo && x.Company == company, cancellationToken);
                    if (header == null)
                    {
                        throw new InvalidOperationException("SV Header not found.");
                    }

                    header.Instructions = model.Particulars;
                    StampEdited(header, userFullName);

                }
                else if (model.TransactionType == "CR")
                {
                    var header = await dbContext.FilprideCollectionReceipts
                        .FirstOrDefaultAsync(x => x.CollectionReceiptNo == model.ReferenceNo && x.Company == company, cancellationToken);
                    if (header == null)
                    {
                        throw new InvalidOperationException("CR Header not found.");
                    }

                    header.Remarks = model.Particulars;
                    StampEdited(header, userFullName);

                }

                await dbContext.SaveChangesAsync(cancellationToken);

                FilprideAuditTrail auditTrail = new(
                    userFullName,
                    $"Updated particulars/metadata for {model.TransactionType}# {model.ReferenceNo} via Master Control",
                    "Master Control",
                    company
                );
                await unitOfWork.FilprideAuditTrail.AddAsync(auditTrail, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                var safeRefNo = model.ReferenceNo.Replace("\r", string.Empty).Replace("\n", string.Empty);
                logger.LogError(ex, "Error updating transaction via Master Control. Ref: {Ref}", safeRefNo);
                throw;
            }
        }

        private static void StampEdited(BaseEntity header, string userFullName)
        {
            header.EditedBy = userFullName;
            header.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
        }

        public async Task<ReJournalBatchResult> ReJournalAllAsync(int month, int year, string company, string userFullName, CancellationToken cancellationToken)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var purchaseCount = await ReJournalPurchaseAsync(month, year, company, cancellationToken);
                var salesCount = await ReJournalSalesAsync(month, year, company, cancellationToken);
                var serviceCount = await ReJournalServiceAsync(month, year, company, userFullName, cancellationToken);
                var collectionCount = await ReJournalCollectionAsync(month, year, company, cancellationToken);
                var provisionalReceiptCount = await ReJournalProvisionalReceiptAsync(month, year, company, cancellationToken);
                var debitMemoCount = await ReJournalDebitMemoAsync(month, year, company, cancellationToken);
                var creditMemoCount = await ReJournalCreditMemoAsync(month, year, company, cancellationToken);
                var paymentCount = await ReJournalPaymentAsync(month, year, company, cancellationToken);
                var jvCount = await ReJournalJvAsync(month, year, company, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return new ReJournalBatchResult
                {
                    PurchaseCount = purchaseCount,
                    SalesCount = salesCount,
                    ServiceCount = serviceCount,
                    CollectionCount = collectionCount,
                    ProvisionalReceiptCount = provisionalReceiptCount,
                    DebitMemoCount = debitMemoCount,
                    CreditMemoCount = creditMemoCount,
                    PaymentCount = paymentCount,
                    JvCount = jvCount
                };
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task<int> ReJournalPurchaseAsync(int month, int year, string company, CancellationToken cancellationToken)
        {
            var receivingReports = await unitOfWork.FilprideReceivingReport
                .GetAllAsync(x =>
                    x.Company == company &&
                    x.Status == nameof(Status.Posted) &&
                    x.Date.Month == month &&
                    x.Date.Year == year,
                    cancellationToken);

            var records = receivingReports
                .OrderBy(x => x.Date)
                .ToList();

            if (records.Count == 0)
            {
                return 0;
            }

            var references = records
                .Select(x => x.ReceivingReportNo!)
                .Distinct()
                .ToList();

            var existingGlEntries = await dbContext.FilprideGeneralLedgerBooks
                .Where(x => x.Company == company && references.Contains(x.Reference))
                .ToListAsync(cancellationToken);

            if (existingGlEntries.Count != 0)
            {
                dbContext.FilprideGeneralLedgerBooks.RemoveRange(existingGlEntries);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var inventory = await dbContext.FilprideInventories
                .Where(x => x.Company == company && references.Contains(x.Reference!))
                .ToListAsync(cancellationToken);

            if (inventory.Count != 0)
            {
                dbContext.FilprideInventories.RemoveRange(inventory);
            }

            foreach (var receivingReport in records)
            {
                await unitOfWork.FilprideReceivingReport.PostAsync(receivingReport, cancellationToken);
                await unitOfWork.FilprideInventory.AddPurchaseToInventoryAsync(receivingReport, cancellationToken);
            }

            return records.Count;
        }

        private async Task<int> ReJournalSalesAsync(int month, int year, string company, CancellationToken cancellationToken)
        {
            var drs = await unitOfWork.FilprideDeliveryReceipt
                .GetAllAsync(x =>
                        x.Company == company &&
                        x.VoidedBy == null &&
                        x.CanceledDate == null &&
                        x.DeliveredDate.HasValue &&
                        x.DeliveredDate.Value.Month == month &&
                        x.DeliveredDate.Value.Year == year,
                    cancellationToken);

            var records = drs
                .OrderBy(x => x.DeliveredDate)
                .ToList();

            if (records.Count == 0)
            {
                return 0;
            }

            var references = records
                .Select(x => x.DeliveryReceiptNo)
                .Distinct()
                .ToList();

            var existingGlEntries = await dbContext.FilprideGeneralLedgerBooks
                .Where(x => x.Company == company && references.Contains(x.Reference))
                .ToListAsync(cancellationToken);

            var inventory = await dbContext.FilprideInventories
                .Where(x => x.Company == company && references.Contains(x.Reference!))
                .ToListAsync(cancellationToken);

            if (inventory.Count != 0)
            {
                dbContext.FilprideInventories.RemoveRange(inventory);
            }

            if (existingGlEntries.Count != 0)
            {
                dbContext.FilprideGeneralLedgerBooks.RemoveRange(existingGlEntries);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            foreach (var dr in records)
            {
                await unitOfWork.FilprideInventory.AddSalesToInventoryAsync(dr, cancellationToken);
                await unitOfWork.FilprideDeliveryReceipt.PostAsync(dr, cancellationToken);
            }

            return records.Count;
        }

        private async Task<int> ReJournalServiceAsync(int month, int year, string company, string userFullName, CancellationToken cancellationToken)
        {
            var serviceInvoices = await unitOfWork.FilprideServiceInvoice
                .GetAllAsync(x =>
                        x.Company == company &&
                        x.Status == nameof(Status.Posted) &&
                        x.Period.Month == month &&
                        x.Period.Year == year,
                    cancellationToken);

            var records = serviceInvoices
                .OrderBy(x => x.Period)
                .ToList();

            if (records.Count == 0)
            {
                return 0;
            }

            var references = records
                .Select(x => x.ServiceInvoiceNo)
                .Distinct()
                .ToList();

            var existingGlEntries = await dbContext.FilprideGeneralLedgerBooks
                .Where(x => x.Company == company && references.Contains(x.Reference))
                .ToListAsync(cancellationToken);

            if (existingGlEntries.Count != 0)
            {
                dbContext.FilprideGeneralLedgerBooks.RemoveRange(existingGlEntries);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            foreach (var service in records.Where(x => x.ServiceName == "TRANSACTION FEE"))
            {
                await RevertTheReversalOfDrEntriesAsync(service.DeliveryReceiptId, company, cancellationToken);
            }

            foreach (var service in records)
            {
                await unitOfWork.FilprideServiceInvoice.PostAsync(service, cancellationToken);

                if (service.ServiceName == "TRANSACTION FEE")
                {
                    await ReverseDrEntriesAsync(service.DeliveryReceiptId, company, userFullName, cancellationToken);
                }
            }

            return records.Count;
        }

        private async Task<int> ReJournalPaymentAsync(int month, int year, string company, CancellationToken cancellationToken)
        {
            var cvs = await dbContext.FilprideCheckVoucherHeaders
                .Include(x => x.Details)
                .Where(x =>
                    x.Company == company &&
                    x.PostedBy != null &&
                    x.Date.Month == month &&
                    x.Date.Year == year)
                .ToListAsync(cancellationToken);

            if (cvs.Count == 0)
            {
                return 0;
            }

            var references = cvs
                .Select(x => x.CheckVoucherHeaderNo!)
                .Distinct()
                .ToList();

            var existingGlEntries = await dbContext.FilprideGeneralLedgerBooks
                .Where(x => x.Company == company && references.Contains(x.Reference))
                .ToListAsync(cancellationToken);

            if (existingGlEntries.Count != 0)
            {
                dbContext.FilprideGeneralLedgerBooks.RemoveRange(existingGlEntries);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            foreach (var cv in cvs.OrderBy(x => x.Date))
            {
                await unitOfWork.FilprideCheckVoucher.PostAsync(cv,
                    cv.Details!.Where(x => !x.IsDisplayEntry),
                    cancellationToken);
            }

            return cvs.Count;
        }

        private async Task<int> ReJournalCollectionAsync(int month, int year, string company, CancellationToken cancellationToken)
        {
            var records = (await unitOfWork.FilprideCollectionReceipt.GetAllAsync(x =>
                    x.Company == company &&
                    x.PostedBy != null &&
                    x.Status != nameof(CollectionReceiptStatus.Voided) &&
                    x.Status != nameof(CollectionReceiptStatus.Canceled) &&
                    x.TransactionDate.Month == month &&
                    x.TransactionDate.Year == year,
                cancellationToken))
                .OrderBy(x => x.TransactionDate)
                .ToList();

            if (records.Count == 0)
            {
                return 0;
            }

            var references = records
                .Select(x => x.CollectionReceiptNo!)
                .Distinct()
                .ToList();

            var existingGlEntries = await dbContext.FilprideGeneralLedgerBooks
                .Where(x => x.Company == company && references.Contains(x.Reference))
                .ToListAsync(cancellationToken);

            if (existingGlEntries.Count != 0)
            {
                dbContext.FilprideGeneralLedgerBooks.RemoveRange(existingGlEntries);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            foreach (var record in records)
            {
                var collectionReceipt = await unitOfWork.FilprideCollectionReceipt
                    .GetAsync(x => x.CollectionReceiptId == record.CollectionReceiptId, cancellationToken)
                    ?? throw new ArgumentException($"Collection receipt '{record.CollectionReceiptNo}' not found.");

                await unitOfWork.FilprideCollectionReceipt.PostAsync(collectionReceipt, cancellationToken);

                if (collectionReceipt.DepositedDate != null && collectionReceipt.ClearedDate != null)
                {
                    await unitOfWork.FilprideCollectionReceipt.DepositAsync(collectionReceipt, cancellationToken);
                    await ReApplyCollectionCostOfMoneyAsync(collectionReceipt, company, cancellationToken);
                }
            }

            return records.Count;
        }

        private async Task<int> ReJournalProvisionalReceiptAsync(int month, int year, string company, CancellationToken cancellationToken)
        {
            var records = (await unitOfWork.ProvisionalReceipt.GetAllAsync(x =>
                    x.Company == company &&
                    x.PostedBy != null &&
                    x.Status != nameof(CollectionReceiptStatus.Voided) &&
                    x.Status != nameof(CollectionReceiptStatus.Canceled) &&
                    x.DepositedDate != null &&
                    x.TransactionDate.Month == month &&
                    x.TransactionDate.Year == year,
                cancellationToken))
                .OrderBy(x => x.TransactionDate)
                .ToList();

            if (records.Count == 0)
            {
                return 0;
            }

            var references = records
                .Select(x => x.SeriesNumber)
                .Distinct()
                .ToList();

            var existingGlEntries = await dbContext.FilprideGeneralLedgerBooks
                .Where(x => x.Company == company && references.Contains(x.Reference))
                .ToListAsync(cancellationToken);

            if (existingGlEntries.Count != 0)
            {
                dbContext.FilprideGeneralLedgerBooks.RemoveRange(existingGlEntries);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            foreach (var record in records)
            {
                var provisionalReceipt = await unitOfWork.ProvisionalReceipt
                    .GetAsync(x => x.Id == record.Id, cancellationToken)
                    ?? throw new ArgumentException($"Provisional receipt '{record.SeriesNumber}' not found.");

                if (provisionalReceipt.DepositedDate != null && provisionalReceipt.ClearedDate != null)
                {
                    await unitOfWork.ProvisionalReceipt.DepositAsync(provisionalReceipt, cancellationToken);
                }
            }

            return records.Count;
        }

        private async Task ReApplyCollectionCostOfMoneyAsync(
            Models.Filpride.AccountsReceivable.FilprideCollectionReceipt collectionReceipt,
            string company,
            CancellationToken cancellationToken)
        {
            if (collectionReceipt.DepositedDate == null)
            {
                return;
            }

            foreach (var receipt in collectionReceipt.ReceiptDetails!)
            {
                var salesInvoice = await unitOfWork.FilprideSalesInvoice
                    .GetAsync(x => x.SalesInvoiceNo == receipt.InvoiceNo && x.Company == company, cancellationToken);

                if (salesInvoice?.DeliveryReceipt == null || salesInvoice.CustomerOrderSlip == null)
                {
                    continue;
                }

                var hasWvat = salesInvoice.CustomerOrderSlip.HasWVAT;
                var hasWtax = salesInvoice.CustomerOrderSlip.HasEWT;
                var isVatable = salesInvoice.CustomerOrderSlip.VatType == SD.VatType_Vatable;
                var dr = salesInvoice.DeliveryReceipt;
                var getHolidays = await DateTimeHelper.GetNonWorkingDays(salesInvoice.DueDate, collectionReceipt.DepositedDate.Value);
                var daysDelayed = collectionReceipt.DepositedDate.Value.DayNumber - salesInvoice.DueDate.DayNumber - getHolidays.Count;

                if (daysDelayed <= 0 || dr.CommissionAmount <= 0 || dr.IsCostOfMoneyApplied)
                {
                    continue;
                }

                var netOfVat = isVatable
                    ? unitOfWork.FilprideCollectionReceipt.ComputeNetOfVat(receipt.Amount)
                    : receipt.Amount;
                var wvatAmount = hasWvat
                    ? unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVat, 0.05m)
                    : 0m;
                var wtaxAmount = hasWtax
                    ? unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVat, 0.01m)
                    : 0m;
                var paymentAmount = receipt.Amount - wvatAmount - wtaxAmount;

                var costOfMoney = paymentAmount * .03m * daysDelayed / 360m;

                await unitOfWork.FilprideCollectionReceipt.ApplyCostOfMoney(dr, costOfMoney,
                    "Batch ReJournal", collectionReceipt.DepositedDate.Value, cancellationToken);
            }
        }

        private async Task<int> ReJournalDebitMemoAsync(int month, int year, string company, CancellationToken cancellationToken)
        {
            var records = (await unitOfWork.FilprideDebitMemo.GetAllAsync(x =>
                    x.Company == company &&
                    x.PostedBy != null &&
                    x.Status == nameof(Status.Posted) &&
                    x.TransactionDate.Month == month &&
                    x.TransactionDate.Year == year,
                cancellationToken))
                .OrderBy(x => x.TransactionDate)
                .ToList();

            if (records.Count == 0)
            {
                return 0;
            }

            var references = records
                .Select(x => x.DebitMemoNo!)
                .Distinct()
                .ToList();

            var existingGlEntries = await dbContext.FilprideGeneralLedgerBooks
                .Where(x => x.Company == company && references.Contains(x.Reference))
                .ToListAsync(cancellationToken);

            if (existingGlEntries.Count != 0)
            {
                dbContext.FilprideGeneralLedgerBooks.RemoveRange(existingGlEntries);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            foreach (var record in records)
            {
                var debitMemo = await unitOfWork.FilprideDebitMemo
                    .GetAsync(x => x.DebitMemoId == record.DebitMemoId, cancellationToken)
                    ?? throw new ArgumentException($"Debit memo '{record.DebitMemoNo}' not found.");

                await unitOfWork.FilprideDebitMemo.PostAsync(debitMemo, cancellationToken);
            }

            return records.Count;
        }

        private async Task<int> ReJournalCreditMemoAsync(int month, int year, string company, CancellationToken cancellationToken)
        {
            var records = (await unitOfWork.FilprideCreditMemo.GetAllAsync(x =>
                    x.Company == company &&
                    x.PostedBy != null &&
                    x.Status == nameof(Status.Posted) &&
                    x.TransactionDate.Month == month &&
                    x.TransactionDate.Year == year,
                cancellationToken))
                .OrderBy(x => x.TransactionDate)
                .ToList();

            if (records.Count == 0)
            {
                return 0;
            }

            var references = records
                .Select(x => x.CreditMemoNo!)
                .Distinct()
                .ToList();

            var existingGlEntries = await dbContext.FilprideGeneralLedgerBooks
                .Where(x => x.Company == company && references.Contains(x.Reference))
                .ToListAsync(cancellationToken);

            if (existingGlEntries.Count != 0)
            {
                dbContext.FilprideGeneralLedgerBooks.RemoveRange(existingGlEntries);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            foreach (var record in records)
            {
                var creditMemo = await unitOfWork.FilprideCreditMemo
                    .GetAsync(x => x.CreditMemoId == record.CreditMemoId, cancellationToken)
                    ?? throw new ArgumentException($"Credit memo '{record.CreditMemoNo}' not found.");

                await unitOfWork.FilprideCreditMemo.PostAsync(creditMemo, cancellationToken);
            }

            return records.Count;
        }

        private async Task<int> ReJournalJvAsync(int month, int year, string company, CancellationToken cancellationToken)
        {
            var jvs = await dbContext.FilprideJournalVoucherHeaders
                .Include(x => x.Details)
                .Where(x =>
                    x.Company == company &&
                    x.PostedBy != null &&
                    x.Date.Month == month &&
                    x.Date.Year == year)
                .ToListAsync(cancellationToken);

            if (jvs.Count == 0)
            {
                return 0;
            }

            var references = jvs
                .Select(x => x.JournalVoucherHeaderNo!)
                .Distinct()
                .ToList();

            var existingGlEntries = await dbContext.FilprideGeneralLedgerBooks
                .Where(x => x.Company == company && references.Contains(x.Reference))
                .ToListAsync(cancellationToken);

            if (existingGlEntries.Count != 0)
            {
                dbContext.FilprideGeneralLedgerBooks.RemoveRange(existingGlEntries);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            foreach (var jv in jvs.OrderBy(x => x.Date))
            {
                await unitOfWork.FilprideJournalVoucher.PostAsync(jv, jv.Details!, cancellationToken);
            }

            return jvs.Count;
        }

        private async Task RevertTheReversalOfDrEntriesAsync(int? deliveryReceiptId, string company, CancellationToken cancellationToken)
        {
            if (!deliveryReceiptId.HasValue)
            {
                return;
            }

            var dr = await unitOfWork.FilprideDeliveryReceipt
                .GetAsync(x => x.DeliveryReceiptId == deliveryReceiptId.Value, cancellationToken);

            if (dr == null)
            {
                return;
            }

            var relatedRrNo = (await unitOfWork.FilprideReceivingReport
                    .GetAsync(x => x.DeliveryReceiptId == dr.DeliveryReceiptId, cancellationToken))?
                .ReceivingReportNo;

            await dbContext.FilprideGeneralLedgerBooks
                .Where(x => (x.Reference == dr.DeliveryReceiptNo || (relatedRrNo != null && x.Reference == relatedRrNo))
                            && x.Company == company && x.Description.Contains("Reversal of entries due to recording of transaction fee."))
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task ReverseDrEntriesAsync(int? deliveryReceiptId, string company, string userFullName, CancellationToken cancellationToken)
        {
            if (!deliveryReceiptId.HasValue)
            {
                return;
            }

            var dr = await unitOfWork.FilprideDeliveryReceipt
                .GetAsync(x => x.DeliveryReceiptId == deliveryReceiptId.Value, cancellationToken);

            if (dr == null)
            {
                return;
            }

            var relatedRrNo = (await unitOfWork.FilprideReceivingReport
                    .GetAsync(x => x.DeliveryReceiptId == dr.DeliveryReceiptId, cancellationToken))?
                .ReceivingReportNo;

            var originalEntries = await dbContext.FilprideGeneralLedgerBooks
                .Where(x => (x.Reference == dr.DeliveryReceiptNo || (relatedRrNo != null && x.Reference == relatedRrNo))
                            && x.Company == company)
                .ToListAsync(cancellationToken);

            var reversalEntries = new List<FilprideGeneralLedgerBook>();

            foreach (var originalEntry in originalEntries)
            {
                reversalEntries.Add(new FilprideGeneralLedgerBook
                {
                    Date = new DateOnly(
                        originalEntry.Date.Year,
                        originalEntry.Date.Month,
                        DateTime.DaysInMonth(originalEntry.Date.Year, originalEntry.Date.Month)),
                    Reference = originalEntry.Reference,
                    AccountNo = originalEntry.AccountNo,
                    AccountTitle = originalEntry.AccountTitle,
                    Description = "Reversal of entries due to recording of transaction fee.",
                    Debit = originalEntry.Credit,
                    Credit = originalEntry.Debit,
                    CreatedBy = userFullName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    IsPosted = true,
                    Company = originalEntry.Company,
                    AccountId = originalEntry.AccountId,
                    SubAccountType = originalEntry.SubAccountType,
                    SubAccountId = originalEntry.SubAccountId,
                    SubAccountName = originalEntry.SubAccountName,
                    ModuleType = originalEntry.ModuleType,
                });
            }

            await dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(reversalEntries, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task UpdateGeneralLedgerBooksAsync(string referenceNo, string particulars, string company, CancellationToken cancellationToken)
        {
            await dbContext.FilprideGeneralLedgerBooks
                .Where(x => x.Reference == referenceNo && x.Company == company)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Description, particulars),
                    cancellationToken);
        }
    }
}
