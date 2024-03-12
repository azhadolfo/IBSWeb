using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
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
using static System.Collections.Specialized.BitVector32;

namespace IBS.DataAccess.Repository
{
    public class FuelPurchaseRepository : Repository<FuelPurchase>, IFuelPurchaseRepository
    {
        private ApplicationDbContext _db;

        public FuelPurchaseRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task PostAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                FuelPurchase? fuelPurchase = await _db
                    .FuelPurchase
                    .FindAsync(id, cancellationToken);

                Product? product = await _db
                    .Products
                    .FirstOrDefaultAsync(p => p.ProductCode == fuelPurchase.ProductCode, cancellationToken);

                Inventory? previousInventory = await _db
                    .Inventories
                    .OrderByDescending(i => i.InventoryId)
                    .FirstOrDefaultAsync(i => i.ProductCode == fuelPurchase.ProductCode && i.StationCode == fuelPurchase.StationCode,cancellationToken);

                if (previousInventory == null)
                {
                    throw new ColumnNotFoundException($"Beginning inventory for {fuelPurchase.ProductCode} in station {fuelPurchase.StationCode} not found!");
                }

                fuelPurchase.PostedBy = "Ako";
                fuelPurchase.PostedDate = DateTime.Now;

                var journals = new List<GeneralLedger>();

                journals.Add(new GeneralLedger
                {
                    TransactionDate = fuelPurchase.DeliveryDate,
                    Reference = fuelPurchase.ShiftRecId,
                    Particular = $"{fuelPurchase.Quantity:N2} Lit {product.ProductName} @ {fuelPurchase.PurchasePrice:N2}, DR#{fuelPurchase.DrNo}",
                    AccountNumber = "10100033",
                    AccountTitle = "Merchandise Inventory",
                    Debit = (fuelPurchase.Quantity * fuelPurchase.SellingPrice) / 1.12m,
                    Credit = 0,
                    StationCode = fuelPurchase.StationCode,
                    ProductCode = fuelPurchase.ProductCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                journals.Add(new GeneralLedger
                {
                    TransactionDate = fuelPurchase.DeliveryDate,
                    Reference = fuelPurchase.ShiftRecId,
                    Particular = $"{fuelPurchase.Quantity:N2} Lit {product.ProductName} @ {fuelPurchase.PurchasePrice:N2}, DR#{fuelPurchase.DrNo}",
                    AccountNumber = "10100085",
                    AccountTitle = "Input Tax",
                    Debit = ((fuelPurchase.Quantity * fuelPurchase.SellingPrice) / 1.12m) * 0.12m,
                    Credit = 0,
                    StationCode = fuelPurchase.StationCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                journals.Add(new GeneralLedger
                {
                    TransactionDate = fuelPurchase.DeliveryDate,
                    Reference = fuelPurchase.ShiftRecId,
                    Particular = $"{fuelPurchase.Quantity:N2} Lit {product.ProductName} @ {fuelPurchase.PurchasePrice:N2}, DR#{fuelPurchase.DrNo}",
                    AccountNumber = "20100005",
                    AccountTitle = "Accounts Payable",
                    Debit = 0,
                    Credit = fuelPurchase.Quantity * fuelPurchase.SellingPrice,
                    StationCode = fuelPurchase.StationCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                var totalCost = fuelPurchase.Quantity * fuelPurchase.PurchasePrice;
                var runningCost = previousInventory.RunningCost + totalCost;
                var inventoryBalance = previousInventory.InventoryBalance + fuelPurchase.Quantity;
                var unitCostAverage = runningCost / inventoryBalance;

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
                    InventoryValue = runningCost,
                    TransactionId = fuelPurchase.FuelPurchaseId
                };

                journals.Add(new GeneralLedger
                {
                    TransactionDate = fuelPurchase.DeliveryDate,
                    Reference = fuelPurchase.ShiftRecId,
                    Particular = $"COGS:{product.ProductCode} {fuelPurchase.Quantity:N2} Lit {product.ProductName} @ {fuelPurchase.PurchasePrice:N2}, DR#{fuelPurchase.DrNo}",
                    AccountNumber = "50100005",
                    AccountTitle = "Cost of Goods Sold",
                    Debit = runningCost,
                    Credit = 0,
                    StationCode = fuelPurchase.StationCode,
                    ProductCode = product.ProductCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                journals.Add(new GeneralLedger
                {
                    TransactionDate = fuelPurchase.DeliveryDate,
                    Reference = fuelPurchase.ShiftRecId,
                    Particular = $"COGS:{product.ProductCode} {fuelPurchase.Quantity:N2} Lit {product.ProductName} @ {fuelPurchase.PurchasePrice:N2}, DR#{fuelPurchase.DrNo}",
                    AccountNumber = "10100033",
                    AccountTitle = "Merchandise Inventory",
                    Debit = 0,
                    Credit = runningCost,
                    StationCode = fuelPurchase.StationCode,
                    ProductCode = product.ProductCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

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
                    EmployeeNo = fuelDelivery.empno.Substring(1),
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
                    GainOrLoss = (fuelDelivery.quantity + fuelDelivery.volumebefore) - fuelDelivery.volumeafter,
                    ReceivedBy = fuelDelivery.receivedby,
                    CreatedBy = fuelDelivery.createdby.Substring(1),
                    CreatedDate = fuelDelivery.createddate
                });
            }

            await _db.AddRangeAsync(fuelPurchase, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

        }

        public async Task UpdateAsync(FuelPurchase model, CancellationToken cancellationToken = default)
        {
            FuelPurchase? existingFuelPurchase = await _db
                .FuelPurchase
                .FindAsync(model.FuelPurchaseId, cancellationToken);

            existingFuelPurchase!.PurchasePrice = model.PurchasePrice;

            if (_db.ChangeTracker.HasChanges())
            {
                existingFuelPurchase.EditedBy = "Ako";
                existingFuelPurchase.EditedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }
    }
}
