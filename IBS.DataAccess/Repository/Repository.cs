using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MasterFile;
using IBS.Models.Mobility;
using IBS.Models.Mobility.MasterFile;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.DTOs;

namespace IBS.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<T> dbSet;

        private const decimal VatRate = 0.12m;

        public Repository(ApplicationDbContext db)
        {
            _db = db;
            dbSet = _db.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public virtual async Task<T?> GetAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            dbSet.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public bool IsJournalEntriesBalanced(IEnumerable<MobilityGeneralLedger> journals)
        {
            try
            {
                var totalDebit = Math.Round(journals.Sum(j => j.Debit), 4);
                var totalCredit = Math.Round(journals.Sum(j => j.Credit), 4);

                return totalDebit == totalCredit;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        public bool IsJournalEntriesBalanced(IEnumerable<FilprideGeneralLedgerBook> journals)
        {
            try
            {
                var totalDebit = Math.Round(journals.Sum(j => j.Debit), 4);
                var totalCredit = Math.Round(journals.Sum(j => j.Credit), 4);

                return totalDebit == totalCredit;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        public async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
        {
            dbSet.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            dbSet.RemoveRange(entities);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<ProductDto?> MapProductToDTO(string productCode, CancellationToken cancellationToken = default)
        {
            return await _db.Set<Product>()
                .Where(p => p.ProductCode == productCode)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<StationDto?> MapStationToDTO(string stationCode, CancellationToken cancellationToken = default)
        {
            return await _db.Set<MobilityStation>()
                .Where(s => s.StationCode == stationCode)
                .Select(s => new StationDto
                {
                    StationId = s.StationId,
                    StationCode = s.StationCode,
                    StationName = s.StationName,
                    StationPOSCode = s.PosCode
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<SupplierDto?> MapSupplierToDTO(string supplierCode, CancellationToken cancellationToken = default)
        {
            return await _db.Set<FilprideSupplier>()
                .Where(s => s.SupplierCode == supplierCode)
                .Select(s => new SupplierDto
                {
                    SupplierId = s.SupplierId,
                    SupplierCode = s.SupplierCode!,
                    SupplierName = s.SupplierName
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public (string AccountNo, string AccountTitle) GetSalesAccountTitle(string productCode)
        {
            return productCode switch
            {
                "PET001" => ("401010100", "Sales - Biodiesel"),
                "PET002" => ("401010200", "Sales - Econogas"),
                "PET003" => ("401010300", "Sales - Envirogas"),
                _ => throw new ArgumentException($"Invalid product code: {productCode}"),
            };
        }

        public (string AccountNo, string AccountTitle) GetCogsAccountTitle(string productCode)
        {
            return productCode switch
            {
                "PET001" => ("501010100", "COGS - Biodiesel"),
                "PET002" => ("501010200", "COGS - Econogas"),
                "PET003" => ("501010300", "COGS - Envirogas"),
                _ => throw new ArgumentException($"Invalid product code: {productCode}"),
            };
        }

        public (string AccountNo, string AccountTitle) MobilityGetSalesAccountTitle(string productCode)
        {
            return productCode switch
            {
                "PET001" => ("4010101", "Sales - Biodiesel"),
                "PET002" => ("4010102", "Sales - Econogas"),
                "PET003" => ("4010103", "Sales - Envirogas"),
                _ => ("4010110", "Inventory - Lubes"),
            };
        }

        public (string AccountNo, string AccountTitle) MobilityGetCogsAccountTitle(string productCode)
        {
            return productCode switch
            {
                "PET001" => ("5010101", "COGS - Biodiesel"),
                "PET002" => ("5010102", "COGS - Econogas"),
                "PET003" => ("5010103", "COGS - Envirogas"),
                _ => ("5010110", "Inventory - Lubes"),
            };
        }

        public (string AccountNo, string AccountTitle) GetInventoryAccountTitle(string productCode)
        {
            return productCode switch
            {
                "PET001" => ("101040100", "Inventory - Biodiesel"),
                "PET002" => ("101040200", "Inventory - Econogas"),
                "PET003" => ("101040300", "Inventory - Envirogas"),
                _ => throw new ArgumentException($"Invalid product code: {productCode}"),
            };
        }

        public (string AccountNo, string AccountTitle) MobilityGetInventoryAccountTitle(string productCode)
        {
            return productCode switch
            {
                "PET001" => ("1010401", "Inventory - Biodiesel"),
                "PET002" => ("1010402", "Inventory - Econogas"),
                "PET003" => ("1010403", "Inventory - Envirogas"),
                _ => ("1010410", "Inventory - Lubes"),
            };
        }

        public (string AccountNo, string AccountTitle) GetFreightAccount(string productCode)
        {
            return productCode switch
            {
                "PET001" => ("502010100", "COGS - Freight - Biodiesel"),
                "PET002" => ("502010200", "COGS - Freight - Econogas"),
                "PET003" => ("502010300", "COGS - Freight - Envirogas"),
                _ => throw new ArgumentException($"Invalid product code: {productCode}"),
            };
        }

        public (string AccountNo, string AccountTitle) GetCommissionAccount(string productCode)
        {
            return productCode switch
            {
                "PET001" => ("503010100", "COGS  - Commission - Biodiesel"),
                "PET002" => ("503010200", "COGS - Commission - Econogas"),
                "PET003" => ("503010300", "COGS - Commission - Envirogas"),
                _ => throw new ArgumentException($"Invalid product code: {productCode}"),
            };
        }

        public decimal ComputeNetOfVat(decimal grossAmount)
        {
            if (grossAmount == 0)
            {
                throw new ArgumentException("Gross amount cannot be zero.");
            }

            return grossAmount / (1 + VatRate);
        }

        public decimal ComputeVatAmount(decimal netOfVatAmount)
        {
            if (netOfVatAmount == 0)
            {
                throw new ArgumentException("Net of vat amount cannot be negative or zero.");
            }

            return netOfVatAmount * VatRate;
        }

        public async Task<CustomerDto?> MapCustomerToDTO(int? customerId, string? customerCode, CancellationToken cancellationToken = default)
        {
            return await _db.Set<FilprideCustomer>()
                .Where(c => c.CustomerId == customerId || c.CustomerCode == customerCode)
                .Select(c => new CustomerDto
                {
                    CustomerId = c.CustomerId,
                    CustomerCode = c.CustomerCode!,
                    CustomerName = c.CustomerName,
                    CustomerAddress = c.CustomerAddress,
                    CustomerTin = c.CustomerTin,
                    CustomerTerms = c.CustomerTerms
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task RemoveRecords<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
       where TEntity : class
        {
            var entitySet = _db.Set<TEntity>();
            var entitiesToRemove = await entitySet.Where(predicate).ToListAsync(cancellationToken);

            if (entitiesToRemove.Any())
            {
                foreach (var entity in entitiesToRemove)
                {
                    entitySet.Remove(entity);
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public decimal ComputeEwtAmount(decimal netOfVatAmount, decimal percent)
        {
            if (netOfVatAmount == 0)
            {
                throw new ArgumentException("Net of vat amount cannot be zero.");
            }

            if (percent == 0)
            {
                throw new ArgumentException("Ewt percent cannot be zero.");
            }

            return netOfVatAmount * percent;
        }

        public decimal ComputeNetOfEwt(decimal grossAmount, decimal ewtAmount)
        {
            if (grossAmount == 0)
            {
                throw new ArgumentException("Gross amount cannot be zero.");
            }

            if (ewtAmount == 0)
            {
                throw new ArgumentException("Ewt amount cannot be zero.");
            }

            return grossAmount - ewtAmount;
        }

        public async Task<List<AccountTitleDto>> GetListOfAccountTitleDto(CancellationToken cancellationToken = default)
        {
            return await _db.FilprideChartOfAccounts
               .Where(coa => coa.Level == 4 || coa.Level == 5)
               .Select(coa => new AccountTitleDto
               {
                   AccountId = coa.AccountId,
                   AccountNumber = coa.AccountNumber!,
                   AccountName = coa.AccountName
               })
               .ToListAsync(cancellationToken);
        }
    }
}
