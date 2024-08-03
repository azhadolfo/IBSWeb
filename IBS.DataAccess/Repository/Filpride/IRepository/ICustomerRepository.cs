﻿using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;

namespace IBS.DataAccess.Repository.MasterFile.IRepository
{
    public interface ICustomerRepository : IRepository<FilprideCustomer>
    {
        Task<bool> IsTinNoExistAsync(string tin, CancellationToken cancellationToken = default);

        Task<string> GenerateCodeAsync(string customerType, CancellationToken cancellationToken = default);

        Task UpdateAsync(FilprideCustomer model, CancellationToken cancellationToken = default);
    }
}