using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsReceivable;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IServiceInvoiceRepository : IRepository<FilprideServiceInvoice>
    {
        Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default);
    }
}
