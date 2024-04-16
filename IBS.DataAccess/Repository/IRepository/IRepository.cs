using IBS.Dtos;
using IBS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default);

        Task<T> GetAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);

        Task AddAsync(T entity, CancellationToken cancellationToken = default);

        Task RemoveAsync(T entity, CancellationToken cancellationToken = default);

        Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        bool IsJournalEntriesBalanced(IEnumerable<GeneralLedger> journals);


        // Retrieving DTOs (Data Transfer Objects)

        Task<ProductDto> MapProductToDTO(string productCode, CancellationToken cancellationToken = default);
        Task<StationDto> MapStationToDTO(string stationCode, CancellationToken cancellationToken = default);
        Task<SupplierDto> MapSupplierToDTO(string supplierCode, CancellationToken cancellationToken = default);
    }
}