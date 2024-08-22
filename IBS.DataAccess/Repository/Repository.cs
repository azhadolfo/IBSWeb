using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MasterFile;
using IBS.Models.Mobility;
using IBS.Models.Mobility.MasterFile;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
            this.dbSet = _db.Set<T>();
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

        public virtual async Task<T> GetAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
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
                decimal totalDebit = journals.Sum(j => j.Debit);
                decimal totalCredit = journals.Sum(j => j.Credit);

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
                decimal totalDebit = journals.Sum(j => j.Debit);
                decimal totalCredit = journals.Sum(j => j.Credit);

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

        public async Task<ProductDto> MapProductToDTO(string productCode, CancellationToken cancellationToken = default)
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

        public async Task<StationDto> MapStationToDTO(string stationCode, CancellationToken cancellationToken = default)
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

        public async Task<SupplierDto> MapSupplierToDTO(string supplierCode, CancellationToken cancellationToken = default)
        {
            return await _db.Set<FilprideSupplier>()
                .Where(s => s.SupplierCode == supplierCode)
                .Select(s => new SupplierDto
                {
                    SupplierId = s.SupplierId,
                    SupplierCode = s.SupplierCode,
                    SupplierName = s.SupplierName
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public (string AccountNo, string AccountTitle) GetSalesAccountTitle(string productCode)
        {
            return productCode switch
            {
                "PET001" => ("4010101", "Sales - Biodiesel"),
                "PET002" => ("4010102", "Sales - Econogas"),
                "PET003" => ("4010103", "Sales - Envirogas"),
                _ => ("4011001", "Sales - Lubes"),
            };
        }

        public (string AccountNo, string AccountTitle) GetCogsAccountTitle(string productCode)
        {
            return productCode switch
            {
                "PET001" => ("5010101", "COGS - Biodiesel"),
                "PET002" => ("5010102", "COGS - Econogas"),
                "PET003" => ("5010103", "COGS - Envirogas"),
                _ => ("5011001", "COGS - Lubes"),
            };
        }

        public (string AccountNo, string AccountTitle) GetInventoryAccountTitle(string productCode)
        {
            return productCode switch
            {
                "PET001" => ("1010401", "Inventory - Biodiesel"),
                "PET002" => ("1010402", "Inventory - Econogas"),
                "PET003" => ("1010403", "Inventory - Envirogas"),
                _ => ("1010410", "Inventory - Lubes"),
            };
        }

        public decimal ComputeNetOfVat(decimal grossAmount)
        {
            if (grossAmount < 0)
            {
                throw new ArgumentException("Gross amount cannot be negative.");
            }

            return grossAmount / (1 + VatRate);
        }

        public decimal ComputeVatAmount(decimal grossAmount)
        {
            if (grossAmount < 0)
            {
                throw new ArgumentException("Gross amount cannot be negative.");
            }

            return grossAmount - ComputeNetOfVat(grossAmount);
        }

        public async Task<CustomerDto> MapCustomerToDTO(int? customerId, string? customerCode, CancellationToken cancellationToken = default)
        {
            return await _db.Set<FilprideCustomer>()
                .Where(c => c.CustomerId == customerId || c.CustomerCode == customerCode)
                .Select(c => new CustomerDto
                {
                    CustomerId = c.CustomerId,
                    CustomerCode = c.CustomerCode,
                    CustomerName = c.CustomerName,
                    CustomerAddress = c.CustomerAddress,
                    CustomerTin = c.CustomerTin
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        public async Task<int> RemoveRecords<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
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

                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                    return entitiesToRemove.Count;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            else
            {
                throw new ArgumentException($"No entities found with identifier value: '{predicate.Body.ToString}'");
            }
        }
    }
}