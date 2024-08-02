using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ICheckVoucherRepository : IRepository<FilprideCheckVoucherHeader>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task UpdateInvoicingVoucher(decimal paymentAmount, int invoiceVoucherId, CancellationToken cancellationToken = default);
    }
}