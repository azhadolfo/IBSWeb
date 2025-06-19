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

        public async Task<IActionResult> ExportFilprideCollectionToCsv(CancellationToken cancellationToken = default)
        {
            try
            {
                // Get all collections
                var collections = await _unitOfWork.FilprideCollectionReceipt.GetAllAsync(null, cancellationToken);

                // Map to DTO
                var collectionDtos = collections.Select(c => new ExportCsvDto.FilprideCollectionReceiptDto
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

                // Configure CsvHelper
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                };

                // Create CSV in memory
                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
                using var csv = new CsvWriter(writer, config);

                // Write the DTO collection to CSV
                csv.WriteRecords(collectionDtos);
                await writer.FlushAsync(); // Ensure all data is written
                memoryStream.Position = 0; // Reset stream position

                // Return the CSV file for download
                return File(memoryStream.ToArray(), "text/csv", "COLLECTION.csv");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ExportFilprideDisbursementToCsv(CancellationToken cancellationToken = default)
        {
            try
            {
                // Get all collections
                var checkVoucherHeaders = await _unitOfWork.FilprideCheckVoucher.GetAllAsync(null, cancellationToken);

                // Map to DTO
                var checkVoucherDtos = checkVoucherHeaders.Select(cv => new ExportCsvDto.FilprideCheckVoucherDto
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

                // Configure CsvHelper
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                };

                // Create CSV in memory
                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
                using var csv = new CsvWriter(writer, config);

                // Write the DTO collection to CSV
                csv.WriteRecords(checkVoucherDtos);
                await writer.FlushAsync(); // Ensure all data is written
                memoryStream.Position = 0; // Reset stream position

                // Return the CSV file for download
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
