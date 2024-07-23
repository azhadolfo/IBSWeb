using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Dtos;
using IBS.Models.Mobility;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace IBS.DataAccess.Repository.Mobility
{
    public class FuelPurchaseRepository : Repository<MobilityFuelPurchase>, IFuelPurchaseRepository
    {
        private ApplicationDbContext _db;

        public FuelPurchaseRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public IEnumerable<dynamic> GetFuelPurchaseJoin(IEnumerable<MobilityFuelPurchase> fuelPurchases, CancellationToken cancellationToken = default)
        {
            return from fuel in fuelPurchases
                   join station in _db.MobilityStations on fuel.StationCode equals station.StationCode
                   join product in _db.Products on fuel.ProductCode equals product.ProductCode
                   select new
                   {
                       fuel.FuelPurchaseId,
                       fuel.StationCode,
                       fuel.FuelPurchaseNo,
                       fuel.ShiftDate,
                       fuel.ProductCode,
                       product.ProductName,
                       fuel.ReceivedBy,
                       fuel.PostedBy,
                       station.StationName
                   }.ToExpando();
        }

        public async Task PostAsync(string id, string postedBy, string stationCode, CancellationToken cancellationToken = default)
        {
            try
            {
                MobilityFuelPurchase fuelPurchase = await _db
                    .MobilityFuelPurchase
                    .FirstOrDefaultAsync(f => f.FuelPurchaseNo == id && f.StationCode == stationCode, cancellationToken) ?? throw new InvalidOperationException($"Fuel purchase with id '{id}' not found.");

                if (fuelPurchase.PurchasePrice == 0)
                {
                    throw new ArgumentException("Encode first the buying price for this purchase!");
                }

                var fuelPurchaselist = await _db.MobilityFuelPurchase
                    .Where(f => f.StationCode == fuelPurchase.StationCode && f.ShiftDate <= fuelPurchase.ShiftDate && f.CreatedDate < fuelPurchase.CreatedDate && f.PostedBy == null)
                    .OrderBy(f => f.FuelPurchaseNo)
                    .ToListAsync(cancellationToken);

                if (fuelPurchaselist.Count > 0)
                {
                    throw new InvalidOperationException($"Can't proceed to post, you have unposted {fuelPurchaselist[0].FuelPurchaseNo}");
                }

                ProductDto product = await MapProductToDTO(fuelPurchase.ProductCode, cancellationToken) ?? throw new InvalidOperationException($"Product with code '{fuelPurchase.ProductCode}' not found.");

                var sortedInventory = await _db
                        .MobilityInventories
                        .Where(i => i.ProductCode == fuelPurchase.ProductCode && i.StationCode == fuelPurchase.StationCode)
                        .OrderBy(i => i.Date)
                        .ThenBy(i => i.InventoryId)
                        .ToListAsync(cancellationToken);

                var lastIndex = sortedInventory.FindLastIndex(s => s.Date <= fuelPurchase.ShiftDate);
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

                List<MobilityGeneralLedger> journals = new();

                var (inventoryAcctNo, inventoryAcctTitle) = GetInventoryAccountTitle(product.ProductCode);

                journals.Add(new MobilityGeneralLedger
                {
                    TransactionDate = fuelPurchase.ShiftDate,
                    Reference = fuelPurchase.FuelPurchaseNo,
                    Particular = $"{fuelPurchase.Quantity:N2} Lit {product.ProductName} @ {fuelPurchase.PurchasePrice:N2}, DR#{fuelPurchase.DrNo}",
                    AccountNumber = inventoryAcctNo,
                    AccountTitle = inventoryAcctTitle,
                    Debit = fuelPurchase.Quantity * fuelPurchase.PurchasePrice / 1.12m,
                    Credit = 0,
                    StationCode = fuelPurchase.StationCode,
                    ProductCode = fuelPurchase.ProductCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                journals.Add(new MobilityGeneralLedger
                {
                    TransactionDate = fuelPurchase.ShiftDate,
                    Reference = fuelPurchase.FuelPurchaseNo,
                    Particular = $"{fuelPurchase.Quantity:N2} Lit {product.ProductName} @ {fuelPurchase.PurchasePrice:N2}, DR#{fuelPurchase.DrNo}",
                    AccountNumber = "1010602",
                    AccountTitle = "Vat Input",
                    Debit = fuelPurchase.Quantity * fuelPurchase.PurchasePrice / 1.12m * 0.12m,
                    Credit = 0,
                    StationCode = fuelPurchase.StationCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                journals.Add(new MobilityGeneralLedger
                {
                    TransactionDate = fuelPurchase.ShiftDate,
                    Reference = fuelPurchase.FuelPurchaseNo,
                    Particular = $"{fuelPurchase.Quantity:N2} Lit {product.ProductName} @ {fuelPurchase.PurchasePrice:N2}, DR#{fuelPurchase.DrNo}",
                    AccountNumber = "2010101",
                    AccountTitle = "Accounts Payables - Trade",
                    Debit = 0,
                    Credit = fuelPurchase.Quantity * fuelPurchase.PurchasePrice,
                    StationCode = fuelPurchase.StationCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                decimal totalCost = fuelPurchase.Quantity * fuelPurchase.PurchasePrice;
                decimal runningCost = previousInventory.RunningCost + totalCost;
                decimal inventoryBalance = previousInventory.InventoryBalance + fuelPurchase.Quantity;
                decimal unitCostAverage = runningCost / inventoryBalance;

                var inventory = new MobilityInventory
                {
                    Particulars = nameof(JournalType.Purchase),
                    Date = fuelPurchase.ShiftDate,
                    Reference = $"DR#{fuelPurchase.DrNo}",
                    ProductCode = fuelPurchase.ProductCode,
                    StationCode = fuelPurchase.StationCode,
                    Quantity = fuelPurchase.Quantity,
                    UnitCost = fuelPurchase.PurchasePrice,
                    TotalCost = totalCost,
                    InventoryBalance = inventoryBalance,
                    RunningCost = runningCost,
                    UnitCostAverage = unitCostAverage,
                    InventoryValue = runningCost,
                    TransactionNo = fuelPurchase.FuelPurchaseNo
                };

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

                    var journalEntries = await _db.MobilityGeneralLedgers
                            .Where(j => j.Reference == transaction.TransactionNo && j.ProductCode == transaction.ProductCode &&
                                        (j.AccountNumber.StartsWith("50101") || j.AccountNumber.StartsWith("10104")))
                            .ToListAsync(cancellationToken);

                    if (journalEntries.Count != 0)
                    {
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
                    }

                    _db.MobilityGeneralLedgers.UpdateRange(journalEntries);
                }

                _db.MobilityInventories.UpdateRange(sortedInventory);

                if (IsJournalEntriesBalanced(journals))
                {
                    await _db.AddAsync(inventory, cancellationToken);
                    await _db.MobilityGeneralLedgers.AddRangeAsync(journals, cancellationToken);
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
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);
            using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
            });

            var records = csv.GetRecords<MobilityFuelDelivery>();
            var existingRecords = await _db.Set<MobilityFuelDelivery>().ToListAsync(cancellationToken);
            var recordsToInsert = records.Where(record => !existingRecords.Exists(existingRecord =>
                existingRecord.pagenumber == record.pagenumber && existingRecord.shiftnumber == record.shiftnumber && existingRecord.shiftdate == record.shiftdate && existingRecord.stncode == record.stncode && existingRecord.productcode == record.productcode)).ToList();

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

        public async Task RecordTheDeliveryToPurchase(IEnumerable<MobilityFuelDelivery> fuelDeliveries, CancellationToken cancellationToken = default)
        {
            var fuelPurchase = new List<MobilityFuelPurchase>();

            foreach (var fuelDelivery in fuelDeliveries)
            {
                fuelPurchase.Add(new MobilityFuelPurchase
                {
                    PageNumber = fuelDelivery.pagenumber,
                    StationCode = fuelDelivery.stncode,
                    CashierCode = fuelDelivery.cashiercode.Substring(1),
                    ShiftNo = fuelDelivery.shiftnumber,
                    ShiftDate = fuelDelivery.shiftdate,
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

        public async Task UpdateAsync(MobilityFuelPurchase model, CancellationToken cancellationToken = default)
        {
            MobilityFuelPurchase existingFuelPurchase = await _db
                .MobilityFuelPurchase
                .FirstOrDefaultAsync(f => f.FuelPurchaseId == model.FuelPurchaseId && f.StationCode == model.StationCode, cancellationToken) ?? throw new InvalidOperationException($"Fuel purchase with id '{model.FuelPurchaseId}' not found.");

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
            var lastCashierReport = await _db.MobilityFuelPurchase
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