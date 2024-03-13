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

namespace IBS.DataAccess.Repository
{
    public class LubePurchaseHeaderRepository : Repository<LubePurchaseHeader>, ILubePurchaseHeaderRepository
    {
        private ApplicationDbContext _db;

        public LubePurchaseHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task PostAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                LubeDeliveryVM lubeDeliveryVM = new LubeDeliveryVM
                {
                    Header = await _db.LubePurchaseHeaders.FirstOrDefaultAsync(l => l.LubePurchaseHeaderNo == id, cancellationToken),
                    Details = await _db.LubePurchaseDetails.Where(l => l.LubePurchaseHeaderNo == id).ToListAsync(cancellationToken)
                };

                Supplier? supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierCode == lubeDeliveryVM.Header.SupplierCode, cancellationToken);

                lubeDeliveryVM.Header.PostedBy = "Ako";
                lubeDeliveryVM.Header.PostedDate = DateTime.Now;

                var journals = new List<GeneralLedger>();
                var inventories = new List<Inventory>();

                journals.Add(new GeneralLedger
                {
                    TransactionDate = lubeDeliveryVM.Header.DeliveryDate,
                    Reference = lubeDeliveryVM.Header.ShiftRecId,
                    Particular = $"SI#{lubeDeliveryVM.Header.SalesInvoice} DR#{lubeDeliveryVM.Header.DrNo} LUBES PURCHASE {lubeDeliveryVM.Header.DeliveryDate}",
                    AccountNumber = "10100033",
                    AccountTitle = "Merchandise Inventory",
                    Debit = lubeDeliveryVM.Header.Amount / 1.12m,
                    Credit = 0,
                    StationCode = lubeDeliveryVM.Header.StationCode,
                    JournalReference = nameof(JournalType.Purchase),
                    ProductCode = "LUBES"
                });

                journals.Add(new GeneralLedger
                {
                    TransactionDate = lubeDeliveryVM.Header.DeliveryDate,
                    Reference = lubeDeliveryVM.Header.ShiftRecId,
                    Particular = $"SI#{lubeDeliveryVM.Header.SalesInvoice} DR#{lubeDeliveryVM.Header.DrNo} LUBES PURCHASE {lubeDeliveryVM.Header.DeliveryDate}",
                    AccountNumber = "10100085",
                    AccountTitle = "Input Tax",
                    Debit = (lubeDeliveryVM.Header.Amount / 1.12m) * 0.12m,
                    Credit = 0,
                    StationCode = lubeDeliveryVM.Header.StationCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                journals.Add(new GeneralLedger
                {
                    TransactionDate = lubeDeliveryVM.Header.DeliveryDate,
                    Reference = lubeDeliveryVM.Header.ShiftRecId,
                    Particular = $"SI#{lubeDeliveryVM.Header.SalesInvoice} DR#{lubeDeliveryVM.Header.DrNo} LUBES PURCHASE {lubeDeliveryVM.Header.DeliveryDate}",
                    AccountNumber = "20100005",
                    AccountTitle = "Accounts Payable",
                    Debit = 0,
                    Credit = lubeDeliveryVM.Header.Amount,
                    StationCode = lubeDeliveryVM.Header.StationCode,
                    SupplierCode = supplier.SupplierName.ToUpper(),
                    JournalReference = nameof(JournalType.Purchase)
                });

                foreach (var lube in lubeDeliveryVM.Details)
                {
                    Inventory? previousInventory = await _db
                   .Inventories
                   .OrderByDescending(i => i.InventoryId)
                   .FirstOrDefaultAsync(i => i.ProductCode == lube.ProductCode && i.StationCode == lubeDeliveryVM.Header.StationCode, cancellationToken);

                    if (previousInventory == null)
                    {
                        throw new ColumnNotFoundException($"Beginning inventory for {lube.ProductCode} in station {lubeDeliveryVM.Header.StationCode} not found!");
                    }

                    decimal totalCost = lube.Piece * lube.CostPerPiece;
                    decimal runningCost = previousInventory.RunningCost + totalCost;
                    decimal inventoryBalance = previousInventory.InventoryBalance + lube.Piece;
                    decimal unitCostAverage = runningCost / inventoryBalance;
                    decimal cogs = unitCostAverage * lube.Piece;

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
                        CostOfGoodsSold = cogs,
                        UnitCostAverage = unitCostAverage,
                        InventoryValue = runningCost,
                        TransactionNo = lubeDeliveryVM.Header.LubePurchaseHeaderNo
                    });

                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = lubeDeliveryVM.Header.DeliveryDate,
                        Reference = lubeDeliveryVM.Header.ShiftRecId,
                        Particular = $"COGS:{lube.ProductCode} SI#{lubeDeliveryVM.Header.SalesInvoice} DR#{lubeDeliveryVM.Header.DrNo} LUBES PURCHASE {lubeDeliveryVM.Header.DeliveryDate}",
                        AccountNumber = "50100005",
                        AccountTitle = "Cost of Goods Sold",
                        Debit = cogs,
                        Credit = 0,
                        StationCode = lubeDeliveryVM.Header.StationCode,
                        JournalReference = nameof(JournalType.Purchase)
                    });

                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = lubeDeliveryVM.Header.DeliveryDate,
                        Reference = lubeDeliveryVM.Header.ShiftRecId,
                        Particular = $"COGS:{lube.ProductCode} SI#{lubeDeliveryVM.Header.SalesInvoice} DR#{lubeDeliveryVM.Header.DrNo} LUBES PURCHASE {lubeDeliveryVM.Header.DeliveryDate}",
                        AccountNumber = "10100033",
                        AccountTitle = "Merchandise Inventory",
                        Debit = 0,
                        Credit = cogs,
                        StationCode = lubeDeliveryVM.Header.StationCode,
                        JournalReference = nameof(JournalType.Purchase)
                    });
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
                existingRecord.shiftrecid == record.shiftrecid && existingRecord.stncode == record.stncode)).ToList();

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
                        LubePurchaseHeaderNo = Guid.NewGuid().ToString(),
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
                        VatableSales = g.Key.amount / 1.12m,
                        VatAmount = (g.Key.amount / 1.12m) * .12m,
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
                // Log exception
            }
        }
    }
}
