using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsReceivable;
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

        public async Task<string> GenerateCodeAsync(string company, CancellationToken cancellationToken = default)
        {
            FilprideCollectionReceipt? lastCv = await _db
                .FilprideCollectionReceipts
                .Where(c => c.Company == company)
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

        public async Task<int> RemoveSIPayment(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var si = await _db
                .FilprideSalesInvoices
                .FirstOrDefaultAsync(si => si.SalesInvoiceId == id, cancellationToken);

            if (si != null)
            {
                var total = paidAmount + offsetAmount;
                si.AmountPaid -= total;
                si.Balance += total;

                if (si.IsPaid == true && si.Status == "Paid" || si.IsPaid == true && si.Status == "OverPaid")
                {
                    si.IsPaid = false;
                    si.Status = "Pending";
                }

                return await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }

        public async Task<int> RemoveSVPayment(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var sv = await _db
                .FilprideServiceInvoices
                .FirstOrDefaultAsync(si => si.ServiceInvoiceId == id, cancellationToken);

            if (sv != null)
            {
                var total = paidAmount + offsetAmount;
                sv.AmountPaid -= total;
                sv.Balance += total;

                if (sv.IsPaid == true && sv.Status == "Paid" || sv.IsPaid == true && sv.Status == "OverPaid")
                {
                    sv.IsPaid = false;
                    sv.Status = "Pending";
                }

                return await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }

        public async Task<int> RemoveMultipleSIPayment(int[] id, decimal[] paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
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

                    if (salesInvoices[i].IsPaid == true && salesInvoices[i].Status == "Paid" || salesInvoices[i].IsPaid == true && salesInvoices[i].Status == "OverPaid")
                    {
                        salesInvoices[i].IsPaid = false;
                        salesInvoices[i].Status = "Pending";
                    }
                }

                return await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }

        public async Task<int> UpdateInvoice(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var si = await _db
                .FilprideSalesInvoices
                .FirstOrDefaultAsync(si => si.SalesInvoiceId == id, cancellationToken);

            if (si != null)
            {
                var total = paidAmount + offsetAmount;
                si.AmountPaid += total;
                si.Balance = si.NetDiscount - si.AmountPaid;

                if (si.Balance == 0 && si.AmountPaid == si.NetDiscount)
                {
                    si.IsPaid = true;
                    si.Status = "Paid";
                }
                else if (si.AmountPaid > si.NetDiscount)
                {
                    si.IsPaid = true;
                    si.Status = "OverPaid";
                }

                return await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }

        public async Task<int> UpdateMutipleInvoice(string[] siNo, decimal[] paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
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
                        salesInvoice.AmountPaid += salesInvoice.Amount >= amountPaid ? paidAmount[i] + offsetAmount : paidAmount[i];

                        salesInvoice.Balance = salesInvoice.NetDiscount - salesInvoice.AmountPaid;

                        if (salesInvoice.Balance == 0 && salesInvoice.AmountPaid == salesInvoice.NetDiscount)
                        {
                            salesInvoice.IsPaid = true;
                            salesInvoice.Status = "Paid";
                        }
                        else if (salesInvoice.AmountPaid > salesInvoice.NetDiscount)
                        {
                            salesInvoice.IsPaid = true;
                            salesInvoice.Status = "OverPaid";
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

                return await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }

        public async Task<int> UpdateSV(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
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
                    sv.Status = "Paid";
                }
                else if (sv.AmountPaid > (sv.Total - sv.Discount))
                {
                    sv.IsPaid = true;
                    sv.Status = "OverPaid";
                }

                return await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
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