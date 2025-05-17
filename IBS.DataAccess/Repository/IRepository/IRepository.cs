using IBS.Dtos;
using IBS.Models.Filpride.Books;
using IBS.Models.Mobility;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default);

        Task<T?> GetAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);

        Task AddAsync(T entity, CancellationToken cancellationToken = default);

        Task RemoveAsync(T entity, CancellationToken cancellationToken = default);

        Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        Task RemoveRecords<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class;

        bool IsJournalEntriesBalanced(IEnumerable<MobilityGeneralLedger> journals);

        bool IsJournalEntriesBalanced(IEnumerable<FilprideGeneralLedgerBook> journals);

        (string AccountNo, string AccountTitle) GetSalesAccountTitle(string productCode);

        (string AccountNo, string AccountTitle) GetCogsAccountTitle(string productCode);

        (string AccountNo, string AccountTitle) MobilityGetSalesAccountTitle(string productCode);

        (string AccountNo, string AccountTitle) MobilityGetCogsAccountTitle(string productCode);

        (string AccountNo, string AccountTitle) GetInventoryAccountTitle(string productCode);

        (string AccountNo, string AccountTitle) MobilityGetInventoryAccountTitle(string productCode);

        (string AccountNo, string AccountTitle) GetFreightAccount(string productCode);

        (string AccountNo, string AccountTitle) GetCommissionAccount(string productCode);

        decimal ComputeNetOfVat(decimal grossAmount);

        decimal ComputeVatAmount(decimal netOfVatAmount);

        decimal ComputeEwtAmount(decimal netOfVatAmount, decimal percent);

        decimal ComputeNetOfEwt(decimal grossAmount, decimal ewtAmount);

        // Retrieving DTOs (Data Transfer Objects)

        Task<ProductDto?> MapProductToDTO(string productCode, CancellationToken cancellationToken = default);

        Task<StationDto?> MapStationToDTO(string stationCode, CancellationToken cancellationToken = default);

        Task<SupplierDto?> MapSupplierToDTO(string supplierCode, CancellationToken cancellationToken = default);

        Task<CustomerDto?> MapCustomerToDTO(int? customerId, string? customerCode, CancellationToken cancellationToken = default);

        Task<List<AccountTitleDto>> GetListOfAccountTitleDto(CancellationToken cancellationToken = default);
    }
}
