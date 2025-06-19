using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using IBS.DTOs;
using IBS.Utility.Helpers;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class ExportCsvController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;

        public ExportCsvController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ExportFilprideCollectionReceiptCsvForDcr(CancellationToken cancellationToken = default)
        {
            try
            {
                var collectionReceipts = await _unitOfWork.FilprideCollectionReceipt.GetAllAsync(null, cancellationToken);

                var collectionReceiptDtosList = collectionReceipts.Select(c => new ExportCsvDto.FilprideCollectionReceiptCsvForDcrDto
                {
                    DATE = c.TransactionDate.ToString("MM/dd/yyyy"),
                    PAYEE = c.Customer!.CustomerName,
                    CVNO = c.CollectionReceiptNo ?? string.Empty,
                    CHECKNO = c.CheckNo ?? string.Empty,
                    PARTICULARS = c.Remarks ?? string.Empty,
                    AMOUNT = (c.CashAmount + c.CheckAmount + c.EWT + c.WVAT),
                    ACCOUNTNO = c.BankAccount?.AccountNo ?? string.Empty,
                    CHECKDATE = c.CheckDate?.ToString("MM/dd/yyy") ?? string.Empty,
                    ISORCANCEL = ((c.CanceledBy != null || (c.Status == nameof(Status.Voided)))),
                    DATEDEPOSITED = c.TransactionDate.ToString("MM/dd/yyyy")
                }).ToList();

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                };

                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
                using var csv = new CsvWriter(writer, config);
                csv.WriteRecords(collectionReceiptDtosList);
                await writer.FlushAsync();
                memoryStream.Position = 0;
                return File(memoryStream.ToArray(), "text/csv", "COLLECTION.csv");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ExportFilprideCheckVoucherHeaderToCsvForDcr(CancellationToken cancellationToken = default)
        {
            try
            {
                var checkVoucherHeaders = await _unitOfWork.FilprideCheckVoucher.GetAllAsync(null, cancellationToken);

                var checkVoucherHeaderDtosList = checkVoucherHeaders.Select(cv => new ExportCsvDto.FilprideCheckVoucherHeaderCsvForDcrDto
                {
                    VOUCHER_NO = cv.CheckVoucherHeaderNo ?? string.Empty,
                    VCH_DATE = cv.Date.ToString("MM/dd/yyyy"),
                    PAYEE = cv.Supplier!.SupplierName,
                    AMOUNT = cv.Total,
                    PARTICULARS = cv.Particulars ?? string.Empty,
                    CHECKNO = cv.CheckNo ?? string.Empty,
                    CHKDATE = cv.CheckDate?.ToString("MM/dd/yyyy") ?? string.Empty,
                    ACCOUNTNO = cv.BankAccount?.AccountNo ?? string.Empty,
                    CASHPODATE = cv.DcpDate?.ToString("MM/dd/yyyy") ?? string.Empty,
                    DCRDATE = cv.DcrDate?.ToString("MM/dd/yyyy") ?? string.Empty,
                    ISCANCELLED = ((cv.CanceledBy != null || (cv.Status == nameof(Status.Voided)))),
                }).ToList();

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                };

                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
                using var csv = new CsvWriter(writer, config);
                csv.WriteRecords(checkVoucherHeaderDtosList);
                await writer.FlushAsync();
                memoryStream.Position = 0;
                return File(memoryStream.ToArray(), "text/csv", "DISBURSEMENT.csv");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
