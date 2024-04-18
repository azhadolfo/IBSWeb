using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace IBS.DataAccess.Repository
{
    public class FuelPurchaseRepository : Repository<FuelPurchase>, IFuelPurchaseRepository
    {
        private ApplicationDbContext _db;

        public FuelPurchaseRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task PostAsync(string id, string postedBy, CancellationToken cancellationToken = default)
        {
            try
            {
                FuelPurchase fuelPurchase = await _db
                    .FuelPurchase
                    .FirstOrDefaultAsync(f => f.FuelPurchaseNo == id, cancellationToken) ?? throw new InvalidOperationException($"Fuel purchase with id '{id}' not found.");

                if (fuelPurchase.PurchasePrice == 0)
                {
                    throw new ArgumentException("Encode first the buying price for this purchase!");
                }

                ProductDto product = await MapProductToDTO(fuelPurchase.ProductCode, cancellationToken) ?? throw new InvalidOperationException($"Product with code '{fuelPurchase.ProductCode}' not found.");

                var sortedInventory = _db
                        .Inventories
                        .OrderBy(i => i.Date)
                        .Where(i => i.ProductCode == fuelPurchase.ProductCode && i.StationCode == fuelPurchase.StationCode)
                        .ToList();

                var lastIndex = sortedInventory.FindLastIndex(s => s.Date <= fuelPurchase.DeliveryDate);
                if (lastIndex >= 0)
                {
                    sortedInventory = sortedInventory.Skip(lastIndex).ToList();
                }
                else
                {
                    throw new ArgumentException($"Beginning inventory for {fuelPurchase.ProductCode} in station {fuelPurchase.StationCode} not found!");
                }

                var previousInventory = sortedInventory.FirstOrDefault();

                fuelPurchase.PostedBy = postedBy;
                fuelPurchase.PostedDate = DateTime.Now;

                List<GeneralLedger> journals = new();

                journals.Add(new GeneralLedger
                {
                    TransactionDate = fuelPurchase.DeliveryDate,
                    Reference = fuelPurchase.FuelPurchaseNo,
                    Particular = $"{fuelPurchase.Quantity:N2} Lit {product.ProductName} @ {fuelPurchase.PurchasePrice:N2}, DR#{fuelPurchase.DrNo}",
                    AccountNumber = "1010401",
                    AccountTitle = "Inventory - Fuel",
                    Debit = (fuelPurchase.Quantity * fuelPurchase.SellingPrice) / 1.12m,
                    Credit = 0,
                    StationCode = fuelPurchase.StationCode,
                    ProductCode = fuelPurchase.ProductCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                journals.Add(new GeneralLedger
                {
                    TransactionDate = fuelPurchase.DeliveryDate,
                    Reference = fuelPurchase.FuelPurchaseNo,
                    Particular = $"{fuelPurchase.Quantity:N2} Lit {product.ProductName} @ {fuelPurchase.PurchasePrice:N2}, DR#{fuelPurchase.DrNo}",
                    AccountNumber = "1010602",
                    AccountTitle = "Vat Input",
                    Debit = ((fuelPurchase.Quantity * fuelPurchase.SellingPrice) / 1.12m) * 0.12m,
                    Credit = 0,
                    StationCode = fuelPurchase.StationCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                journals.Add(new GeneralLedger
                {
                    TransactionDate = fuelPurchase.DeliveryDate,
                    Reference = fuelPurchase.FuelPurchaseNo,
                    Particular = $"{fuelPurchase.Quantity:N2} Lit {product.ProductName} @ {fuelPurchase.PurchasePrice:N2}, DR#{fuelPurchase.DrNo}",
                    AccountNumber = "2010101",
                    AccountTitle = "Accounts Payables - Trade",
                    Debit = 0,
                    Credit = fuelPurchase.Quantity * fuelPurchase.SellingPrice,
                    StationCode = fuelPurchase.StationCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                decimal totalCost = fuelPurchase.Quantity * fuelPurchase.PurchasePrice;
                decimal runningCost = previousInventory.RunningCost + totalCost;
                decimal inventoryBalance = previousInventory.InventoryBalance + fuelPurchase.Quantity;
                decimal unitCostAverage = runningCost / inventoryBalance;
                decimal cogs = unitCostAverage * fuelPurchase.Quantity;

                var inventory = new Inventory
                {
                    Particulars = nameof(JournalType.Purchase),
                    Date = fuelPurchase.DeliveryDate,
                    Reference = $"DR#{fuelPurchase.DrNo}",
                    ProductCode = fuelPurchase.ProductCode,
                    StationCode = fuelPurchase.StationCode,
                    Quantity = fuelPurchase.Quantity,
                    UnitCost = fuelPurchase.PurchasePrice,
                    TotalCost = totalCost,
                    InventoryBalance = inventoryBalance,
                    RunningCost = runningCost,
                    UnitCostAverage = unitCostAverage,
                    CostOfGoodsSold = cogs,
                    InventoryValue = runningCost,
                    TransactionNo = fuelPurchase.FuelPurchaseNo
                };

                journals.Add(new GeneralLedger
                {
                    TransactionDate = fuelPurchase.DeliveryDate,
                    Reference = fuelPurchase.FuelPurchaseNo,
                    Particular = $"COGS:{product.ProductCode} {fuelPurchase.Quantity:N2} Lit {product.ProductName} @ {fuelPurchase.PurchasePrice:N2}, DR#{fuelPurchase.DrNo}",
                    AccountNumber = "5010101",
                    AccountTitle = "COGS - Fuel",
                    Debit = cogs,
                    Credit = 0,
                    StationCode = fuelPurchase.StationCode,
                    ProductCode = product.ProductCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                journals.Add(new GeneralLedger
                {
                    TransactionDate = fuelPurchase.DeliveryDate,
                    Reference = fuelPurchase.FuelPurchaseNo,
                    Particular = $"COGS:{product.ProductCode} {fuelPurchase.Quantity:N2} Lit {product.ProductName} @ {fuelPurchase.PurchasePrice:N2}, DR#{fuelPurchase.DrNo}",
                    AccountNumber = "5010101",
                    AccountTitle = "Inventory - Fuel",
                    Debit = 0,
                    Credit = cogs,
                    StationCode = fuelPurchase.StationCode,
                    ProductCode = product.ProductCode,
                    JournalReference = nameof(JournalType.Purchase)
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

                        unitCostAverage = transaction.UnitCostAverage;
                        runningCost = transaction.RunningCost;
                        inventoryBalance = transaction.InventoryBalance;
                    }
                    else if (transaction.Particulars == nameof(JournalType.Purchase))
                    {
                        transaction.RunningCost = runningCost + transaction.TotalCost;
                        transaction.InventoryBalance = inventoryBalance + transaction.Quantity;
                        transaction.UnitCostAverage = transaction.RunningCost / transaction.InventoryBalance;
                        transaction.CostOfGoodsSold = transaction.UnitCostAverage * transaction.Quantity;

                        unitCostAverage = transaction.UnitCostAverage;
                        runningCost = transaction.RunningCost;
                        inventoryBalance = transaction.InventoryBalance;
                    }

                    var journalEntries = _db.GeneralLedgers
                            .Where(j => j.Reference == transaction.TransactionNo && j.ProductCode == transaction.ProductCode &&
                                        (j.AccountNumber == "5010101" || j.AccountNumber == "1010401"))
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


                if (IsJournalEntriesBalanced(journals))
                {
                    await _db.AddAsync(inventory, cancellationToken);
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

        public async Task<int> ProcessFuelDelivery(string file, CancellationToken cancellationToken = default)
        {
            using var stream = new FileStream(file, FileMode.Open);
            using var reader = new StreamReader(stream);
            using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
            });

            var records = csv.GetRecords<FuelDelivery>();
            var existingRecords = await _db.Set<FuelDelivery>().ToListAsync(cancellationToken);
            var recordsToInsert = records.Where(record => !existingRecords.Exists(existingRecord =>
                existingRecord.shiftrecid == record.shiftrecid && existingRecord.stncode == record.stncode && existingRecord.productcode == record.productcode)).ToList();

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

        public async Task RecordTheDeliveryToPurchase(IEnumerable<FuelDelivery> fuelDeliveries, CancellationToken cancellationToken = default)
        {
            var fuelPurchase = new List<FuelPurchase>();

            foreach (var fuelDelivery in fuelDeliveries)
            {
                fuelPurchase.Add(new FuelPurchase
                {
                    ShiftRecId = fuelDelivery.shiftrecid,
                    StationCode = fuelDelivery.stncode,
                    CashierCode = fuelDelivery.cashiercode.Substring(1),
                    ShiftNo = fuelDelivery.shiftnumber,
                    DeliveryDate = fuelDelivery.deliverydate,
                    TimeIn = fuelDelivery.timein,
                    TimeOut = fuelDelivery.timeout,
                    Driver = fuelDelivery.driver,
                    Hauler = fuelDelivery.hauler,
                    PlateNo = fuelDelivery.platenumber,
                    DrNo = fuelDelivery.drnumber.Substring(2),
                    WcNo = fuelDelivery.wcnumber,
                    TankNo = fuelDelivery.tanknumber,
                    ProductCode = fuelDelivery.productcode,
                    PurchasePrice = fuelDelivery.purchaseprice,
                    SellingPrice = fuelDelivery.sellprice,
                    Quantity = fuelDelivery.quantity,
                    QuantityBefore = fuelDelivery.volumebefore,
                    QuantityAfter = fuelDelivery.volumeafter,
                    ShouldBe = fuelDelivery.quantity + fuelDelivery.volumebefore,
                    GainOrLoss = fuelDelivery.volumeafter - (fuelDelivery.quantity + fuelDelivery.volumebefore),
                    ReceivedBy = fuelDelivery.receivedby,
                    CreatedBy = fuelDelivery.createdby.Substring(1),
                    CreatedDate = fuelDelivery.createddate
                });
            }

            foreach (var fd in fuelPurchase)
            {
                fd.FuelPurchaseNo = await GenerateSeriesNumber(fd.StationCode);

                await _db.AddAsync(fd, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

        }

        public async Task UpdateAsync(FuelPurchase model, CancellationToken cancellationToken = default)
        {
            FuelPurchase existingFuelPurchase = await _db
                .FuelPurchase
                .FindAsync(model.FuelPurchaseId, cancellationToken) ?? throw new InvalidOperationException($"Fuel purchase with id '{model.FuelPurchaseId}' not found.");

            existingFuelPurchase.PurchasePrice = model.PurchasePrice;

            if (_db.ChangeTracker.HasChanges())
            {
                existingFuelPurchase.EditedBy = model.EditedBy;
                existingFuelPurchase.EditedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        private async Task<string> GenerateSeriesNumber(string stationCode)
        {
            var lastCashierReport = await _db.FuelPurchase
                .OrderBy(s => s.FuelPurchaseNo)
                .Where(s => s.StationCode == stationCode)
                .LastOrDefaultAsync();

            if (lastCashierReport != null)
            {
                string lastSeries = lastCashierReport.FuelPurchaseNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "FD0000000001";
            }
        }
    }
}
