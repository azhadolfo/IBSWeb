﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class InventoryRepository : Repository<FilprideInventory>, IInventoryRepository
    {
        private ApplicationDbContext _db;

        public InventoryRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddActualInventory(ActualInventoryViewModel viewModel, string company, CancellationToken cancellationToken = default)
        {
            #region -- Actual Inventory Entry --

            var total = viewModel.Variance * viewModel.AverageCost;
            var inventoryBalance = viewModel.Variance + viewModel.PerBook;
            var totalBalance = viewModel.TotalBalance + total;
            var particular = viewModel.Variance < 0 ? "Actual Inventory(Loss)" : "Actual Inventory(Gain)";

            FilprideInventory inventory = new()
            {
                Date = viewModel.Date,
                ProductId = viewModel.ProductId,
                Quantity = Math.Abs(viewModel.Variance),
                Cost = viewModel.AverageCost,
                Particular = particular,
                Total = Math.Abs(total),
                InventoryBalance = inventoryBalance,
                AverageCost = totalBalance / inventoryBalance,
                TotalBalance = totalBalance,
                POId = viewModel.POId,
                Company = company
            };
            await _db.FilprideInventories.AddAsync(inventory, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            #endregion -- Actual Inventory Entry --

            #region -- General Book Entry --

            var ledger = new List<FilprideGeneralLedgerBook>();
            for (int i = 0; i < viewModel.AccountNumber.Length; i++)
            {
                ledger.Add(
                    new FilprideGeneralLedgerBook
                    {
                        Date = viewModel.Date,
                        Reference = inventory.InventoryId.ToString(),
                        AccountNo = viewModel.AccountNumber[i],
                        AccountTitle = viewModel.AccountTitle[i],
                        Description = particular,
                        Debit = Math.Abs(viewModel.Debit[i]),
                        Credit = Math.Abs(viewModel.Credit[i]),
                        CreatedBy = viewModel.CurrentUser,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        IsPosted = false
                    });
            }

            await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledger, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            #endregion -- General Book Entry --
        }

        public async Task AddBeginningInventory(BeginningInventoryViewModel viewModel, string company, CancellationToken cancellationToken = default)
        {
            FilprideInventory inventory = new()
            {
                Date = viewModel.Date,
                ProductId = viewModel.ProductId,
                POId = viewModel.POId,
                Quantity = viewModel.Quantity,
                Cost = viewModel.Cost,
                Particular = "Beginning Balance",
                Total = viewModel.Quantity * viewModel.Cost,
                InventoryBalance = viewModel.Quantity,
                AverageCost = viewModel.Cost,
                TotalBalance = viewModel.Quantity * viewModel.Cost,
                IsValidated = true,
                ValidatedBy = viewModel.CurrentUser,
                ValidatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                Company = company
            };

            await _db.FilprideInventories.AddAsync(inventory, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task AddPurchaseToInventoryAsync(FilprideReceivingReport receivingReport, CancellationToken cancellationToken = default)
        {
            var sortedInventory = await _db.FilprideInventories
            .Where(i => i.Company == receivingReport.Company && i.ProductId == receivingReport.PurchaseOrder.Product.ProductId && i.POId == receivingReport.POId)
            .OrderBy(i => i.Date)
            .ThenBy(i => i.InventoryId)
            .ToListAsync(cancellationToken);

            var lastIndex = sortedInventory.FindLastIndex(s => s.Date <= receivingReport.Date);
            if (lastIndex >= 0)
            {
                sortedInventory = sortedInventory.Skip(lastIndex).ToList();
            }

            var previousInventory = sortedInventory.FirstOrDefault();

            decimal total = ComputeNetOfVat(receivingReport.Amount);
            decimal inventoryBalance = lastIndex >= 0 ? previousInventory.InventoryBalance + receivingReport.QuantityReceived : receivingReport.QuantityReceived;
            decimal totalBalance = lastIndex >= 0 ? previousInventory.TotalBalance + total : total;
            decimal averageCost = totalBalance / inventoryBalance;

            FilprideInventory inventory = new()
            {
                Date = receivingReport.Date,
                ProductId = receivingReport.PurchaseOrder.ProductId,
                POId = receivingReport.POId,
                Particular = "Purchases",
                Reference = receivingReport.ReceivingReportNo,
                Quantity = receivingReport.QuantityReceived,
                Cost = total / receivingReport.QuantityReceived, //unit cost
                IsValidated = true,
                Total = total,
                InventoryBalance = inventoryBalance,
                TotalBalance = totalBalance,
                AverageCost = averageCost,
                Company = receivingReport.Company
            };

            foreach (var transaction in sortedInventory.Skip(1))
            {
                var costOfGoodsSold = 0m;
                if (transaction.Particular == "Sales")
                {
                    transaction.Cost = averageCost;
                    transaction.Total = transaction.Quantity * averageCost;
                    transaction.TotalBalance = totalBalance != 0 ? totalBalance - transaction.Total : transaction.Total;
                    transaction.InventoryBalance = inventoryBalance != 0 ? inventoryBalance - transaction.Quantity : transaction.Quantity;
                    transaction.AverageCost = transaction.TotalBalance == 0 && transaction.InventoryBalance == 0 ? transaction.Cost : transaction.TotalBalance / transaction.InventoryBalance;
                    costOfGoodsSold = transaction.AverageCost * transaction.Quantity;

                    averageCost = transaction.AverageCost;
                    totalBalance = transaction.TotalBalance;
                    inventoryBalance = transaction.InventoryBalance;

                    var journalEntries = await _db.FilprideGeneralLedgerBooks
                        .Where(j => j.Reference == transaction.Reference &&
                                    (j.AccountNo.StartsWith("50101") || j.AccountNo.StartsWith("10104")))
                        .ToListAsync(cancellationToken);

                    if (journalEntries.Count != 0)
                    {
                        foreach (var journal in journalEntries)
                        {
                            if (journal.Debit != 0)
                            {
                                if (journal.Debit != costOfGoodsSold)
                                {
                                    journal.Debit = costOfGoodsSold;
                                    journal.Credit = 0;
                                }
                            }
                            else
                            {
                                if (journal.Credit != costOfGoodsSold)
                                {
                                    journal.Credit = costOfGoodsSold;
                                    journal.Debit = 0;
                                }
                            }
                        }
                    }

                    _db.FilprideGeneralLedgerBooks.UpdateRange(journalEntries);
                }
                else if (transaction.Particular == "Purchases")
                {
                    transaction.TotalBalance = totalBalance + transaction.Total;
                    transaction.InventoryBalance = inventoryBalance + transaction.Quantity;
                    transaction.AverageCost = transaction.TotalBalance / transaction.InventoryBalance;

                    averageCost = transaction.AverageCost;
                    totalBalance = transaction.TotalBalance;
                    inventoryBalance = transaction.InventoryBalance;
                }
            }

            _db.FilprideInventories.UpdateRange(sortedInventory);

            await _db.AddAsync(inventory, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task AddSalesToInventoryAsync(FilprideSalesInvoice salesInvoice, CancellationToken cancellationToken = default)
        {
            var sortedInventory = await _db.FilprideInventories
            .Where(i => i.Company == salesInvoice.Company && i.ProductId == salesInvoice.Product.ProductId && i.POId == salesInvoice.PurchaseOrderId)
            .OrderBy(i => i.Date)
            .ThenBy(i => i.InventoryId)
            .ToListAsync(cancellationToken);

            var lastIndex = sortedInventory.FindLastIndex(s => s.Date <= salesInvoice.TransactionDate);
            if (lastIndex >= 0)
            {
                sortedInventory = sortedInventory.Skip(lastIndex).ToList();
            }
            else
            {
                throw new ArgumentException($"Beginning inventory for {salesInvoice.Product.ProductName} not found!");
            }

            var previousInventory = sortedInventory.FirstOrDefault();

            if (previousInventory != null)
            {
                if (previousInventory.InventoryBalance < salesInvoice.Quantity)
                {
                    throw new InvalidOperationException($"The requested quantity exceeds the available inventory for '{salesInvoice.Product.ProductName}'. " +
                                                        $"Please contact the Logistics department to verify the delivery date if it aligns with your transaction date.");
                }

                decimal total = salesInvoice.Quantity * previousInventory.AverageCost;
                decimal inventoryBalance = previousInventory.InventoryBalance - salesInvoice.Quantity;
                decimal totalBalance = previousInventory.TotalBalance - total;
                decimal averageCost = inventoryBalance <= 0 || totalBalance <= 0 ? previousInventory.AverageCost : totalBalance / inventoryBalance;

                FilprideInventory inventory = new()
                {
                    Date = salesInvoice.TransactionDate,
                    ProductId = salesInvoice.Product.ProductId,
                    Particular = "Sales",
                    Reference = salesInvoice.SalesInvoiceNo,
                    Quantity = salesInvoice.Quantity,
                    Cost = previousInventory.AverageCost,
                    POId = salesInvoice.PurchaseOrderId,
                    IsValidated = true,
                    ValidatedBy = salesInvoice.CreatedBy,
                    ValidatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    Total = total,
                    InventoryBalance = inventoryBalance,
                    TotalBalance = totalBalance,
                    AverageCost = averageCost,
                    Company = salesInvoice.Company
                };

                foreach (var transaction in sortedInventory.Skip(1))
                {
                    var costOfGoodsSold = 0m;
                    if (transaction.Particular == "Sales")
                    {
                        transaction.Cost = averageCost;
                        transaction.Total = transaction.Quantity * averageCost;
                        transaction.TotalBalance = totalBalance != 0 ? totalBalance - transaction.Total : transaction.Total;
                        transaction.InventoryBalance = inventoryBalance != 0 ? inventoryBalance - transaction.Quantity : transaction.Quantity;
                        transaction.AverageCost = transaction.TotalBalance == 0 && transaction.InventoryBalance == 0 ? transaction.Cost : transaction.TotalBalance / transaction.InventoryBalance;
                        costOfGoodsSold = transaction.AverageCost * transaction.Quantity;

                        averageCost = transaction.AverageCost;
                        totalBalance = transaction.TotalBalance;
                        inventoryBalance = transaction.InventoryBalance;

                        var journalEntries = await _db.FilprideGeneralLedgerBooks
                            .Where(j => j.Reference == transaction.Reference &&
                                        (j.AccountNo.StartsWith("50101") || j.AccountNo.StartsWith("10104")))
                            .ToListAsync(cancellationToken);

                        if (journalEntries.Count != 0)
                        {
                            foreach (var journal in journalEntries)
                            {
                                if (journal.Debit != 0)
                                {
                                    if (journal.Debit != costOfGoodsSold)
                                    {
                                        journal.Debit = costOfGoodsSold;
                                        journal.Credit = 0;
                                    }
                                }
                                else
                                {
                                    if (journal.Credit != costOfGoodsSold)
                                    {
                                        journal.Credit = costOfGoodsSold;
                                        journal.Debit = 0;
                                    }
                                }
                            }
                        }

                        _db.FilprideGeneralLedgerBooks.UpdateRange(journalEntries);
                    }
                    else if (transaction.Particular == "Purchases")
                    {
                        transaction.TotalBalance = totalBalance + transaction.Total;
                        transaction.InventoryBalance = inventoryBalance + transaction.Quantity;
                        transaction.AverageCost = transaction.TotalBalance / transaction.InventoryBalance;

                        averageCost = transaction.AverageCost;
                        totalBalance = transaction.TotalBalance;
                        inventoryBalance = transaction.InventoryBalance;
                    }
                }

                var (salesAccountNo, salesAccountTitle) = GetSalesAccountTitle(salesInvoice.Product.ProductCode);

                var ledgers = new List<FilprideGeneralLedgerBook>
                {
                    new FilprideGeneralLedgerBook
                    {
                        Date = salesInvoice.TransactionDate,
                        Reference = salesInvoice.SalesInvoiceNo,
                        Description = salesInvoice.Product.ProductName,
                        AccountNo = salesAccountNo,
                        AccountTitle = salesAccountTitle,
                        Debit = inventory.Total,
                        Credit = 0,
                        Company = salesInvoice.Company,
                        CreatedBy = salesInvoice.CreatedBy,
                        CreatedDate = salesInvoice.CreatedDate
                    },
                    new FilprideGeneralLedgerBook
                    {
                        Date = salesInvoice.TransactionDate,
                        Reference = salesInvoice.SalesInvoiceNo,
                        Description = salesInvoice.Product.ProductName,
                        AccountNo = salesAccountNo,
                        AccountTitle = salesAccountTitle,
                        Debit = 0,
                        Credit = inventory.Total,
                        Company = salesInvoice.Company,
                        CreatedBy = salesInvoice.CreatedBy,
                        CreatedDate = salesInvoice.CreatedDate
                    }
                };

                _db.FilprideInventories.UpdateRange(sortedInventory);

                if (IsJournalEntriesBalanced(ledgers))
                {
                    await _db.FilprideInventories.AddAsync(inventory, cancellationToken);
                    await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }
            }
            else
            {
                throw new InvalidOperationException($"Beginning inventory for this product '{salesInvoice.Product.ProductName}' not found!");
            }
        }

        public async Task ChangePriceToInventoryAsync(PurchaseChangePriceViewModel viewModel, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingPO = await _db.FilpridePurchaseOrders
                .Include(po => po.Supplier)
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == viewModel.POId, cancellationToken);

                var previousInventory = await _db.FilprideInventories
                    .Where(i => i.Company == existingPO.Company && i.ProductId == existingPO.ProductId && i.POId == viewModel.POId)
                    .OrderByDescending(i => i.Date)
                    .ThenByDescending(i => i.ProductId)
                    .Include(i => i.Product)
                    .FirstOrDefaultAsync(cancellationToken);

                var previousInventoryList = await _db.FilprideInventories
                    .Where(i => i.Company == existingPO.Company && i.ProductId == existingPO.ProductId && i.POId == viewModel.POId)
                    .OrderByDescending(i => i.Date)
                    .ThenByDescending(i => i.ProductId)
                    .ToListAsync();
                if (previousInventory != null && previousInventoryList.Any())
                {
                    var findRR = await _db.FilprideReceivingReports
                    .Where(rr => rr.POId == previousInventory.POId)
                    .ToListAsync(cancellationToken);

                    #region -- Inventory Entry --

                    var _journalVoucherRepo = new JournalVoucherRepository(_db);
                    var generateJVNo = await _journalVoucherRepo.GenerateCodeAsync(previousInventory.Company, cancellationToken);
                    FilprideInventory inventory = new()
                    {
                        Date = DateOnly.FromDateTime(DateTime.UtcNow),
                        ProductId = existingPO.ProductId,
                        POId = viewModel.POId,
                        Particular = "Change Price",
                        Reference = generateJVNo,
                        Quantity = 0,
                        Cost = 0,
                        IsValidated = true,
                        ValidatedBy = viewModel.CurrentUser,
                        ValidatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        Company = existingPO.Company
                    };

                    viewModel.FinalPrice /= 1.12m;
                    var newTotal = (viewModel.FinalPrice - previousInventory.AverageCost) * previousInventory.InventoryBalance;

                    inventory.Total = newTotal;
                    inventory.InventoryBalance = previousInventory.InventoryBalance;
                    inventory.TotalBalance = previousInventory.TotalBalance + newTotal;
                    inventory.AverageCost = inventory.TotalBalance / inventory.InventoryBalance;

                    decimal computeRRTotalAmount = findRR.Sum(rr => rr.Amount);
                    decimal productAmount = newTotal < 0 ? newTotal / 1.12m : (computeRRTotalAmount + newTotal) / 1.12m;
                    decimal vatInput = productAmount * 0.12m;
                    decimal wht = productAmount * 0.01m;
                    decimal apTradePayable = newTotal < 0 ? newTotal - wht : (computeRRTotalAmount + newTotal) - wht;

                    #endregion -- Inventory Entry --

                    #region -- Journal Voucher Entry --

                    var journalVoucherHeader = new FilprideJournalVoucherHeader
                    {
                        Date = DateOnly.FromDateTime(DateTime.UtcNow),
                        JournalVoucherHeaderNo = generateJVNo,
                        References = "",
                        Particulars = $"Change price of {existingPO.PurchaseOrderNo} from {existingPO.Price} to {existingPO.FinalPrice}",
                        CRNo = "",
                        JVReason = "Change Price",
                        CreatedBy = viewModel.CurrentUser,
                        PostedBy = viewModel.CurrentUser,
                        PostedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        Company = existingPO.Company
                    };

                    await _db.FilprideJournalVoucherHeaders.AddAsync(journalVoucherHeader, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    #region -- JV Detail Entry --

                    if (inventory.Total > 0)
                    {
                        #region -- JV Detail Entry --

                        var journalVoucherDetail = new List<FilprideJournalVoucherDetail>
                    {
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = previousInventory.Product.ProductCode == "PET001" ? "1010401" : previousInventory.Product.ProductCode == "PET002" ? "1010402" : "1010403",
                            AccountName = previousInventory.Product.ProductCode == "PET001" ? "Inventory - Biodiesel" : previousInventory.Product.ProductCode == "PET002" ? "Inventory - Econogas" : "Inventory - Envirogas",
                            TransactionNo = generateJVNo,
                            Debit = Math.Abs(productAmount),
                            Credit = 0,
                            JournalVoucherHeaderId = journalVoucherHeader.JournalVoucherHeaderId
                        },
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = "1010602",
                            AccountName = "Vat Input",
                            TransactionNo = generateJVNo,
                            Debit = Math.Abs(vatInput),
                            Credit = 0,
                            JournalVoucherHeaderId = journalVoucherHeader.JournalVoucherHeaderId
                        },
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = "2010302",
                            AccountName = "Expanded Witholding Tax 1%",
                            TransactionNo = generateJVNo,
                            Debit = 0,
                            Credit = Math.Abs(wht),
                            JournalVoucherHeaderId = journalVoucherHeader.JournalVoucherHeaderId
                        },
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = "2010101",
                            AccountName = "AP-Trade Payable",
                            TransactionNo = generateJVNo,
                            Debit = 0,
                            Credit = Math.Abs(apTradePayable),
                            JournalVoucherHeaderId = journalVoucherHeader.JournalVoucherHeaderId
                        }
                    };

                        await _db.FilprideJournalVoucherDetails.AddRangeAsync(journalVoucherDetail, cancellationToken);

                        #endregion -- JV Detail Entry --
                    }
                    else
                    {
                        #region -- JV Detail Entry --

                        var journalVoucherDetail = new List<FilprideJournalVoucherDetail>
                    {
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = previousInventory.Product.ProductCode == "PET001" ? "1010401" : previousInventory.Product.ProductCode == "PET002" ? "1010402" : "1010403",
                            AccountName = previousInventory.Product.ProductCode == "PET001" ? "Inventory - Biodiesel" : previousInventory.Product.ProductCode == "PET002" ? "Inventory - Econogas" : "Inventory - Envirogas",
                            TransactionNo = generateJVNo,
                            Debit = 0,
                            Credit = Math.Abs(productAmount),
                            JournalVoucherHeaderId = journalVoucherHeader.JournalVoucherHeaderId
                        },
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = "1010602",
                            AccountName = "Vat Input",
                            TransactionNo = generateJVNo,
                            Debit = 0,
                            Credit = Math.Abs(vatInput),
                            JournalVoucherHeaderId = journalVoucherHeader.JournalVoucherHeaderId
                        },
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = "2010302",
                            AccountName = "Expanded Witholding Tax 1%",
                            TransactionNo = generateJVNo,
                            Debit = Math.Abs(wht),
                            Credit = 0,
                            JournalVoucherHeaderId = journalVoucherHeader.JournalVoucherHeaderId
                        },
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = "2010101",
                            AccountName = "AP-Trade Payable",
                            TransactionNo = generateJVNo,
                            Debit = Math.Abs(apTradePayable),
                            Credit = 0,
                            JournalVoucherHeaderId = journalVoucherHeader.JournalVoucherHeaderId
                        }
                    };

                        await _db.AddRangeAsync(journalVoucherDetail, cancellationToken);

                        #endregion -- JV Detail Entry --
                    }

                    #endregion -- JV Detail Entry --

                    #endregion -- Journal Voucher Entry --

                    #region -- Journal Book Entry --

                    if (inventory.Total > 0)
                    {
                        #region -- Journal Book Entry --

                        var journalBook = new List<FilprideJournalBook>
                    {
                        new FilprideJournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = previousInventory.Product.ProductCode == "PET001" ? "1010401 Inventory - Biodiesel" : previousInventory.Product.ProductCode == "PET002" ? "1010402 Inventory - Econogas" : "1010403 Inventory - Envirogas",
                            Debit = Math.Abs(productAmount),
                            Credit = 0,
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company,
                        },
                        new FilprideJournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = "1010602 Vat Input",
                            Debit = Math.Abs(vatInput),
                            Credit = 0,
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company,
                        },
                        new FilprideJournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = "2010302 Expanded Witholding Tax 1%",
                            Debit = 0,
                            Credit = Math.Abs(wht),
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        },
                        new FilprideJournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = "2010101 AP-Trade Payable",
                            Debit = 0,
                            Credit = Math.Abs(apTradePayable),
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company,
                        }
                    };
                        await _db.AddRangeAsync(journalBook, cancellationToken);

                        #endregion -- Journal Book Entry --
                    }
                    else
                    {
                        #region -- Journal Book Entry --

                        var journalBook = new List<FilprideJournalBook>
                    {
                        new FilprideJournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = previousInventory.Product.ProductCode == "PET001" ? "1010401 Inventory - Biodiesel" : previousInventory.Product.ProductCode == "PET002" ? "1010402 Inventory - Econogas" : "1010403 Inventory - Envirogas",
                            Debit = 0,
                            Credit = Math.Abs(productAmount),
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        },
                        new FilprideJournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = "1010602 Vat Input",
                            Debit = 0,
                            Credit = Math.Abs(vatInput),
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        },
                        new FilprideJournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = "2010302 Expanded Witholding Tax 1%",
                            Debit = Math.Abs(wht),
                            Credit = 0,
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        },
                        new FilprideJournalBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountTitle = "2010101 AP-Trade Payable",
                            Debit = Math.Abs(apTradePayable),
                            Credit = 0,
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        }
                    };
                        await _db.AddRangeAsync(journalBook, cancellationToken);

                        #endregion -- Journal Book Entry --
                    }

                    #endregion -- Journal Book Entry --

                    #region -- Purchase Book Entry --

                    var purchaseBook = new List<FilpridePurchaseBook>
                    {
                        new FilpridePurchaseBook
                        {
                            Date = existingPO.Date,
                            SupplierName = existingPO.Supplier.SupplierName,
                            SupplierTin = existingPO.Supplier.SupplierTin,
                            SupplierAddress = existingPO.Supplier.SupplierAddress,
                            DocumentNo = "",
                            Description = existingPO.Product.ProductName,
                            Discount = 0,
                            VatAmount = inventory.Total * 0.12m,
                            Amount = newTotal,
                            WhtAmount = inventory.Total * 0.01m,
                            NetPurchases = inventory.Total,
                            PONo = existingPO.PurchaseOrderNo,
                            DueDate = DateOnly.FromDateTime(DateTime.MinValue),
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        }
                    };

                    #endregion -- Purchase Book Entry --

                    #region -- General Book Entry --

                    if (inventory.Total > 0)
                    {
                        #region -- General Book Entry --

                        var ledgers = new List<FilprideGeneralLedgerBook>
                    {
                        new FilprideGeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = previousInventory.Product.ProductCode == "PET001" ? "1010401" : previousInventory.Product.ProductCode == "PET002" ? "1010402" : "1010403",
                            AccountTitle = previousInventory.Product.ProductCode == "PET001" ? "Inventory - Biodiesel" : previousInventory.Product.ProductCode == "PET002" ? "Inventory - Econogas" : "Inventory - Envirogas",
                            Debit = Math.Abs(productAmount),
                            Credit = 0,
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        },
                        new FilprideGeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = "1010602",
                            AccountTitle = "Vat Input",
                            Debit = Math.Abs(vatInput),
                            Credit = 0,
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        },
                        new FilprideGeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = "2010302",
                            AccountTitle = "Expanded Witholding Tax 1%",
                            Debit = 0,
                            Credit = Math.Abs(wht),
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        },
                        new FilprideGeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = "2010101",
                            AccountTitle = "AP-Trade Payable",
                            Debit = 0,
                            Credit = Math.Abs(apTradePayable),
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        }
                    };
                        await _db.AddRangeAsync(ledgers, cancellationToken);

                        #endregion -- General Book Entry --
                    }
                    else
                    {
                        #region -- General Book Entry --

                        var ledgers = new List<FilprideGeneralLedgerBook>
                    {
                        new FilprideGeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = previousInventory.Product.ProductCode == "PET001" ? "1010401" : previousInventory.Product.ProductCode == "PET002" ? "1010402" : "1010403",
                            AccountTitle = previousInventory.Product.ProductCode == "PET001" ? "Inventory - Biodiesel" : previousInventory.Product.ProductCode == "PET002" ? "Inventory - Econogas" : "Inventory - Envirogas",
                            Debit = 0,
                            Credit = Math.Abs(productAmount),
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        },
                        new FilprideGeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = "1010602",
                            AccountTitle = "Vat Input",
                            Debit = 0,
                            Credit = Math.Abs(vatInput),
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        },
                        new FilprideGeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = "2010302",
                            AccountTitle = "Expanded Witholding Tax 1%",
                            Debit = Math.Abs(wht),
                            Credit = 0,
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        },
                        new FilprideGeneralLedgerBook
                        {
                            Date = existingPO.Date,
                            Reference = inventory.Reference,
                            Description = "Change Price",
                            AccountNo = "2010101",
                            AccountTitle = "AP-Trade Payable",
                            Debit = Math.Abs(apTradePayable),
                            Credit = 0,
                            CreatedBy = viewModel.CurrentUser,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            Company = inventory.Company
                        }
                    };
                        await _db.AddRangeAsync(ledgers, cancellationToken);

                        #endregion -- General Book Entry --
                    }

                    await _db.AddRangeAsync(purchaseBook, cancellationToken);
                    await _db.AddAsync(inventory, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    #endregion -- General Book Entry --
                }
                else
                {
                    throw new InvalidOperationException($"Beginning inventory for this product '{existingPO.Product.ProductName}' not found!");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> HasAlreadyBeginningInventory(int productId, int poId, string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideInventories
                .AnyAsync(i => i.Company == company && i.ProductId == productId && i.POId == poId, cancellationToken);
        }

        public async Task VoidInventory(FilprideInventory model, CancellationToken cancellationToken = default)
        {
            var sortedInventory = await _db.FilprideInventories
            .Where(i => i.Company == model.Company && i.ProductId == model.ProductId && i.POId == model.POId && i.Date >= model.Date)
            .OrderBy(i => i.Date)
            .ThenBy(i => i.InventoryId)
            .ToListAsync(cancellationToken);

            sortedInventory.Remove(model);

            if (sortedInventory.Count != 0)
            {
                var previousInventory = sortedInventory.FirstOrDefault();

                decimal total = previousInventory.Total;
                decimal inventoryBalance = previousInventory.InventoryBalance;
                decimal totalBalance = previousInventory.TotalBalance;
                decimal averageCost = inventoryBalance == 0 && totalBalance == 0 ? previousInventory.AverageCost : totalBalance / inventoryBalance;

                foreach (var transaction in sortedInventory.Skip(1))
                {
                    var costOfGoodsSold = 0m;
                    if (transaction.Particular == "Sales")
                    {
                        transaction.Cost = averageCost;
                        transaction.Total = transaction.Quantity * averageCost;
                        transaction.TotalBalance = totalBalance != 0 ? totalBalance - transaction.Total : transaction.Total;
                        transaction.InventoryBalance = inventoryBalance != 0 ? inventoryBalance - transaction.Quantity : transaction.Quantity;
                        transaction.AverageCost = transaction.TotalBalance == 0 && transaction.InventoryBalance == 0 ? previousInventory.AverageCost : transaction.TotalBalance / transaction.InventoryBalance;
                        costOfGoodsSold = transaction.AverageCost * transaction.Quantity;

                        averageCost = transaction.AverageCost;
                        totalBalance = transaction.TotalBalance;
                        inventoryBalance = transaction.InventoryBalance;

                        var journalEntries = await _db.FilprideGeneralLedgerBooks
                            .Where(j => j.Reference == transaction.Reference &&
                                        (j.AccountNo.StartsWith("50101") || j.AccountNo.StartsWith("10104")))
                            .ToListAsync(cancellationToken);

                        if (journalEntries.Count != 0)
                        {
                            foreach (var journal in journalEntries)
                            {
                                if (journal.Debit != 0)
                                {
                                    if (journal.Debit != costOfGoodsSold)
                                    {
                                        journal.Debit = costOfGoodsSold;
                                        journal.Credit = 0;
                                    }
                                }
                                else
                                {
                                    if (journal.Credit != costOfGoodsSold)
                                    {
                                        journal.Credit = costOfGoodsSold;
                                        journal.Debit = 0;
                                    }
                                }
                            }
                        }

                        _db.FilprideGeneralLedgerBooks.UpdateRange(journalEntries);
                    }
                    else if (transaction.Particular == "Purchases")
                    {
                        transaction.TotalBalance = totalBalance + transaction.Total;
                        transaction.InventoryBalance = inventoryBalance + transaction.Quantity;
                        transaction.AverageCost = transaction.TotalBalance / transaction.InventoryBalance;

                        averageCost = transaction.AverageCost;
                        totalBalance = transaction.TotalBalance;
                        inventoryBalance = transaction.InventoryBalance;
                    }
                }
            }

            _db.FilprideInventories.UpdateRange(sortedInventory);
            _db.FilprideInventories.Remove(model);

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ReCalculateInventoryAsync(List<FilprideInventory> inventories, CancellationToken cancellationToken = default)
        {
            var previousInventory = inventories.FirstOrDefault();

            decimal total = previousInventory.Total;
            decimal inventoryBalance = previousInventory.InventoryBalance;
            decimal totalBalance = previousInventory.TotalBalance;
            decimal averageCost = inventoryBalance == 0 && totalBalance == 0 ? previousInventory.AverageCost : totalBalance / inventoryBalance;

            foreach (var transaction in inventories.Skip(1))
            {
                var costOfGoodsSold = 0m;
                if (transaction.Particular == "Sales")
                {
                    transaction.Cost = averageCost;
                    transaction.Total = transaction.Quantity * averageCost;
                    transaction.TotalBalance = totalBalance != 0 ? totalBalance - transaction.Total : transaction.Total;
                    transaction.InventoryBalance = inventoryBalance != 0 ? inventoryBalance - transaction.Quantity : transaction.Quantity;
                    transaction.AverageCost = transaction.TotalBalance == 0 && transaction.InventoryBalance == 0 ? previousInventory.AverageCost : transaction.TotalBalance / transaction.InventoryBalance;
                    costOfGoodsSold = transaction.AverageCost * transaction.Quantity;

                    averageCost = transaction.AverageCost;
                    totalBalance = transaction.TotalBalance;
                    inventoryBalance = transaction.InventoryBalance;

                    var journalEntries = await _db.FilprideGeneralLedgerBooks
                        .Where(j => j.Reference == transaction.Reference &&
                                    (j.AccountNo.StartsWith("50101") || j.AccountNo.StartsWith("10104")))
                        .ToListAsync(cancellationToken);

                    if (journalEntries.Count != 0)
                    {
                        foreach (var journal in journalEntries)
                        {
                            if (journal.Debit != 0)
                            {
                                if (journal.Debit != costOfGoodsSold)
                                {
                                    journal.Debit = costOfGoodsSold;
                                    journal.Credit = 0;
                                }
                            }
                            else
                            {
                                if (journal.Credit != costOfGoodsSold)
                                {
                                    journal.Credit = costOfGoodsSold;
                                    journal.Debit = 0;
                                }
                            }
                        }
                    }

                    _db.FilprideGeneralLedgerBooks.UpdateRange(journalEntries);
                }
                else if (transaction.Particular == "Purchases")
                {
                    transaction.TotalBalance = totalBalance + transaction.Total;
                    transaction.InventoryBalance = inventoryBalance + transaction.Quantity;
                    transaction.AverageCost = transaction.TotalBalance / transaction.InventoryBalance;

                    averageCost = transaction.AverageCost;
                    totalBalance = transaction.TotalBalance;
                    inventoryBalance = transaction.InventoryBalance;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
