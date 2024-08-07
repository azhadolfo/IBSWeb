using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsReceivable;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ISalesInvoiceRepository : IRepository<FilprideSalesInvoice>
    {
        Task<string> GenerateCodeAsync(string company, CancellationToken cancellationToken = default);

        Task<DateOnly> ComputeDueDateAsync(string customerTerms, DateOnly transactionDate);
    }
}