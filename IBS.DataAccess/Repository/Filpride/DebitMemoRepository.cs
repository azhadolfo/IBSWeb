using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class DebitMemoRepository : Repository<FilprideDebitMemo>, IDebitMemoRepository
    {
        private readonly ApplicationDbContext _db;

        public DebitMemoRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            return type switch
            {
                nameof(DocumentType.Documented) => await GenerateCodeForDocumented(company, cancellationToken),
                nameof(DocumentType.Undocumented) => await GenerateCodeForUnDocumented(company, cancellationToken),
                _ => throw new ArgumentException("Invalid type")
            };
        }

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastDm = await _db
                .FilprideDebitMemos
                .AsNoTracking()
                .OrderByDescending(x => x.DebitMemoNo!.Length)
                .ThenByDescending(x => x.DebitMemoNo)
                .FirstOrDefaultAsync(x =>
                    x.Company == company &&
                    x.Type == nameof(DocumentType.Documented),
                    cancellationToken);

            if (lastDm == null)
            {
                return "DM0000000001";
            }

            var lastSeries = lastDm.DebitMemoNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastDm = await _db
                .FilprideDebitMemos
                .AsNoTracking()
                .OrderByDescending(x => x.DebitMemoNo!.Length)
                .ThenByDescending(x => x.DebitMemoNo)
                .FirstOrDefaultAsync(x =>
                        x.Company == company &&
                        x.Type == nameof(DocumentType.Undocumented),
                    cancellationToken);

            if (lastDm == null)
            {
                return "DMU000000001";
            }

            var lastSeries = lastDm.DebitMemoNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
        }

        public async Task PostAsync(FilprideDebitMemo model, CancellationToken cancellationToken = default)
        {
            var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
            var arTradeReceivableTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020100") ?? throw new ArgumentException("Account title '101020100' not found.");
            var arNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020500") ?? throw new ArgumentException("Account title '101020500' not found.");
            var arTradeCwt = accountTitlesDto.Find(c => c.AccountNumber == "101020200") ?? throw new ArgumentException("Account title '101020200' not found.");
            var arTradeCwv = accountTitlesDto.Find(c => c.AccountNumber == "101020300") ?? throw new ArgumentException("Account title '101020300' not found.");
            var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account title '201030100' not found.");

            if (model.SalesInvoiceId != null)
            {
                var salesInvoice = model.SalesInvoice
                    ?? throw new ArgumentException("Sales invoice is required.");
                var customerOrderSlip = salesInvoice.CustomerOrderSlip
                    ?? throw new ArgumentException("Customer order slip is required.");
                var product = salesInvoice.Product
                    ?? throw new ArgumentException("Product is required.");

                var (salesAcctNo, _) = GetSalesAccountTitle(product.ProductCode);
                var salesTitle = accountTitlesDto.Find(c => c.AccountNumber == salesAcctNo)
                    ?? throw new ArgumentException($"Account title '{salesAcctNo}' not found.");

                var netOfVatAmount = customerOrderSlip.VatType == SD.VatType_Vatable
                    ? ComputeNetOfVat(model.DebitAmount)
                    : model.DebitAmount;

                var vatAmount = customerOrderSlip.VatType == SD.VatType_Vatable
                    ? ComputeVatAmount(netOfVatAmount)
                    : 0m;

                var withHoldingTaxAmount = customerOrderSlip.HasEWT
                    ? ComputeEwtAmount(netOfVatAmount, 0.01m)
                    : 0m;

                var withHoldingVatAmount = customerOrderSlip.HasWVAT
                    ? ComputeEwtAmount(netOfVatAmount, 0.05m)
                    : 0m;

                var ledgers = new List<FilprideGeneralLedgerBook>
                {
                    new()
                    {
                        Date = model.TransactionDate,
                        Reference = model.DebitMemoNo!,
                        Description = product.ProductName,
                        AccountId = arTradeReceivableTitle.AccountId,
                        AccountNo = arTradeReceivableTitle.AccountNumber,
                        AccountTitle = arTradeReceivableTitle.AccountName,
                        Debit = model.DebitAmount - (withHoldingTaxAmount + withHoldingVatAmount),
                        Credit = 0,
                        Company = model.Company,
                        CreatedBy = model.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        SubAccountType = SubAccountType.Customer,
                        SubAccountId = customerOrderSlip.CustomerId,
                        SubAccountName = customerOrderSlip.CustomerName,
                        ModuleType = nameof(ModuleType.DebitMemo)
                    }
                };

                if (withHoldingTaxAmount > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = model.TransactionDate,
                        Reference = model.DebitMemoNo!,
                        Description = product.ProductName,
                        AccountId = arTradeCwt.AccountId,
                        AccountNo = arTradeCwt.AccountNumber,
                        AccountTitle = arTradeCwt.AccountName,
                        Debit = withHoldingTaxAmount,
                        Credit = 0,
                        Company = model.Company,
                        CreatedBy = model.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.DebitMemo)
                    });
                }

                if (withHoldingVatAmount > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = model.TransactionDate,
                        Reference = model.DebitMemoNo!,
                        Description = product.ProductName,
                        AccountId = arTradeCwv.AccountId,
                        AccountNo = arTradeCwv.AccountNumber,
                        AccountTitle = arTradeCwv.AccountName,
                        Debit = withHoldingVatAmount,
                        Credit = 0,
                        Company = model.Company,
                        CreatedBy = model.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.DebitMemo)
                    });
                }

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = model.TransactionDate,
                    Reference = model.DebitMemoNo!,
                    Description = product.ProductName,
                    AccountId = salesTitle.AccountId,
                    AccountNo = salesTitle.AccountNumber,
                    AccountTitle = salesTitle.AccountName,
                    Debit = 0,
                    Credit = netOfVatAmount,
                    Company = model.Company,
                    CreatedBy = model.PostedBy!,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModuleType = nameof(ModuleType.DebitMemo)
                });

                if (vatAmount > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = model.TransactionDate,
                        Reference = model.DebitMemoNo!,
                        Description = product.ProductName,
                        AccountId = vatOutputTitle.AccountId,
                        AccountNo = vatOutputTitle.AccountNumber,
                        AccountTitle = vatOutputTitle.AccountName,
                        Debit = 0,
                        Credit = vatAmount,
                        Company = model.Company,
                        CreatedBy = model.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.DebitMemo)
                    });
                }

                if (!IsJournalEntriesBalanced(ledgers))
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

                await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
            }

            if (model.ServiceInvoiceId != null)
            {
                var serviceInvoice = model.ServiceInvoice
                    ?? throw new ArgumentException("Service invoice is required.");
                var service = serviceInvoice.Service
                    ?? throw new ArgumentException("Service is required.");
                var serviceTitle = accountTitlesDto.Find(c => c.AccountNumber == service.CurrentAndPreviousNo)
                    ?? throw new ArgumentException($"Account title '{service.CurrentAndPreviousNo}' not found.");

                var netDiscount = (model.Amount ?? 0m) - serviceInvoice.Discount;
                var netOfVatAmount = serviceInvoice.VatType == SD.VatType_Vatable
                    ? ComputeNetOfVat(netDiscount)
                    : netDiscount;
                var vatAmount = serviceInvoice.VatType == SD.VatType_Vatable
                    ? ComputeVatAmount(netOfVatAmount)
                    : 0m;
                var ewt = serviceInvoice.HasEwt
                    ? ComputeEwtAmount(netOfVatAmount, serviceInvoice.ServicePercent / 100m)
                    : 0m;
                var wvat = serviceInvoice.HasWvat
                    ? ComputeEwtAmount(netOfVatAmount, 0.05m)
                    : 0m;

                var ledgers = new List<FilprideGeneralLedgerBook>
                {
                    new()
                    {
                        Date = model.TransactionDate,
                        Reference = model.DebitMemoNo!,
                        Description = serviceInvoice.ServiceName,
                        AccountId = arNonTradeTitle.AccountId,
                        AccountNo = arNonTradeTitle.AccountNumber,
                        AccountTitle = arNonTradeTitle.AccountName,
                        Debit = netDiscount - (ewt + wvat),
                        Credit = 0,
                        Company = model.Company,
                        CreatedBy = model.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        SubAccountType = SubAccountType.Customer,
                        SubAccountId = serviceInvoice.CustomerId,
                        SubAccountName = serviceInvoice.CustomerName,
                        ModuleType = nameof(ModuleType.DebitMemo)
                    }
                };

                if (ewt > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = model.TransactionDate,
                        Reference = model.DebitMemoNo!,
                        Description = serviceInvoice.ServiceName,
                        AccountId = arTradeCwt.AccountId,
                        AccountNo = arTradeCwt.AccountNumber,
                        AccountTitle = arTradeCwt.AccountName,
                        Debit = ewt,
                        Credit = 0,
                        Company = model.Company,
                        CreatedBy = model.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.DebitMemo)
                    });
                }

                if (wvat > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = model.TransactionDate,
                        Reference = model.DebitMemoNo!,
                        Description = serviceInvoice.ServiceName,
                        AccountId = arTradeCwv.AccountId,
                        AccountNo = arTradeCwv.AccountNumber,
                        AccountTitle = arTradeCwv.AccountName,
                        Debit = wvat,
                        Credit = 0,
                        Company = model.Company,
                        CreatedBy = model.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.DebitMemo)
                    });
                }

                if (netOfVatAmount > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = model.TransactionDate,
                        Reference = model.DebitMemoNo!,
                        Description = serviceInvoice.ServiceName,
                        AccountId = serviceTitle.AccountId,
                        AccountNo = serviceTitle.AccountNumber,
                        AccountTitle = serviceTitle.AccountName,
                        Debit = 0,
                        Credit = netOfVatAmount,
                        Company = model.Company,
                        CreatedBy = model.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.DebitMemo)
                    });
                }

                if (vatAmount > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = model.TransactionDate,
                        Reference = model.DebitMemoNo!,
                        Description = serviceInvoice.ServiceName,
                        AccountId = vatOutputTitle.AccountId,
                        AccountNo = vatOutputTitle.AccountNumber,
                        AccountTitle = vatOutputTitle.AccountName,
                        Debit = 0,
                        Credit = vatAmount,
                        Company = model.Company,
                        CreatedBy = model.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.DebitMemo)
                    });
                }

                if (!IsJournalEntriesBalanced(ledgers))
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

                await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public override async Task<FilprideDebitMemo?> GetAsync(Expression<Func<FilprideDebitMemo, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.Product)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .Include(si => si.SalesInvoice)
                .ThenInclude(cos => cos!.CustomerOrderSlip)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.DeliveryReceipt)
                .ThenInclude(dr => dr!.PurchaseOrder)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideDebitMemo>> GetAllAsync(Expression<Func<FilprideDebitMemo, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideDebitMemo> query = dbSet
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.Product)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.Customer)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.CustomerOrderSlip)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.DeliveryReceipt)
                .ThenInclude(dr => dr!.PurchaseOrder);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override IQueryable<FilprideDebitMemo> GetAllQuery(Expression<Func<FilprideDebitMemo, bool>>? filter = null)
        {
            IQueryable<FilprideDebitMemo> query = dbSet
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.Product)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.Customer)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.CustomerOrderSlip)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .AsSplitQuery()
                .AsNoTracking();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return query;
        }
    }
}
