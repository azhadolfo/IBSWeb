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
using IBS.Services;
using IBS.Utility.Helpers;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class ExportCsvController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGoogleDriveService _googleDriveService;
        private string dcrCsvFolderId = "1pc5pAZsTNpNHAZhPecbwpm0QtPfdCPB-";

        public ExportCsvController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork, IGoogleDriveService googleDriveService)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _googleDriveService = googleDriveService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ExportFilprideCollectionReceiptCsvToGDrive(CancellationToken cancellationToken = default)
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

                // Uploading
                string fileName = "COLLECTION.csv";
                var fileId = await _googleDriveService.UploadFileAsync(memoryStream, fileName, dcrCsvFolderId, "text/csv");
                TempData["success"] = $"{fileName} uploaded to Google Drive.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ExportFilprideCheckVoucherHeaderToGDrive(CancellationToken cancellationToken = default)
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

                // Uploading
                string fileName = "DISBURSEMENT.csv";
                var fileId = await _googleDriveService.UploadFileAsync(memoryStream, fileName, dcrCsvFolderId, "text/csv");
                TempData["success"] = $"{fileName} uploaded to Google Drive.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ExportFilprideCheckVoucherDetailsToGDrive(CancellationToken cancellationToken = default)
        {
            try
            {
                var cvHeaders = await _unitOfWork.FilprideCheckVoucher.GetAllAsync(null, cancellationToken);

                foreach (var cvHeader in cvHeaders)
                {
                    cvHeader.Details = await _dbContext.FilprideCheckVoucherDetails
                        .Where(cvd => cvd.CheckVoucherHeaderId == cvHeader.CheckVoucherHeaderId)
                        .Include(cvd => cvd.Employee)
                        .Include(cvd => cvd.Company)
                        .Include(cvd => cvd.BankAccount)
                        .Include(cvd => cvd.Customer)
                        .ToListAsync(cancellationToken);
                }

                var checkVoucherHeaderDtosList = new List<ExportCsvDto.FilprideCheckVoucherDetailsCsvForDcrDto>();

                foreach (var cvHeader in cvHeaders)
                {
                    if (cvHeader.Details != null)
                    {
                        var tempCvd = cvHeader.Details.Select(cvd => new ExportCsvDto.FilprideCheckVoucherDetailsCsvForDcrDto
                        {
                            ACCTCD = cvd.AccountNo,
                            ACCTNAME = cvd.AccountName,
                            CVNO = cvd.TransactionNo,
                            DEBIT = cvd.Debit,
                            CREDIT = cvd.Credit,
                            CUSTOMER_NAME = cvd.Customer?.CustomerName ?? string.Empty,
                            BANK = cvd.BankAccount?.AccountNo ?? string.Empty,
                            EMPLOYEE_NAME = $"{cvd.Employee?.FirstName ?? string.Empty} {cvd.Employee?.LastName ?? string.Empty}",
                            COMPANY_NAME = cvd.Company?.CompanyName ?? string.Empty
                        }).ToList();

                        checkVoucherHeaderDtosList.AddRange(tempCvd);
                    }
                }

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

                // Uploading
                string fileName = "CV_DETAILS.csv";
                var fileId = await _googleDriveService.UploadFileAsync(memoryStream, fileName, dcrCsvFolderId, "text/csv");
                TempData["success"] = $"{fileName} uploaded to Google Drive.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ExportFilprideCollectionDetailsToGDrive(CancellationToken cancellationToken = default)
        {
            try
            {
                var collectionHeaders = await _unitOfWork.FilprideCollectionReceipt.GetAllAsync(null, cancellationToken);

                foreach (var collectionHeader in collectionHeaders)
                {
                    var collectionDetails = await _dbContext.FilprideGeneralLedgerBooks
                        .Where(gl => gl.Reference == collectionHeader.CollectionReceiptNo)
                        .ToListAsync(cancellationToken);

                    if (collectionDetails.Count != 0)
                    {
                        collectionHeader.Details = collectionDetails;

                        foreach (var gl in collectionHeader.Details)
                        {
                            if (gl.CustomerId.HasValue)
                            {
                                gl.Customer = await _unitOfWork.FilprideCustomer
                                    .GetAsync(c => c.CustomerId == gl.CustomerId, cancellationToken);
                            }
                        }
                    }
                }

                var collectionDetailsDtosList = new List<ExportCsvDto.FilprideCollectionDetailsCsvForDcrDto>();

                foreach (var collectionHeader in collectionHeaders)
                {
                    if (collectionHeader.Details != null)
                    {
                        var tempGl = collectionHeader.Details.Select(gl => new ExportCsvDto.FilprideCollectionDetailsCsvForDcrDto
                        {
                            ACCTCD = gl?.AccountNo ?? string.Empty,
                            ACCTNAME = gl?.AccountTitle ?? string.Empty,
                            CRNO = gl?.Reference ?? string.Empty,
                            DEBIT = gl?.Debit ?? 0,
                            CREDIT = gl?.Credit ?? 0,
                            CUSTOMER_NAME = gl?.Customer?.CustomerName ?? string.Empty
                        }).ToList();

                        collectionDetailsDtosList.AddRange(tempGl);
                    }
                }

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                };

                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
                using var csv = new CsvWriter(writer, config);
                csv.WriteRecords(collectionDetailsDtosList);
                await writer.FlushAsync();
                memoryStream.Position = 0;

                // Uploading
                string fileName = "CR_DETAILS.csv";
                var fileId = await _googleDriveService.UploadFileAsync(memoryStream, fileName, dcrCsvFolderId, "text/csv");
                TempData["success"] = $"{fileName} uploaded to Google Drive.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
