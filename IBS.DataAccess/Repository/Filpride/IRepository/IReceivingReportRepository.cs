﻿using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.ViewModels;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IReceivingReportRepository : IRepository<FilprideReceivingReport>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task<DateOnly> CalculateDueDateAsync(string terms, DateOnly transactionDate, CancellationToken cancellationToken = default);

        Task UpdateAsync(ReceivingReportViewModel viewModel, string userName, CancellationToken cancellationToken = default);

        Task PostAsync(FilprideReceivingReport receivingReport, string userName, CancellationToken cancellationToken = default);
    }
}