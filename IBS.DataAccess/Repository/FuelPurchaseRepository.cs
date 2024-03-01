using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.ViewModels;
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

                fuelPurchase.PostedBy = "Ako";
                fuelPurchase.PostedDate = DateTime.Now;

                var journal = new List<GeneralLedger>();

                journal.Add(new GeneralLedger
                {
                    TransactionDate = fuelPurchase.DeliveryDate,
                    Reference = $"{fuelPurchase.ShiftRecId}{fuelPurchase.StationCode}",
                    Particular = $"{fuelPurchase.Quantity} Lit {product.ProductName} @ {fuelPurchase.SellingPrice}, DR {fuelPurchase.DrNo}",
                    AccountNumber = 10100033,
                    AccountTitle = "Merchandise Inventory",
                    Debit = (fuelPurchase.Quantity * fuelPurchase.SellingPrice)/1.12m,
                    Credit = 0,
                    StationCode = fuelPurchase.StationCode,
                    ProductCode = fuelPurchase.ProductCode
                });

                journal.Add(new GeneralLedger
                {
                    TransactionDate = fuelPurchase.DeliveryDate,
                    Reference = $"{fuelPurchase.ShiftRecId}{fuelPurchase.StationCode}",
                    Particular = $"{fuelPurchase.Quantity} Lit {product.ProductName} @ {fuelPurchase.SellingPrice}, DR {fuelPurchase.DrNo}",
                    AccountNumber = 10100085,
                    AccountTitle = "Input Tax",
                    Debit = ((fuelPurchase.Quantity * fuelPurchase.SellingPrice) / 1.12m) * 0.12m,
                    Credit = 0,
                    StationCode = fuelPurchase.StationCode
                });

                journal.Add(new GeneralLedger
                {
                    TransactionDate = fuelPurchase.DeliveryDate,
                    Reference = $"{fuelPurchase.ShiftRecId}{fuelPurchase.StationCode}",
                    Particular = $"{fuelPurchase.Quantity} Lit {product.ProductName} @ {fuelPurchase.SellingPrice}, DR {fuelPurchase.DrNo}",
                    AccountNumber = 20100005,
                    AccountTitle = "Accounts Payable",
                    Debit = 0,
                    Credit = fuelPurchase.Quantity * fuelPurchase.SellingPrice,
                    StationCode = fuelPurchase.StationCode
                });

                await _db.GeneralLedgers.AddRangeAsync(journal);
                await _db.SaveChangesAsync();
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
                existingRecord.shiftrecid == record.shiftrecid || existingRecord.stncode == record.stncode)).ToList();

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

    }
}
