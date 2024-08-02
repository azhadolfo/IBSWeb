using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsReceivable;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ServiceInvoiceRepository : Repository<FilprideServiceInvoice>, IServiceInvoiceRepository
    {
        private ApplicationDbContext _db;

        public ServiceInvoiceRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            FilprideServiceInvoice? lastSv = await _db
                .FilprideServiceInvoices
                .OrderBy(c => c.ServiceInvoiceNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastSv != null)
            {
                string lastSeries = lastSv.ServiceInvoiceNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "SV0000000001";
            }
        }
    }
}