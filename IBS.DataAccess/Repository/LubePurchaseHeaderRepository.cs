using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.ViewModels;
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

        public async Task PostAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                LubeDeliveryVM lubeDeliveryVM = new LubeDeliveryVM
                {
                    Header = await _db.LubePurchaseHeaders.FindAsync(id, cancellationToken),
                    Details = await _db.LubePurchaseDetails.Where(l => l.LubeDeliveryHeaderId == id).ToListAsync(cancellationToken)
                };

                Supplier? supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierCode == lubeDeliveryVM.Header.SupplierCode, cancellationToken);

                lubeDeliveryVM.Header.PostedBy = "Ako";
                lubeDeliveryVM.Header.PostedDate = DateTime.Now;

                var journals = new List<GeneralLedger>();

                journals.Add(new GeneralLedger
                {
                    TransactionDate = lubeDeliveryVM.Header.DeliveryDate,
                    Reference = $"{lubeDeliveryVM.Header.ShiftRecId}{lubeDeliveryVM.Header.StationCode}",
                    Particular = $"SI#{lubeDeliveryVM.Header.SalesInvoice} DR#{lubeDeliveryVM.Header.DrNo} LUBES PURCHASE {lubeDeliveryVM.Header.DeliveryDate}",
                    AccountNumber = 10100033,
                    AccountTitle = "Merchandise Inventory",
                    Debit = lubeDeliveryVM.Header.Amount / 1.12m,
                    Credit = 0,
                    StationCode = lubeDeliveryVM.Header.StationCode
                });

                journals.Add(new GeneralLedger
                {
                    TransactionDate = lubeDeliveryVM.Header.DeliveryDate,
                    Reference = $"{lubeDeliveryVM.Header.ShiftRecId}{lubeDeliveryVM.Header.StationCode}",
                    Particular = $"SI#{lubeDeliveryVM.Header.SalesInvoice} DR#{lubeDeliveryVM.Header.DrNo} LUBES PURCHASE {lubeDeliveryVM.Header.DeliveryDate}",
                    AccountNumber = 10100085,
                    AccountTitle = "Input Tax",
                    Debit = (lubeDeliveryVM.Header.Amount / 1.12m) * 0.12m,
                    Credit = 0,
                    StationCode = lubeDeliveryVM.Header.StationCode
                });

                journals.Add(new GeneralLedger
                {
                    TransactionDate = lubeDeliveryVM.Header.DeliveryDate,
                    Reference = $"{lubeDeliveryVM.Header.ShiftRecId}{lubeDeliveryVM.Header.StationCode}",
                    Particular = $"SI#{lubeDeliveryVM.Header.SalesInvoice} DR#{lubeDeliveryVM.Header.DrNo} LUBES PURCHASE {lubeDeliveryVM.Header.DeliveryDate}",
                    AccountNumber = 20100005,
                    AccountTitle = "Accounts Payable",
                    Debit = 0,
                    Credit = lubeDeliveryVM.Header.Amount,
                    StationCode = lubeDeliveryVM.Header.StationCode,
                    SupplierCode = supplier.SupplierName
                });

                if (IsJournalEntriesBalance(journals))
                {
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

        public async Task RecordTheDeliveryToPurchase(IEnumerable<LubeDelivery> lubeDeliveries, CancellationToken cancellationToken = default)
        {
            try
            {
                var lubePurchaseHeaders = lubeDeliveries
                    .GroupBy(l => new { l.shiftrecid, l.stncode, l.empno, l.shiftnumber, l.deliverydate, l.suppliercode, l.invoiceno, l.drno, l.pono, l.amount, l.rcvdby, l.createdby, l.createddate })
                    .Select(g => new LubePurchaseHeader
                    {
                        ShiftRecId = g.Key.shiftrecid,
                        StationCode = g.Key.stncode,
                        EmployeeNo = g.Key.empno.Substring(1),
                        ShiftNo = g.Key.shiftnumber,
                        DeliveryDate = g.Key.deliverydate,
                        SalesInvoice = g.Key.invoiceno.Substring(2),
                        SupplierCode = g.Key.suppliercode,
                        DrNo = g.Key.drno.Substring(2),
                        PoNo = g.Key.pono.Substring(2),
                        Amount = g.Key.amount,
                        ReceivedBy = g.Key.rcvdby,
                        CreatedBy = g.Key.createdby.Substring(1),
                        CreatedDate = g.Key.createddate
                    })
                    .ToList();

                await _db.LubePurchaseHeaders.AddRangeAsync(lubePurchaseHeaders, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                foreach (var lubeDelivery in lubeDeliveries)
                {
                    var lubeHeader = lubePurchaseHeaders.Find(l => l.ShiftRecId == lubeDelivery.shiftrecid && l.StationCode == lubeDelivery.stncode);

                    var lubesPurchaseDetail = new LubePurchaseDetail
                    {
                        LubeDeliveryHeaderId = lubeHeader.LubeDeliveryHeaderId,
                        Quantity = lubeDelivery.quantity,
                        Unit = lubeDelivery.unit,
                        Description = lubeDelivery.description,
                        UnitPrice = lubeDelivery.unitprice,
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
                // Log exception
            }
        }
    }
}
