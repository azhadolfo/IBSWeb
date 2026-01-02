using System.Globalization;
using System.Security.Claims;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MMSI;
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
            var customerCSVPath = "C:\\csv\\customer.CSV";
            var portCSVPath = "C:\\MSAP_To_IBS_Import\\dbs(raw)\\portDB.csv";
            var terminalCSVPath = "C:\\csv\\terminal.csv";
            var principalCSVPath = "C:\\MSAP_To_IBS_Import\\dbs(raw)\\principalDB(nullables)v2.csv";
            var serviceCSVPath = "C:\\MSAP_To_IBS_Import\\dbs(raw)\\servicesDB.csv";
            var tugboatCSVPath = "C:\\MSAP_To_IBS_Import\\dbs(raw)\\tugboatDB.csv";
            var tugboatOwnerCSVPath = "C:\\MSAP_To_IBS_Import\\dbs(raw)\\tugboatOwnerDBv2.csv";
            var tugMasterCSVPath = "C:\\MSAP_To_IBS_Import\\dbs(raw)\\tugMasterDBv2.csv";
            var vesselCSVPath = "C:\\MSAP_To_IBS_Import\\dbs(raw)\\vesselDB.csv";

            var dispatchTicketCSVPath = "C:\\csv\\dispatchTest.CSV";
            var billingCSVPath = "C:\\csv\\billingTest.CSV";
            var collectionCSVPath = "C:\\csv\\collection.CSV";

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                switch (field)
                {
                    #region -- Masterfiles --

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
                            var customerName = record.name as string ?? string.Empty;

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
                                default:
                                    newCustomer.CustomerTerms = "COD";
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

                            customerList.Add(newCustomer);
                        }

                        await _dbContext.FilprideCustomers.AddRangeAsync(customerList, cancellationToken);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
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
                            string original = record.port_number ?? string.Empty;
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
                        }

                        await _dbContext.MMSIPorts.AddRangeAsync(newRecords, cancellationToken);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        return $"{field} imported successfully, {newRecords.Count} new records";
                    }
                    case "Terminal":
                    {
                        var existingIdentifier = (await _dbContext.MMSITerminals.Include(t => t.Port).ToListAsync(cancellationToken))
                            .Select(c => new { c.Port!.PortNumber, c.TerminalNumber}).ToList();

                        var existingPorts = (await _dbContext.MMSIPorts.ToListAsync(cancellationToken))
                            .Select(p => new { p.PortId, p.PortNumber}).ToList();

                        var newRecords = new List<MMSITerminal>();
                        using var reader = new StreamReader(terminalCSVPath);
                        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                        var records = csv.GetRecords<dynamic>().ToList();

                        foreach (var record in records)
                        {
                            string originalPortNumber = (record.number as string)!.Substring(0, 3);
                            string paddedPortNumber = int.Parse(originalPortNumber).ToString("D3");
                            string originalTerminalNumber = (record.number as string)!.Substring((record.number as string)!.Length - 3, 3);
                            string paddedTerminalNumber = int.Parse(originalTerminalNumber).ToString("D3");

                            // check if already in the database
                            if (existingIdentifier.Contains(new { PortNumber = paddedPortNumber, TerminalNumber = paddedTerminalNumber }!))
                            {
                                continue;
                            }

                            MMSITerminal newRecord = new MMSITerminal();

                            newRecord.PortId = existingPorts.FirstOrDefault(p => p.PortNumber == paddedPortNumber)!.PortId;
                            newRecord.TerminalName = record.name;
                            newRecord.TerminalNumber = paddedTerminalNumber;

                            newRecords.Add(newRecord);
                        }

                        await _dbContext.MMSITerminals.AddRangeAsync(newRecords, cancellationToken);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
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
                            string original = record.number ?? string.Empty;
                            var padded = int.Parse(original).ToString("D4");
                            var identity = new
                            {
                                PrincipalNumber = padded,
                                PrincipalName = record.name as string ?? string.Empty,
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
                        }

                        await _dbContext.MMSIPrincipals.AddRangeAsync(newRecords, cancellationToken);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
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
                            string originalNumber = record.number ?? string.Empty;
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
                        }

                        await _dbContext.MMSIServices.AddRangeAsync(newRecords, cancellationToken);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
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
                            string originalNumber = record.number ?? string.Empty;
                            var padded = int.Parse(originalNumber).ToString("D3");

                            var paddedOwnerNumber = int.Parse(record.owner ?? string.Empty).ToString("D3");
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
                        }

                        await _dbContext.MMSITugboats.AddRangeAsync(newRecords, cancellationToken);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
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
                            string originalNumber = record.number ?? string.Empty;
                            var padded = int.Parse(originalNumber).ToString("D3");

                            if (existingIdentifier.Contains(padded))
                            {
                                continue;
                            }

                            MMSITugboatOwner newRecord = new MMSITugboatOwner();

                            newRecord.TugboatOwnerNumber = padded;
                            newRecord.TugboatOwnerName = record.name;

                            newRecords.Add(newRecord);
                        }

                        await _dbContext.MMSITugboatOwners.AddRangeAsync(newRecords, cancellationToken);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
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
                        }

                        await _dbContext.MMSITugMasters.AddRangeAsync(newRecords, cancellationToken);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
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
                            string originalNumber = record.number ?? string.Empty;
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
                        }

                        await _dbContext.MMSIVessels.AddRangeAsync(newRecords, cancellationToken);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        return $"{field} imported successfully, {newRecords.Count} new records";
                    }

                    #endregion -- Masterfiles --

                    #region -- Data entries --

                    case "DispatchTicket":
                    {
                        using var reader0 = new StreamReader(customerCSVPath);
                        using var csv0 = new CsvReader(reader0, CultureInfo.InvariantCulture);

                        var msapCustomerRecords = csv0.GetRecords<dynamic>().Select(c => new
                        {
                            c.number,
                            c.name,
                            address = c.address1 == string.Empty ? "-" : $"{c.address1} {c.address2} {c.address3}"
                        }).ToList();

                        #region -- Creating Identifier Variables --

                        var existingIdentifier = await _dbContext.MMSIDispatchTickets
                            .AsNoTracking()
                            .Select(dt => new { dt.DispatchNumber, dt.CreatedDate })
                            .ToListAsync(cancellationToken);

                        var existingVessels = await _dbContext.MMSIVessels
                            .AsNoTracking()
                            .Select(v => new { v.VesselNumber, v.VesselId })
                            .ToListAsync(cancellationToken);

                        var existingTerminals = await _dbContext.MMSITerminals
                            .Include(t => t.Port)
                            .Select(dt => new { dt.TerminalNumber, dt.TerminalId, dt.Port!.PortNumber, dt.Port.PortId })
                            .ToListAsync(cancellationToken);

                        var existingTugboats = await _dbContext.MMSITugboats
                            .AsNoTracking()
                            .Select(dt => new { dt.TugboatNumber, dt.TugboatId })
                            .ToListAsync(cancellationToken);

                        var existingTugMasters = await _dbContext.MMSITugMasters
                            .AsNoTracking()
                            .Select(dt => new { dt.TugMasterNumber, dt.TugMasterId })
                            .ToListAsync(cancellationToken);

                        var existingServices = await _dbContext.MMSIServices
                            .AsNoTracking()
                            .Select(dt => new { dt.ServiceNumber, dt.ServiceId })
                            .ToListAsync(cancellationToken);

                        var ibsCustomerList = await _dbContext.FilprideCustomers
                            .Where(c => c.Company == "MMSI")
                            .AsNoTracking()
                            .ToListAsync(cancellationToken);

                        var existingBilling = await _dbContext.MMSIBillings
                            .AsNoTracking()
                            .Select(b => new { b.MMSIBillingNumber, b.MMSIBillingId })
                            .ToListAsync(cancellationToken);

                        #endregion -- Creating Identifier Variables --

                        var newRecords = new List<MMSIDispatchTicket>();

                        using var reader = new StreamReader(dispatchTicketCSVPath);
                        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                        var records = csv.GetRecords<dynamic>().ToList();

                        foreach (var record in records)
                        {
                            var comparableVariable = new
                            {
                                DispatchNumber = record.number as string ?? "",
                                CreatedDate = DateTime.Parse((string)record.entrydate)
                            };

                            if (existingIdentifier.Contains(comparableVariable))
                            {
                                continue;
                            }

                            MMSIDispatchTicket newRecord = new MMSIDispatchTicket();

                            var portTerminalOriginal = record.terminal as string;

                            string portNumber = new string(
                                portTerminalOriginal!.Where(c => !char.IsWhiteSpace(c)).Take(3).ToArray()
                            );

                            string terminalNumber = new string(
                                portTerminalOriginal!.Where(c => !char.IsWhiteSpace(c)).Reverse().Take(3).Reverse().ToArray()
                            );

                            var originalVesselNum = record.vesselnum ?? string.Empty;
                            var originalTugboatNum = record.tugnum ?? string.Empty;
                            var originalServiceNum = record.srvctype ?? string.Empty;
                            var paddedVesselNum = int.Parse(originalVesselNum).ToString("D4");
                            var paddedTugboatNum = int.Parse(originalTugboatNum).ToString("D3");
                            var paddedServiceNum = int.Parse(originalServiceNum).ToString("D3");

                            // get customer from msap that replicates the record's customer number
                            var msapCustomer = msapCustomerRecords.FirstOrDefault(mc => mc.number == record.custno);

                            if (msapCustomer != null)
                            {
                                var customer = ibsCustomerList.FirstOrDefault(c => c.CustomerName == msapCustomer.name && c.CustomerAddress == msapCustomer.address);

                                if (customer != null)
                                {
                                    newRecord.CustomerId = customer.CustomerId;
                                }

                                newRecord.CustomerId = null;
                            }

                            #region -- Assigning Values --

                            newRecord.BillingId = existingBilling.FirstOrDefault(b => b.MMSIBillingNumber == record.number as string)?.MMSIBillingId;
                            newRecord.BillingNumber = record.billnum == string.Empty ? null : record.billnum as string;
                            newRecord.DispatchNumber = record.number;
                            newRecord.Date = DateOnly.Parse(record.date);
                            newRecord.COSNumber = record.cosno == string.Empty ? null : record.cosno;
                            newRecord.DateLeft = DateOnly.Parse(record.dateleft);
                            newRecord.DateArrived = DateOnly.Parse(record.datearrived);
                            newRecord.TimeLeft =  TimeOnly.ParseExact(record.timeleft, "HHmm", CultureInfo.InvariantCulture);
                            newRecord.TimeArrived = TimeOnly.ParseExact(record.timearrived, "HHmm", CultureInfo.InvariantCulture);
                            newRecord.BaseOrStation = record.basestation == string.Empty ? null : record.basestation;
                            newRecord.VoyageNumber = record.voyage == string.Empty ? null : record.voyage;
                            newRecord.DispatchRate = decimal.Parse(record.dispatchrate);
                            newRecord.DispatchBillingAmount = decimal.Parse(record.dispatchbillamt);
                            newRecord.DispatchNetRevenue = decimal.Parse(record.dispatchnetamt);
                            newRecord.BAFRate = decimal.Parse(record.bafrate);
                            newRecord.BAFBillingAmount = decimal.Parse(record.bafbillamt);
                            newRecord.BAFNetRevenue = decimal.Parse(record.bafnetamt);
                            newRecord.TotalBilling = newRecord.DispatchBillingAmount + newRecord.BAFBillingAmount;
                            newRecord.TotalNetRevenue = newRecord.DispatchNetRevenue + newRecord.BAFNetRevenue;
                            newRecord.TugBoatId = existingTugboats.FirstOrDefault(tb => tb.TugboatNumber == paddedTugboatNum)!.TugboatId;
                            newRecord.TugMasterId = existingTugMasters.FirstOrDefault(tm => tm.TugMasterNumber == record.masterno)!.TugMasterId;
                            newRecord.VesselId = existingVessels.FirstOrDefault(v => v.VesselNumber == paddedVesselNum)!.VesselId;
                            newRecord.TerminalId = record.terminal == string.Empty ? null : existingTerminals.FirstOrDefault(t => t.PortNumber == portNumber && t.TerminalNumber == terminalNumber)!.PortId;
                            newRecord.ServiceId = existingServices.FirstOrDefault(t => t.ServiceNumber == paddedServiceNum)!.ServiceId;
                            newRecord.CreatedBy = record.entryby;
                            newRecord.CreatedDate = DateTime.Parse(record.entrydate);
                            newRecord.ApOtherTugs = Decimal.Parse(record.apothertug);
                            newRecord.DispatchChargeType = null;
                            newRecord.BAFChargeType = null;
                            newRecord.Status = "For Billing";
                            newRecord.Remarks = null;
                            newRecord.TariffBy = null;
                            newRecord.TariffEditedBy = null;
                            newRecord.DispatchChargeType = record.perhour == "T" ? "Per hour" : "Per move";
                            newRecord.BAFChargeType = "Per move";
                            newRecord.Status = record.billnum == string.Empty ? "For Billing" : "Billed";


                            if (newRecord.DateLeft != null && newRecord.DateArrived != null && newRecord.TimeLeft != null && newRecord.TimeArrived != null)
                            {
                                DateTime dateTimeLeft = newRecord.DateLeft.Value.ToDateTime(newRecord.TimeLeft.Value);
                                DateTime dateTimeArrived = newRecord.DateArrived.Value.ToDateTime(newRecord.TimeArrived.Value);
                                TimeSpan timeDifference = dateTimeArrived - dateTimeLeft;
                                var totalHours = Math.Round((decimal)timeDifference.TotalHours, 2);

                                // find the nearest half hour if the customer is phil-ceb
                                if (newRecord.CustomerId == 179)
                                {
                                    var wholeHours = Math.Truncate(totalHours);
                                    var fractionalPart = totalHours - wholeHours;

                                    if (fractionalPart >= 0.75m)
                                    {
                                        totalHours = wholeHours + 1.0m; // round up to next hour
                                    }
                                    else if (fractionalPart >= 0.25m)
                                    {
                                        totalHours = wholeHours + 0.5m; // round to half hour
                                    }
                                    else
                                    {
                                        totalHours = wholeHours; // keep as is
                                    }
                                }

                                newRecord.TotalHours = totalHours;
                            }

                            // dispatch discount -- none
                            // baf discount -- none
                            //  tariff by -- none
                            //  tariff date -- none
                            //  tariff edited by -- none
                            //  tariff edited date -- none
                            // video url
                            //  video name
                            //  video saved url
                            //  image name
                            //  image saved url
                            //  image signed url

                            #endregion -- Assigning Values --

                            newRecords.Add(newRecord);
                            _dbContext.MMSIDispatchTickets.Add(newRecord);
                        }

                        await _dbContext.MMSIDispatchTickets.AddRangeAsync(newRecords, cancellationToken);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        return $"{field} imported successfully, {newRecords.Count} new records";
                    }
                    case "Billing":
                    {
                        using var reader0 = new StreamReader(customerCSVPath);
                        using var csv0 = new CsvReader(reader0, CultureInfo.InvariantCulture);

                        var msapCustomerRecords = csv0.GetRecords<dynamic>().Select(c => new
                        {
                            c.number,
                            c.name,
                            address = c.address1 == string.Empty ? "-" : $"{c.address1} {c.address2} {c.address3}"
                        }).ToList();

                        using var reader = new StreamReader(billingCSVPath);
                        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                        var records = csv.GetRecords<dynamic>().ToList();
                        var newRecords = new List<MMSIBilling>();

                        #region -- Identifier Variables --

                        var existingIdentifier = await _dbContext.MMSIBillings
                            .AsNoTracking()
                            .Select(b => b.MMSIBillingNumber)
                            .ToListAsync(cancellationToken);

                        var existingVessels = await _dbContext.MMSIVessels
                            .AsNoTracking()
                            .Select(v => new { v.VesselNumber, v.VesselId })
                            .ToListAsync(cancellationToken);

                        var existingPorts = await _dbContext.MMSIPorts
                            .AsNoTracking()
                            .Select(p => new { p.PortNumber, p.PortId })
                            .ToListAsync(cancellationToken);

                        var existingTerminals = await _dbContext.MMSITerminals
                            .AsNoTracking()
                            .Include(t => t.Port)
                            .Select(p => new { p.TerminalNumber, p.TerminalId, p.Port!.PortNumber })
                            .ToListAsync(cancellationToken);

                        var existingCustomers = await _dbContext.FilprideCustomers
                            .Where(c => c.Company == "MMSI")
                            .AsNoTracking()
                            .Select(c => new { c.CustomerId, c.CustomerName })
                            .ToListAsync(cancellationToken);

                        var existingPrincipals = await _dbContext.MMSIPrincipals
                            .AsNoTracking()
                            .Select(p => new { p.PrincipalId, p.CustomerId, p.PrincipalNumber })
                            .ToListAsync(cancellationToken);

                        var existingCollection = await _dbContext.MMSICollections
                            .AsNoTracking()
                            .Select(c => new { c.MMSICollectionId, c.MMSICollectionNumber })
                            .ToListAsync(cancellationToken);

                        var ibsCustomerList = await _dbContext.FilprideCustomers
                            .Where(c => c.Company == "MMSI")
                            .AsNoTracking()
                            .ToListAsync(cancellationToken);

                         #endregion -- Identifier Variables --

                        foreach (var record in records)
                        {
                            if (existingIdentifier.Contains(record.number))
                            {
                                continue;
                            }

                            var originalVesselNum = record.vesselnum ?? string.Empty;
                            var paddedVesselNum = int.Parse(originalVesselNum).ToString("D4");
                            var originalPortNum = record.portnum ?? string.Empty;
                            var paddedPortNum = int.Parse(originalPortNum).ToString("D3");
                            var originalTerminalNum = record.terminal ?? string.Empty;
                            var paddedTerminalNum = int.Parse(originalTerminalNum).ToString("D3");
                            var originalPrincipalNum = record.billto ?? string.Empty;
                            var paddedPrincipalNum = int.Parse(originalPrincipalNum).ToString("D4");

                            MMSIBilling newRecord = new MMSIBilling();

                            var msapCustomer = msapCustomerRecords.FirstOrDefault(mc => mc.number == record.custno);

                            if (msapCustomer != null)
                            {
                                var customer = ibsCustomerList.FirstOrDefault(c => c.CustomerName == msapCustomer.name && c.CustomerAddress == msapCustomer.address);

                                if (customer != null)
                                {
                                    newRecord.CustomerId = customer.CustomerId;
                                }

                                newRecord.CustomerId = null;
                            }

                            newRecord.MMSIBillingNumber = record.number;
                            newRecord.Date = DateOnly.ParseExact(record.date, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                            newRecord.VesselId = existingVessels.FirstOrDefault(v => v.VesselNumber == paddedVesselNum)!.VesselId;
                            newRecord.PortId = existingPorts.FirstOrDefault(p => p.PortNumber == paddedPortNum)!.PortId;
                            newRecord.Amount = decimal.Parse(record.amount);
                            newRecord.IsUndocumented = record.undocumented == "T";
                            newRecord.ApOtherTug = decimal.Parse(record.apothertug);
                            newRecord.CreatedDate = DateTime.ParseExact( record.entrydate, "MM/dd/yyyy  hh:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None );
                            newRecord.CreatedBy = record.entryby as string == string.Empty ? string.Empty : record.entryby;
                            newRecord.VoyageNumber = record.voyage == string.Empty ? null : record.voyage;
                            newRecord.DispatchAmount = decimal.Parse(record.dispatchamount);
                            newRecord.BAFAmount = decimal.Parse(record.bafamount);
                            newRecord.CollectionId = existingCollection.FirstOrDefault(c => c.MMSICollectionNumber == record.crnum)!.MMSICollectionId;
                            newRecord.CollectionNumber = record.crnum == string.Empty ? null : record.crnum;
                            newRecord.IsUndocumented = record.undocumented == "T";
                            newRecord.TerminalId = record.terminal == string.Empty ? null :
                                existingTerminals.FirstOrDefault(t => t.TerminalNumber == paddedTerminalNum && t.PortNumber == paddedPortNum)!.TerminalId;
                            newRecord.IsVatable = record.vat == "T";
                            newRecord.PrincipalId = existingPrincipals.FirstOrDefault(p => p.PrincipalNumber == paddedPrincipalNum)!.PrincipalId;
                            newRecord.IsPrinted = record.printed == "T";

                            newRecords.Add(newRecord);
                        }

                        return "Billing import successful";
                    }
                    case "Collection":
                    {
                        using var reader0 = new StreamReader(customerCSVPath);
                        using var csv0 = new CsvReader(reader0, CultureInfo.InvariantCulture);

                        var msapCustomerRecords = csv0.GetRecords<dynamic>().Select(c => new
                        {
                            c.number,
                            c.name
                        }).ToList();

                        using var reader = new StreamReader(collectionCSVPath);
                        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                        var records = csv.GetRecords<dynamic>().ToList();
                        var newRecords = new List<MMSICollection>();

                        #region -- Identifier Variables --

                        var existingIdentifier = await _dbContext.MMSICollections
                            .AsNoTracking()
                            .Select(b => b.MMSICollectionNumber)
                            .ToListAsync(cancellationToken);

                        var ibsCustomerList = await _dbContext.FilprideCustomers
                            .Where(c => c.Company == "MMSI")
                            .AsNoTracking()
                            .ToListAsync(cancellationToken);

                         #endregion -- Identifier Variables --

                        foreach (var record in records)
                        {
                            if (existingIdentifier.Contains(record.crnum))
                            {
                                continue;
                            }

                            MMSICollection newRecord = new MMSICollection();

                            var msapCustomer = msapCustomerRecords.FirstOrDefault(mc => mc.number as string == record.custno as string);

                            if (msapCustomer != null)
                            {
                                var customerName = msapCustomer.name as string;

                                var customer = ibsCustomerList.FirstOrDefault(c => c.CustomerName.Trim() == customerName!.Trim());
                                newRecord.CustomerId = customer!.CustomerId;
                            }

                            newRecord.MMSICollectionNumber = record.crnum;
                            newRecord.CheckNumber = record.checkno;
                            newRecord.Status = "Create";
                            newRecord.Date = DateOnly.ParseExact(record.crdate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                            newRecord.CheckDate = record.checkdate == "/  /" ? DateOnly.MinValue : DateOnly.ParseExact(record.checkdate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                            newRecord.DepositDate = DateOnly.ParseExact(record.datedeposited, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                            newRecord.Amount = decimal.Parse(record.amount);
                            newRecord.EWT = decimal.Parse(record.n2307);
                            newRecord.IsUndocumented = record.undocumented == "T";
                            newRecord.CreatedBy = record.createdby;
                            newRecord.CreatedDate = DateTime.ParseExact(record.createddate, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                            newRecords.Add(newRecord);
                            }

                            await _dbContext.MMSICollections.AddRangeAsync(newRecords, cancellationToken);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                            await transaction.CommitAsync(cancellationToken);
                            return $"Collection import successful, {newRecords.Count} new records";
                        }

                        #endregion -- Data entries --

                        default:
                            return $"{field} field is invalid";
                }
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
