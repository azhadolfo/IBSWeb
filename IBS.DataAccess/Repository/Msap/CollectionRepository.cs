using IBS.Models.Msap.Books;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.Msap.Enums;
using IBS.Models;
using IBS.Models.Msap;
using IBS.Utility.Msap;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class CollectionRepository(ApplicationDbContext db): Repository<Collection>(db), ICollectionRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public override async Task<Collection?> GetAsync(Expression<Func<Collection, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(c => c.Customer)
                .Include(c => c.BankAccount)
                .Include(c => c.PaidBills)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<Collection>> GetAllAsync(Expression<Func<Collection, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<Collection> query = dbSet
                .Include(c => c.Customer)
                .Include(c => c.BankAccount);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMsapCustomersById(CancellationToken cancellationToken = default)
        {
            return await _db.MsapCustomers
                .OrderBy(s => s.CustomerName)
                .Select(s => new SelectListItem
                {
                    Value = s.CustomerId.ToString(),
                    Text = s.CustomerName
                }).ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMsapCustomersWithCollectiblesSelectList(int collectionId, string type, CancellationToken cancellationToken = default)
        {
            var billingsToBeCollected = await _db.MsapBillings
                .Where(t => (t.Balance > 0 || (collectionId != 0 && t.CollectionId == collectionId)) && t.Status == SD.BillingStatus.ForCollection)
                .Include(t => t.Customer)
                .ToListAsync(cancellationToken);

            var listOfCustomerWithCollectibleBillings = billingsToBeCollected
                .Where(t => t.Customer != null)
                .Select(t => t.Customer.CustomerId)
                .Distinct()
                .ToList();

            return await _db.MsapCustomers
                .Where(c => listOfCustomerWithCollectibleBillings.Contains(c.CustomerId) &&
                            (string.IsNullOrEmpty(type) || c.Type == type))
                .OrderBy(s => s.CustomerName)
                .Select(s => new SelectListItem
                {
                    Value = s.CustomerId.ToString(),
                    Text = s.CustomerName
                }).ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMsapUncollectedBillingsById(CancellationToken cancellationToken = default)
        {
            var billingsList = await _db.MsapBillings
                .Where(dt => dt.Balance > 0)
                .OrderBy(dt => dt.MsapBillingNumber).Select(s => new SelectListItem
                {
                    Value = s.MsapBillingId.ToString(),
                    Text = $"{s.MsapBillingNumber} - {s.Customer.CustomerName}, {s.Date}"
                }).ToListAsync(cancellationToken);

            return billingsList;
        }

        public async Task<List<SelectListItem>> GetMsapCollectedBillsById(int collectionId, CancellationToken cancellationToken = default)
        {
            var billingsList = await _db.MsapBillings
                .Where(dt => dt.CollectionId == collectionId)
                .OrderBy(dt => dt.MsapBillingNumber).Select(b => new SelectListItem
                {
                    Value = b.MsapBillingId.ToString(),
                    Text = $"{b.MsapBillingNumber}"
                }).ToListAsync(cancellationToken);

            return billingsList;
        }

        public async Task<List<SelectListItem>?> GetMsapUncollectedBillingsByCustomer(int? customerId, CancellationToken cancellationToken)
        {
            var billings = await _db
                .MsapBillings
                .Where(b => b.CustomerId == customerId && b.Balance > 0 && b.Status == SD.BillingStatus.ForCollection)
                .Include(b => b.Customer)
                .OrderBy(b => b.MsapBillingNumber)
                .ToListAsync(cancellationToken);

            var billingsList = billings.Select(b => new SelectListItem
            {
                Value = b.MsapBillingId.ToString(),
                Text = $"{b.MsapBillingNumber}"
            }).ToList();

            return billingsList;
        }

        public async Task<List<Billing>> GetMsapUncollectedBillingsByCustomerList(int? customerId, CancellationToken cancellationToken)
        {
            return await _db
                .MsapBillings
                .Where(b => b.CustomerId == customerId && b.Balance > 0 && b.Status == SD.BillingStatus.ForCollection)
                .OrderBy(b => b.MsapBillingNumber)
                .ToListAsync(cancellationToken);
        }

        public async Task PostAsync(Collection collection, List<Offsettings> offsettings, CancellationToken cancellationToken = default)
        {
            var ledgers = new List<GeneralLedgerBook>();
            var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
            var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == SD.MsapAccounts.CashInBank) ?? throw new ArgumentException($"Account title '{SD.MsapAccounts.CashInBank}' not found.");
            var arTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == SD.MsapAccounts.ArTrade) ?? throw new ArgumentException($"Account title '{SD.MsapAccounts.ArTrade}' not found.");
            var arTradeCwt = accountTitlesDto.Find(c => c.AccountNumber == SD.MsapAccounts.ArTradeCwt) ?? throw new ArgumentException($"Account title '{SD.MsapAccounts.ArTradeCwt}' not found.");
            var arTradeCwv = accountTitlesDto.Find(c => c.AccountNumber == SD.MsapAccounts.ArTradeCwv) ?? throw new ArgumentException($"Account title '{SD.MsapAccounts.ArTradeCwv}' not found.");
            var cwt = accountTitlesDto.Find(c => c.AccountNumber == SD.MsapAccounts.Cwt) ?? throw new ArgumentException($"Account title '{SD.MsapAccounts.Cwt}' not found.");
            var cwv = accountTitlesDto.Find(c => c.AccountNumber == SD.MsapAccounts.Cwv) ?? throw new ArgumentException($"Account title '{SD.MsapAccounts.Cwv}' not found.");
            var offsetAmount = 0m;

            var customerName = collection.Customer?.CustomerName ?? "Unknown Customer";

            if (collection.CashAmount > 0 || collection.CheckAmount > 0)
            {
                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collection.Date,
                        Reference = collection.MsapCollectionNumber,
                        Description = "Collection for Receivable",
                        AccountId = cashInBankTitle.AccountId,
                        AccountNo = cashInBankTitle.AccountNumber,
                        AccountTitle = cashInBankTitle.AccountName,
                        Debit = collection.CashAmount + collection.CheckAmount,
                        Credit = 0,
                        Company = collection.Company,
                        CreatedBy = collection.CreatedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        SubAccountType = SubAccountType.BankAccount,
                        SubAccountId = collection.BankId,
                        SubAccountName = collection.BankId.HasValue
                            ? $"{collection.BankAccountNumber} {collection.BankAccountName}"
                            : null,
                        ModuleType = nameof(ModuleType.Collection)
                    }
                );
            }

            if (collection.EWT > 0)
            {
                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collection.Date,
                        Reference = collection.MsapCollectionNumber,
                        Description = "Collection for Receivable",
                        AccountId = cwt.AccountId,
                        AccountNo = cwt.AccountNumber,
                        AccountTitle = cwt.AccountName,
                        Debit = collection.EWT,
                        Credit = 0,
                        Company = collection.Company,
                        CreatedBy = collection.CreatedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Collection)
                    }
                );
            }

            if (collection.WVAT > 0)
            {
                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collection.Date,
                        Reference = collection.MsapCollectionNumber,
                        Description = "Collection for Receivable",
                        AccountId = cwv.AccountId,
                        AccountNo = cwv.AccountNumber,
                        AccountTitle = cwv.AccountName,
                        Debit = collection.WVAT,
                        Credit = 0,
                        Company = collection.Company,
                        CreatedBy = collection.CreatedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Collection)
                    }
                );
            }

            foreach (var item in offsettings)
            {
                var account = accountTitlesDto.Find(c => c.AccountNumber == item.AccountNo) ??
                              throw new ArgumentException($"Account title '{item.AccountNo}' not found.");

                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collection.Date,
                        Reference = collection.MsapCollectionNumber,
                        Description = "Collection for Receivable",
                        AccountId = account.AccountId,
                        AccountNo = account.AccountNumber,
                        AccountTitle = account.AccountName,
                        Debit = item.Amount,
                        Credit = 0,
                        Company = collection.Company,
                        CreatedBy = collection.CreatedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Collection)
                    }
                );

                offsetAmount += item.Amount;
            }

            if (collection.CashAmount > 0 || collection.CheckAmount > 0 || offsetAmount > 0)
            {
                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collection.Date,
                        Reference = collection.MsapCollectionNumber,
                        Description = "Collection for Receivable",
                        AccountId = arTradeTitle.AccountId,
                        AccountNo = arTradeTitle.AccountNumber,
                        AccountTitle = arTradeTitle.AccountName,
                        Debit = 0,
                        Credit = collection.CashAmount + collection.CheckAmount + offsetAmount,
                        Company = collection.Company,
                        CreatedBy = collection.CreatedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        SubAccountType = SubAccountType.Customer,
                        SubAccountId = collection.CustomerId,
                        SubAccountName = customerName,
                        ModuleType = nameof(ModuleType.Collection)
                    }
                );
            }

            if (collection.EWT > 0)
            {
                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collection.Date,
                        Reference = collection.MsapCollectionNumber,
                        Description = "Collection for Receivable",
                        AccountId = arTradeCwt.AccountId,
                        AccountNo = arTradeCwt.AccountNumber,
                        AccountTitle = arTradeCwt.AccountName,
                        Debit = 0,
                        Credit = collection.EWT,
                        Company = collection.Company,
                        CreatedBy = collection.CreatedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Collection)
                    }
                );
            }

            if (collection.WVAT > 0)
            {
                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collection.Date,
                        Reference = collection.MsapCollectionNumber,
                        Description = "Collection for Receivable",
                        AccountId = arTradeCwv.AccountId,
                        AccountNo = arTradeCwv.AccountNumber,
                        AccountTitle = arTradeCwv.AccountName,
                        Debit = 0,
                        Credit = collection.WVAT,
                        Company = collection.Company,
                        CreatedBy = collection.CreatedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Collection)
                    }
                );
            }


            #region Cash Receipt Book Recording

            var particulars = string.Join(", ", collection.PaidBills?.Select(b => b.MsapBillingNumber) ?? new List<string>());

            var crb = new List<CashReceiptBook>
            {
                new()
                {
                    Date = collection.Date,
                    RefNo = collection.MsapCollectionNumber,
                    CustomerName = customerName,
                    Bank = collection.BankAccount?.Bank ?? "--",
                    CheckNo = collection.CheckNumber ?? "--",
                    COA = $"{cashInBankTitle.AccountNumber} {cashInBankTitle.AccountName}",
                    Particulars = particulars,
                    Debit = collection.CashAmount + collection.CheckAmount,
                    Credit = 0,
                    Company = collection.Company,
                    CreatedBy = collection.CreatedBy,
                    CreatedDate = collection.CreatedDate,
                }
            };

            if (collection.EWT > 0)
            {
                crb.Add(
                    new CashReceiptBook
                    {
                        Date = collection.Date,
                        RefNo = collection.MsapCollectionNumber,
                        CustomerName = customerName,
                        Bank = collection.BankAccount?.Bank ?? "--",
                        CheckNo = collection.CheckNumber ?? "--",
                        COA = $"{cwt.AccountNumber} {cwt.AccountName}",
                        Particulars = particulars,
                        Debit = collection.EWT,
                        Credit = 0,
                        Company = collection.Company,
                        CreatedBy = collection.CreatedBy,
                        CreatedDate = collection.CreatedDate,
                    }
                );
            }

            if (collection.WVAT > 0)
            {
                crb.Add(
                    new CashReceiptBook
                    {
                        Date = collection.Date,
                        RefNo = collection.MsapCollectionNumber,
                        CustomerName = customerName,
                        Bank = collection.BankAccount?.Bank ?? "--",
                        CheckNo = collection.CheckNumber ?? "--",
                        COA = $"{cwv.AccountNumber} {cwv.AccountName}",
                        Particulars = particulars,
                        Debit = collection.WVAT,
                        Credit = 0,
                        Company = collection.Company,
                        CreatedBy = collection.CreatedBy,
                        CreatedDate = collection.CreatedDate,
                    }
                );
            }

            foreach (var item in offsettings)
            {
                var account = accountTitlesDto.Find(c => c.AccountNumber == item.AccountNo) ??
                              throw new ArgumentException($"Account title '{item.AccountNo}' not found.");

                crb.Add(
                    new CashReceiptBook
                    {
                        Date = collection.Date,
                        RefNo = collection.MsapCollectionNumber,
                        CustomerName = customerName,
                        Bank = collection.BankAccount?.Bank ?? "--",
                        CheckNo = collection.CheckNumber ?? "--",
                        COA = $"{account.AccountNumber} {account.AccountName}",
                        Particulars = particulars,
                        Debit = item.Amount,
                        Credit = 0,
                        Company = collection.Company,
                        CreatedBy = collection.CreatedBy,
                        CreatedDate = collection.CreatedDate,
                    }
                );
            }

            crb.Add(
                new CashReceiptBook
                {
                    Date = collection.Date,
                    RefNo = collection.MsapCollectionNumber,
                    CustomerName = customerName,
                    Bank = collection.BankAccount?.Bank ?? "--",
                    CheckNo = collection.CheckNumber ?? "--",
                    COA = $"{arTradeTitle.AccountNumber} {arTradeTitle.AccountName}",
                    Particulars = particulars,
                    Debit = 0,
                    Credit = collection.CashAmount + collection.CheckAmount + offsetAmount,
                    Company = collection.Company,
                    CreatedBy = collection.CreatedBy,
                    CreatedDate = collection.CreatedDate,
                }
            );

            if (collection.EWT > 0)
            {
                crb.Add(
                    new CashReceiptBook
                    {
                        Date = collection.Date,
                        RefNo = collection.MsapCollectionNumber,
                        CustomerName = customerName,
                        Bank = collection.BankAccount?.Bank ?? "--",
                        CheckNo = collection.CheckNumber ?? "--",
                        COA = $"{arTradeCwt.AccountNumber} {arTradeCwt.AccountName}",
                        Particulars = particulars,
                        Debit = 0,
                        Credit = collection.EWT,
                        Company = collection.Company,
                        CreatedBy = collection.CreatedBy,
                        CreatedDate = collection.CreatedDate,
                    }
                );
            }

            if (collection.WVAT > 0)
            {
                crb.Add(
                    new CashReceiptBook
                    {
                        Date = collection.Date,
                        RefNo = collection.MsapCollectionNumber,
                        CustomerName = customerName,
                        Bank = collection.BankAccount?.Bank ?? "--",
                        CheckNo = collection.CheckNumber ?? "--",
                        COA = $"{arTradeCwv.AccountNumber} {arTradeCwv.AccountName}",
                        Particulars = particulars,
                        Debit = 0,
                        Credit = collection.WVAT,
                        Company = collection.Company,
                        CreatedBy = collection.CreatedBy,
                        CreatedDate = collection.CreatedDate,
                    }
                );
            }

            await _db.SaveChangesAsync(cancellationToken);

            #endregion Cash Receipt Book Recording
        }

        public async Task UpdateBillingPayment(int billingId, decimal paidAmount, CancellationToken cancellationToken = default)
        {
            var billing = await _db.MsapBillings.FirstOrDefaultAsync(b => b.MsapBillingId == billingId, cancellationToken);
            if (billing != null)
            {
                billing.AmountPaid += paidAmount;
                billing.Balance = billing.Amount - billing.AmountPaid;

                if (billing.Balance <= 0)
                {
                    billing.IsPaid = true;
                    billing.Status = SD.BillingStatus.Collected;
                }
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task RemoveBillingPayment(int billingId, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var billing = await _db.MsapBillings.FirstOrDefaultAsync(b => b.MsapBillingId == billingId, cancellationToken);
            if (billing != null)
            {
                var total = paidAmount + offsetAmount;
                billing.AmountPaid -= total;
                billing.Balance += total;
                billing.IsPaid = false;
                billing.Status = SD.BillingStatus.ForCollection;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<string> GenerateCollectionNumber(CancellationToken cancellationToken = default)
        {
            var lastRecord = await _db.MsapCollections
                .Where(b => b.IsUndocumented && !string.IsNullOrEmpty(b.MsapCollectionNumber))
                .OrderByDescending(b => b.MsapCollectionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastRecord == null)
            {
                return "C000001";
            }

            var lastSeries = lastRecord.MsapCollectionNumber.Substring(1); // "C" is 1 char
            if (int.TryParse(lastSeries, out int lastNumber))
            {
                return "C" + ((lastNumber + 1).ToString("D6"));
            }

            return "C" + (DateTime.Now.Ticks % 1000000).ToString("D6");
        }

        public async Task<(IEnumerable<Collection> Data, int RecordsFiltered, int TotalRecords)> GetPagedCollectionsAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            var query = dbSet
                .Include(c => c.Customer)
                .Include(c => c.BankAccount)
                .AsQueryable();

            if (!string.IsNullOrEmpty(parameters.Search.Value))
            {
                var s = parameters.Search.Value.ToLower();
                query = query.Where(c =>
                    c.MsapCollectionNumber.ToLower().Contains(s) ||
                    c.Customer.CustomerName.ToLower().Contains(s)
                );
            }

            // Column-specific search
            if (parameters.Columns != null)
            {
                foreach (var column in parameters.Columns)
                {
                    if (column.Search?.Value is { Length: > 0 } searchValue)
                    {
                        if (column.Data == "date" || column.Data == "Date")
                        {
                            if (DateOnly.TryParse(searchValue, out var parsedDate))
                            {
                                query = query.Where(c => c.Date == parsedDate);
                            }
                        }
                    }
                }
            }

            var totalRecords = await dbSet.CountAsync(cancellationToken);
            var recordsFiltered = await query.CountAsync(cancellationToken);

            if (parameters.Order?.Count > 0 && parameters.Columns != null)
            {
                var col = parameters.Columns[parameters.Order[0].Column].Data;
                var dir = parameters.Order[0].Dir.ToLower() == "asc" ? "ascending" : "descending";
                query = query.OrderBy($"{col} {dir}");
            }
            else
            {
                query = query.OrderByDescending(c => c.Date);
            }

            var data = await query
                .Skip(parameters.Start)
                .Take(parameters.Length)
                .ToListAsync(cancellationToken);

            return (data, recordsFiltered, totalRecords);
        }
    }
}


