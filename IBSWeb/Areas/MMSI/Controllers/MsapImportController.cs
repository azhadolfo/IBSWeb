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
            var customerCSVPath = "C:\\Users\\MIS2\\Desktop\\Import files to IBS from MMSI\\dbs(raw)\\customerDB(nullables).csv";
            var portCSVPath = "C:\\Users\\MIS2\\Desktop\\Import files to IBS from MMSI\\dbs(raw)\\portDB.csv";
            var principalCSVPath = "C:\\Users\\MIS2\\Desktop\\Import files to IBS from MMSI\\dbs(raw)\\principalDB(nullables)v2.csv";
            var serviceCSVPath = "C:\\Users\\MIS2\\Desktop\\Import files to IBS from MMSI\\dbs(raw)\\servicesDB.csv";
            var tugboatCSVPath = "C:\\Users\\MIS2\\Desktop\\Import files to IBS from MMSI\\dbs(raw)\\tugboatDB.csv";
            var tugboatOwnerCSVPath = "C:\\Users\\MIS2\\Desktop\\Import files to IBS from MMSI\\dbs(raw)\\tugboatOwnerDBv2.csv";
            var tugMasterCSVPath = "C:\\Users\\MIS2\\Desktop\\Import files to IBS from MMSI\\dbs(raw)\\tugMasterDBv2.csv";
            var vesselCSVPath = "C:\\Users\\MIS2\\Desktop\\Import files to IBS from MMSI\\dbs(raw)\\vesselDB.csv";

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

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
                            .Select(c => c.PortNumber).ToList();

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
                            newRecord.HasSBMA = record.has_sbma == "T";

                            newRecords.Add(newRecord);
                            _dbContext.Add(newRecord);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }

                        //await transaction.CommitAsync(cancellationToken);
                        return $"{field} imported successfully, {newRecords.Count} new records";
                    }
                    case "Principal":
                    {
                        var existingIdentifier = (await _dbContext.MMSIPrincipals.ToListAsync(cancellationToken))
                            .Select(c => new { c.PrincipalNumber, c.PrincipalName}).ToList();

                        var mmsiCustomers = await _unitOfWork.FilprideCustomer
                            .GetAllAsync(c => c.Company == "MMSI", cancellationToken);

                        var newRecords = new List<MMSIPrincipal>();
                        using var reader = new StreamReader(principalCSVPath);
                        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                        var records = csv.GetRecords<dynamic>().ToList();

                        using var reader2 = new StreamReader(customerCSVPath);
                        using var csv2 = new CsvReader(reader2, CultureInfo.InvariantCulture);
                        var customers = csv2.GetRecords<dynamic>().ToList();

                        var customersList = customers
                            .Select(c => new
                            {
                                CustomerId = mmsiCustomers.FirstOrDefault(cu => cu.CustomerName == c.name)!.CustomerId,
                                CustomerNumber = c.number,
                                CustomerName = c.name
                            })
                            .ToList();

                        foreach (var record in records)
                        {
                            string original = record.number ?? "";
                            var padded = int.Parse(original).ToString("D4");
                            var identity = new
                            {
                                PrincipalNumber = padded,
                                PrincipalName = record.name as string ?? "",
                            };

                            // check if already in the database
                            if (existingIdentifier.Contains(identity))
                            {
                                continue;
                            }

                            var paddedCustomerNumber = int.Parse(record.agent).ToString("D4");

                            MMSIPrincipal newRecord = new MMSIPrincipal();
                            var agent = customersList.FirstOrDefault(c => c.CustomerNumber == paddedCustomerNumber);

                            if (agent == null)
                            {
                                throw new NullReferenceException("Agent not found");
                            }

                            switch (record.terms)
                            {
                                case "7":
                                    newRecord.Terms = "7D";
                                    break;
                                case "0":
                                    newRecord.Terms = "COD";
                                    break;
                                case "15":
                                    newRecord.Terms = "15D";
                                    break;
                                case "30":
                                    newRecord.Terms = "30D";
                                    break;
                                case "60":
                                    newRecord.Terms = "60D";
                                    break;
                            }

                            newRecord.CustomerId = agent.CustomerId;
                            newRecord.PrincipalNumber = padded;
                            newRecord.PrincipalName = record.name;
                            var addressConcatenated = $"{record.address1} {record.address2} {record.address3}";
                            newRecord.Address = addressConcatenated.IsNullOrWhiteSpace() ? "-" : addressConcatenated;
                            newRecord.TIN = record.tin as string ?? "000-000-000000";
                            newRecord.BusinessType = record.business;
                            newRecord.Landline1 = record.landline1;
                            newRecord.Landline2 = record.landline2;
                            newRecord.Mobile1 = record.mobile1;
                            newRecord.Mobile2 = record.mobile2;
                            newRecord.IsVatable = record.vatable == "T";
                            newRecord.IsActive = record.active == "T";

                            newRecords.Add(newRecord);
                            _dbContext.Add(newRecord);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }

                        //await transaction.CommitAsync(cancellationToken);
                        return $"{field} imported successfully, {newRecords.Count} new records";
                    }
                    case "Service":
                    {
                        var existingIdentifier = (await _dbContext.MMSIServices.ToListAsync(cancellationToken))
                            .Select(c => c.ServiceNumber).ToList();

                        var newRecords = new List<MMSIService>();
                        using var reader = new StreamReader(serviceCSVPath);
                        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                        var records = csv.GetRecords<dynamic>().ToList();

                        foreach (var record in records)
                        {
                            string originalNumber = record.number ?? "";
                            var padded = int.Parse(originalNumber).ToString("D3");

                            // check if already in the database
                            if (existingIdentifier.Contains(padded))
                            {
                                continue;
                            }

                            MMSIService newRecord = new MMSIService();

                            newRecord.ServiceNumber = padded;
                            newRecord.ServiceName = record.name;

                            newRecords.Add(newRecord);
                            _dbContext.Add(newRecord);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }

                        //await transaction.CommitAsync(cancellationToken);
                        return $"{field} imported successfully, {newRecords.Count} new records";
                    }
                    case "Tugboat":
                    {
                        var existingIdentifier = (await _dbContext.MMSITugboats.ToListAsync(cancellationToken))
                            .Select(c => c.TugboatNumber).ToList();
                        var existingTugboatOwners = (await _dbContext.MMSITugboatOwners.ToListAsync(cancellationToken))
                            .Select(c => new { c.TugboatOwnerId, c.TugboatOwnerNumber }).ToList();

                        var newRecords = new List<MMSITugboat>();
                        using var reader = new StreamReader(tugboatCSVPath);
                        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                        var records = csv.GetRecords<dynamic>().ToList();

                        foreach (var record in records)
                        {
                            string originalNumber = record.number ?? "";
                            var padded = int.Parse(originalNumber).ToString("D3");

                            var paddedOwnerNumber = int.Parse(record.owner ?? "").ToString("D3");
                            var owner = existingTugboatOwners.FirstOrDefault(t => t.TugboatOwnerNumber == paddedOwnerNumber);

                            if (existingIdentifier.Contains(padded))
                            {
                                continue;
                            }

                            MMSITugboat newRecord = new MMSITugboat();

                            if (owner != null)
                            {
                                newRecord.TugboatOwnerId = owner.TugboatOwnerId;
                            }

                            newRecord.TugboatNumber = padded;
                            newRecord.TugboatName = record.name;
                            newRecord.IsCompanyOwned = record.companyowner == "T";

                            newRecords.Add(newRecord);
                            _dbContext.Add(newRecord);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }

                        //await transaction.CommitAsync(cancellationToken);
                        return $"{field} imported successfully, {newRecords.Count} new records";
                    }
                    case "TugboatOwner":
                    {
                        var existingIdentifier = (await _dbContext.MMSITugboatOwners.ToListAsync(cancellationToken))
                            .Select(c => c.TugboatOwnerNumber).ToList();

                        var newRecords = new List<MMSITugboatOwner>();
                        using var reader = new StreamReader(tugboatOwnerCSVPath);
                        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                        var records = csv.GetRecords<dynamic>().ToList();

                        foreach (var record in records)
                        {
                            string originalNumber = record.number ?? "";
                            var padded = int.Parse(originalNumber).ToString("D3");

                            if (existingIdentifier.Contains(padded))
                            {
                                continue;
                            }

                            MMSITugboatOwner newRecord = new MMSITugboatOwner();

                            newRecord.TugboatOwnerNumber = padded;
                            newRecord.TugboatOwnerName = record.name;

                            newRecords.Add(newRecord);
                            _dbContext.Add(newRecord);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }

                        //await transaction.CommitAsync(cancellationToken);
                        return $"{field} imported successfully, {newRecords.Count} new records";
                    }
                    case "TugMaster":
                    {
                        var existingIdentifier = (await _dbContext.MMSITugMasters.ToListAsync(cancellationToken))
                            .Select(c => c.TugMasterNumber).ToList();

                        var newRecords = new List<MMSITugMaster>();
                        using var reader = new StreamReader(tugMasterCSVPath);
                        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                        var records = csv.GetRecords<dynamic>().ToList();

                        foreach (var record in records)
                        {
                            if (existingIdentifier.Contains(record.number))
                            {
                                continue;
                            }

                            MMSITugMaster newRecord = new MMSITugMaster();

                            newRecord.TugMasterNumber = record.number;
                            newRecord.TugMasterName = record.name;
                            newRecord.IsActive = record.active == "T";

                            newRecords.Add(newRecord);
                            _dbContext.Add(newRecord);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }

                        //await transaction.CommitAsync(cancellationToken);
                        return $"{field} imported successfully, {newRecords.Count} new records";
                    }
                    case "Vessel":
                    {
                        var existingIdentifier = (await _dbContext.MMSIVessels.ToListAsync(cancellationToken))
                            .Select(c => c.VesselNumber).ToList();

                        var newRecords = new List<MMSIVessel>();
                        using var reader = new StreamReader(vesselCSVPath);
                        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                        var records = csv.GetRecords<dynamic>().ToList();

                        foreach (var record in records)
                        {
                            string originalNumber = record.number ?? "";
                            var padded = int.Parse(originalNumber).ToString("D4");

                            if (existingIdentifier.Contains(padded))
                            {
                                continue;
                            }

                            MMSIVessel newRecord = new MMSIVessel();

                            newRecord.VesselNumber = padded;
                            newRecord.VesselName = record.name;
                            newRecord.VesselType = record.type == "L" ? "LOCAL" : "FOREIGN";

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
