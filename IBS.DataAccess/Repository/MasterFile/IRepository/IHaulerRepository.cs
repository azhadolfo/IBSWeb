﻿using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MasterFile.IRepository
{
    public interface IHaulerRepository : IRepository<Hauler>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task<bool> IsHaulerNameExistAsync(string haulerName, CancellationToken cancellationToken);

        Task UpdateAsync(Hauler model, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetHaulerListAsync(CancellationToken cancellationToken = default);
    }
}