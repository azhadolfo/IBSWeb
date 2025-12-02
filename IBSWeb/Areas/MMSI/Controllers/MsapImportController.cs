using System.Globalization;
using System.Security.Claims;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MMSI.MasterFile;
using IBS.Services;
using IBS.Services.Attributes;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz.Util;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class MsapImportController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BillingController> _logger;
        private readonly IUserAccessService _userAccessService;

        public MsapImportController(IUnitOfWork unitOfWork, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager,
            ILogger<BillingController> logger, IUserAccessService userAccessService)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _userAccessService = userAccessService;
            _logger = logger;
        }

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(List<string> fieldList)
        {
            try
            {
                var sb = new StringBuilder();

                foreach (string field in fieldList)
                {
                    string importResult = await ImportFromCSV(field);
                    sb.AppendLine(importResult);
                }

                TempData["success"] = sb.ToString().Replace(Environment.NewLine, "\\n");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<string> ImportFromCSV(string field, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var customerCSVPath = "C:\\Users\\MIS2\\Desktop\\Import files to IBS from MMSI\\dbs(raw)\\customerDB(nullables).csv";
            var portCSVPath = "C:\\Users\\MIS2\\Desktop\\Import files to IBS from MMSI\\dbs(raw)\\portDB.csv";

            try
            {
                switch (field)
                {
                    case "Customer":
                    {
                        var existingNames = (await _unitOfWork.FilprideCustomer.GetAllAsync(c => c.Company == "MMSI", cancellationToken))
                            .Select(c => c.CustomerName).ToList();

                        var customerList = new List<FilprideCustomer>();
                        using var reader = new StreamReader(customerCSVPath);
                        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                        var records = csv.GetRecords<dynamic>().ToList();

                        foreach (var record in records)
                        {
                            var customerName = record.name as string ?? "";

                            // check if already in the database
                            if (existingNames.Contains(customerName))
                            {
                                continue;
                            }

                            FilprideCustomer newCustomer = new FilprideCustomer();

                            #region -- Saving Values --

                            switch (record.terms)
                            {
                                case "7":
                                    newCustomer.CustomerTerms = "7D";
                                    break;
                                case "0":
                                    newCustomer.CustomerTerms = "COD";
                                    break;
                                case "15":
                                    newCustomer.CustomerTerms = "15D";
                                    break;
                                case "30":
                                    newCustomer.CustomerTerms = "30D";
                                    break;
                                case "60":
                                    newCustomer.CustomerTerms = "60D";
                                    break;
                            }

                            newCustomer.CustomerCode = await _unitOfWork.FilprideCustomer.GenerateCodeAsync("Industrial", cancellationToken);
                            newCustomer.CustomerName = customerName;
                            var addressConcatenated = $"{record.address1} {record.address2} {record.address3}";
                            newCustomer.CustomerAddress = addressConcatenated.IsNullOrWhiteSpace() ? "-" : addressConcatenated;
                            newCustomer.CustomerTin = record.tin as string ?? "000-000-000-00000";
                            newCustomer.BusinessStyle = record.business as string ?? null;
                            newCustomer.CustomerType = "Industrial";
                            newCustomer.WithHoldingVat = record.vatable == "T";
                            newCustomer.WithHoldingTax = false;
                            newCustomer.CreatedBy = $"Import: {GetUserFullName()}";
                            newCustomer.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                            newCustomer.VatType = record.vatable == "T" ? "Vatable" : "Zero-Rated";
                            newCustomer.IsActive = record.active == "T";
                            newCustomer.Company = await GetCompanyClaimAsync() ?? "MMSI";
                            newCustomer.ZipCode = "0000";
                            newCustomer.IsMMSI = true;
                            newCustomer.Type = "Documented";

                            #endregion -- Saving Values --

                            _dbContext.Add(newCustomer);
                            customerList.Add(newCustomer);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }

                        //await transaction.CommitAsync(cancellationToken);

                        return $"{field} imported successfully, {customerList.Count} new records";
                    }
                    case "Port":
                    {
                        var existingIdentifier = (await _dbContext.MMSIPorts.ToListAsync(cancellationToken))
                            .Select(c => c.PortName).ToList();

                        var newRecords = new List<MMSIPort>();
                        using var reader = new StreamReader(portCSVPath);
                        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                        var records = csv.GetRecords<dynamic>().ToList();

                        foreach (var record in records)
                        {
                            string original = record.port_number ?? "";
                            string padded = int.Parse(original).ToString("D3");

                            // check if already in the database
                            if (existingIdentifier.Contains(padded))
                            {
                                continue;
                            }

                            MMSIPort newRecord = new MMSIPort();

                            newRecord.PortNumber = padded;
                            newRecord.PortName = record.port_name;
                            newRecord.HasSBMA = record.has_sbma;

                            newRecords.Add(newRecord);
                            _dbContext.Add(newRecord);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }

                        //await transaction.CommitAsync(cancellationToken);
                        return $"{field} imported successfully, {newRecords.Count} new records";
                    }
                }

                return $"{field} field is invalid";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException(ex.Message);
            }
        }
    }
}
