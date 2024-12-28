using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class CollectionReceiptRepository : Repository<FilprideCollectionReceipt>, ICollectionReceiptRepository
    {
        private ApplicationDbContext _db;

        public CollectionReceiptRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(company, cancellationToken);
            }
            else
            {
                return await GenerateCodeForUnDocumented(company, cancellationToken);
            }
        }

        public async Task<string> GenerateCodeForSIAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(company, cancellationToken);
            }
            else
            {
                return await GenerateCodeForUnDocumented(company, cancellationToken);
            }
        }

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken = default)
        {
            FilprideCollectionReceipt? lastCv = await _db
                .FilprideCollectionReceipts
                .Where(c => c.Company == company && c.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.CollectionReceiptNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CollectionReceiptNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "CR0000000001";
            }
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            FilprideCollectionReceipt? lastCv = await _db
                .FilprideCollectionReceipts
                .Where(c => c.Company == company && c.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.CollectionReceiptNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CollectionReceiptNo;
                string numericPart = lastSeries.Substring(3);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
            }
            else
            {
                return "CRU000000001";
            }
        }

        public async Task<List<FilprideOffsettings>> GetOffsettings(string source, string reference, string company, CancellationToken cancellationToken = default)
        {
            var result = await _db
                .FilprideOffsettings
                .Where(o => o.Company == company && o.Source == source && o.Reference == reference)
                .ToListAsync(cancellationToken);

            if (result != null)
            {
                return result;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task RemoveSIPayment(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var si = await _db
                .FilprideSalesInvoices
                .FirstOrDefaultAsync(si => si.SalesInvoiceId == id, cancellationToken);

            if (si != null)
            {
                var total = paidAmount + offsetAmount;
                si.AmountPaid -= total;
                si.Balance += total;

                if (si.IsPaid == true && si.PaymentStatus == "Paid" || si.IsPaid == true && si.PaymentStatus == "OverPaid")
                {
                    si.IsPaid = false;
                    si.PaymentStatus = "Pending";
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task RemoveSVPayment(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var sv = await _db
                .FilprideServiceInvoices
                .FirstOrDefaultAsync(si => si.ServiceInvoiceId == id, cancellationToken);

            if (sv != null)
            {
                var total = paidAmount + offsetAmount;
                sv.AmountPaid -= total;
                sv.Balance += total;

                if (sv.IsPaid == true && sv.PaymentStatus == "Paid" || sv.IsPaid == true && sv.PaymentStatus == "OverPaid")
                {
                    sv.IsPaid = false;
                    sv.PaymentStatus = "Pending";
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task RemoveMultipleSIPayment(int[] id, decimal[] paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var salesInvoices = await _db
                .FilprideSalesInvoices
                .Where(si => id.Contains(si.SalesInvoiceId))
                .ToListAsync(cancellationToken);

            if (salesInvoices != null)
            {
                for (int i = 0; i < paidAmount.Length; i++)
                {
                    var total = paidAmount[i] + offsetAmount;
                    salesInvoices[i].AmountPaid -= total;
                    salesInvoices[i].Balance += total;

                    if (salesInvoices[i].IsPaid == true && salesInvoices[i].PaymentStatus == "Paid" || salesInvoices[i].IsPaid == true && salesInvoices[i].PaymentStatus == "OverPaid")
                    {
                        salesInvoices[i].IsPaid = false;
                        salesInvoices[i].PaymentStatus = "Pending";
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task UpdateInvoice(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var si = await _db
                .FilprideSalesInvoices
                .FirstOrDefaultAsync(si => si.SalesInvoiceId == id, cancellationToken);

            if (si != null)
            {
                decimal netDiscount = si.Amount - si.Discount;

                var total = paidAmount + offsetAmount;
                si.AmountPaid += total;
                si.Balance = netDiscount - si.AmountPaid;

                if (si.Balance == 0 && si.AmountPaid == netDiscount)
                {
                    si.IsPaid = true;
                    si.PaymentStatus = "Paid";
                }
                else if (si.AmountPaid > netDiscount)
                {
                    si.IsPaid = true;
                    si.PaymentStatus = "OverPaid";
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task UpdateMutipleInvoice(string[] siNo, decimal[] paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            if (siNo != null)
            {
                var salesInvoice = new FilprideSalesInvoice();
                for (int i = 0; i < siNo.Length; i++)
                {
                    var siValue = siNo[i];
                    salesInvoice = await _db.FilprideSalesInvoices
                                .FirstOrDefaultAsync(p => p.SalesInvoiceNo == siValue);

                    var amountPaid = salesInvoice.AmountPaid + paidAmount[i] + offsetAmount;

                    if (!salesInvoice.IsPaid)
                    {
                        decimal netDiscount = salesInvoice.Amount - salesInvoice.Discount;

                        salesInvoice.AmountPaid += salesInvoice.Amount >= amountPaid ? paidAmount[i] + offsetAmount : paidAmount[i];

                        salesInvoice.Balance = netDiscount - salesInvoice.AmountPaid;

                        if (salesInvoice.Balance == 0 && salesInvoice.AmountPaid == netDiscount)
                        {
                            salesInvoice.IsPaid = true;
                            salesInvoice.PaymentStatus = "Paid";
                        }
                        else if (salesInvoice.AmountPaid > netDiscount)
                        {
                            salesInvoice.IsPaid = true;
                            salesInvoice.PaymentStatus = "OverPaid";
                        }
                    }
                    else
                    {
                        continue;
                    }
                    if (salesInvoice.Amount >= amountPaid)
                    {
                        offsetAmount = 0;
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task UpdateSV(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var sv = await _db
                .FilprideServiceInvoices
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

        public override async Task<IEnumerable<FilprideCollectionReceipt>> GetAllAsync(Expression<Func<FilprideCollectionReceipt, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideCollectionReceipt> query = dbSet
                .Include(cr => cr.Customer)
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s.Product)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv.Service);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<FilprideCollectionReceipt> GetAsync(Expression<Func<FilprideCollectionReceipt, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(cr => cr.Customer)
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s.Product)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
