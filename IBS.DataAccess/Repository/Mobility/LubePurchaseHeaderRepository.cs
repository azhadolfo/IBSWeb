using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Dtos;
using IBS.Models.Mobility;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Mobility
{
    public class LubePurchaseHeaderRepository : Repository<MobilityLubePurchaseHeader>, ILubePurchaseHeaderRepository
    {
        private ApplicationDbContext _db;

        public LubePurchaseHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public IEnumerable<dynamic> GetLubePurchaseJoin(IEnumerable<MobilityLubePurchaseHeader> lubePurchases, CancellationToken cancellationToken = default)
        {
            return from lube in lubePurchases
                   join station in _db.MobilityStations on lube.StationCode equals station.StationCode
                   join supplier in _db.FilprideSuppliers on lube.SupplierCode equals supplier.SupplierCode
                   select new
                   {
                       lube.LubePurchaseHeaderId,
                       lube.StationCode,
                       lube.LubePurchaseHeaderNo,
                       lube.ShiftDate,
                       lube.SupplierCode,
                       supplier.SupplierName,
                       lube.SalesInvoice,
                       lube.ReceivedBy,
                       lube.PostedBy,
                       station.StationName
                   }.ToExpando();
        }

        public async Task PostAsync(string id, string postedBy, string stationCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var lubes = await GetAsync(l => l.LubePurchaseHeaderNo == id, cancellationToken);

                if (lubes == null)
                {
                    throw new InvalidOperationException($"Lube purchase header/detail with id '{id}' not found.");
                }

                var lubePurchaseList = await _db.MobilityLubePurchaseHeaders
                    .Where(l => l.StationCode == lubes.StationCode && l.ShiftDate <= lubes.ShiftDate && l.CreatedDate < lubes.CreatedDate && l.PostedBy == null)
                    .OrderBy(l => l.LubePurchaseHeaderNo)
                    .ToListAsync(cancellationToken);

                if (lubePurchaseList.Count > 0)
                {
                    throw new InvalidOperationException($"Can't proceed to post, you have unposted {lubePurchaseList[0].LubePurchaseHeaderNo}");
                }

                SupplierDto supplier = await MapSupplierToDTO(lubes.SupplierCode, cancellationToken) ?? throw new InvalidOperationException($"Supplier with code '{lubes.SupplierCode}' not found.");

                lubes.PostedBy = postedBy;
                lubes.PostedDate = DateTime.Now;

                List<MobilityGeneralLedger> journals = new();
                List<MobilityInventory> inventories = new();

                journals.Add(new MobilityGeneralLedger
                {
                    TransactionDate = lubes.ShiftDate,
                    Reference = lubes.LubePurchaseHeaderNo,
                    Particular = $"SI#{lubes.SalesInvoice} DR#{lubes.DrNo} LUBES PURCHASE {lubes.ShiftDate}",
                    AccountNumber = "1010410",
                    AccountTitle = "Inventory - Lubes",
                    Debit = lubes.Amount / 1.12m,
                    Credit = 0,
                    StationCode = lubes.StationCode,
                    JournalReference = nameof(JournalType.Purchase),
                    ProductCode = "LUBES"
                });

                journals.Add(new MobilityGeneralLedger
                {
                    TransactionDate = lubes.ShiftDate,
                    Reference = lubes.LubePurchaseHeaderNo,
                    Particular = $"SI#{lubes.SalesInvoice} DR#{lubes.DrNo} LUBES PURCHASE {lubes.ShiftDate}",
                    AccountNumber = "1010602",
                    AccountTitle = "Vat Input",
                    Debit = lubes.Amount / 1.12m * 0.12m,
                    Credit = 0,
                    StationCode = lubes.StationCode,
                    JournalReference = nameof(JournalType.Purchase)
                });

                journals.Add(new MobilityGeneralLedger
                {
                    TransactionDate = lubes.ShiftDate,
                    Reference = lubes.LubePurchaseHeaderNo,
                    Particular = $"SI#{lubes.SalesInvoice} DR#{lubes.DrNo} LUBES PURCHASE {lubes.ShiftDate}",
                    AccountNumber = "2010101",
                    AccountTitle = "Accounts Payables - Trade",
                    Debit = 0,
                    Credit = lubes.Amount,
                    StationCode = lubes.StationCode,
                    SupplierCode = supplier.SupplierName.ToUpper(),
                    JournalReference = nameof(JournalType.Purchase)
                });

                foreach (var lube in lubes.LubePurchaseDetails)
                {
                    var sortedInventory = _db
                        .MobilityInventories
                        .OrderBy(i => i.Date)
                        .Where(i => i.ProductCode == lube.ProductCode && i.StationCode == lube.StationCode)
                        .ToList();

                    var lastIndex = sortedInventory.FindLastIndex(s => s.Date <= lubes.ShiftDate);
                    if (lastIndex >= 0)
                    {
                        sortedInventory = sortedInventory.Skip(lastIndex).ToList();
                    }
                    else
                    {
                        throw new ArgumentException($"Beginning inventory for {lube.ProductCode} in station {lubes.StationCode} not found!");
                    }

                    var previousInventory = sortedInventory.FirstOrDefault();

                    decimal totalCost = lube.Piece * lube.CostPerPiece;
                    decimal runningCost = previousInventory.RunningCost + totalCost;
                    decimal inventoryBalance = previousInventory.InventoryBalance + lube.Piece;
                    decimal unitCostAverage = runningCost / inventoryBalance;

                    inventories.Add(new MobilityInventory
                    {
                        Particulars = nameof(JournalType.Purchase),
                        Date = lubes.ShiftDate,
                        Reference = $"DR#{lubes.DrNo}",
                        ProductCode = lube.ProductCode,
                        StationCode = lubes.StationCode,
                        Quantity = lube.Piece,
                        UnitCost = lube.CostPerPiece,
                        TotalCost = totalCost,
                        InventoryBalance = inventoryBalance,
                        RunningCost = runningCost,
                        UnitCostAverage = unitCostAverage,
                        InventoryValue = runningCost,
                        TransactionNo = lubes.LubePurchaseHeaderNo
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
                }

                if (IsJournalEntriesBalanced(journals))
                {
                    await _db.MobilityInventories.AddRangeAsync(inventories, cancellationToken);
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

        public async Task<int> ProcessLubeDelivery(string file, CancellationToken cancellationToken = default)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);
            using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
            });

            var records = csv.GetRecords<LubeDelivery>();
            var existingRecords = await _db.Set<LubeDelivery>().ToListAsync(cancellationToken);
            var recordsToInsert = records.Where(record => !existingRecords.Exists(existingRecord =>
                existingRecord.shiftdate == record.shiftdate && existingRecord.pagenumber == record.pagenumber && existingRecord.stncode == record.stncode && existingRecord.shiftnumber == record.shiftnumber)).ToList();

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
                    .GroupBy(l => new { l.pagenumber, l.stncode, l.cashiercode, l.shiftnumber, l.shiftdate, l.suppliercode, l.invoiceno, l.drno, l.pono, l.amount, l.rcvdby, l.createdby, l.createddate })
                    .Select(g => new MobilityLubePurchaseHeader
                    {
                        PageNumber = g.Key.pagenumber,
                        StationCode = g.Key.stncode,
                        CashierCode = g.Key.cashiercode.Substring(1),
                        ShiftNo = g.Key.shiftnumber,
                        ShiftDate = g.Key.shiftdate,
                        SalesInvoice = g.Key.invoiceno.Substring(2),
                        SupplierCode = g.Key.suppliercode,
                        DrNo = g.Key.drno.Substring(2),
                        PoNo = g.Key.pono.Substring(2),
                        Amount = g.Key.amount,
                        VatableSales = g.Key.amount / 1.12m,
                        VatAmount = g.Key.amount / 1.12m * .12m,
                        ReceivedBy = g.Key.rcvdby,
                        CreatedBy = g.Key.createdby.Substring(1),
                        CreatedDate = g.Key.createddate
                    })
                    .ToList();

                foreach (var ld in lubePurchaseHeaders)
                {
                    ld.LubePurchaseHeaderNo = await GenerateSeriesNumber(ld.StationCode);

                    await _db.MobilityLubePurchaseHeaders.AddAsync(ld, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                foreach (var lubeDelivery in lubeDeliveries)
                {
                    var lubeHeader = lubePurchaseHeaders.Find(l => l.ShiftDate == lubeDelivery.shiftdate && l.ShiftNo == lubeDelivery.shiftnumber && l.PageNumber == lubeDelivery.pagenumber && l.StationCode == lubeDelivery.stncode);

                    var lubesPurchaseDetail = new MobilityLubePurchaseDetail
                    {
                        LubePurchaseHeaderId = lubeHeader.LubePurchaseHeaderId,
                        LubePurchaseHeaderNo = lubeHeader.LubePurchaseHeaderNo,
                        StationCode = lubeHeader.StationCode,
                        Quantity = lubeDelivery.quantity,
                        Unit = lubeDelivery.unit,
                        Description = lubeDelivery.description,
                        CostPerCase = lubeDelivery.unitprice,
                        CostPerPiece = lubeDelivery.unitprice / lubeDelivery.piece,
                        ProductCode = lubeDelivery.productcode,
                        Piece = lubeDelivery.piece,
                        Amount = lubeDelivery.quantity * lubeDelivery.unitprice
                    };

                    await _db.MobilityLubePurchaseDetails.AddAsync(lubesPurchaseDetail, cancellationToken);
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
            var lastCashierReport = await _db.MobilityLubePurchaseHeaders
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

        public override async Task<MobilityLubePurchaseHeader> GetAsync(Expression<Func<MobilityLubePurchaseHeader, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet
                .Include(sh => sh.LubePurchaseDetails)
                .FirstOrDefaultAsync(filter, cancellationToken);
        }

        public override async Task<IEnumerable<MobilityLubePurchaseHeader>> GetAllAsync(Expression<Func<MobilityLubePurchaseHeader, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MobilityLubePurchaseHeader> query = dbSet
                .Include(sh => sh.LubePurchaseDetails);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}