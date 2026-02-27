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
    public class ServiceInvoiceRepository : Repository<FilprideServiceInvoice>, IServiceInvoiceRepository
    {
        private readonly ApplicationDbContext _db;

        public ServiceInvoiceRepository(ApplicationDbContext db) : base(db)
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

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken)
        {
            var lastSv = await _db
                .FilprideServiceInvoices
                .AsNoTracking()
                .OrderByDescending(x => x.ServiceInvoiceNo!.Length)
                .ThenByDescending(x => x.ServiceInvoiceNo)
                .FirstOrDefaultAsync(x =>
                    x.Company == company &&
                    x.Type == nameof(DocumentType.Documented),
                    cancellationToken);

            if (lastSv == null)
            {
                return "SV0000000001";
            }

            var lastSeries = lastSv.ServiceInvoiceNo;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken)
        {
            var lastSv = await _db
                .FilprideServiceInvoices
                .AsNoTracking()
                .OrderByDescending(x => x.ServiceInvoiceNo!.Length)
                .ThenByDescending(x => x.ServiceInvoiceNo)
                .FirstOrDefaultAsync(x =>
                        x.Company == company &&
                        x.Type == nameof(DocumentType.Undocumented),
                    cancellationToken);

            if (lastSv == null)
            {
                return "SVU000000001";
            }

            var lastSeries = lastSv.ServiceInvoiceNo;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
        }

        public override async Task<FilprideServiceInvoice?> GetAsync(Expression<Func<FilprideServiceInvoice, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(s => s.Customer)
                .Include(s => s.Service)
                .Include(s => s.DeliveryReceipt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideServiceInvoice>> GetAllAsync(Expression<Func<FilprideServiceInvoice, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideServiceInvoice> query = dbSet
                .Include(s => s.Customer)
                .Include(s => s.Service)
                .Include(s => s.DeliveryReceipt);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task PostAsync(FilprideServiceInvoice model, CancellationToken cancellationToken = default)
        {
            #region --Sales Book Recording

            decimal withHoldingTaxAmount = 0;
            decimal withHoldingVatAmount = 0;
            decimal netOfVatAmount;
            decimal vatAmount = 0;

            if (model.VatType == SD.VatType_Vatable)
            {
                netOfVatAmount = ComputeNetOfVat(model.Total);
                vatAmount = ComputeVatAmount(netOfVatAmount);
            }
            else
            {
                netOfVatAmount = model.Total;
            }

            if (model.HasEwt)
            {
                withHoldingTaxAmount = ComputeEwtAmount(netOfVatAmount, 0.01m);
            }

            if (model.HasWvat)
            {
                withHoldingVatAmount = ComputeEwtAmount(netOfVatAmount, 0.05m);
            }

            var sales = new FilprideSalesBook
            {
                TransactionDate = model.Period,
                SerialNo = model.ServiceInvoiceNo,
                SoldTo = model.CustomerName,
                TinNo = model.CustomerTin,
                Address = model.CustomerAddress,
                Description = model.ServiceName,
                Amount = model.Total,
                VatAmount = vatAmount,
                VatableSales = netOfVatAmount,
                Discount = model.Discount,
                NetSales = netOfVatAmount,
                CreatedBy = model.CreatedBy,
                CreatedDate = model.CreatedDate,
                DueDate = model.DueDate,
                DocumentId = model.ServiceInvoiceId,
                Company = model.Company,
            };

            switch (model.VatType)
            {
                case SD.VatType_Vatable:
                    sales.VatAmount = vatAmount;
                    sales.VatableSales = netOfVatAmount;
                    break;

                case SD.VatType_Exempt:
                    sales.VatExemptSales = model.Total;
                    break;

                default:
                    sales.ZeroRated = model.Total;
                    break;
            }

            await _db.FilprideSalesBooks.AddAsync(sales, cancellationToken);

            #endregion --Sales Book Recording

            #region --General Ledger Book Recording

            var ledgers = new List<FilprideGeneralLedgerBook>();
            var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
            var arTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020100")
                               ?? throw new ArgumentException("Account title '101020100' not found.");
            var arTradeCwt = accountTitlesDto.Find(c => c.AccountNumber == "101020200")
                             ?? throw new ArgumentException("Account title '101020200' not found.");
            var arTradeCwv = accountTitlesDto.Find(c => c.AccountNumber == "101020300")
                             ?? throw new ArgumentException("Account title '101020300' not found.");
            var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100")
                                 ?? throw new ArgumentException("Account title '201030100' not found.");
            var servicesTitle = accountTitlesDto.Find(c => c.AccountNumber == model.Service!.CurrentAndPreviousNo!)
                                ?? throw new ArgumentException($"Account title '{model.Service!.CurrentAndPreviousNo}' not found.");

            ledgers.Add(
                new FilprideGeneralLedgerBook
                {
                    Date = model.Period.AddMonths(1).AddDays(-1),
                    Reference = model.ServiceInvoiceNo,
                    Description = model.ServiceName,
                    AccountId = arTradeTitle.AccountId,
                    AccountNo = arTradeTitle.AccountNumber,
                    AccountTitle = arTradeTitle.AccountName,
                    Debit = model.Total - (withHoldingTaxAmount + withHoldingVatAmount),
                    Credit = 0,
                    Company = model.Company,
                    CreatedBy = model.PostedBy!,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    SubAccountType = SubAccountType.Customer,
                    SubAccountId = model.CustomerId,
                    SubAccountName = model.CustomerName,
                    ModuleType = nameof(ModuleType.Sales)
                }
            );
            if (withHoldingTaxAmount > 0)
            {
                ledgers.Add(
                    new FilprideGeneralLedgerBook
                    {
                        Date = model.Period.AddMonths(1).AddDays(-1),
                        Reference = model.ServiceInvoiceNo,
                        Description = model.ServiceName,
                        AccountId = arTradeCwt.AccountId,
                        AccountNo = arTradeCwt.AccountNumber,
                        AccountTitle = arTradeCwt.AccountName,
                        Debit = withHoldingTaxAmount,
                        Credit = 0,
                        Company = model.Company,
                        CreatedBy = model.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Sales)
                    }
                );
            }
            if (withHoldingVatAmount > 0)
            {
                ledgers.Add(
                    new FilprideGeneralLedgerBook
                    {
                        Date = model.Period.AddMonths(1).AddDays(-1),
                        Reference = model.ServiceInvoiceNo,
                        Description = model.ServiceName,
                        AccountId = arTradeCwv.AccountId,
                        AccountNo = arTradeCwv.AccountNumber,
                        AccountTitle = arTradeCwv.AccountName,
                        Debit = withHoldingVatAmount,
                        Credit = 0,
                        Company = model.Company,
                        CreatedBy = model.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Sales)
                    }
                );
            }

            ledgers.Add(
                new FilprideGeneralLedgerBook
                {
                    Date = model.Period.AddMonths(1).AddDays(-1),
                    Reference = model.ServiceInvoiceNo,
                    Description = model.ServiceName,
                    AccountId = servicesTitle.AccountId,
                    AccountNo = servicesTitle.AccountNumber,
                    AccountTitle = servicesTitle.AccountName,
                    Debit = 0,
                    Credit = netOfVatAmount,
                    Company = model.Company,
                    CreatedBy = model.PostedBy!,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModuleType = nameof(ModuleType.Sales)
                }
            );

            if (vatAmount > 0)
            {
                ledgers.Add(
                    new FilprideGeneralLedgerBook
                    {
                        Date = model.Period.AddMonths(1).AddDays(-1),
                        Reference = model.ServiceInvoiceNo,
                        Description = model.ServiceName,
                        AccountId = vatOutputTitle.AccountId,
                        AccountNo = vatOutputTitle.AccountNumber,
                        AccountTitle = vatOutputTitle.AccountName,
                        Debit = 0,
                        Credit = vatAmount,
                        Company = model.Company,
                        CreatedBy = model.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Sales)
                    }
                );
            }

            if (!IsJournalEntriesBalanced(ledgers))
            {
                throw new ArgumentException("Debit and Credit is not equal, check your entries.");
            }

            await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            #endregion --General Ledger Book Recording
        }
    }
}
