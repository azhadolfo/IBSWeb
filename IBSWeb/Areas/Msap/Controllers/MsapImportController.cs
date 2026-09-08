using IBS.Models.Msap.MasterFile;
using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.Models.Msap;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using IBS.Models.Enums;
using IBS.Services.Msap.Attributes;

namespace IBSWeb.Areas.Msap.Controllers
{
    [Area("Msap")]
    public class MsapImportController(ApplicationDbContext dbContext, ILogger<MsapImportController> logger) : Controller
    {
        private readonly CsvConfiguration _csvConfig = new(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.Trim().ToLower(),
            HeaderValidated = null,
            MissingFieldFound = null,
        };

        private readonly List<string> _importErrors = new();

        private string GetUserFullName() =>
            User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
            ?? User.Identity?.Name
            ?? "Unknown";

        // -------------------------------------------------------------------------
        // Actions
        // -------------------------------------------------------------------------

        [RequireAnyAccess("You do not have permission to import Msap data.", ProcedureEnum.ManageMsapImport)]
        [HttpGet]
        public IActionResult Index() => View();

        [RequireAnyAccess("You do not have permission to reset Msap data.", ProcedureEnum.ManageMsapImport)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset()
        {
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(@"
                    TRUNCATE TABLE msap_collection_bills RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE msap_dispatch_tickets RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE msap_billings RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE msap_job_orders RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE msap_collections RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE msap_tariff_rates RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE msap_principals RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE msap_tugboats RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE msap_terminals RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE msap_vessels RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE msap_tug_masters RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE msap_tugboat_owners RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE msap_services RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE msap_ports RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE chart_of_accounts RESTART IDENTITY CASCADE;
                    DELETE FROM customers WHERE company = 'MMSI';
                ");
                TempData["success"] = "All MSAP tables have been reset successfully.";
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Error resetting tables: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [RequireAnyAccess("You do not have permission to import Msap data.", ProcedureEnum.ManageMsapImport)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            IFormFile? customerFile,
            IFormFile? portFile,
            IFormFile? terminalFile,
            IFormFile? principalFile,
            IFormFile? serviceFile,
            IFormFile? tugboatOwnerFile,
            IFormFile? tugboatFile,
            IFormFile? tugMasterFile,
            IFormFile? vesselFile,
            IFormFile? tariffFile,
            IFormFile? dispatchTicketFile,
            IFormFile? billingFile,
            IFormFile? bankAccountFile,
            IFormFile? collectionFile,
            IFormFile? collectBillFile,
            IFormFile? coaFile,
            List<IFormFile>? bulkFiles)
        {
            var sb = new StringBuilder();
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                // Resolve individual vs bulk files
                customerFile      = ResolveFile(customerFile, bulkFiles, "customer", "customer.csv");
                portFile          = ResolveFile(portFile, bulkFiles, "port", "port.csv");
                terminalFile      = ResolveFile(terminalFile, bulkFiles, "terminal", "terminal.csv");
                principalFile     = ResolveFile(principalFile, bulkFiles, "principal", "principal.csv");
                serviceFile       = ResolveFile(serviceFile, bulkFiles, "service", "service.csv");
                tugboatOwnerFile  = ResolveFile(tugboatOwnerFile, bulkFiles, "tugboatowner", "tugboatowner.csv");
                tugboatFile       = tugboatFile ?? bulkFiles?.FirstOrDefault(f =>
                    (f.FileName.Equals("tugboat.csv", StringComparison.OrdinalIgnoreCase)
                     || f.FileName.Contains("tugboat", StringComparison.OrdinalIgnoreCase))
                    && !f.FileName.Contains("owner", StringComparison.OrdinalIgnoreCase));
                tugMasterFile     = ResolveFile(tugMasterFile, bulkFiles, "tugmaster", "tugmaster.csv")
                                    ?? ResolveFile(null, bulkFiles, "master", "master.csv");
                vesselFile        = ResolveFile(vesselFile, bulkFiles, "vessel", "vessel.csv");
                tariffFile        = ResolveFile(tariffFile, bulkFiles, "tariff", "tariff.csv");
                dispatchTicketFile = ResolveFile(dispatchTicketFile, bulkFiles, "dispatch", "dispatch.csv");
                billingFile       = ResolveFile(billingFile, bulkFiles, "billing", "billing.csv");
                bankAccountFile   = ResolveFile(bankAccountFile, bulkFiles, "bank", "bankacct.csv");
                collectionFile    = ResolveFile(collectionFile, bulkFiles, "collection", "collection.csv");
                collectBillFile   = ResolveFile(collectBillFile, bulkFiles, "collect_bill", "collect_bill.csv");
                coaFile           = ResolveFile(coaFile, bulkFiles, "chart_of_accounts", "msap_chart_of_accounts.csv");

                // Preload maps from existing DB records
                var maps = new ImportMaps();
                await LoadExistingMapsAsync(maps, CancellationToken.None);

                // Level 1 Ã¢â‚¬â€ Independent master files
                if (coaFile != null)
                {
                    sb.AppendLine(await ImportChartOfAccountsAsync(coaFile, maps));
                }

                if (bankAccountFile != null)
                {
                    sb.AppendLine(await ImportBankAccountsAsync(bankAccountFile, maps));
                }

                if (customerFile != null)
                {
                    sb.AppendLine(await ImportCustomersAsync(customerFile, maps));
                }

                if (portFile != null)
                {
                    sb.AppendLine(await ImportPortsAsync(portFile, maps));
                }

                if (serviceFile != null)
                {
                    sb.AppendLine(await ImportServicesAsync(serviceFile, maps));
                }

                if (tugboatOwnerFile != null)
                {
                    sb.AppendLine(await ImportTugboatOwnersAsync(tugboatOwnerFile, maps));
                }

                if (tugMasterFile != null)
                {
                    sb.AppendLine(await ImportTugMastersAsync(tugMasterFile, maps));
                }

                if (vesselFile != null)
                {
                    sb.AppendLine(await ImportVesselsAsync(vesselFile, maps));
                }

                // Level 2 Ã¢â‚¬â€ Single dependency
                if (terminalFile != null)
                {
                    sb.AppendLine(await ImportTerminalsAsync(terminalFile, maps));
                }

                if (tugboatFile != null)
                {
                    sb.AppendLine(await ImportTugboatsAsync(tugboatFile, maps));
                }

                if (principalFile != null)
                {
                    sb.AppendLine(await ImportPrincipalsAsync(principalFile, maps));
                }

                // Level 3 Ã¢â‚¬â€ Mixed dependencies
                if (tariffFile != null)
                {
                    sb.AppendLine(await ImportTariffRatesAsync(tariffFile, maps));
                }

                if (collectionFile != null)
                {
                    sb.AppendLine(await ImportCollectionsAsync(collectionFile, maps));
                }

                if (collectBillFile != null)
                {
                    sb.AppendLine(await ImportCollectBillAsync(collectBillFile, maps));
                }

                // Level 4 Ã¢â‚¬â€ Transactional
                if (billingFile != null)
                {
                    sb.AppendLine(await ImportBillingsAsync(billingFile, maps));
                }

                // Level 5 Ã¢â‚¬â€ Final
                if (dispatchTicketFile != null)
                {
                    sb.AppendLine(await ImportDispatchTicketsAsync(dispatchTicketFile, maps));
                }

                if (sb.Length == 0 && _importErrors.Count == 0)
                {
                    TempData["error"] = "Please upload at least one CSV file.";
                    return RedirectToAction(nameof(Index));
                }

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                if (_importErrors.Count > 0)
                {
                    sb.AppendLine("\nERRORS/SKIPPED RECORDS:");
                    foreach (var error in _importErrors.Take(100))
                    {
                        sb.AppendLine(error);
                    }

                    if (_importErrors.Count > 100)
                    {
                        sb.AppendLine("... (more errors truncated)");
                    }
                }

                TempData["success"] = sb.ToString();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // -------------------------------------------------------------------------
        // Import Maps
        // -------------------------------------------------------------------------

        private sealed class BillingMapInfo
        {
            public int Id { get; init; }
            public int PortId { get; init; }
            public int TerminalId { get; init; }
        }

        private sealed class ImportMaps
        {
            public Dictionary<string, int> Customer { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> CustomerLegacyMap { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Port { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> PortLegacyMap { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Service { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> ServiceLegacyMap { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Owner { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> OwnerLegacyMap { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Tugboat { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> TugboatLegacyMap { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> TugMaster { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> TugMasterLegacyMap { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Vessel { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> VesselLegacyMap { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Terminal { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, (int PortId, int TerminalId)> TerminalLegacyMap { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Principal { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> PrincipalLegacyMap { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Collection { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> BankAccount { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, List<string>> CollectBill { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, BillingMapInfo> Billing { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, BillingMapInfo> BillingByRecId { get; } = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> TariffRate { get; } = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> DispatchTicket { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, int> PortToFirstTerminal { get; } = new();
            public Dictionary<int, int> ChartOfAccount { get; } = new();
        }

        private async Task LoadExistingMapsAsync(ImportMaps maps, CancellationToken ct)
        {
            maps.ChartOfAccount.Clear();
            maps.CollectBill.Clear();

            // Try to load collect_bill.csv from Imports folder if it exists
            var collectBillPath = Path.Combine(Directory.GetCurrentDirectory(), "Imports", "collect_bill.csv");
            if (System.IO.File.Exists(collectBillPath))
            {
                try
                {
                    using var reader = new StreamReader(collectBillPath);
                    using var csv = new CsvReader(reader, _csvConfig);
                    var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
                    foreach (var record in records)
                    {
                        string billNum = GetString(record, "billnum");
                        string crNum = GetString(record, "crnum");
                        bool active = ParseBool(record, "active");

                        if (!active || billNum == "-" || crNum == "-") continue;

                        if (!maps.CollectBill.ContainsKey(billNum))
                        {
                            maps.CollectBill[billNum] = new List<string>();
                        }
                        maps.CollectBill[billNum].Add(crNum);
                    }
                }
                catch (Exception ex) { logger.LogWarning(ex, "Failed to load collect_bill.csv"); }
            }

            maps.Port.Clear();
            maps.PortLegacyMap.Clear();
            foreach (var p in await dbContext.MsapPorts.AsNoTracking().ToListAsync(ct))
            {
                maps.Port[p.PortNumber] = p.PortId;
                if (!string.IsNullOrEmpty(p.MsapRecId))
                {
                    maps.PortLegacyMap[p.MsapRecId] = p.PortId;
                }
            }

            maps.Service.Clear();
            maps.ServiceLegacyMap.Clear();
            foreach (var s in await dbContext.MsapServices.AsNoTracking().ToListAsync(ct))
            {
                maps.Service[s.ServiceNumber] = s.ServiceId;
                if (!string.IsNullOrEmpty(s.MsapRecId))
                {
                    maps.ServiceLegacyMap[s.MsapRecId] = s.ServiceId;
                }
            }

            maps.Owner.Clear();
            maps.OwnerLegacyMap.Clear();
            foreach (var o in await dbContext.MsapTugboatOwners.AsNoTracking().ToListAsync(ct))
            {
                maps.Owner[o.TugboatOwnerNumber] = o.TugboatOwnerId;
                if (!string.IsNullOrEmpty(o.MsapRecId))
                {
                    maps.OwnerLegacyMap[o.MsapRecId] = o.TugboatOwnerId;
                }
            }

            maps.TugMaster.Clear();
            maps.TugMasterLegacyMap.Clear();
            foreach (var m in await dbContext.MsapTugMasters.AsNoTracking().ToListAsync(ct))
            {
                maps.TugMaster[m.TugMasterNumber] = m.TugMasterId;
                if (!string.IsNullOrEmpty(m.MsapRecId))
                {
                    maps.TugMasterLegacyMap[m.MsapRecId] = m.TugMasterId;
                }
            }

            maps.Vessel.Clear();
            maps.VesselLegacyMap.Clear();
            foreach (var v in await dbContext.MsapVessels.AsNoTracking().ToListAsync(ct))
            {
                maps.Vessel[v.VesselNumber] = v.VesselId;
                if (!string.IsNullOrEmpty(v.MsapRecId))
                {
                    maps.VesselLegacyMap[v.MsapRecId] = v.VesselId;
                }
            }

            maps.Tugboat.Clear();
            maps.TugboatLegacyMap.Clear();
            foreach (var t in await dbContext.MsapTugboats.AsNoTracking().ToListAsync(ct))
            {
                maps.Tugboat[t.TugboatNumber] = t.TugboatId;
                if (!string.IsNullOrEmpty(t.MsapRecId))
                {
                    maps.TugboatLegacyMap[t.MsapRecId] = t.TugboatId;
                }
            }

            maps.Terminal.Clear();
            maps.TerminalLegacyMap.Clear();
            foreach (var t in await dbContext.MsapTerminals.Include(x => x.Port).AsNoTracking().ToListAsync(ct))
            {
                if (t.Port is { PortNumber: not null })
                {
                    maps.Terminal[$"{t.Port.PortNumber}{t.TerminalNumber}"] = t.TerminalId;
                }
                if (!string.IsNullOrEmpty(t.MsapRecId))
                {
                    maps.TerminalLegacyMap[t.MsapRecId] = (t.PortId, t.TerminalId);
                }
            }

            maps.Billing.Clear();
            maps.BillingByRecId.Clear();
            foreach (var b in await dbContext.MsapBillings.AsNoTracking().ToListAsync(ct))
            {
                var info = new BillingMapInfo { Id = b.MsapBillingId, PortId = b.PortId, TerminalId = b.TerminalId };
                maps.Billing[b.MsapBillingNumber] = info; // Use Number for lookups from Dispatch
                maps.BillingByRecId[b.MsapBillingId.ToString()] = info; // Use RECID for deduplication
            }

            maps.Collection.Clear();
            foreach (var c in await dbContext.MsapCollections.AsNoTracking().ToListAsync(ct))
            {
                maps.Collection[c.MsapCollectionNumber] = c.MsapCollectionId;
            }

            maps.BankAccount.Clear();
            foreach (var b in await dbContext.BankAccounts.AsNoTracking().ToListAsync(ct))
            {
                if (b.BankAccountCode != null)
                {
                    maps.BankAccount[b.BankAccountCode] = b.BankAccountId;
                }
            }

            maps.Customer.Clear();
            maps.CustomerLegacyMap.Clear();
            foreach (var c in await dbContext.Customers.AsNoTracking().Where(x => x.Company == "MMSI").ToListAsync(ct))
            {
                if (c.CustomerCode != null)
                {
                    maps.Customer[c.CustomerCode] = c.CustomerId;
                }
                if (!string.IsNullOrEmpty(c.MsapRecId))
                {
                    maps.CustomerLegacyMap[c.MsapRecId] = c.CustomerId;
                }
            }

            maps.Principal.Clear();
            maps.PrincipalLegacyMap.Clear();
            foreach (var p in await dbContext.MsapPrincipals.AsNoTracking().ToListAsync(ct))
            {
                // Composite key because Number is only unique per Agent
                maps.Principal[$"{p.CustomerId}|{p.PrincipalNumber}"] = p.PrincipalId;
                if (!string.IsNullOrEmpty(p.MsapRecId))
                {
                    maps.PrincipalLegacyMap[p.MsapRecId] = p.PrincipalId;
                }
            }

            maps.TariffRate.Clear();
            foreach (var tr in await dbContext.MsapTariffRates.AsNoTracking().ToListAsync(ct))
            {
                maps.TariffRate.Add($"{tr.AsOfDate:yyyy-MM-dd}|{tr.CustomerId}|{tr.TerminalId}|{tr.ServiceId}");
            }

            maps.DispatchTicket.Clear();
            foreach (var dt in await dbContext.MsapDispatchTickets.AsNoTracking().ToListAsync(ct))
            {
                maps.DispatchTicket.Add(dt.DispatchTicketId.ToString());
            }

            maps.PortToFirstTerminal.Clear();
            var allTerminals = await dbContext.MsapTerminals.AsNoTracking().OrderBy(t => t.TerminalNumber).ToListAsync(ct);
            foreach (var t in allTerminals)
            {
                if (!maps.PortToFirstTerminal.ContainsKey(t.PortId))
                {
                    maps.PortToFirstTerminal[t.PortId] = t.TerminalId;
                }
            }
        }

        // -------------------------------------------------------------------------
        // Master file imports (Level 1 & 2)
        // -------------------------------------------------------------------------

        private async Task<string> ImportChartOfAccountsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            var newRecords = new List<(ChartOfAccount Entity, int LegacyId, int? LegacyParentId)>();

            var existingAccNumbers = await dbContext.ChartOfAccounts.AsNoTracking().Where(a => a.AccountNumber != null).Select(a => a.AccountNumber!).ToListAsync();
            var existingAccNames = await dbContext.ChartOfAccounts.AsNoTracking().Select(a => a.AccountName).ToListAsync();
            var existingAccSet = new HashSet<string>(existingAccNumbers, StringComparer.OrdinalIgnoreCase);
            var existingNameSet = new HashSet<string>(existingAccNames, StringComparer.OrdinalIgnoreCase);

            int skipped = 0;
            foreach (var record in records)
            {
                int legacyId = (int)ParseDecimal(record, "account_id");
                int? legacyParentId = null;
                string parentIdStr = GetString(record, "parent_account_id");
                if (parentIdStr != "-" && int.TryParse(parentIdStr, out int pid))
                {
                    legacyParentId = pid;
                }

                string? accNum = GetString(record, "account_number") == "-" ? null : GetString(record, "account_number");
                string accName = GetString(record, "account_name");

                if ((accNum != null && existingAccSet.Contains(accNum)) || existingNameSet.Contains(accName))
                {
                    skipped++;
                    continue;
                }

                var entity = new ChartOfAccount
                {
                    IsMain = ParseBool(record, "is_main"),
                    AccountNumber = accNum,
                    AccountName = accName,
                    AccountType = GetString(record, "account_type") == "-" ? null : GetString(record, "account_type"),
                    NormalBalance = GetString(record, "normal_balance") == "-" ? null : GetString(record, "normal_balance"),
                    Level = (int)ParseDecimal(record, "level"),
                    HasChildren = ParseBool(record, "has_children"),
                    FinancialStatementType = GetString(record, "financial_statement_type"),
                    CreatedBy = GetString(record, "created_by"),
                    CreatedDate = ParseDateTime(record, "created_date") ?? DateTimeHelper.GetCurrentPhilippineTime(),
                    EditedBy = GetString(record, "edited_by") == "-" ? null : GetString(record, "edited_by"),
                    EditedDate = ParseDateTime(record, "edited_date")
                };

                newRecords.Add((entity, legacyId, legacyParentId));
                if (accNum != null) existingAccSet.Add(accNum);
                existingNameSet.Add(accName);
            }

            if (newRecords.Count > 0)
            {
                await dbContext.ChartOfAccounts.AddRangeAsync(newRecords.Select(x => x.Entity));
                await dbContext.SaveChangesAsync();
            }

            foreach (var item in newRecords)
            {
                maps.ChartOfAccount[item.LegacyId] = item.Entity.AccountId;
            }

            // Second pass: Update ParentAccountId
            bool updated = false;
            foreach (var item in newRecords)
            {
                if (item.LegacyParentId.HasValue && maps.ChartOfAccount.TryGetValue(item.LegacyParentId.Value, out int newParentId))
                {
                    item.Entity.ParentAccountId = newParentId;
                    updated = true;
                }
            }

            if (updated)
            {
                await dbContext.SaveChangesAsync();
            }

            return $"Chart of Accounts: {newRecords.Count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportBankAccountsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0, skipped = 0;
            var newRecords = new List<BankAccount>();

            foreach (var record in records)
            {
                string code = GetString(record, "code").TrimStart('0');
                if (string.IsNullOrEmpty(code)) code = "0";

                if (maps.BankAccount.ContainsKey(code) || maps.BankAccount.ContainsKey(GetString(record, "code")))
                {
                    skipped++;
                    continue;
                }

                var entity = new BankAccount
                {
                    BankAccountCode = code,
                    Bank = GetString(record, "bank"),
                    Branch = GetString(record, "bankname"), // bankname often contains branch
                    AccountNo = GetString(record, "accountno"),
                    AccountName = GetString(record, "accountnam"),
                    Company = "MMSI",
                    CreatedBy = $"Import: {GetUserFullName()}",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                };

                newRecords.Add(entity);
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.BankAccounts.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
                foreach (var r in newRecords)
                {
                    if (r.BankAccountCode != null)
                    {
                        maps.BankAccount[r.BankAccountCode] = r.BankAccountId;
                    }
                }
            }

            return $"Bank Accounts: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportCustomersAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0, skipped = 0;
            var newRecords = new List<(Customer Entity, string LegacyId)>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 4);
                string recid = GetString(record, "recid");

                if (maps.Customer.TryGetValue(msapNumber, out int existingId))
                {
                    if (recid != "-")
                    {
                        maps.CustomerLegacyMap[recid] = existingId;
                        var existing = await dbContext.Customers.FindAsync(existingId);
                        if (existing != null && existing.MsapRecId != recid)
                        {
                            existing.MsapRecId = recid;
                        }
                    }
                    skipped++;
                    continue;
                }

                string name = GetString(record, "name");
                if (name == "-") { _importErrors.Add($"Customer {msapNumber}: Name is missing. Skipping."); continue; }

                var entity = new Customer
                {
                    CustomerCode = msapNumber,
                    CustomerName = name,
                    Address1 = GetString(record, "address1"),
                    Address2 = GetString(record, "address2"),
                    Address3 = GetString(record, "address3"),
                    CustomerAddress = JoinAddress(GetString(record, "address1"), GetString(record, "address2"), GetString(record, "address3")),
                    CustomerTin = NormalizeTin(GetString(record, "tin"), "000-000-000-00000"),
                    BusinessStyle = GetString(record, "business"),
                    CustomerTerms = MapTerms(GetString(record, "terms")),
                    CustomerType = "Industrial",
                    WithHoldingVat = ParseBool(record, "vatable"),
                    VatType = ParseBool(record, "vatable") ? "Vatable" : "Zero-Rated",
                    IsActive = ParseBool(record, "active"),
                    ZipCode = "0000",
                    Type = "Documented",
                    Company = "MMSI",
                    CreatedBy = $"Import: {GetUserFullName()}",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    MsapRecId = recid != "-" ? recid : null
                };

                newRecords.Add((entity, recid));
                count++;

                if (newRecords.Count >= 500)
                {
                    await dbContext.Customers.AddRangeAsync(newRecords.Select(x => x.Entity));
                    await dbContext.SaveChangesAsync();
                    foreach (var item in newRecords)
                    {
                        maps.Customer[item.Entity.CustomerCode] = item.Entity.CustomerId;
                        if (item.LegacyId != "-")
                        {
                            maps.CustomerLegacyMap[item.LegacyId] = item.Entity.CustomerId;
                        }
                    }

                    newRecords.Clear();
                }
            }

            if (newRecords.Count > 0)
            {
                await dbContext.Customers.AddRangeAsync(newRecords.Select(x => x.Entity));
                await dbContext.SaveChangesAsync();
                foreach (var item in newRecords)
                {
                    maps.Customer[item.Entity.CustomerCode] = item.Entity.CustomerId;
                    if (item.LegacyId != "-")
                    {
                        maps.CustomerLegacyMap[item.LegacyId] = item.Entity.CustomerId;
                    }
                }
            }

            return $"Customers: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportPortsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0, skipped = 0;
            var newRecords = new List<(Port Entity, string LegacyId)>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 3);
                string recid = GetString(record, "recid");

                if (maps.Port.TryGetValue(msapNumber, out int existingId))
                {
                    if (recid != "-")
                    {
                        maps.PortLegacyMap[recid] = existingId;
                        var existing = await dbContext.MsapPorts.FindAsync(existingId);
                        if (existing != null && existing.MsapRecId != recid)
                        {
                            existing.MsapRecId = recid;
                        }
                    }
                    skipped++;
                    continue;
                }

                var entity = new Port
                {
                    PortNumber = msapNumber,
                    PortName = GetString(record, "name"),
                    MsapRecId = recid != "-" ? recid : null
                };

                newRecords.Add((entity, recid));
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MsapPorts.AddRangeAsync(newRecords.Select(x => x.Entity));
                await dbContext.SaveChangesAsync();
                foreach (var item in newRecords)
                {
                    maps.Port[item.Entity.PortNumber] = item.Entity.PortId;
                    if (item.LegacyId != "-")
                    {
                        maps.PortLegacyMap[item.LegacyId] = item.Entity.PortId;
                    }
                }
            }

            return $"Ports: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportServicesAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0, skipped = 0;
            var newRecords = new List<(Service Entity, string LegacyId)>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 3);
                string recid = GetString(record, "recid");

                if (maps.Service.TryGetValue(msapNumber, out int existingId))
                {
                    if (recid != "-")
                    {
                        maps.ServiceLegacyMap[recid] = existingId;
                        var existing = await dbContext.MsapServices.FindAsync(existingId);
                        if (existing != null && existing.MsapRecId != recid)
                        {
                            existing.MsapRecId = recid;
                        }
                    }
                    skipped++;
                    continue;
                }

                var entity = new Service
                {
                    ServiceNumber = msapNumber,
                    ServiceName = GetString(record, "desc"),
                    MsapRecId = recid != "-" ? recid : null
                };

                newRecords.Add((entity, recid));
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MsapServices.AddRangeAsync(newRecords.Select(x => x.Entity));
                await dbContext.SaveChangesAsync();
                foreach (var item in newRecords)
                {
                    maps.Service[item.Entity.ServiceNumber] = item.Entity.ServiceId;
                    if (item.LegacyId != "-")
                    {
                        maps.ServiceLegacyMap[item.LegacyId] = item.Entity.ServiceId;
                    }
                }
            }

            return $"Services: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportTugboatOwnersAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0, skipped = 0;
            var newRecords = new List<(TugboatOwner Entity, string LegacyId)>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 3);
                string recid = GetString(record, "recid");

                if (maps.Owner.TryGetValue(msapNumber, out int existingId))
                {
                    if (recid != "-")
                    {
                        maps.OwnerLegacyMap[recid] = existingId;
                        var existing = await dbContext.MsapTugboatOwners.FindAsync(existingId);
                        if (existing != null && existing.MsapRecId != recid)
                        {
                            existing.MsapRecId = recid;
                        }
                    }
                    skipped++;
                    continue;
                }

                var entity = new TugboatOwner
                {
                    TugboatOwnerNumber = msapNumber,
                    TugboatOwnerName = GetString(record, "name"),
                    MsapRecId = recid != "-" ? recid : null
                };

                newRecords.Add((entity, recid));
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MsapTugboatOwners.AddRangeAsync(newRecords.Select(x => x.Entity));
                await dbContext.SaveChangesAsync();
                foreach (var item in newRecords)
                {
                    maps.Owner[item.Entity.TugboatOwnerNumber] = item.Entity.TugboatOwnerId;
                    if (item.LegacyId != "-")
                    {
                        maps.OwnerLegacyMap[item.LegacyId] = item.Entity.TugboatOwnerId;
                    }
                }
            }

            return $"Tugboat Owners: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportTugMastersAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0, skipped = 0;
            var newRecords = new List<(TugMaster Entity, string LegacyId)>();

            foreach (var record in records)
            {
                string empNo = GetString(record, "empno");
                string recid = GetString(record, "recid");

                if (maps.TugMaster.TryGetValue(empNo, out int existingId))
                {
                    if (recid != "-")
                    {
                        maps.TugMasterLegacyMap[recid] = existingId;
                        var existing = await dbContext.MsapTugMasters.FindAsync(existingId);
                        if (existing != null && existing.MsapRecId != recid)
                        {
                            existing.MsapRecId = recid;
                        }
                    }
                    skipped++;
                    continue;
                }

                var entity = new TugMaster
                {
                    TugMasterNumber = empNo,
                    TugMasterName = GetString(record, "name"),
                    IsActive = ParseBool(record, "active"),
                    MsapRecId = recid != "-" ? recid : null
                };

                newRecords.Add((entity, recid));
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MsapTugMasters.AddRangeAsync(newRecords.Select(x => x.Entity));
                await dbContext.SaveChangesAsync();
                foreach (var item in newRecords)
                {
                    maps.TugMaster[item.Entity.TugMasterNumber] = item.Entity.TugMasterId;
                    if (item.LegacyId != "-")
                    {
                        maps.TugMasterLegacyMap[item.LegacyId] = item.Entity.TugMasterId;
                    }
                }
            }

            return $"Tug Masters: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportVesselsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0, skipped = 0;
            var newRecords = new List<(Vessel Entity, string LegacyId)>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 4);
                string recid = GetString(record, "recid");

                if (maps.Vessel.TryGetValue(msapNumber, out int existingId))
                {
                    if (recid != "-")
                    {
                        maps.VesselLegacyMap[recid] = existingId;
                        var existing = await dbContext.MsapVessels.FindAsync(existingId);
                        if (existing != null && existing.MsapRecId != recid)
                        {
                            existing.MsapRecId = recid;
                        }
                    }
                    skipped++;
                    continue;
                }

                var entity = new Vessel
                {
                    VesselNumber = msapNumber,
                    VesselName = GetString(record, "name"),
                    VesselType = GetString(record, "type") == "L" ? "LOCAL" : "FOREIGN",
                    MsapRecId = recid != "-" ? recid : null
                };

                newRecords.Add((entity, recid));
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MsapVessels.AddRangeAsync(newRecords.Select(x => x.Entity));
                await dbContext.SaveChangesAsync();
                foreach (var item in newRecords)
                {
                    maps.Vessel[item.Entity.VesselNumber] = item.Entity.VesselId;
                    if (item.LegacyId != "-")
                    {
                        maps.VesselLegacyMap[item.LegacyId] = item.Entity.VesselId;
                    }
                }
            }

            return $"Vessels: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportTerminalsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0, skipped = 0;
            var newRecords = new List<(Terminal Entity, string LegacyId)>();

            foreach (var record in records)
            {
                string msapNumber = GetString(record, "number");
                string recid = GetString(record, "recid");
                string? mapKey = NormalizeTerminalCode(msapNumber);
                if (mapKey == null) { _importErrors.Add($"Terminal {msapNumber}: Invalid code format. Skipping."); continue; }

                if (!maps.Port.TryGetValue(mapKey.Substring(0, 3), out int portId))
                {
                    _importErrors.Add($"Terminal {msapNumber}: Port {mapKey.Substring(0, 3)} not found. Skipping.");
                    continue;
                }

                if (maps.Terminal.TryGetValue(mapKey, out int existingId))
                {
                    if (recid != "-")
                    {
                        maps.TerminalLegacyMap[recid] = (portId, existingId);
                        var existing = await dbContext.MsapTerminals.FindAsync(existingId);
                        if (existing != null && existing.MsapRecId != recid)
                        {
                            existing.MsapRecId = recid;
                        }
                    }
                    skipped++;
                    continue;
                }

                var entity = new Terminal
                {
                    PortId = portId,
                    TerminalNumber = mapKey.Substring(3, 3),
                    TerminalName = GetString(record, "name"),
                    MsapRecId = recid != "-" ? recid : null
                };

                newRecords.Add((entity, recid));
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MsapTerminals.AddRangeAsync(newRecords.Select(x => x.Entity));
                await dbContext.SaveChangesAsync();
                foreach (var item in newRecords)
                {
                    var r = item.Entity;
                    string pNum = maps.Port.First(x => x.Value == r.PortId).Key;
                    string mapKey = $"{pNum}{r.TerminalNumber}";
                    maps.Terminal[mapKey] = r.TerminalId;
                    if (item.LegacyId != "-")
                    {
                        maps.TerminalLegacyMap[item.LegacyId] = (r.PortId, r.TerminalId);
                    }

                    if (!maps.PortToFirstTerminal.ContainsKey(r.PortId))
                    {
                        maps.PortToFirstTerminal[r.PortId] = r.TerminalId;
                    }
                }
            }

            return $"Terminals: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportTugboatsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0, skipped = 0;
            var newRecords = new List<(Tugboat Entity, string LegacyId)>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 3);
                string recid = GetString(record, "recid");

                if (maps.Tugboat.TryGetValue(msapNumber, out int existingId))
                {
                    if (recid != "-")
                    {
                        maps.TugboatLegacyMap[recid] = existingId;
                        var existing = await dbContext.MsapTugboats.FindAsync(existingId);
                        if (existing != null && existing.MsapRecId != recid)
                        {
                            existing.MsapRecId = recid;
                        }
                    }
                    skipped++;
                    continue;
                }

                string ownerNoRaw = GetString(record, "owner");
                string ownerNo = PadNumber(ownerNoRaw, 3);
                int? ownerId = null;
                if (ownerNo != "-" && ownerNo != "000")
                {
                    if (maps.Owner.TryGetValue(ownerNo, out int id) || maps.OwnerLegacyMap.TryGetValue(ownerNoRaw, out id))
                    {
                        ownerId = id;
                    }
                    else
                    {
                        _importErrors.Add($"Tugboat {msapNumber}: Owner {ownerNo} not found. Setting as null.");
                    }
                }

                var entity = new Tugboat
                {
                    TugboatNumber = msapNumber,
                    TugboatName = GetString(record, "name"),
                    IsCompanyOwned = ParseBool(record, "companyown"),
                    TugboatOwnerId = ownerId,
                    PortId = 1, // Defaulting to first port for now; can be updated manually
                    MsapRecId = recid != "-" ? recid : null
                };

                newRecords.Add((entity, recid));
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MsapTugboats.AddRangeAsync(newRecords.Select(x => x.Entity));
                await dbContext.SaveChangesAsync();
                foreach (var item in newRecords)
                {
                    maps.Tugboat[item.Entity.TugboatNumber] = item.Entity.TugboatId;
                    if (item.LegacyId != "-")
                    {
                        maps.TugboatLegacyMap[item.LegacyId] = item.Entity.TugboatId;
                    }
                }
            }

            return $"Tugboats: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportPrincipalsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0, skipped = 0;
            var newRecords = new List<(Principal Entity, string LegacyId)>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 4);
                string recid = GetString(record, "recid");

                if (recid != "-" && maps.PrincipalLegacyMap.ContainsKey(recid))
                {
                    skipped++;
                    continue;
                }

                string agentNo = PadNumber(GetString(record, "agent"), 4);
                if (!maps.Customer.TryGetValue(agentNo, out int customerId) && !maps.CustomerLegacyMap.TryGetValue(GetString(record, "agent"), out customerId))
                {
                    _importErrors.Add($"Principal {msapNumber}: Agent/Customer {agentNo} not found. Skipping.");
                    continue;
                }

                var entity = new Principal
                {
                    PrincipalNumber = msapNumber,
                    PrincipalName = GetString(record, "name"),
                    Agent = GetString(record, "agent"),
                    Address1 = GetString(record, "address1"),
                    Address2 = GetString(record, "address2"),
                    Address3 = GetString(record, "address3"),
                    TIN = NormalizeTin(GetString(record, "tin"), "000-000-000000"),
                    BusinessType = GetString(record, "business"),
                    Terms = MapTerms(GetString(record, "terms")),
                    Landline1 = GetString(record, "landline1"),
                    Landline2 = GetString(record, "landline2"),
                    Mobile1 = GetString(record, "mobile1"),
                    Mobile2 = GetString(record, "mobile2"),
                    IsVatable = ParseBool(record, "vatable"),
                    IsActive = ParseBool(record, "active"),
                    CustomerId = customerId,
                    MsapRecId = recid != "-" ? recid : null
                };

                newRecords.Add((entity, recid));
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MsapPrincipals.AddRangeAsync(newRecords.Select(x => x.Entity));
                await dbContext.SaveChangesAsync();
                foreach (var item in newRecords)
                {
                    maps.Principal[$"{item.Entity.CustomerId}|{item.Entity.PrincipalNumber}"] = item.Entity.PrincipalId;
                    if (item.LegacyId != "-")
                    {
                        maps.PrincipalLegacyMap[item.LegacyId] = item.Entity.PrincipalId;
                    }
                }
            }

            return $"Principals: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportTariffRatesAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0, skipped = 0;
            var newRecords = new List<TariffRate>();

            var terminalToPortMap = await dbContext.MsapTerminals.AsNoTracking().ToDictionaryAsync(t => t.TerminalId, t => t.PortId);

            foreach (var record in records)
            {
                string custNo = PadNumber(GetString(record, "custno"), 4);
                string terminalRaw = GetString(record, "terminal");
                string serviceNum = PadNumber(GetString(record, "service"), 3);

                if (!maps.Customer.TryGetValue(custNo, out int customerId) && !maps.CustomerLegacyMap.TryGetValue(GetString(record, "custno"), out customerId))
                {
                    _importErrors.Add($"Tariff: Customer {custNo} not found. Skipping."); continue;
                }

                string? terminalKey = NormalizeTerminalCode(terminalRaw);
                int? terminalId = null;
                if (terminalKey != null && maps.Terminal.TryGetValue(terminalKey, out int tid))
                {
                    terminalId = tid;
                }
                else if (maps.TerminalLegacyMap.TryGetValue(terminalRaw, out var termInfo))
                {
                    terminalId = termInfo.TerminalId;
                }

                if (terminalId == null)
                {
                    _importErrors.Add($"Tariff: Terminal {terminalRaw} not found. Skipping.");
                    continue;
                }

                if (!maps.Service.TryGetValue(serviceNum, out int serviceId) && !maps.ServiceLegacyMap.TryGetValue(GetString(record, "service"), out serviceId))
                {
                    _importErrors.Add($"Tariff: Service {serviceNum} not found. Skipping."); continue;
                }

                var asOfDate = ParseDateOnly(record, "date") ?? DateOnly.FromDateTime(DateTime.Today);
                string key = $"{asOfDate:yyyy-MM-dd}|{customerId}|{terminalId}|{serviceId}";
                if (maps.TariffRate.Contains(key)) { skipped++; continue; }

                var entity = new TariffRate
                {
                    AsOfDate = asOfDate,
                    CustomerId = customerId,
                    TerminalId = terminalId.Value,
                    ServiceId = serviceId,
                    PortId = terminalToPortMap.GetValueOrDefault(terminalId.Value),
                    Dispatch = ParseDecimal(record, "dispatch"),
                    BAF = ParseDecimal(record, "baf"),
                    CreatedBy = GetString(record, "createdby"),
                    CreatedDate = ParseDateTime(record, "createddat") ?? DateTimeHelper.GetCurrentPhilippineTime()
                };

                if (entity.PortId == 0) { _importErrors.Add($"Tariff: Port not found for terminal {terminalRaw}. Skipping."); continue; }

                newRecords.Add(entity);
                count++;
                maps.TariffRate.Add(key);
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MsapTariffRates.AddRangeAsync(newRecords);
            }

            return $"Tariff Rates: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportCollectionsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0, skipped = 0;
            var newRecords = new List<Collection>();

            foreach (var record in records)
            {
                string crNum = GetString(record, "crnum");
                if (maps.Collection.ContainsKey(crNum)) { skipped++; continue; }

                string custNo = PadNumber(GetString(record, "custno"), 4);
                if (!maps.Customer.TryGetValue(custNo, out int customerId) && !maps.CustomerLegacyMap.TryGetValue(GetString(record, "custno"), out customerId))
                {
                    _importErrors.Add($"Collection {crNum}: Customer {custNo} not found. Skipping.");
                    continue;
                }

                string checkDate = GetString(record, "checkdate");
                string bankCode = GetString(record, "bankacctco").TrimStart('0');
                if (string.IsNullOrEmpty(bankCode)) bankCode = "0";

                int? bankId = null;
                if (maps.BankAccount.TryGetValue(bankCode, out int bid))
                {
                    bankId = bid;
                }
                else if (maps.BankAccount.TryGetValue(GetString(record, "bankacctco"), out bid))
                {
                    bankId = bid;
                }

                var entity = new Collection
                {
                    MsapCollectionNumber = crNum,
                    CustomerId = customerId,
                    BankId = bankId,
                    CheckNumber = GetString(record, "checkno"),
                    Company = "MMSI",
                    CashAmount = 0,
                    WVAT = 0,
                    Date = ParseDateOnly(record, "crdate") ?? DateOnly.FromDateTime(DateTime.Today),
                    DepositDate = ParseDateOnly(record, "datedeposi"),
                    Amount = ParseDecimal(record, "amount"),
                    EWT = ParseDecimal(record, "n2307"),
                    IsUndocumented = ParseBool(record, "undocument"),
                    CreatedDate = ParseDateTime(record, "createddat") ?? DateTimeHelper.GetCurrentPhilippineTime(),
                    CreatedBy = GetString(record, "createdby"),
                    CheckDate = (checkDate != "/  /") ? ParseDateOnly(record, "checkdate") : null,
                };

                entity.CheckAmount = entity.Amount;
                entity.Total = entity.Amount + entity.EWT;

                newRecords.Add(entity);
                count++;
                maps.Collection[crNum] = -1; // Temp mark to avoid duplicates in same file

                if (newRecords.Count >= 500)
                {
                    await dbContext.MsapCollections.AddRangeAsync(newRecords);
                    await dbContext.SaveChangesAsync();
                    foreach (var r in newRecords)
                    {
                        maps.Collection[r.MsapCollectionNumber] = r.MsapCollectionId;
                    }

                    newRecords.Clear();
                }
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MsapCollections.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
                foreach (var r in newRecords)
                {
                    maps.Collection[r.MsapCollectionNumber] = r.MsapCollectionId;
                }
            }

            return $"Collections: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportCollectBillAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0;

            foreach (var record in records)
            {
                string billNum = GetString(record, "billnum");
                string crNum = GetString(record, "crnum");
                bool active = ParseBool(record, "active");

                if (!active || billNum == "-" || crNum == "-") continue;

                if (!maps.CollectBill.ContainsKey(billNum))
                {
                    maps.CollectBill[billNum] = new List<string>();
                }
                maps.CollectBill[billNum].Add(crNum);
                count++;
            }

            return $"Collect-Bill Links: {count} loaded.";
        }

        private async Task<string> ImportBillingsAsync(IFormFile file, ImportMaps maps)
        {
            // Clear existing data to fix scrambled IDs as requested by user
            await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE msap_dispatch_tickets RESTART IDENTITY CASCADE");
            await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE msap_billings RESTART IDENTITY CASCADE");
            // Clear maps to reflect empty tables
            maps.Billing.Clear();
            maps.BillingByRecId.Clear();
            maps.DispatchTicket.Clear();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r);
            int count = 0;
            var newRecords = new List<Billing>();
            var seenBillingKeys = new HashSet<string>();

            foreach (var record in records)
            {
                string billingNumber = GetString(record, "number");
                int legacyRecId = (int)ParseDecimal(record, "recid");

                // 1. Skip if RECID already exists (redundant but safe)
                if (maps.BillingByRecId.ContainsKey(legacyRecId.ToString())) {
                    continue; }

                var billingDate = ParseDateOnly(record, "date") ?? DateOnly.FromDateTime(DateTime.Today);
                int billingYear = billingDate.Year;
                string billingYearKey = $"{billingYear}|{billingNumber}";

                // 2. Skip if (year, number) already seen — same number in same year
                if (!seenBillingKeys.Add(billingYearKey))
                {
                    _importErrors.Add($"Billing {billingNumber} (RECID {legacyRecId}): Duplicate (year, number) found. Skipping.");
                    continue;
                }

                string vesselNumRaw = GetString(record, "vesselnum");
                string vesselNum = PadNumber(vesselNumRaw, 4);
                string terminalRaw = GetString(record, "terminal");
                string portNumRaw = PadNumber(GetString(record, "portnum"), 3);
                string principalNum = PadNumber(GetString(record, "billto"), 4);
                string crNum = GetString(record, "crnum");

                // If crNum is missing from billing.csv, check collect_bill map
                if ((crNum == "-" || crNum == "0" || string.IsNullOrWhiteSpace(crNum)) && maps.CollectBill.TryGetValue(billingNumber, out var crNums))
                {
                    crNum = crNums.FirstOrDefault() ?? "-";
                }

                string custNo = GetString(record, "custno");

                if (!maps.Customer.TryGetValue(custNo, out int customerId) && !maps.CustomerLegacyMap.TryGetValue(GetString(record, "custno"), out customerId))
                {
                    _importErrors.Add($"Billing {billingNumber}: Customer {custNo} not found. Skipping."); continue;
                }

                // Vessel lookup: try Number then Legacy ID (with/without padding)
                if (!maps.Vessel.TryGetValue(vesselNum, out int vesselId))
                {
                    if (!maps.VesselLegacyMap.TryGetValue(vesselNumRaw, out vesselId))
                    {
                        string unpadded = vesselNumRaw.TrimStart('0');
                        if (string.IsNullOrEmpty(unpadded))
                        {
                            unpadded = "0";
                        }

                        if (!maps.VesselLegacyMap.TryGetValue(unpadded, out vesselId))
                        {
                            _importErrors.Add($"Billing {billingNumber}: Vessel {vesselNumRaw} not found. Skipping.");
                            continue;
                        }
                    }
                }

                var (pId, tId) = ResolvePortTerminal(terminalRaw, portNumRaw, maps, useFallback: true);
                if (pId == null || tId == null) { _importErrors.Add($"Billing {billingNumber}: Port/Terminal resolution failed (TerminalRaw='{terminalRaw}', PortNumRaw='{portNumRaw}'). Skipping."); continue; }

                // Principal lookup using composite key (CustomerId|PrincipalNumber)
                int? principalId = maps.Principal.TryGetValue($"{customerId}|{principalNum}", out int pid) ? pid : null;
                int? collectionId = maps.Collection.TryGetValue(crNum, out int cid) ? cid : null;

                var entity = new Billing
                {
                    MsapBillingId = legacyRecId, // Force Legacy ID
                    MsapBillingNumber = billingNumber,
                    CustomerId = customerId,
                    VesselId = vesselId,
                    PortId = pId.Value,
                    TerminalId = tId.Value,
                    PrincipalId = principalId,
                    IsPrincipal = principalId.HasValue,
                    BilledTo = principalId.HasValue ? "PRINCIPAL" : "LOCAL",
                    CollectionId = collectionId,
                    CollectionNumber = crNum != "-" ? crNum : null,
                    Status = collectionId.HasValue ? "Collected" : "For Collection",
                    Year = billingDate.Year,
                    Date = billingDate,
                    DueDate = billingDate,
                    Amount = ParseDecimal(record, "amount"),
                    Balance = collectionId.HasValue ? 0 : ParseDecimal(record, "amount"),
                    AmountPaid = collectionId.HasValue ? ParseDecimal(record, "amount") : 0,
                    IsPaid = collectionId.HasValue,
                    Company = "MMSI",
                    IsUndocumented = ParseBool(record, "undocument"),
                    ApOtherTug = ParseDecimal(record, "apothertug"),
                    DispatchAmount = ParseDecimal(record, "dispatcham"),
                    BAFAmount = ParseDecimal(record, "bafamount"),
                    Discount = 0,
                    IsVatable = ParseBool(record, "vat"),
                    IsPrinted = ParseBool(record, "printed"),
                    VoyageNumber = NullIfDash(GetString(record, "voyage")),
                    CreatedDate = ParseDateTime(record, "entrydate") ?? DateTimeHelper.GetCurrentPhilippineTime(),
                    CreatedBy = GetString(record, "entryby"),
                };

                newRecords.Add(entity);
                count++;
                var tempInfo = new BillingMapInfo { Id = legacyRecId, PortId = pId.Value, TerminalId = tId.Value };
                maps.Billing[billingNumber] = tempInfo;
                maps.BillingByRecId[legacyRecId.ToString()] = tempInfo;

                if (newRecords.Count >= 500)
                {
                    await dbContext.MsapBillings.AddRangeAsync(newRecords);
                    await dbContext.SaveChangesAsync();
                    newRecords.Clear();
                }
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MsapBillings.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
            }

            // Sync sequence after manual ID insertion
            await dbContext.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('msap_billings', 'RECID'), COALESCE(MAX(\"RECID\"), 1)) FROM msap_billings");

            return $"Billings: {count} imported (Tables cleared first).";
        }

        private async Task<string> ImportDispatchTicketsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().Select(r => (IDictionary<string, object?>)r).ToList();
            int count = 0, skipped = 0;
            var newRecords = new List<DispatchTicket>();

            foreach (var record in records)
            {
                string dispatchNo = GetString(record, "number");
                int legacyRecId = (int)ParseDecimal(record, "recid");
                if (maps.DispatchTicket.Contains(legacyRecId.ToString())) { skipped++; continue; }

                string custNoRaw = GetString(record, "custno");
                string custNo = (custNoRaw == "-" || string.IsNullOrWhiteSpace(custNoRaw)) ? "0000" : PadNumber(custNoRaw, 4);
                string tugboatNum = PadNumber(GetString(record, "tugnum"), 3);
                string vesselNumRaw = GetString(record, "vesselnum");
                string vesselNum = PadNumber(vesselNumRaw, 4);
                string serviceNum = PadNumber(GetString(record, "srvctype"), 3);
                string portNumRaw = PadNumber(GetString(record, "portnum"), 3);
                string terminalRaw = GetString(record, "terminal");
                string billNumStr = GetString(record, "billnum");
                string masterNoRaw = GetString(record, "masterno");

                if (!maps.Customer.TryGetValue(custNo, out int customerId))
                {
                    if (!maps.CustomerLegacyMap.TryGetValue(custNoRaw, out customerId))
                    {
                        string unpadded = custNoRaw.TrimStart('0');
                        if (string.IsNullOrEmpty(unpadded))
                        {
                            unpadded = "0";
                        }

                        if (!maps.CustomerLegacyMap.TryGetValue(unpadded, out customerId))
                        {
                            if (custNo == "0000")
                            {
                                var unknown = await dbContext.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "0000" && c.Company == "MMSI");
                                if (unknown == null)
                                {
                                    unknown = new Customer
                                    {
                                        CustomerCode = "0000",
                                        CustomerName = "-",
                                        CustomerAddress = "-",
                                        CustomerTin = "000-000-000-00000",
                                        CustomerTerms = "COD",
                                        CustomerType = "Industrial",
                                        VatType = "Vatable",
                                        ZipCode = "0000",
                                        Company = "MMSI",
                                        CreatedBy = "System Import Fallback",
                                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                                    };
                                    dbContext.Customers.Add(unknown);
                                    await dbContext.SaveChangesAsync();
                                }
                                maps.Customer["0000"] = unknown.CustomerId;
                                customerId = unknown.CustomerId;
                            }
                            else
                            {
                                _importErrors.Add($"Dispatch {dispatchNo}: Customer {custNoRaw} not found. Skipping.");
                                continue;
                            }
                        }
                    }
                }

                if (!maps.Tugboat.TryGetValue(tugboatNum, out int tugboatId) && !maps.TugboatLegacyMap.TryGetValue(GetString(record, "tugnum"), out tugboatId))
                {
                    _importErrors.Add($"Dispatch {dispatchNo}: Tugboat {tugboatNum} not found. Skipping."); continue;
                }

                // Vessel lookup: try Number then Legacy ID (with/without padding)
                if (!maps.Vessel.TryGetValue(vesselNum, out int vesselId))
                {
                    if (!maps.VesselLegacyMap.TryGetValue(vesselNumRaw, out vesselId))
                    {
                        string unpadded = vesselNumRaw.TrimStart('0');
                        if (string.IsNullOrEmpty(unpadded))
                        {
                            unpadded = "0";
                        }

                        if (!maps.VesselLegacyMap.TryGetValue(unpadded, out vesselId))
                        {
                            _importErrors.Add($"Dispatch {dispatchNo}: Vessel {vesselNumRaw} not found. Skipping.");
                            continue;
                        }
                    }
                }

                if (!maps.Service.TryGetValue(serviceNum, out int serviceId) && !maps.ServiceLegacyMap.TryGetValue(GetString(record, "srvctype"), out serviceId))
                {
                    _importErrors.Add($"Dispatch {dispatchNo}: Service {serviceNum} not found. Skipping."); continue;
                }

                var (pId, tId) = ResolvePortTerminal(terminalRaw, portNumRaw, maps, useFallback: true);

                if ((pId == null || tId == null) && billNumStr != "-")
                {
                    if (maps.Billing.TryGetValue(billNumStr, out var bInfo) || maps.BillingByRecId.TryGetValue(billNumStr, out bInfo))
                    {
                        pId ??= bInfo.PortId;
                        tId ??= bInfo.TerminalId;
                    }
                }

                if (pId == null || tId == null) { _importErrors.Add($"Dispatch {dispatchNo}: Port/Terminal not found (TerminalRaw='{terminalRaw}', PortNumRaw='{portNumRaw}', BillNum='{billNumStr}'). Skipping."); continue; }

                BillingMapInfo? info = null;
                if (billNumStr != "-")
                {
                    maps.Billing.TryGetValue(billNumStr, out info);
                    info ??= maps.BillingByRecId.GetValueOrDefault(billNumStr);
                }
                int? billingId = info?.Id == -1 ? null : info?.Id;

                // Resolve optional TugMaster (MASTERNO) not required, so skip if not found
                int? tugMasterId = null;
                if (masterNoRaw != "-" && !string.IsNullOrWhiteSpace(masterNoRaw))
                {
                    string masterNumPadded = PadNumber(masterNoRaw, 4);
                    if (maps.TugMaster.TryGetValue(masterNumPadded, out int tmId)
                        || maps.TugMasterLegacyMap.TryGetValue(masterNoRaw, out tmId))
                    {
                        tugMasterId = tmId;
                    }
                }

                var entity = new DispatchTicket
                {
                    DispatchTicketId = legacyRecId, // Force Legacy ID
                    DispatchNumber = dispatchNo,
                    CustomerId = customerId,
                    TugBoatId = tugboatId,
                    TugMasterId = tugMasterId,
                    VesselId = vesselId,
                    ServiceId = serviceId,
                    PortId = pId.Value,
                    TerminalId = tId.Value,
                    BillingId = billingId,
                    BaseOrStation = NullIfDash(GetString(record, "base")),
                    ApOtherTugs = ParseDecimal(record, "apothertug"),
                    DispatchChargeType = ParseBool(record, "perhour") ? "Per hour" : "Per move",
                    BAFChargeType = "Per move",
                    COSNumber = NullIfDash(GetString(record, "cosno")),
                    VoyageNumber = NullIfDash(GetString(record, "voyage")),
                    Remarks = NullIfDash(GetString(record, "remarks")),
                    DateLeft = ParseDateOnly(record, "dateleft"),
                    TimeLeft = ParseTimeOnly(record, "timeleft"),
                    DateArrived = ParseDateOnly(record, "datearrive"),
                    TimeArrived = ParseTimeOnly(record, "timearrive"),
                    Date = ParseDateOnly(record, "date") ?? DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()),
                    CreatedBy = NullIfDash(GetString(record, "entryby")) ?? $"Import: {GetUserFullName()}",
                    CreatedDate = ParseDateTime(record, "entrydate") ?? DateTimeHelper.GetCurrentPhilippineTime(),
                    Status = ParseBool(record, "approved")
                        ? (billNumStr != "-" ? "Billed" : "For Billing")
                        : "For Tariff",
                    DispatchRate = ParseDecimal(record, "dispatchra"),
                    DispatchBillingAmount = ParseDecimal(record, "dispatchbi"),
                    DispatchDiscount = ParseDecimal(record, "dispatchr2"),
                    DispatchNetRevenue = ParseDecimal(record, "dispatchne"),
                    BAFRate = ParseDecimal(record, "bafrate"),
                    BAFBillingAmount = ParseDecimal(record, "bafbillamt"),
                    BAFDiscount = ParseDecimal(record, "bafrateadj"),
                    BAFNetRevenue = ParseDecimal(record, "bafnetamt"),
                };

                entity.TotalBilling = entity.DispatchBillingAmount + entity.BAFBillingAmount;
                entity.TotalNetRevenue = entity.DispatchNetRevenue + entity.BAFNetRevenue;
                entity.TotalHours = ComputeTotalHours(entity, entity.CustomerId);

                newRecords.Add(entity);
                count++;
                maps.DispatchTicket.Add(legacyRecId.ToString());

                if (newRecords.Count >= 500)
                {
                    await dbContext.MsapDispatchTickets.AddRangeAsync(newRecords);
                    await dbContext.SaveChangesAsync();
                    newRecords.Clear();
                }
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MsapDispatchTickets.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
            }

            // Sync sequence after manual ID insertion
            await dbContext.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('msap_dispatch_tickets', 'RECID'), COALESCE(MAX(\"RECID\"), 1)) FROM msap_dispatch_tickets");

            return $"Dispatch Tickets: {count} imported, {skipped} already existed.";
        }

        // -------------------------------------------------------------------------
        // Shared helpers
        // -------------------------------------------------------------------------

        private static (int? PortId, int? TerminalId) ResolvePortTerminal(
            string terminalRaw, string portNumRaw, ImportMaps maps, bool useFallback = false)
        {
            // 1. Try by Terminal Code (PPP-TTT or PPPTTT)
            string? terminalKey = NormalizeTerminalCode(terminalRaw);
            if (terminalKey != null)
            {
                string portPart = terminalKey.Substring(0, 3);
                int? portId = maps.Port.TryGetValue(portPart, out int pid) ? pid : null;
                int? terminalId = maps.Terminal.TryGetValue(terminalKey, out int tid) ? tid : null;
                if (portId != null && terminalId != null)
                {
                    return (portId, terminalId);
                }
            }

            if (portNumRaw != "-")
            {
                // 2. Try as Port Number (padded e.g. "005")
                if (maps.Port.TryGetValue(portNumRaw, out int pId))
                {
                    int? tId = null;
                    if (useFallback)
                    {
                        maps.PortToFirstTerminal.TryGetValue(pId, out int firstTid);
                        if (firstTid != 0)
                        {
                            tId = firstTid;
                        }
                        else if (portNumRaw == "012" && maps.Port.TryGetValue("001", out int bId))
                        {
                            // Fallback for the duplicate Batangas port that has no terminals
                            maps.PortToFirstTerminal.TryGetValue(bId, out int bTid);
                            if (bTid != 0)
                            {
                                return (bId, bTid); // Redirect to the original Batangas port/terminal
                            }
                        }
                    }
                    if (tId != null || !useFallback) return (pId, tId);
                }

                // 3. Try as Port Legacy ID (from port.csv RECID)
                if (maps.PortLegacyMap.TryGetValue(portNumRaw.TrimStart('0'), out int plId))
                {
                    int? tId = null;
                    if (useFallback)
                    {
                        maps.PortToFirstTerminal.TryGetValue(plId, out int firstTid);
                        if (firstTid != 0)
                        {
                            tId = firstTid;
                        }
                    }
                    return (plId, tId);
                }

                // 4. Try as Terminal Legacy ID (from terminal.csv RECID)
                string unpadded = portNumRaw.TrimStart('0');
                if (string.IsNullOrEmpty(unpadded))
                {
                    unpadded = "0";
                }

                if (maps.TerminalLegacyMap.TryGetValue(unpadded, out var termInfo))
                {
                    return (termInfo.PortId, termInfo.TerminalId);
                }
            }

            if (useFallback)
            {
                // Try Davao (005) as a preferred default
                if (maps.Port.TryGetValue("005", out int davaoId))
                {
                    maps.PortToFirstTerminal.TryGetValue(davaoId, out int firstTid);
                    return (davaoId, firstTid != 0 ? firstTid : null);
                }

                // Ultimate fallback: first available port and its first terminal
                var firstPortId = maps.Port.Values.FirstOrDefault(id => id != 0);
                if (firstPortId != 0)
                {
                    maps.PortToFirstTerminal.TryGetValue(firstPortId, out int firstTid);
                    return (firstPortId, firstTid != 0 ? firstTid : null);
                }
            }

            return (null, null);
        }

        private static decimal ComputeTotalHours(DispatchTicket dt, int customerId)
        {
            if (dt.DateLeft == null || dt.DateArrived == null
                || dt.TimeLeft == null || dt.TimeArrived == null)
            {
                return 0;
            }

            var dtl       = dt.DateLeft.Value.ToDateTime(dt.TimeLeft.Value);
            var dta       = dt.DateArrived.Value.ToDateTime(dt.TimeArrived.Value);
            var totalHours = Math.Round((decimal)(dta - dtl).TotalHours, 2);

            // Customer 179 uses quarter-hour rounding rules
            if (customerId == 179)
            {
                var whole      = Math.Truncate(totalHours);
                var fractional = totalHours - whole;
                totalHours = fractional >= 0.75m ? whole + 1.0m
                           : fractional >= 0.25m ? whole + 0.5m
                           : whole;
            }

            return totalHours;
        }

        private static IFormFile? ResolveFile(
            IFormFile? individual, List<IFormFile>? bulk, string keyword, string? exactName = null)
        {
            if (individual != null)
            {
                return individual;
            }

            if (bulk == null || bulk.Count == 0)
            {
                return null;
            }

            if (exactName != null)
            {
                var exact = bulk.FirstOrDefault(f =>
                    f.FileName.Equals(exactName, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                {
                    return exact;
                }
            }

            var byCsv = bulk.FirstOrDefault(f =>
                f.FileName.Equals($"{keyword}.csv", StringComparison.OrdinalIgnoreCase));
            if (byCsv != null)
            {
                return byCsv;
            }

            return bulk.FirstOrDefault(f =>
                f.FileName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetString(IDictionary<string, object?>? record, string propertyName)
        {
            if (record == null) return "-";
            var targetKey = propertyName.Trim().ToLower();

            foreach (var kvp in record)
            {
                if (kvp.Key.Trim().Equals(targetKey, StringComparison.OrdinalIgnoreCase))
                {
                    var str = kvp.Value?.ToString()?.Trim();
                    return string.IsNullOrWhiteSpace(str) ? "-" : str;
                }
            }

            return "-";
        }

        private static bool ParseBool(IDictionary<string, object?>? record, string propertyName)
        {
            var val = GetString(record, propertyName).ToLower();
            return val == "t" || val == "true";
        }

        private static decimal ParseDecimal(IDictionary<string, object?>? record, string propertyName)
        {
            var val = GetString(record, propertyName);
            return decimal.TryParse(val, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result)
                ? result : 0;
        }

        private static DateOnly? ParseDateOnly(IDictionary<string, object?>? record, string propertyName)
        {
            var val = GetString(record, propertyName);
            return DateOnly.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly result)
                ? result : null;
        }

        private static DateTime? ParseDateTime(IDictionary<string, object?>? record, string propertyName)
        {
            var val = GetString(record, propertyName);
            return DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result)
                ? result : null;
        }

        private static TimeOnly? ParseTimeOnly(IDictionary<string, object?>? record, string propertyName)
        {
            var val = GetString(record, propertyName).Trim();

            // Handle legacy compact format: "330" 03:30, "1915" 19:15
            if (int.TryParse(val, out int _) && val.Length <= 4)
            {
                int hours = 0, minutes;
                if      (val.Length == 4) { hours = int.Parse(val.Substring(0, 2)); minutes = int.Parse(val.Substring(2, 2)); }
                else if (val.Length == 3) { hours = int.Parse(val.Substring(0, 1)); minutes = int.Parse(val.Substring(1, 2)); }
                else                      { minutes = int.Parse(val); }

                if (hours is >= 0 and < 24 && minutes is >= 0 and < 60)
                {
                    return new TimeOnly(hours, minutes);
                }
            }

            return TimeOnly.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly result)
                ? result : null;
        }

        private static string PadNumber(string? value, int width)
        {
            if (string.IsNullOrEmpty(value) || value == "-")
            {
                return value ?? string.Empty;
            }

            return int.TryParse(value, out var n)
                ? n.ToString($"D{width}")
                : value.PadLeft(width, '0');
        }

        private static string? NormalizeTerminalCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-")
            {
                return null;
            }

            if (value.Length < 6)
            {
                return null;
            }

            string portPart = PadNumber(value.Substring(0, 3), 3);
            string terminalPart = PadNumber(value.Substring(value.Length - 3, 3), 3);
            return $"{portPart}{terminalPart}";
        }

        private static string MapTerms(string raw) => raw switch
        {
            "7"  => "7D",
            "15" => "15D",
            "30" => "30D",
            "60" => "60D",
            _    => "COD"
        };

        /// <summary>
        /// Returns the fallback value when the field is the "-" sentinel,
        /// used only for fields that require a specific format (TIN).
        /// </summary>
        private static string NormalizeTin(string value, string fallback) =>
            value == "-" ? fallback : value;

        /// <summary>Returns null for "-" sentinel on optional string fields.</summary>
        private static string? NullIfDash(string value) =>
            value == "-" ? null : value;

        private static string JoinAddress(string a1, string a2, string a3)
        {
            var joined = $"{a1} {a2} {a3}".Replace("-", "").Trim();
            return string.IsNullOrWhiteSpace(joined) ? "-" : joined;
        }
    }
}

