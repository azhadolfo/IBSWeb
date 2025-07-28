using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Utility.Enums;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class CollectionReceiptRepository : Repository<MobilityCollectionReceipt>, ICollectionReceiptRepository
    {
        private readonly ApplicationDbContext _db;

        public CollectionReceiptRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string stationCode, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(stationCode, cancellationToken);
            }

            return await GenerateCodeForUnDocumented(stationCode, cancellationToken);
        }

        private async Task<string> GenerateCodeForDocumented(string stationCode, CancellationToken cancellationToken = default)
        {
            var lastCv = await _db
                .MobilityCollectionReceipts
                .Where(c => c.StationCode == stationCode && c.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.CollectionReceiptNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv == null)
            {
                return "CR0000000001";
            }

            var lastSeries = lastCv.CollectionReceiptNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");

        }

        private async Task<string> GenerateCodeForUnDocumented(string stationCode, CancellationToken cancellationToken = default)
        {
            var lastCv = await _db
                .MobilityCollectionReceipts
                .Where(c => c.StationCode == stationCode && c.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.CollectionReceiptNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv == null)
            {
                return "CRU000000001";
            }

            var lastSeries = lastCv.CollectionReceiptNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");

        }

        public async Task<List<MobilityOffsettings>> GetOffsettings(string source, string reference, string stationCode, CancellationToken cancellationToken = default)
        {
            var result = await _db
                .MobilityOffsettings
                .Where(o => o.StationCode == stationCode && o.Source == source && o.Reference == reference)
                .ToListAsync(cancellationToken);

            return result;
        }

        public async Task UpdateSV(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var sv = await _db
                .MobilityServiceInvoices
                .FirstOrDefaultAsync(si => si.ServiceInvoiceId == id, cancellationToken);

            if (sv != null)
            {
                var total = paidAmount + offsetAmount;
                sv.AmountPaid += total;
                sv.Balance = (sv.Total - sv.Discount) - sv.AmountPaid;

                if (sv.Balance == 0 && sv.AmountPaid == (sv.Total - sv.Discount))
                {
                    sv.IsPaid = true;
                    sv.PaymentStatus = "Paid";
                }
                else if (sv.AmountPaid > (sv.Total - sv.Discount))
                {
                    sv.IsPaid = true;
                    sv.PaymentStatus = "OverPaid";
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task RemoveSVPayment(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var sv = await _db
                .MobilityServiceInvoices
                .FirstOrDefaultAsync(si => si.ServiceInvoiceId == id, cancellationToken);

            if (sv != null)
            {
                var total = paidAmount + offsetAmount;
                sv.AmountPaid -= total;
                sv.Balance += total;

                if (sv.IsPaid && sv.PaymentStatus == "Paid" || sv.IsPaid && sv.PaymentStatus == "OverPaid")
                {
                    sv.IsPaid = false;
                    sv.PaymentStatus = "Pending";
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public override async Task<IEnumerable<MobilityCollectionReceipt>> GetAllAsync(Expression<Func<MobilityCollectionReceipt, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MobilityCollectionReceipt> query = dbSet
                .Include(cr => cr.Customer)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv!.Service);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<MobilityCollectionReceipt?> GetAsync(Expression<Func<MobilityCollectionReceipt, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(cr => cr.Customer)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
