using System.Reflection;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace IBS.Services.Msap
{
    public class SuperAdminService(
        IUnitOfWork unitOfWork,
        ILogger<SuperAdminService> logger)
    {
        private static readonly HashSet<string> _immutableProps =
        [
            nameof(BaseEntity.CreatedBy), nameof(BaseEntity.CreatedDate),
            nameof(BaseEntity.EditedBy), nameof(BaseEntity.EditedDate),
            nameof(BaseEntity.CanceledBy), nameof(BaseEntity.CanceledDate),
            nameof(BaseEntity.CancellationRemarks),
            nameof(BaseEntity.VoidedBy), nameof(BaseEntity.VoidedDate),
            nameof(BaseEntity.PostedBy), nameof(BaseEntity.PostedDate),
            "UnpostedBy", "UnpostedDate", "UnpostRemarks",
            "BillingId", "Billing",
            "CollectionId", "Collection", "CollectionNumber",
        ];

        public string[] SupportedTables => ["JobOrder", "DispatchTicket", "Billing", "Collection"];

        public string DisplayName(string table) => table switch
        {
            "JobOrder" => "Job Orders",
            "DispatchTicket" => "Dispatch Tickets",
            "Billing" => "Billings",
            "Collection" => "Collections",
            _ => table
        };

        public string IdColumn(string table) => table switch
        {
            "JobOrder" => "JobOrderId",
            "DispatchTicket" => "DispatchTicketId",
            "Billing" => "MsapBillingId",
            "Collection" => "MsapCollectionId",
            _ => "Id"
        };

        public string ReferenceColumn(string table) => table switch
        {
            "JobOrder" => "JobOrderNumber",
            "DispatchTicket" => "DispatchNumber",
            "Billing" => "MsapBillingNumber",
            "Collection" => "MsapCollectionNumber",
            _ => "Id"
        };

        public List<TableColumnDef> GetColumns(string table)
        {
            return table switch
            {
                "JobOrder" =>
                [
                    new("JobOrderId", "ID"),
                    new("JobOrderNumber", "Job Order #"),
                    new("Date", "Date"),
                    new("Status", "Status"),
                    new("CustomerId", "Customer"),
                    new("VesselId", "Vessel"),
                    new("PortId", "Port"),
                    new("TerminalId", "Terminal"),
                    new("COSNumber", "COS #"),
                    new("VoyageNumber", "Voyage #"),
                    new("PlannedStartTime", "Planned Start"),
                    new("PlannedEndTime", "Planned End"),
                    new("RequiredTugCount", "Tug Count"),
                    new("Remarks", "Remarks"),
                ],

                "DispatchTicket" =>
                [
                    new("DispatchTicketId", "ID"),
                    new("DispatchNumber", "Ticket #"),
                    new("Date", "Date"),
                    new("Status", "Status"),
                    new("CustomerId", "Customer"),
                    new("VesselId", "Vessel"),
                    new("PortId", "Port"),
                    new("TerminalId", "Terminal"),
                    new("TugBoatId", "Tugboat"),
                    new("ServiceId", "Service"),
                    new("DateLeft", "Date Left"),
                    new("DateArrived", "Date Arrived"),
                    new("TotalHours", "Total Hours"),
                    new("DispatchRate", "Dispatch Rate"),
                    new("DispatchBillingAmount", "Dispatch Amt"),
                    new("BAFRate", "BAF Rate"),
                    new("BAFBillingAmount", "BAF Amt"),
                    new("TotalBilling", "Total Billing"),
                    new("TotalNetRevenue", "Net Revenue"),
                    new("Remarks", "Remarks"),
                ],

                "Billing" =>
                [
                    new("MsapBillingId", "ID"),
                    new("MsapBillingNumber", "Billing #"),
                    new("Date", "Date"),
                    new("Status", "Status"),
                    new("CustomerId", "Customer"),
                    new("VesselId", "Vessel"),
                    new("PortId", "Port"),
                    new("TerminalId", "Terminal"),
                    new("Amount", "Amount"),
                    new("Balance", "Balance"),
                    new("IsPaid", "Paid?"),
                    new("DueDate", "Due Date"),
                    new("VoyageNumber", "Voyage #"),
                    new("IsVatable", "Vatable?"),
                    new("PrintWht", "Print WHT?"),
                    new("Discount", "Discount"),
                ],

                "Collection" =>
                [
                    new("MsapCollectionId", "ID"),
                    new("MsapCollectionNumber", "CR #"),
                    new("Date", "Date"),
                    new("CustomerId", "Customer"),
                    new("CashAmount", "Cash"),
                    new("CheckAmount", "Check"),
                    new("CheckNumber", "Check #"),
                    new("Amount", "Amount"),
                    new("EWT", "EWT"),
                    new("WVAT", "WVAT"),
                    new("Total", "Total"),
                    new("Remarks", "Remarks"),
                    new("DepositDate", "Deposit Date"),
                ],

                _ => []
            };
        }

        public List<FieldDefinition> GetEditableFields(string table)
        {
            return table switch
            {
                "JobOrder" =>
                [
                    new("JobOrderNumber", "Job Order #", "string", true),
                    new("Date", "Date", "date", true),
                    new("Status", "Status", "select", true,
                        Options: [new("Open", "Open"), new("Closed", "Closed")]),
                    new("CustomerId", "Customer", "select_lookup", true, "Customer"),
                    new("VesselId", "Vessel", "select_lookup", true, "Vessel"),
                    new("PortId", "Port", "select_lookup", true, "Port"),
                    new("TerminalId", "Terminal", "select_lookup", true, "Terminal"),
                    new("COSNumber", "COS #", "string"),
                    new("VoyageNumber", "Voyage #", "string"),
                    new("Remarks", "Remarks", "textarea"),
                    new("PlannedStartTime", "Planned Start", "datetime"),
                    new("PlannedEndTime", "Planned End", "datetime"),
                    new("RequiredTugCount", "Tug Count", "number"),
                    new("PreferredTugboatId", "Preferred Tugboat", "select_lookup", LookupKey: "Tugboat"),
                ],

                "DispatchTicket" =>
                [
                    new("DispatchNumber", "Ticket #", "string", true),
                    new("Date", "Date", "date", true),
                    new("Status", "Status", "select", true,
                        Options: SD.DispatchTicketStatus.All.Select(s => new SelectListItem(s, s)).ToList()),
                    new("CustomerId", "Customer", "select_lookup", true, "Customer"),
                    new("VesselId", "Vessel", "select_lookup", true, "Vessel"),
                    new("PortId", "Port", "select_lookup", true, "Port"),
                    new("TerminalId", "Terminal", "select_lookup", true, "Terminal"),
                    new("TugBoatId", "Tugboat", "select_lookup", true, "Tugboat"),
                    new("ServiceId", "Service", "select_lookup", true, "Service"),
                    new("DateLeft", "Date Left", "date"),
                    new("DateArrived", "Date Arrived", "date"),
                    new("TimeLeft", "Time Left", "time"),
                    new("TimeArrived", "Time Arrived", "time"),
                    new("TotalHours", "Total Hours", "number"),
                    new("DispatchRate", "Dispatch Rate", "number"),
                    new("DispatchBillingAmount", "Dispatch Amount", "number"),
                    new("DispatchDiscount", "Dispatch Discount", "number"),
                    new("BAFRate", "BAF Rate", "number"),
                    new("BAFBillingAmount", "BAF Amount", "number"),
                    new("BAFDiscount", "BAF Discount", "number"),
                    new("TotalBilling", "Total Billing", "number"),
                    new("TotalNetRevenue", "Net Revenue", "number"),
                    new("ApOtherTugs", "AP Other Tugs", "number"),
                    new("Remarks", "Remarks", "textarea"),
                    new("JobOrderId", "Job Order", "select_lookup", LookupKey: "JobOrder"),
                ],

                "Billing" =>
                [
                    new("MsapBillingNumber", "Billing #", "string", true),
                    new("Date", "Date", "date", true),
                    new("Status", "Status", "select", true,
                        Options: [new("For Posting", "For Posting"), new("For Collection", "For Collection"), new("Collected", "Collected")]),
                    new("CustomerId", "Customer", "select_lookup", true, "Customer"),
                    new("VesselId", "Vessel", "select_lookup", true, "Vessel"),
                    new("PortId", "Port", "select_lookup", true, "Port"),
                    new("TerminalId", "Terminal", "select_lookup", true, "Terminal"),
                    new("Amount", "Amount", "number"),
                    new("Balance", "Balance", "number"),
                    new("IsPaid", "Is Paid?", "boolean"),
                    new("IsVatable", "Is Vatable?", "boolean"),
                    new("IsVatInclusive", "VAT Inclusive?", "boolean"),
                    new("PrintWht", "Print WHT?", "boolean"),
                    new("Discount", "Discount", "number"),
                    new("DueDate", "Due Date", "date"),
                    new("Terms", "Terms", "string"),
                    new("VoyageNumber", "Voyage #", "string"),
                    new("COSNumber", "COS #", "string"),
                    new("ApOtherTug", "AP Other Tug", "number"),
                    new("JobOrderId", "Job Order", "select_lookup", LookupKey: "JobOrder"),
                    new("PrincipalId", "Principal", "select_lookup", LookupKey: "Principal"),
                ],

                "Collection" =>
                [
                    new("MsapCollectionNumber", "CR #", "string", true),
                    new("Date", "Date", "date", true),
                    new("CustomerId", "Customer", "select_lookup", true, "Customer"),
                    new("CashAmount", "Cash Amount", "number"),
                    new("CheckAmount", "Check Amount", "number"),
                    new("CheckNumber", "Check #", "string"),
                    new("CheckBank", "Bank", "string"),
                    new("CheckBranch", "Branch", "string"),
                    new("CheckDate", "Check Date", "date"),
                    new("BankId", "Bank Account", "select_lookup", LookupKey: "BankAccount"),
                    new("Amount", "Amount", "number"),
                    new("EWT", "EWT", "number"),
                    new("WVAT", "WVAT", "number"),
                    new("Total", "Total", "number"),
                    new("Remarks", "Remarks", "textarea"),
                    new("DepositDate", "Deposit Date", "date"),
                    new("IsPrinted", "Is Printed?", "boolean"),
                    new("IsUndocumented", "Undocumented?", "boolean"),
                    new("ReferenceNo", "Reference #", "string"),
                ],

                _ => []
            };
        }

        public async Task<(IEnumerable<Dictionary<string, object?>> Data, int Total)> GetDataAsync(
            string table, int skip, int take, string? search,
            string? sortColumn, string? sortDir,
            CancellationToken ct)
        {
            sortDir ??= "asc";

            switch (table)
            {
                case "JobOrder":
                {
                    System.Linq.Expressions.Expression<Func<JobOrder, bool>>? filter = string.IsNullOrWhiteSpace(search) ? null : j =>
                        j.JobOrderNumber.Contains(search) ||
                        (j.COSNumber != null && j.COSNumber.Contains(search)) ||
                        (j.VoyageNumber != null && j.VoyageNumber.Contains(search)) ||
                        (j.Remarks != null && j.Remarks.Contains(search));
                    var (items, total) = await unitOfWork.JobOrder.GetPagedAsync(filter, sortColumn, sortDir, skip, take, ct);
                    return (items.Select(MapJobOrder), total);
                }
                case "DispatchTicket":
                {
                    System.Linq.Expressions.Expression<Func<DispatchTicket, bool>>? filter = string.IsNullOrWhiteSpace(search) ? null : d =>
                        d.DispatchNumber.Contains(search) ||
                        (d.Remarks != null && d.Remarks.Contains(search));
                    var (items, total) = await unitOfWork.DispatchTicket.GetPagedAsync(filter, sortColumn, sortDir, skip, take, ct);
                    return (items.Select(MapDispatchTicket), total);
                }
                case "Billing":
                {
                    System.Linq.Expressions.Expression<Func<Billing, bool>>? filter = string.IsNullOrWhiteSpace(search) ? null : b =>
                        b.MsapBillingNumber.Contains(search) ||
                        (b.VoyageNumber != null && b.VoyageNumber.Contains(search)) ||
                        (b.COSNumber != null && b.COSNumber.Contains(search));
                    var (items, total) = await unitOfWork.Billing.GetPagedAsync(filter, sortColumn, sortDir, skip, take, ct);
                    return (items.Select(MapBilling), total);
                }
                case "Collection":
                {
                    System.Linq.Expressions.Expression<Func<Collection, bool>>? filter = string.IsNullOrWhiteSpace(search) ? null : c =>
                        c.MsapCollectionNumber.Contains(search) ||
                        (c.CheckNumber != null && c.CheckNumber.Contains(search)) ||
                        (c.Remarks != null && c.Remarks.Contains(search));
                    var (items, total) = await unitOfWork.Collection.GetPagedAsync(filter, sortColumn, sortDir, skip, take, ct);
                    return (items.Select(MapCollection), total);
                }
                default:
                    return ([], 0);
            }
        }

        public async Task<Dictionary<string, object?>?> GetRecordAsync(string table, int id, CancellationToken ct)
        {
            return table switch
            {
                "JobOrder" => MapJobOrder(await unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == id, ct)),
                "DispatchTicket" => MapDispatchTicket(await unitOfWork.DispatchTicket.GetAsync(d => d.DispatchTicketId == id, ct)),
                "Billing" => MapBilling(await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, ct)),
                "Collection" => MapCollection(await unitOfWork.Collection.GetAsync(c => c.MsapCollectionId == id, ct)),
                _ => null
            };
        }

        public async Task<ServiceResult> SaveAsync(
            string table, int id, Dictionary<string, string> changes,
            string remarks, string username, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(remarks) || remarks.Length < 10)
            {
                return ServiceResult.Failure("Remarks are required (min 10 characters).");
            }

            try
            {
                ServiceResult? result = null;
                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    result = table switch
                    {
                        "JobOrder" => await SaveJobOrderAsync(id, changes, username, ct),
                        "DispatchTicket" => await SaveDispatchTicketAsync(id, changes, username, ct),
                        "Billing" => await SaveBillingAsync(id, changes, username, ct),
                        "Collection" => await SaveCollectionAsync(id, changes, username, ct),
                        _ => ServiceResult.Failure($"Unknown table: {table}")
                    };

                    if (result!.IsSuccess)
                    {
                        await unitOfWork.SaveAsync(ct);
                    }
                }, ct);

                return result!;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SuperAdmin Save failed for {Table}#{Id}", table, id);
                return ServiceResult.Failure($"Save failed: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        private async Task<ServiceResult> SaveJobOrderAsync(int id, Dictionary<string, string> changes, string username, CancellationToken ct)
        {
            var entity = await unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == id, ct);
            return await ApplyChangesAsync(entity, entity?.JobOrderNumber, "Job Order", id, changes, username, ct);
        }

        private async Task<ServiceResult> SaveDispatchTicketAsync(int id, Dictionary<string, string> changes, string username, CancellationToken ct)
        {
            var entity = await unitOfWork.DispatchTicket.GetAsync(d => d.DispatchTicketId == id, ct);
            return await ApplyChangesAsync(entity, entity?.DispatchNumber, "Dispatch Ticket", id, changes, username, ct);
        }

        private async Task<ServiceResult> SaveBillingAsync(int id, Dictionary<string, string> changes, string username, CancellationToken ct)
        {
            var entity = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, ct);
            return await ApplyChangesAsync(entity, entity?.MsapBillingNumber, "Billing", id, changes, username, ct);
        }

        private async Task<ServiceResult> SaveCollectionAsync(int id, Dictionary<string, string> changes, string username, CancellationToken ct)
        {
            var entity = await unitOfWork.Collection.GetAsync(c => c.MsapCollectionId == id, ct);
            return await ApplyChangesAsync(entity, entity?.MsapCollectionNumber, "Collection", id, changes, username, ct);
        }

        public async Task<List<SelectListItem>> GetLookupAsync(string lookupKey, CancellationToken ct)
        {
            return lookupKey switch
            {
                "Customer" => await unitOfWork.GetCustomerListAsyncById(ct),
                "Vessel" => (await unitOfWork.Vessel.GetAllAsync(null, ct))
                    .Select(v => new SelectListItem($"{v.VesselName} ({v.VesselNumber})", v.VesselId.ToString())).ToList(),
                "Port" => (await unitOfWork.Port.GetAllAsync(null, ct))
                    .Select(p => new SelectListItem(p.PortName, p.PortId.ToString())).ToList(),
                "Terminal" => (await unitOfWork.Terminal.GetAllAsync(null, ct))
                    .Select(t => new SelectListItem(t.TerminalName, t.TerminalId.ToString())).ToList(),
                "Tugboat" => (await unitOfWork.Tugboat.GetAllAsync(null, ct))
                    .Select(t => new SelectListItem($"{t.TugboatName} ({t.TugboatNumber})", t.TugboatId.ToString())).ToList(),
                "Service" => (await unitOfWork.Service.GetAllAsync(null, ct))
                    .Select(s => new SelectListItem(s.ServiceName, s.ServiceId.ToString())).ToList(),
                "JobOrder" => (await unitOfWork.JobOrder.GetAllAsync(null, ct))
                    .Select(j => new SelectListItem(j.JobOrderNumber, j.JobOrderId.ToString())).ToList(),
                "Principal" => (await unitOfWork.Principal.GetAllAsync(null, ct))
                    .Select(p => new SelectListItem(p.PrincipalName, p.PrincipalId.ToString())).ToList(),
                "BankAccount" => (await unitOfWork.BankAccount.GetAllAsync(null, ct))
                    .Select(b => new SelectListItem($"{b.Bank} - {b.AccountName}", b.BankAccountId.ToString())).ToList(),
                _ => []
            };
        }

        private async Task<ServiceResult> ApplyChangesAsync(
            object? entity, string? refVal,
            string documentType, int id,
            Dictionary<string, string> changes, string username,
            CancellationToken ct)
        {
            if (entity == null)
            {
                return ServiceResult.Failure($"{documentType} not found.", ServiceResultStatus.NotFound);
            }

            var auditEntries = new List<AuditTrail>();
            var type = entity.GetType();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p is { CanRead: true, CanWrite: true })
                .ToDictionary(p => p.Name, p => p);

            foreach (var (key, rawValue) in changes)
            {
                if (!props.TryGetValue(key, out var prop))
                {
                    continue;
                }

                if (_immutableProps.Contains(key))
                {
                    continue;
                }

                var currentValue = prop.GetValue(entity);
                var newValue = ConvertValue(rawValue, prop.PropertyType);

                if (Equals(currentValue, newValue))
                {
                    continue;
                }

                prop.SetValue(entity, newValue);

                var oldStr = FormatValue(currentValue);
                var newStr = FormatValue(newValue);
                auditEntries.Add(new AuditTrail(
                    username,
                    $"Changed {FieldDisplayName(key)} from '{oldStr}' to '{newStr}' on {documentType} #{refVal}",
                    documentType, id, refVal));
            }

            if (props.TryGetValue("EditedBy", out var eb) && eb.CanWrite)
            {
                eb.SetValue(entity, username);
            }

            if (props.TryGetValue("EditedDate", out var ed) && ed.CanWrite)
            {
                ed.SetValue(entity, DateTimeHelper.GetCurrentPhilippineTime());
            }

            if (auditEntries.Count == 0)
            {
                return ServiceResult.Success("No changes detected.");
            }

            foreach (var entry in auditEntries)
            {
                await unitOfWork.AuditTrail.AddAsync(entry, ct);
            }

            return ServiceResult.Success(
                $"{documentType} #{refVal} updated ({auditEntries.Count} field(s) changed).");
        }

        private static object? ConvertValue(string? value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlying == typeof(int))
            {
                return int.TryParse(value, out var i) ? i : 0;
            }

            if (underlying == typeof(decimal))
            {
                return decimal.TryParse(value, out var d) ? d : 0m;
            }

            if (underlying == typeof(bool))
            {
                return value is "true" or "True" or "1";
            }

            if (underlying == typeof(DateOnly))
            {
                return DateOnly.TryParse(value, out var dt) ? dt : default(DateOnly?);
            }

            if (underlying == typeof(DateTime))
            {
                return DateTime.TryParse(value, out var dtm) ? dtm : default(DateTime?);
            }

            if (underlying == typeof(TimeOnly))
            {
                return TimeOnly.TryParse(value, out var t) ? t : default(TimeOnly?);
            }

            return value;
        }

        private static string FormatValue(object? value)
        {
            return value switch
            {
                null => "",
                DateOnly d => d.ToString("yyyy-MM-dd"),
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm"),
                decimal m => m.ToString("N2"),
                bool b => b ? "Yes" : "No",
                _ => value.ToString() ?? ""
            };
        }

        private static string FieldDisplayName(string propName)
        {
            if (propName.EndsWith("Id") && propName.Length > 2)
            {
                return propName[..^2];
            }

            return propName;
        }

        private static Dictionary<string, object?> MapJobOrder(JobOrder? j)
        {
            if (j == null)
            {
                return [];
            }

            return new()
            {
                ["JobOrderId"] = j.JobOrderId,
                ["JobOrderNumber"] = j.JobOrderNumber,
                ["Date"] = j.Date.ToString("yyyy-MM-dd"),
                ["Status"] = j.Status,
                ["CustomerId"] = j.CustomerId,
                ["VesselId"] = j.VesselId,
                ["PortId"] = j.PortId,
                ["TerminalId"] = j.TerminalId,
                ["COSNumber"] = j.COSNumber,
                ["VoyageNumber"] = j.VoyageNumber,
                ["PlannedStartTime"] = j.PlannedStartTime?.ToString("yyyy-MM-ddTHH:mm"),
                ["PlannedEndTime"] = j.PlannedEndTime?.ToString("yyyy-MM-ddTHH:mm"),
                ["RequiredTugCount"] = j.RequiredTugCount,
                ["PreferredTugboatId"] = j.PreferredTugboatId,
                ["Remarks"] = j.Remarks,
                ["CreatedBy"] = j.CreatedBy,
                ["CreatedDate"] = j.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                ["EditedBy"] = j.EditedBy,
                ["EditedDate"] = j.EditedDate?.ToString("yyyy-MM-dd HH:mm"),
            };
        }

        private static Dictionary<string, object?> MapDispatchTicket(DispatchTicket? d)
        {
            if (d == null)
            {
                return [];
            }

            return new()
            {
                ["DispatchTicketId"] = d.DispatchTicketId,
                ["DispatchNumber"] = d.DispatchNumber,
                ["Date"] = d.Date.ToString("yyyy-MM-dd"),
                ["Status"] = d.Status,
                ["CustomerId"] = d.CustomerId,
                ["VesselId"] = d.VesselId,
                ["PortId"] = d.PortId,
                ["TerminalId"] = d.TerminalId,
                ["TugBoatId"] = d.TugBoatId,
                ["ServiceId"] = d.ServiceId,
                ["DateLeft"] = d.DateLeft?.ToString("yyyy-MM-dd"),
                ["DateArrived"] = d.DateArrived?.ToString("yyyy-MM-dd"),
                ["TimeLeft"] = d.TimeLeft?.ToString("HH:mm"),
                ["TimeArrived"] = d.TimeArrived?.ToString("HH:mm"),
                ["TotalHours"] = d.TotalHours,
                ["DispatchRate"] = d.DispatchRate,
                ["DispatchBillingAmount"] = d.DispatchBillingAmount,
                ["DispatchDiscount"] = d.DispatchDiscount,
                ["BAFRate"] = d.BAFRate,
                ["BAFBillingAmount"] = d.BAFBillingAmount,
                ["BAFDiscount"] = d.BAFDiscount,
                ["TotalBilling"] = d.TotalBilling,
                ["TotalNetRevenue"] = d.TotalNetRevenue,
                ["ApOtherTugs"] = d.ApOtherTugs,
                ["Remarks"] = d.Remarks,
                ["JobOrderId"] = d.JobOrderId,
                ["CreatedBy"] = d.CreatedBy,
                ["CreatedDate"] = d.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
            };
        }

        private static Dictionary<string, object?> MapBilling(Billing? b)
        {
            if (b == null)
            {
                return [];
            }

            return new()
            {
                ["MsapBillingId"] = b.MsapBillingId,
                ["MsapBillingNumber"] = b.MsapBillingNumber,
                ["Date"] = b.Date.ToString("yyyy-MM-dd"),
                ["Status"] = b.Status,
                ["CustomerId"] = b.CustomerId,
                ["VesselId"] = b.VesselId,
                ["PortId"] = b.PortId,
                ["TerminalId"] = b.TerminalId,
                ["Amount"] = b.Amount,
                ["AmountPaid"] = b.AmountPaid,
                ["Balance"] = b.Balance,
                ["IsPaid"] = b.IsPaid,
                ["IsVatable"] = b.IsVatable,
                ["IsVatInclusive"] = b.IsVatInclusive,
                ["PrintWht"] = b.PrintWht,
                ["IsPrinted"] = b.IsPrinted,
                ["Discount"] = b.Discount,
                ["DueDate"] = b.DueDate.ToString("yyyy-MM-dd"),
                ["Terms"] = b.Terms,
                ["VoyageNumber"] = b.VoyageNumber,
                ["COSNumber"] = b.COSNumber,
                ["ApOtherTug"] = b.ApOtherTug,
                ["JobOrderId"] = b.JobOrderId,
                ["PrincipalId"] = b.PrincipalId,
                ["CreatedBy"] = b.CreatedBy,
                ["CreatedDate"] = b.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
            };
        }

        private static Dictionary<string, object?> MapCollection(Collection? c)
        {
            if (c == null)
            {
                return [];
            }

            return new()
            {
                ["MsapCollectionId"] = c.MsapCollectionId,
                ["MsapCollectionNumber"] = c.MsapCollectionNumber,
                ["Date"] = c.Date.ToString("yyyy-MM-dd"),
                ["CustomerId"] = c.CustomerId,
                ["CashAmount"] = c.CashAmount,
                ["CheckAmount"] = c.CheckAmount,
                ["CheckNumber"] = c.CheckNumber,
                ["CheckBank"] = c.CheckBank,
                ["CheckBranch"] = c.CheckBranch,
                ["CheckDate"] = c.CheckDate?.ToString("yyyy-MM-dd"),
                ["BankId"] = c.BankId,
                ["Amount"] = c.Amount,
                ["EWT"] = c.EWT,
                ["WVAT"] = c.WVAT,
                ["Total"] = c.Total,
                ["Remarks"] = c.Remarks,
                ["DepositDate"] = c.DepositDate?.ToString("yyyy-MM-dd"),
                ["IsPrinted"] = c.IsPrinted,
                ["IsUndocumented"] = c.IsUndocumented,
                ["ReferenceNo"] = c.ReferenceNo,
                ["CreatedBy"] = c.CreatedBy,
                ["CreatedDate"] = c.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
            };
        }
    }

    public record TableColumnDef(string Data, string Title);

    public record FieldDefinition(
        string Name,
        string Label,
        string Type,
        bool IsRequired = false,
        string? LookupKey = null,
        List<SelectListItem>? Options = null);
}
