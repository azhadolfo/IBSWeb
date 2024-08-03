﻿using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsReceivable;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ICollectionReceiptRepository : IRepository<FilprideCollectionReceipt>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task<int> UpdateInvoice(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task<int> UpdateMutipleInvoice(string[] siNo, decimal[] paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task<int> RemoveSIPayment(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task<int> RemoveSVPayment(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task<int> UpdateSV(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task<List<FilprideOffsettings>> GetOffsettings(string source, string reference, CancellationToken cancellationToken = default);
    }
}