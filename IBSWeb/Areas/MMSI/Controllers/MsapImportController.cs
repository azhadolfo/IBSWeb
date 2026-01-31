using CsvHelper;
using CsvHelper.Configuration;
using Google.Protobuf.WellKnownTypes;
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
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class MsapImportController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MsapImportController> _logger;
        private readonly IUserAccessService _userAccessService;

        public MsapImportController(IUnitOfWork unitOfWork, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager,
            ILogger<MsapImportController> logger, IUserAccessService userAccessService)
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
                    ?? User.Identity?.Name
                    ?? "Unknown";
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(List<string> fieldList)
        {
            try
            {
                if (fieldList == null || fieldList.Count == 0)
                {
                    TempData["error"] = "Please select at least one import field.";
                    return RedirectToAction(nameof(Index));
                }
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

            try
            {
                var customerCSVPath = "C:\\csv\\customer.CSV";
                var portCSVPath = "C:\\csv\\port.csv";
                var terminalCSVPath = "C:\\csv\\terminal.csv";
                var principalCSVPath = "C:\\csv\\principal.csv";
                var serviceCSVPath = "C:\\csv\\services.csv";
                var tugboatCSVPath = "C:\\csv\\tugboat.csv";
                var tugboatOwnerCSVPath = "C:\\csv\\tugboatOwner.csv";
                var tugMasterCSVPath = "C:\\csv\\tugMaster.csv";
                var vesselCSVPath = "C:\\csv\\vessel.csv";

                var dispatchTicketCSVPath = "C:\\csv\\dispatch.CSV";
                var billingCSVPath = "C:\\csv\\billing.CSV";
                var collectionCSVPath = "C:\\csv\\collection.CSV";

                string result;

                switch (field)
                {
                    #region -- Masterfiles --

                    case "Customer":
                    {
                        result = await ImportMsapCustomers(customerCSVPath, cancellationToken);
                        break;
                    }
                    case "Port":
                    {
                        result = await ImportMsapPorts(portCSVPath, cancellationToken);
                        break;
                    }
                    case "Terminal":
                    {
                        result = await ImportMsapTerminals(terminalCSVPath, cancellationToken);
                        break;
                    }
                    case "Principal":
                    {
                        result = await ImportMsapPrincipals(principalCSVPath, customerCSVPath, cancellationToken);
                        break;
                    }
                    case "Service":
                    {
                        result = await ImportMsapServices(serviceCSVPath, cancellationToken);
                        break;
                    }
                    case "Tugboat":
                    {
                        result = await ImportMsapTugboats(tugboatCSVPath, cancellationToken);
                        break;
                    }
                    case "TugboatOwner":
                    {
                        result = await ImportMsapTugboatOwners(tugboatOwnerCSVPath, cancellationToken);
                        break;
                    }
                    case "TugMaster":
                    {
                        result = await ImportMsapTugMasters(tugMasterCSVPath, cancellationToken);
                        break;
                    }
                    case "Vessel":
                    {
                        result = await ImportMsapVessels(vesselCSVPath, cancellationToken);
                        break;
                    }

                    #endregion -- Masterfiles --

                    #region -- Data entries --

                    case "DispatchTicket":
                    {
                        result = await ImportMsapDispatchTickets(dispatchTicketCSVPath, customerCSVPath, cancellationToken);
                        break;
                    }
                    case "Billing":
                    {
                        result = await ImportMsapBillings(billingCSVPath, customerCSVPath, cancellationToken);
                        break;
                    }
                    case "Collection":
                    {
                        result = await ImportMsapCollections(collectionCSVPath, customerCSVPath, cancellationToken);
                        break;
                    }

                    #endregion -- Data entries --

                    default:
                        result = $"{field} field is invalid";
                        break;
                }

                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException(ex.Message);
            }
        }

        #region -- Masterfiles --

        public async Task<string> ImportMsapCustomers(string customerCSVPath, CancellationToken cancellationToken)
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
            return $"Customers imported successfully, {customerList.Count} new records";
        }

        public async Task<string> ImportMsapPorts(string portCSVPath, CancellationToken cancellationToken)
        {
            var existingIdentifier = (await _dbContext.MMSIPorts.ToListAsync(cancellationToken))
                .Select(c => c.PortNumber).ToList();

            var newRecords = new List<MMSIPort>();
            using var reader = new StreamReader(portCSVPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                string original = record.number ?? string.Empty;
                if (!int.TryParse(original, out var portNum))
                {
                    continue; // or collect/report invalid rows
                }
                string padded = portNum.ToString("D3");

                // check if already in the database
                if (existingIdentifier.Contains(padded))
                {
                    continue;
                }

                MMSIPort newRecord = new MMSIPort();

                newRecord.PortNumber = padded;
                newRecord.PortName = record.name;

                newRecords.Add(newRecord);
            }

            await _dbContext.MMSIPorts.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Ports imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapTerminals(string terminalCSVPath, CancellationToken cancellationToken)
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
                var terminalComposite = record.number as string ?? string.Empty;
                if (terminalComposite.Length < 6) { continue; }
                var portPart = terminalComposite.Substring(0, 3);
                var terminalPart = terminalComposite.Substring(terminalComposite.Length - 3, 3);
                if (!int.TryParse(portPart, out var portNum) || !int.TryParse(terminalPart, out var terminalNum))
                {
                    continue;
                }
                string paddedPortNumber = portNum.ToString("D3");
                string paddedTerminalNumber = terminalNum.ToString("D3");

                // check if already in the database
                if (existingIdentifier.Contains(new { PortNumber = paddedPortNumber, TerminalNumber = paddedTerminalNumber }!))
                {
                    continue;
                }

                MMSITerminal newRecord = new MMSITerminal();

                var port = existingPorts.FirstOrDefault(p => p.PortNumber == paddedPortNumber);
                if (port == null) { continue; }
                newRecord.PortId = port.PortId;
                newRecord.TerminalName = record.name;
                newRecord.TerminalNumber = paddedTerminalNumber;

                newRecords.Add(newRecord);
            }

            await _dbContext.MMSITerminals.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Terminals imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapPrincipals(string principalCSVPath, string customerCSVPath, CancellationToken cancellationToken)
        {
            var existingIdentifier = (await _dbContext.MMSIPrincipals.ToListAsync(cancellationToken))
                .Select(c => new { c.PrincipalNumber, c.PrincipalName, c.CustomerId}).ToList();

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
                .Select(c =>
                {
                    var matchedCustomer = mmsiCustomers.FirstOrDefault(cu => cu.CustomerName == c.name);
                    return matchedCustomer == null ? null : new
                    {
                        CustomerId = matchedCustomer.CustomerId,
                        CustomerNumber = c.number,
                        CustomerName = c.name
                    };
                })
                .Where(c => c != null)
                .ToList();

            foreach (var record in records)
            {
                string original = record.number ?? string.Empty;
                if (!int.TryParse(original, out var principalNum))
                {
                    continue;
                }
                var padded = principalNum.ToString("D4");

                var agentStr = record.agent as string ?? string.Empty;
                if (!int.TryParse(agentStr, out var agentNum))
                {
                    continue;
                }
                var paddedCustomerNumber = agentNum.ToString("D4");
                var agent = customersList.FirstOrDefault(c => c?.CustomerNumber == paddedCustomerNumber);
                if (agent == null)
                {
                    continue; // or throw with a clearer message
                }
                var identity = new
                {
                    PrincipalNumber = padded,
                    PrincipalName = record.name as string ?? string.Empty,
                    CustomerId = agent.CustomerId
                };

                // check if already in the database
                if (existingIdentifier.Contains(identity))
                {
                    continue;
                }


                MMSIPrincipal newRecord = new MMSIPrincipal();

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
            return $"Principals imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapServices(string serviceCSVPath, CancellationToken cancellationToken)
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
                if (!int.TryParse(originalNumber, out var serviceNum))
                {
                    continue; // or log invalid rows
                }
                var padded = serviceNum.ToString("D3");

                // check if already in the database
                if (existingIdentifier.Contains(padded))
                {
                    continue;
                }

                MMSIService newRecord = new MMSIService();

                newRecord.ServiceNumber = padded;
                newRecord.ServiceName = record.desc;

                newRecords.Add(newRecord);
            }

            await _dbContext.MMSIServices.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Services imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapTugboats(string tugboatCSVPath, CancellationToken cancellationToken)
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
                newRecord.IsCompanyOwned = record.companyowned == "T";

                newRecords.Add(newRecord);
            }

            await _dbContext.MMSITugboats.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Tugboats imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapTugboatOwners(string tugboatOwnerCSVPath, CancellationToken cancellationToken)
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
            return $"Tugboat Owners imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapTugMasters(string tugMasterCSVPath, CancellationToken cancellationToken)
        {
            var existingIdentifier = (await _dbContext.MMSITugMasters.ToListAsync(cancellationToken))
                .Select(c => c.TugMasterNumber).ToList();

            var newRecords = new List<MMSITugMaster>();
            using var reader = new StreamReader(tugMasterCSVPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                if (existingIdentifier.Contains(record.empno))
                {
                    continue;
                }

                MMSITugMaster newRecord = new MMSITugMaster();

                newRecord.TugMasterNumber = record.empno;
                newRecord.TugMasterName = record.name;
                newRecord.IsActive = record.active == "T";

                newRecords.Add(newRecord);
            }

            await _dbContext.MMSITugMasters.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Tug Masters imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapVessels(string vesselCSVPath, CancellationToken cancellationToken)
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
                if (!int.TryParse(originalNumber, out var vesselNum))
                {
                    continue;
                }
                var padded = vesselNum.ToString("D4");

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
            return $"Vessels imported successfully, {newRecords.Count} new records";
        }

        #endregion -- Masterfiles --

        public async Task<string> ImportMsapDispatchTickets(string dispatchTicketCSVPath, string customerCSVPath, CancellationToken cancellationToken)
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
                .OrderBy(dt => dt.DispatchNumber)
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
                .Select(b => new { b.MMSIBillingNumber, b.MMSIBillingId, b.CustomerId })
                .ToListAsync(cancellationToken);

            #endregion -- Creating Identifier Variables --

            var newRecords = new List<MMSIDispatchTicket>();

            using var reader = new StreamReader(dispatchTicketCSVPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                 if (!DateTime.TryParse(record.entrydate as string, out var entryDate))
                {
                    continue;
                }
                var comparableVariable = new
                {
                    DispatchNumber = (record.number as string)?.Trim() ?? "",
                    CreatedDate = entryDate
                };

                if (existingIdentifier.Contains(comparableVariable))
                {
                    continue;
                }

                MMSIDispatchTicket newRecord = new MMSIDispatchTicket();

                var portTerminalOriginal = record.terminal as string ?? string.Empty;
                if (string.IsNullOrWhiteSpace(portTerminalOriginal)) { continue; }

                string portNumber = new string(
                    portTerminalOriginal!.Where(c => !char.IsWhiteSpace(c)).Take(3).ToArray()
                );

                string terminalNumber = new string(
                    portTerminalOriginal!.Where(c => !char.IsWhiteSpace(c)).Reverse().Take(3).Reverse().ToArray()
                );

                var originalVesselNum = record.vesselnum ?? string.Empty;
                var originalTugboatNum = record.tugnum ?? string.Empty;
                var originalServiceNum = record.srvctype ?? string.Empty;
                static string PadNumber(string? value, int width)
                {
                    return int.TryParse(value, out var n)
                        ? n.ToString($"D{width}")
                        : string.Empty;
                }

                var paddedVesselNum  = PadNumber(record.vesselnum, 4);
                var paddedTugboatNum = PadNumber(record.tugnum, 3);
                var paddedServiceNum = PadNumber(record.srvctype, 3);


                // get customer from msap that replicates the record's customer number
                var msapCustomer = msapCustomerRecords.FirstOrDefault(mc => mc.number == record.custno);

                if (msapCustomer != null)
                {
                    var customer = ibsCustomerList.FirstOrDefault(c => c.CustomerName == msapCustomer.name);

                    // use the customer file from msap as identifier between the customer number from dispatch and customer id from ibs
                    if (customer != null)
                    {
                        newRecord.CustomerId = customer.CustomerId;
                    }
                    // customer is null, will try to look for customer based on the tagged billing number
                    else if (record.billnum != string.Empty)
                    {
                        newRecord.CustomerId = existingBilling.FirstOrDefault(b => b.MMSIBillingNumber == record.number as string)?.CustomerId;
                    }
                    else
                    {
                        newRecord.CustomerId = null;
                    }
                }

                #region -- Assigning Values --

                newRecord.BillingId = existingBilling.FirstOrDefault(b => b.MMSIBillingNumber == record.number as string)?.MMSIBillingId;
                newRecord.BillingNumber = record.billnum == string.Empty ? null : record.billnum as string;
                newRecord.DispatchNumber = record.number;
                newRecord.Date = DateOnly.Parse(record.date);
                newRecord.COSNumber = record.cosno == string.Empty ? null : record.cosno;
                newRecord.DateLeft = DateOnly.Parse(record.dateleft);
                newRecord.DateArrived = DateOnly.Parse(record.datearrived);
                if (!int.TryParse(record.timeleft as string, out var timeLeftInt) ||
                    !int.TryParse(record.timearrived as string, out var timeArrivedInt))
                {
                    continue; // Skip records with invalid time values
                }
                newRecord.TimeLeft = TimeOnly.ParseExact(timeLeftInt.ToString("D4"), "HHmm", CultureInfo.InvariantCulture);
                newRecord.TimeArrived = TimeOnly.ParseExact(timeArrivedInt.ToString("D4"), "HHmm", CultureInfo.InvariantCulture);                newRecord.BaseOrStation = record.basestation == string.Empty ? null : record.basestation;
                newRecord.VoyageNumber = record.voyage == string.Empty ? null : record.voyage;
                if (!decimal.TryParse(record.dispatchrate, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal dispatchRate))
                {
                    continue;
                }
                newRecord.DispatchRate = dispatchRate;
                if (!decimal.TryParse(record.dispatchbillamt, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal dispatchBillingAmount))
                {
                    continue;
                }
                newRecord.DispatchBillingAmount = dispatchBillingAmount;
                // newRecord.DispatchNetRevenue = decimal.Parse(record.dispatchnetamt);
                if (!decimal.TryParse(record.bafrate, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal bafRate))
                {
                    continue;
                }
                newRecord.BAFRate = bafRate;
                // newRecord.BAFBillingAmount = decimal.Parse(record.bafbillamt);
                if (!decimal.TryParse(record.bafbillamt, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal bafBillingAmount))
                {
                    continue;
                }
                newRecord.BAFBillingAmount = bafBillingAmount;
                // newRecord.BAFNetRevenue = decimal.Parse(record.bafnetamt);
                if (!decimal.TryParse(record.bafnetamt, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal bafNetRevenue))
                {
                    continue;
                }
                newRecord.BAFNetRevenue = bafNetRevenue;
                newRecord.TotalBilling = newRecord.DispatchBillingAmount + newRecord.BAFBillingAmount;
                newRecord.TotalNetRevenue = newRecord.DispatchNetRevenue + newRecord.BAFNetRevenue;
                if (string.IsNullOrEmpty(paddedTugboatNum) ||
                      string.IsNullOrEmpty(paddedVesselNum) ||
                      string.IsNullOrEmpty(paddedServiceNum))
                {
                    continue;
                }
                var tugboat = existingTugboats.FirstOrDefault(tb => tb.TugboatNumber == paddedTugboatNum);
                var tugMaster = existingTugMasters.FirstOrDefault(tm => tm.TugMasterNumber == record.masterno);
                var vessel = existingVessels.FirstOrDefault(v => v.VesselNumber == paddedVesselNum);
                var service = existingServices.FirstOrDefault(s => s.ServiceNumber == paddedServiceNum);
                if (tugboat == null || tugMaster == null || vessel == null || service == null)
                    newRecord.TugBoatId = existingTugboats.FirstOrDefault(tb => tb.TugboatNumber == paddedTugboatNum)!.TugboatId;
                newRecord.TugBoatId = tugboat?.TugboatId;
                newRecord.TugMasterId = tugMaster?.TugMasterId;
                newRecord.VesselId = vessel?.VesselId;
                newRecord.ServiceId = service?.ServiceId;
                newRecord.TugMasterId = existingTugMasters.FirstOrDefault(tm => tm.TugMasterNumber == record.masterno)!.TugMasterId;
                newRecord.VesselId = existingVessels.FirstOrDefault(v => v.VesselNumber == paddedVesselNum)!.VesselId;
                newRecord.TerminalId = record.terminal == string.Empty ? null : existingTerminals.FirstOrDefault(t => t.PortNumber == portNumber && t.TerminalNumber == terminalNumber)!.PortId;
                newRecord.ServiceId = existingServices.FirstOrDefault(t => t.ServiceNumber == paddedServiceNum)!.ServiceId;
                newRecord.CreatedBy = record.entryby;
                newRecord.CreatedDate = DateTime.Parse(record.entrydate);
                if (!decimal.TryParse(record.apothertug, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal apOtherTugs))
                {
                    continue;
                }
                newRecord.ApOtherTugs = apOtherTugs;
                newRecord.DispatchChargeType = null;
                newRecord.BAFChargeType = null;
                newRecord.Status = "For Billing";
                newRecord.Remarks = null;
                newRecord.TariffBy = null;
                newRecord.TariffEditedBy = null;
                newRecord.DispatchChargeType = record.perhour == "T" ? "Per hour" : "Per move";
                newRecord.BAFChargeType = "Per move";

                // if dispatch is approved, assume that it can be billed
                if (record.approved == "T")
                {
                    // if already has billing number, mark as billed
                    newRecord.Status = record.billnum == string.Empty ? "For Billing" : "Billed";
                }
                // if is not yet approved, mark as for tariff
                else
                {
                    newRecord.Status = "For Tariff";
                }

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
                // _dbContext.MMSIDispatchTickets.Add(newRecord); Fix Duplicated Entries
            }

            await _dbContext.MMSIDispatchTickets.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Dispatch Tickets imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapBillings(string billingCSVPath, string customerCSVPath, CancellationToken cancellationToken)
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

                static string PadNumber(string? value, int width)
                {
                    return int.TryParse(value, out var n)
                        ? n.ToString($"D{width}")
                        : string.Empty;
                }

                // Vessel
                var paddedVesselNum = PadNumber(record.vesselnum, 4);
                if (paddedVesselNum == string.Empty)
                {
                    continue;
                }

                // Port + Terminal (expects at least 6 chars: 3 for port, 3 for terminal)
                var paddedPortNum = string.Empty;
                var paddedTerminalNum = string.Empty;

                var terminalRaw = record.terminal ?? string.Empty;
                if (terminalRaw != string.Empty)
                {
                    if (terminalRaw.Length < 6)
                    {
                        continue;
                    }

                    var portPart = terminalRaw.Substring(0, 3);
                    var terminalPart = terminalRaw.Substring(terminalRaw.Length - 3, 3);

                    paddedPortNum = PadNumber(portPart, 3);
                    paddedTerminalNum = PadNumber(terminalPart, 3);

                    if (paddedPortNum == string.Empty || paddedTerminalNum == string.Empty)
                    {
                        continue;
                    }
                }

                // Principal
                var paddedPrincipalNum = string.Empty;
                if (!string.IsNullOrEmpty(record.billto))
                {
                    paddedPrincipalNum = PadNumber(record.billto, 4);
                    if (paddedPrincipalNum == string.Empty)
                    {
                        continue;
                    }
                }


                MMSIBilling newRecord = new MMSIBilling();

                var msapCustomer = msapCustomerRecords.FirstOrDefault(mc => mc.number == record.custno);

                if (msapCustomer != null)
                {
                    var customer = ibsCustomerList.FirstOrDefault(c => c.CustomerName == msapCustomer.name);

                    if (customer != null)
                    {
                        newRecord.CustomerId = customer.CustomerId;
                    }
                    else
                    {
                        newRecord.CustomerId = null;
                    }
                }

                var vessel = existingVessels.FirstOrDefault(v => v.VesselNumber == paddedVesselNum);
                if (vessel == null)
                {
                    continue;
                }
                newRecord.VesselId = vessel.VesselId;

                newRecord.MMSIBillingNumber = record.number;
               if (!DateOnly.TryParseExact(record.date, "M/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly billingDate))
                {
                    continue;
                }
                newRecord.Date = billingDate;
                if(paddedPortNum != string.Empty)
                {
                    newRecord.PortId = existingPorts.FirstOrDefault(p => p.PortNumber == paddedPortNum)?.PortId;
                }

                if (!decimal.TryParse(record.amount, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount))
                {
                    continue;
                }
                newRecord.Amount = amount;
                newRecord.IsUndocumented = record.undocumented == "T";
                if (!decimal.TryParse(record.apothertug, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal apOtherTug))
                {
                    continue;
                }
                newRecord.ApOtherTug = apOtherTug;
                if (!DateTime.TryParseExact(record.entrydate, "M/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime createdDate))
                {
                    continue;
                }
                newRecord.CreatedDate = createdDate;
                newRecord.CreatedBy = record.entryby as string == string.Empty ? string.Empty : record.entryby;
                newRecord.VoyageNumber = record.voyage == string.Empty ? null : record.voyage;
                 if (!decimal.TryParse(record.dispatchamount, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal dispatchAmount))
                {
                    continue;
                }
                newRecord.DispatchAmount = dispatchAmount;
                if (!decimal.TryParse(record.bafamount, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal bafAmount))
                {
                    continue;
                }
                newRecord.BAFAmount = bafAmount;

                if (record.crnum != string.Empty)
                {
                     var collection = existingCollection.FirstOrDefault(c => c.MMSICollectionNumber == record.crnum);
                    if (collection != null)
                    {
                        newRecord.CollectionId = collection.MMSICollectionId;
                        newRecord.Status = "Collected";
                    }
                    else
                    {
                        newRecord.Status = "For Collection";
                    }
                }
                else
                {
                    newRecord.Status = "For Collection";
                }

                newRecord.CollectionNumber = record.crnum == string.Empty ? null : record.crnum;
                newRecord.IsUndocumented = record.undocumented == "T";

                if (paddedTerminalNum != string.Empty)
                {
                    newRecord.TerminalId = existingTerminals.FirstOrDefault(t => t.TerminalNumber == paddedTerminalNum && t.PortNumber == paddedPortNum)?.TerminalId;
                }

                newRecord.IsVatable = record.vat == "T";

                if (paddedPrincipalNum != string.Empty)
                {
                    var principal = existingPrincipals.FirstOrDefault(p => p.PrincipalNumber == paddedPrincipalNum && p.CustomerId == newRecord.CustomerId);
                    if (principal != null)
                    {
                        newRecord.PrincipalId = principal.PrincipalId;
                    }
                    newRecord.BilledTo = "PRINCIPAL";
                }
                else
                {
                    newRecord.BilledTo = "LOCAL";
                }

                    newRecord.IsPrinted = record.printed == "T";

                newRecords.Add(newRecord);
            }

            await _dbContext.MMSIBillings.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Billings imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapCollections(string collectionCSVPath, string customerCSVPath, CancellationToken cancellationToken)
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

                if (msapCustomer == null)
                {
                    continue; // Skip records without matching customer
                }

                var customerName = msapCustomer.name as string;
                if (string.IsNullOrWhiteSpace(customerName))
                {
                    continue;
                }
                var customer = ibsCustomerList.FirstOrDefault(c => c.CustomerName.Trim() == customerName.Trim());

                if (customer == null)
                {
                    continue; // or set to null with a warning
                }
                newRecord.CustomerId = customer.CustomerId;

                newRecord.MMSICollectionNumber = record.crnum;
                newRecord.CheckNumber = record.checkno;
                newRecord.Status = "Create";

                DateOnly crDate = default;
                DateOnly depositDate = default;
                decimal amount = default;
                decimal ewt = default;

                if (!DateOnly.TryParseExact(record.crdate, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out crDate) ||
                    !DateOnly.TryParseExact(record.datedeposited, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out depositDate) ||
                    !decimal.TryParse(record.amount, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) ||
                    !decimal.TryParse(record.n2307, NumberStyles.Number, CultureInfo.InvariantCulture, out ewt))
                {
                    continue;
                }

                newRecord.Date = crDate;

                DateOnly checkDate = default;
                newRecord.CheckDate = record.checkdate == "/  /"
                    ? DateOnly.MinValue
                    : (DateOnly.TryParseExact(record.checkdate, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out checkDate) ? checkDate : DateOnly.MinValue);

                newRecord.DepositDate = depositDate;
                newRecord.Amount = amount;
                newRecord.EWT = ewt;
                newRecord.IsUndocumented = record.undocumented == "T";
                if (!DateTime.TryParseExact(record.createddate, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime createdDate))
                {
                    continue;
                }
                newRecord.CreatedBy = record.createdby;
                newRecord.CreatedDate = createdDate;

                newRecords.Add(newRecord);
            }

            await _dbContext.MMSICollections.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Collection import successful, {newRecords.Count} new records";
        }
    }
}
