using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using IBS.DTOs;
using IBS.Services;
using Microsoft.IdentityModel.Tokens;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class ExportToDCRController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly string _dcrCsvFolderId = "1pc5pAZsTNpNHAZhPecbwpm0QtPfdCPB-";

        public ExportToDCRController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork, IGoogleDriveService googleDriveService)
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

                var collectionReceiptDtosList = collectionReceipts.Select(c => new FilprideCollectionReceiptCsvForDcrDto
                {
                    DATE = c.TransactionDate.ToString("MM/dd/yyyy"),
                    PAYEE = c.Customer!.CustomerName,
                    CVNO = c.CollectionReceiptNo ?? string.Empty,
                    CHECKNO = c.CheckNo ?? string.Empty,
                    PARTICULARS = c.Remarks ?? string.Empty,
                    AMOUNT = c.CashAmount + c.CheckAmount + c.EWT + c.WVAT,
                    ACCOUNTNO = c.BankAccount?.AccountNo ?? string.Empty,
                    CHECKDATE = c.CheckDate?.ToString("MM/dd/yyy") ?? string.Empty,
                    ISORCANCEL = c.CanceledBy != null || c.Status == nameof(Status.Voided),
                    DATEDEPOSITED = c.TransactionDate.ToString("MM/dd/yyyy")
                }).ToList();

                var fileName = "COLLECTION.csv";
                await UploadDcrFile(fileName, collectionReceiptDtosList, cancellationToken);
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

                var checkVoucherHeaderDtosList = checkVoucherHeaders.Select(cv => new FilprideCheckVoucherHeaderCsvForDcrDto
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

                string fileName = "DISBURSEMENT.csv";
                await UploadDcrFile(fileName, checkVoucherHeaderDtosList, cancellationToken);
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

                var checkVoucherHeaderDtosList = new List<FilprideCheckVoucherDetailsCsvForDcrDto>();

                foreach (var cvHeader in cvHeaders)
                {
                    if (cvHeader.Details != null)
                    {
                        var tempCvd = cvHeader.Details.Select(cvd => new FilprideCheckVoucherDetailsCsvForDcrDto
                        {
                            ACCTCD = cvd.AccountNo,
                            ACCTNAME = cvd.AccountName,
                            CVNO = cvd.TransactionNo,
                            DEBIT = cvd.Debit,
                            CREDIT = cvd.Credit,
                            CUSTOMER_NAME = cvd.Customer?.CustomerName ?? string.Empty,
                            BANK = (cvd.BankAccount?.AccountName).IsNullOrEmpty() ? string.Empty : $"{cvd.BankAccount?.AccountName} - {cvd.BankAccount?.AccountNo}",
                            EMPLOYEE_NAME = $"{cvd.Employee?.FirstName ?? string.Empty} {cvd.Employee?.LastName ?? string.Empty}",
                            COMPANY_NAME = cvd?.Company?.CompanyName ?? string.Empty
                        }).ToList();

                        checkVoucherHeaderDtosList.AddRange(tempCvd);
                    }
                }

                var fileName = "CV_DETAILS.csv";
                await UploadDcrFile(fileName, checkVoucherHeaderDtosList, cancellationToken);
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

                var collectionDetailsDtosList = new List<FilprideCollectionDetailsCsvForDcrDto>();

                foreach (var collectionHeader in collectionHeaders)
                {
                    if (collectionHeader.Details != null)
                    {
                        var tempGl = collectionHeader.Details.Select(gl => new FilprideCollectionDetailsCsvForDcrDto
                        {
                            ACCTCD = gl?.AccountNo ?? string.Empty,
                            ACCTNAME = gl?.AccountTitle ?? string.Empty,
                            CRNO = gl?.Reference ?? string.Empty,
                            DEBIT = gl?.Debit ?? 0,
                            CREDIT = gl?.Credit ?? 0,
                            CUSTOMER_NAME = gl?.Customer?.CustomerName ?? string.Empty,
                            BANK = (gl?.BankAccount?.AccountName).IsNullOrEmpty() ? string.Empty : $"{gl?.BankAccount?.AccountName} - {gl?.BankAccount?.AccountNo}"
                        }).ToList();

                        collectionDetailsDtosList.AddRange(tempGl);
                    }
                }

                string fileName = "CR_DETAILS.csv";
                await UploadDcrFile(fileName, collectionDetailsDtosList, cancellationToken);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        private async Task UploadDcrFile<T>(string fileName, IEnumerable<T> entities, CancellationToken cancellationToken)
            where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    throw new ArgumentException("File name cannot be empty.", nameof(fileName));
                }
                if (entities == null)
                {
                    throw new ArgumentException("Entities cannot be null.", nameof(entities));
                }

                var existingFile = await _googleDriveService.GetFileByNameAsync(fileName, _dcrCsvFolderId);

                if (existingFile.DoesExist)
                {
                    await _googleDriveService.DeleteFileAsync(existingFile.FileId);
                }

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                };

                using var memoryStream = new MemoryStream();
                await using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
                await using var csv = new CsvWriter(writer, config);
                await csv.WriteRecordsAsync(entities, cancellationToken);
                await writer.FlushAsync(cancellationToken);
                memoryStream.Position = 0;
                await _googleDriveService.UploadFileAsync(memoryStream, fileName, _dcrCsvFolderId, "text/csv");
                TempData["success"] = $"{fileName} uploaded to Google Drive.";
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
