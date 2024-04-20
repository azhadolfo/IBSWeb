using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models;
using IBS.Models.ViewModels;
using IBS.Utility;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository
{
    public class LubePurchaseHeaderRepository : Repository<LubePurchaseHeader>, ILubePurchaseHeaderRepository
    {
        private ApplicationDbContext _db;

        public LubePurchaseHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task PostAsync(string id, string postedBy, CancellationToken cancellationToken = default)
        {
            try
            {
                LubeDeliveryVM lubeDeliveryVM = new LubeDeliveryVM
                {
                    Header = await _db.LubePurchaseHeaders.FirstOrDefaultAsync(l => l.LubePurchaseHeaderNo == id, cancellationToken),
                    Details = await _db.LubePurchaseDetails.Where(l => l.LubePurchaseHeaderNo == id).ToListAsync(cancellationToken)
                };

                if (lubeDeliveryVM.Header == null || lubeDeliveryVM.Details == null)
                {
                    throw new InvalidOperationException($"Lube purchase header/detail with id '{id}' not found.");
                }

                var lubePurchaseList = await _db.LubePurchaseHeaders
                    .Where(l => l.StationCode == lubeDeliveryVM.Header.StationCode && l.DeliveryDate <= lubeDeliveryVM.Header.DeliveryDate && l.CreatedDate < lubeDeliveryVM.Header.CreatedDate && l.PostedBy == null)
                    .OrderBy(l => l.LubePurchaseHeaderNo)
                    .ToListAsync(cancellationToken);

                if (lubePurchaseList.Count > 0)
                {
                    throw new InvalidOperationException($"Can't proceed to post, you have unposted {lubePurchaseList.First().LubePurchaseHeaderNo}");
                }

                SupplierDto supplier = await MapSupplierToDTO(lubeDeliveryVM.Header.SupplierCode, cancellationToken) ?? throw new InvalidOperationException($"Supplier with code '{lubeDeliveryVM.Header.SupplierCode}' not found.");

                lubeDeliveryVM.Header.PostedBy = postedBy;
                lubeDeliveryVM.Header.PostedDate = DateTime.Now;

                List<GeneralLedger> journals = new();
                List<Inventory> inventories = new();

                journals.Add(new GeneralLedger
                {
                    TransactionDate = lubeDeliveryVM.Header.DeliveryDate,
                    Reference = lubeDeliveryVM.Header.LubePurchaseHeaderNo,
                    Particular = $"SI#{lubeDeliveryVM.Header.SalesInvoice} DR#{lubeDeliveryVM.Header.DrNo} LUBES PURCHASE {lubeDeliveryVM.Header.DeliveryDate}",
                    AccountNumber = "1010410",
                    AccountTitle = "Inventory - Lubes",
                    Debit = lubeDeliveryVM.Header.Amount / 1.12m,
                    Credit = 0,
                    StationCode = lubeDeliveryVM.Header.StationCode,
                    JournalReference = nameof(JournalType.Purchase),
                    ProductCode = "LUBES"
                });

                journals.Add(new GeneralLedger
                {
                    TransactionDate = lubeDeliveryVM.Header.DeliveryDate,
                    Reference = lubeDeliveryVM.Header.LubePurchaseHeaderNo,
                    Particular = $"SI#{lubeDeliveryVM.Header.SalesInvoice} DR#{lubeDeliveryVM.Header.DrNo} LUBES PURCHASE {lubeDeliveryVM.Header.DeliveryDate}",
                    AccountNumber = "1010602",
                    AccountTitle = "Vat Input",
                    Debit = (lubeDeliveryVM.Header.Amount / 1.12m) * 0.12m,
                    Credit = 0,
                    StationCode = lubeDeliveryVM.Header.StationCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                journals.Add(new GeneralLedger
                {
                    TransactionDate = lubeDeliveryVM.Header.DeliveryDate,
                    Reference = lubeDeliveryVM.Header.LubePurchaseHeaderNo,
                    Particular = $"SI#{lubeDeliveryVM.Header.SalesInvoice} DR#{lubeDeliveryVM.Header.DrNo} LUBES PURCHASE {lubeDeliveryVM.Header.DeliveryDate}",
                    AccountNumber = "2010101",
                    AccountTitle = "Accounts Payables - Trade",
                    Debit = 0,
                    Credit = lubeDeliveryVM.Header.Amount,
                    StationCode = lubeDeliveryVM.Header.StationCode,
                    SupplierCode = supplier.SupplierName.ToUpper(),
                    JournalReference = nameof(JournalType.Purchase)
                });

                foreach (var lube in lubeDeliveryVM.Details)
                {
                    var sortedInventory = _db
                        .Inventories
                        .OrderBy(i => i.Date)
                        .Where(i => i.ProductCode == lube.ProductCode && i.StationCode == lubeDeliveryVM.Header.StationCode)
                        .ToList();

                    var lastIndex = sortedInventory.FindLastIndex(s => s.Date <= lubeDeliveryVM.Header.DeliveryDate);
                    if (lastIndex >= 0)
                    {
                        sortedInventory = sortedInventory.Skip(lastIndex).ToList();
                    }
                    else
                    {
                        throw new ArgumentException($"Beginning inventory for {lube.ProductCode} in station {lubeDeliveryVM.Header.StationCode} not found!");
                    }

                    var previousInventory = sortedInventory.FirstOrDefault();

                    decimal totalCost = lube.Piece * lube.CostPerPiece;
                    decimal runningCost = previousInventory.RunningCost + totalCost;
                    decimal inventoryBalance = previousInventory.InventoryBalance + lube.Piece;
                    decimal unitCostAverage = runningCost / inventoryBalance;

                    inventories.Add(new Inventory
                    {
                        Particulars = nameof(JournalType.Purchase),
                        Date = lubeDeliveryVM.Header.DeliveryDate,
                        Reference = $"DR#{lubeDeliveryVM.Header.DrNo}",
                        ProductCode = lube.ProductCode,
                        StationCode = lubeDeliveryVM.Header.StationCode,
                        Quantity = lube.Piece,
                        UnitCost = lube.CostPerPiece,
                        TotalCost = totalCost,
                        InventoryBalance = inventoryBalance,
                        RunningCost = runningCost,
                        UnitCostAverage = unitCostAverage,
                        InventoryValue = runningCost,
                        TransactionNo = lubeDeliveryVM.Header.LubePurchaseHeaderNo
                    });

                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = lubeDeliveryVM.Header.DeliveryDate,
                        Reference = lubeDeliveryVM.Header.LubePurchaseHeaderNo,
                        Particular = $"COGS:{lube.ProductCode} SI#{lubeDeliveryVM.Header.SalesInvoice} DR#{lubeDeliveryVM.Header.DrNo} LUBES PURCHASE {lubeDeliveryVM.Header.DeliveryDate}",
                        AccountNumber = "5011001",
                        AccountTitle = "COGS - Lubes",
                        Debit = cogs,
                        Credit = 0,
                        StationCode = lubeDeliveryVM.Header.StationCode,
                        JournalReference = nameof(JournalType.Purchase),
                        ProductCode = lube.ProductCode
                    });

                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = lubeDeliveryVM.Header.DeliveryDate,
                        Reference = lubeDeliveryVM.Header.LubePurchaseHeaderNo,
                        Particular = $"COGS:{lube.ProductCode} SI#{lubeDeliveryVM.Header.SalesInvoice} DR#{lubeDeliveryVM.Header.DrNo} LUBES PURCHASE {lubeDeliveryVM.Header.DeliveryDate}",
                        AccountNumber = "1010410",
                        AccountTitle = "Inventory - Lubes",
                        Debit = 0,
                        Credit = cogs,
                        StationCode = lubeDeliveryVM.Header.StationCode,
                        JournalReference = nameof(JournalType.Purchase),
                        ProductCode = lube.ProductCode
                    });

                    foreach (var transaction in sortedInventory.Skip(1))
                    {

                        if (transaction.Particulars == nameof(JournalType.Sales))
                        {
                            transaction.UnitCost = unitCostAverage;
                            transaction.TotalCost = transaction.Quantity * unitCostAverage;
                            transaction.RunningCost = runningCost - transaction.TotalCost;
                            transaction.InventoryBalance = inventoryBalance - transaction.Quantity;
                            transaction.UnitCostAverage = transaction.RunningCost / transaction.InventoryBalance;
                            transaction.CostOfGoodsSold = transaction.UnitCostAverage * transaction.Quantity;
                            transaction.InventoryValue = transaction.RunningCost;

                            unitCostAverage = transaction.UnitCostAverage;
                            runningCost = transaction.RunningCost;
                            inventoryBalance = transaction.InventoryBalance;
                        }
                        else if (transaction.Particulars == nameof(JournalType.Purchase))
                        {
                            transaction.RunningCost = runningCost + transaction.TotalCost;
                            transaction.InventoryBalance = inventoryBalance + transaction.Quantity;
                            transaction.UnitCostAverage = transaction.RunningCost / transaction.InventoryBalance;
                            transaction.InventoryValue = transaction.RunningCost;

                            unitCostAverage = transaction.UnitCostAverage;
                            runningCost = transaction.RunningCost;
                            inventoryBalance = transaction.InventoryBalance;
                        }

                        var journalEntries = _db.GeneralLedgers
                            .Where(j => j.Reference == transaction.TransactionNo && j.ProductCode == transaction.ProductCode &&
                                        (j.AccountNumber == "5011001" || j.AccountNumber == "1010410"))
                            .ToList();

                        foreach (var journal in journalEntries)
                        {
                            if (journal.Debit != 0)
                            {
                                if (journal.Debit != transaction.CostOfGoodsSold)
                                {
                                    journal.Debit = transaction.CostOfGoodsSold;
                                    journal.Credit = 0;
                                }
                            }
                            else
                            {
                                if (journal.Credit != transaction.CostOfGoodsSold)
                                {
                                    journal.Credit = transaction.CostOfGoodsSold;
                                    journal.Debit = 0;
                                }
                            }
                        }

                        _db.GeneralLedgers.UpdateRange(journalEntries);
                    }

                    _db.Inventories.UpdateRange(sortedInventory);
                }

                if (IsJournalEntriesBalanced(journals))
                {
                    await _db.Inventories.AddRangeAsync(inventories, cancellationToken);
                    await _db.GeneralLedgers.AddRangeAsync(journals, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

            }
            catch (Exception ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
        }

        public async Task<int> ProcessLubeDelivery(string file, CancellationToken cancellationToken = default)
        {
            using var stream = new FileStream(file, FileMode.Open);
            using var reader = new StreamReader(stream);
            using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
            });

            var records = csv.GetRecords<LubeDelivery>();
            var existingRecords = await _db.Set<LubeDelivery>().ToListAsync(cancellationToken);
            var recordsToInsert = records.Where(record => !existingRecords.Exists(existingRecord =>
                existingRecord.shiftrecid == record.shiftrecid && existingRecord.stncode == record.stncode && existingRecord.dtllink == record.dtllink)).ToList();

            if (recordsToInsert.Count != 0)
            {
                await _db.AddRangeAsync(recordsToInsert, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                await RecordTheDeliveryToPurchase(recordsToInsert, cancellationToken);

                return recordsToInsert.Count;
            }
            else
            {
                return 0;
            }
        }

        public async Task RecordTheDeliveryToPurchase(IEnumerable<LubeDelivery> lubeDeliveries, CancellationToken cancellationToken = default)
        {
            try
            {
                var lubePurchaseHeaders = lubeDeliveries
                    .GroupBy(l => new { l.shiftrecid, l.dtllink, l.stncode, l.cashiercode, l.shiftnumber, l.deliverydate, l.suppliercode, l.invoiceno, l.drno, l.pono, l.amount, l.rcvdby, l.createdby, l.createddate })
                    .Select(g => new LubePurchaseHeader
                    {
                        ShiftRecId = g.Key.shiftrecid,
                        DetailLink = g.Key.dtllink,
                        StationCode = g.Key.stncode,
                        CashierCode = g.Key.cashiercode.Substring(1),
                        ShiftNo = g.Key.shiftnumber,
                        DeliveryDate = g.Key.deliverydate,
                        SalesInvoice = g.Key.invoiceno.Substring(2),
                        SupplierCode = g.Key.suppliercode,
                        DrNo = g.Key.drno.Substring(2),
                        PoNo = g.Key.pono.Substring(2),
                        Amount = g.Key.amount,
                        VatableSales = g.Key.amount / 1.12m,
                        VatAmount = (g.Key.amount / 1.12m) * .12m,
                        ReceivedBy = g.Key.rcvdby,
                        CreatedBy = g.Key.createdby.Substring(1),
                        CreatedDate = g.Key.createddate
                    })
                    .ToList();

                foreach (var ld in lubePurchaseHeaders)
                {
                    ld.LubePurchaseHeaderNo = await GenerateSeriesNumber(ld.StationCode);

                    await _db.LubePurchaseHeaders.AddAsync(ld, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                foreach (var lubeDelivery in lubeDeliveries)
                {
                    var lubeHeader = lubePurchaseHeaders.Find(l => l.ShiftRecId == lubeDelivery.shiftrecid && l.StationCode == lubeDelivery.stncode);

                    var lubesPurchaseDetail = new LubePurchaseDetail
                    {
                        LubePurchaseHeaderId = lubeHeader.LubePurchaseHeaderId,
                        LubePurchaseHeaderNo = lubeHeader.LubePurchaseHeaderNo,
                        Quantity = lubeDelivery.quantity,
                        Unit = lubeDelivery.unit,
                        Description = lubeDelivery.description,
                        CostPerCase = lubeDelivery.unitprice,
                        CostPerPiece = lubeDelivery.unitprice / lubeDelivery.piece,
                        ProductCode = lubeDelivery.productcode,
                        Piece = lubeDelivery.piece,
                        Amount = lubeDelivery.quantity * lubeDelivery.unitprice
                    };

                    await _db.LubePurchaseDetails.AddAsync(lubesPurchaseDetail, cancellationToken);
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        private async Task<string> GenerateSeriesNumber(string stationCode)
        {
            var lastCashierReport = await _db.LubePurchaseHeaders
                .OrderBy(s => s.LubePurchaseHeaderNo)
                .Where(s => s.StationCode == stationCode)
                .LastOrDefaultAsync();

            if (lastCashierReport != null)
            {
                string lastSeries = lastCashierReport.LubePurchaseHeaderNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "LD0000000001";
            }
        }
    }
}
