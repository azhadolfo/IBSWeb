using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
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
