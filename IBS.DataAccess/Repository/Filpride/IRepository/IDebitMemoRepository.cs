﻿using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IDebitMemoRepository : IRepository<FilprideDebitMemo>
    {
        Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default);
    }
}