using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository
{
    public class PurchaseOrderRepository : Repository<PurchaseOrder>, IPurchaseOrderRepository
    {
        private ApplicationDbContext _db;

        public PurchaseOrderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<int> ProcessPOSales(string file, CancellationToken cancellationToken = default)
        {
            using var stream = new FileStream(file, FileMode.Open);
            using var reader = new StreamReader(stream);
            using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
            });

            var records = csv.GetRecords<POSales>();
            var existingRecords = await _db.Set<POSales>().ToListAsync(cancellationToken);
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

        public async Task RecordThePurchaseOrder(IEnumerable<POSales> poSales, CancellationToken cancellationToken = default)
        {
            var purchaseOrders = new List<PurchaseOrder>();

            foreach (var po in poSales)
            {
                purchaseOrders.Add(new PurchaseOrder
                {
                    PurchaseOrderNo = Guid.NewGuid().ToString(),
                    ShiftRecId = po.shiftrecid,
                    StationCode = po.stncode,
                    CashierCode = po.cashiercode.Substring(1),
                    ShiftNo = po.shiftnumber,
                    PurchaseOrderDate = po.podate,
                    PurchaseOrderTime = po.potime,
                    CustomerCode = po.customercode,
                    Driver = po.driver,
                    PlateNo = po.plateno,
                    DrNo = po.drnumber.Substring(2),
                    TripTicket = po.tripticket,
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
