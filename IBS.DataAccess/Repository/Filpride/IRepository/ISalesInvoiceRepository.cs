using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsReceivable;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ISalesInvoiceRepository : IRepository<FilprideSalesInvoice>
    {
        Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default);

        DateOnly ComputeDueDateAsync(string customerTerms, DateOnly transactionDate);

        Task PostAsync(FilprideSalesInvoice salesInvoice, CancellationToken cancellationToken = default);
    }
}
