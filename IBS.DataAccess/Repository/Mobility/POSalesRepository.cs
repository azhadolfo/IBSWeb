using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using IBS.Models.Mobility.ViewModels;

namespace IBS.DataAccess.Repository.Mobility
{
    public class POSalesRepository : Repository<MobilityPOSales>, IPOSalesRepository
    {
        private ApplicationDbContext _db;

        public POSalesRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<int> ProcessPOSales(string file, CancellationToken cancellationToken = default)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);
            using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
            });

            var records = csv.GetRecords<MobilityPoSalesRaw>();
            var existingRecords = await _db.Set<MobilityPoSalesRaw>().ToListAsync(cancellationToken);
            var recordsToInsert = records.Where(record => !existingRecords.Exists(existingRecord =>
                existingRecord.shiftrecid == record.shiftrecid && existingRecord.stncode == record.stncode && existingRecord.tripticket == record.tripticket)).ToList();

            if (recordsToInsert.Count != 0)
            {
                await _db.AddRangeAsync(recordsToInsert, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                await RecordThePurchaseOrder(recordsToInsert, cancellationToken);

                return recordsToInsert.Count;
            }
            else
            {
                return 0;
            }
        }

        public async Task<int> ProcessPOSalesGoogleDrive(GoogleDriveFile file, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream(file.FileContent);
            using var reader = new StreamReader(stream);
            using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
            });

            var records = csv.GetRecords<MobilityPoSalesRaw>();
            var existingRecords = await _db.Set<MobilityPoSalesRaw>().ToListAsync(cancellationToken);
            var recordsToInsert = records.Where(record => !existingRecords.Exists(existingRecord =>
                existingRecord.shiftrecid == record.shiftrecid && existingRecord.stncode == record.stncode && existingRecord.tripticket == record.tripticket)).ToList();

            if (recordsToInsert.Count != 0)
            {
                await _db.AddRangeAsync(recordsToInsert, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                await RecordThePurchaseOrder(recordsToInsert, cancellationToken);

                return recordsToInsert.Count;
            }
            else
            {
                return 0;
            }
        }

        public async Task RecordThePurchaseOrder(IEnumerable<MobilityPoSalesRaw> poSales, CancellationToken cancellationToken = default)
        {
            var purchaseOrders = new List<MobilityPOSales>();

            foreach (var po in poSales)
            {
                purchaseOrders.Add(new MobilityPOSales
                {
                    POSalesNo = Guid.NewGuid().ToString(),
                    ShiftRecId = po.shiftrecid,
                    StationCode = po.stncode,
                    CashierCode = po.cashiercode.Substring(1),
                    ShiftNo = po.shiftnumber,
                    POSalesDate = po.podate,
                    POSalesTime = po.potime,
                    CustomerCode = po.customercode,
                    Driver = po.driver,
                    PlateNo = po.plateno,
                    DrNo = po.drnumber.Substring(2),
                    TripTicket = po.tripticket.Substring(1),
                    ProductCode = po.productcode,
                    Quantity = po.quantity,
                    Price = po.price,
                    ContractPrice = po.contractprice,
                    CreatedBy = po.createdby.Substring(1),
                    CreatedDate = po.createddate
                });
            }

            await _db.AddRangeAsync(purchaseOrders, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
