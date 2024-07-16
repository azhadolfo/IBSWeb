﻿using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MasterFile.IRepository
{
    public interface IChartOfAccountRepository : IRepository<ChartOfAccount>
    {
        Task<List<SelectListItem>> GetMainAccount(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMemberAccount(string parentAcc, CancellationToken cancellationToken = default);

        Task<ChartOfAccount> GenerateAccount(ChartOfAccount model, string thirdLevel, CancellationToken cancellationToken = default);

        Task UpdateAsync(ChartOfAccount model, CancellationToken cancellationToken = default);

        IEnumerable<ChartOfAccountDto> GetSummaryReportView(CancellationToken cancellationToken = default);
    }
}