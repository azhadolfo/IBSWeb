using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap.Books;
using IBS.Models.Msap.Enums;
using IBS.Models.Msap;
using IBS.Utility.Msap;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using IBS.DTOs;
using static IBS.Utility.Msap.TaxConstants;

namespace IBS.Services.Msap
{
    public class BillingService(
        IUnitOfWork unitOfWork,
        JobOrderService jobOrderService,
        ILogger<BillingService> logger)
    {
        public async Task<Billing?> GetBillingByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
        }

        public async Task<ServiceResult<int>> CreateBillingAsync(Billing model, string username, string company, CancellationToken cancellationToken)
        {
            try
            {
                var guard = await GuardClosedPeriodAsync(model.Date, cancellationToken);
                if (guard != null)
                {
                    return ServiceResult<int>.Failure(guard.Message!);
                }

                if (model.JobOrderId is 0)
                {
                    model.JobOrderId = null;
                }

                if (model.JobOrderId.HasValue)
                {
                    var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(model.JobOrderId.Value, cancellationToken);
                    if (jobOrder != null)
                    {
                        if (model.CustomerId == 0)
                        {
                            model.CustomerId = jobOrder.CustomerId;
                        }

                        if (model.VesselId == 0)
                        {
                            model.VesselId = jobOrder.VesselId;
                        }

                        if (model.PortId == 0)
                        {
                            model.PortId = jobOrder.PortId;
                        }

                        if (model.TerminalId == 0)
                        {
                            model.TerminalId = jobOrder.TerminalId;
                        }

                        if (string.IsNullOrWhiteSpace(model.VoyageNumber))
                        {
                            model.VoyageNumber = jobOrder.VoyageNumber;
                        }

                        if (string.IsNullOrWhiteSpace(model.COSNumber))
                        {
                            model.COSNumber = jobOrder.COSNumber;
                        }

                        var unreadyStatuses = new[]
                        {
                            SD.ServiceRequestStatus.Draft,
                            SD.ServiceRequestStatus.Requested,
                            SD.DispatchTicketStatus.ForTariff,
                            SD.DispatchTicketStatus.ForApproval
                        };

                        var hasUnreadyTickets = jobOrder.DispatchTickets
                            .Any(dt => unreadyStatuses.Contains(dt.Status));

                        if (hasUnreadyTickets)
                        {
                            var unreadyList = jobOrder.DispatchTickets
                                .Where(dt => unreadyStatuses.Contains(dt.Status))
                                .Select(dt => $"#{dt.DispatchNumber} ({dt.Status})")
                                .ToList();

                            return ServiceResult<int>.Failure(
                                $"Cannot create billing — the following ticket(s) under Job Order #{jobOrder.JobOrderNumber} are not yet ready: {string.Join(", ", unreadyList)}.");
                        }
                    }
                }


                var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);
                if (customer == null)
                {
                    return ServiceResult<int>.Failure("Customer not found.");
                }

                model.Customer = customer;
                model.Year = model.Date.Year;
                model.Status = SD.BillingStatus.ForPosting;
                model.CreatedBy = username;
                model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Company = company;

                if (model.PrincipalId.HasValue && model.PrincipalId != 0)
                {
                    var principal = await unitOfWork.Principal.GetAsync(p => p.PrincipalId == model.PrincipalId.Value, cancellationToken);
                    if (principal != null)
                    {
                        model.Principal = principal;
                    }
                }

                model.Terms = model.PrincipalId != null && model.PrincipalId != 0
                    ? model.Principal?.Terms
                    : model.Customer.CustomerTerms;

                if (string.IsNullOrEmpty(model.Terms))
                {
                    model.Terms = "COD";
                }

                model.DueDate = await unitOfWork.Billing.ComputeDueDateAsync(model.Terms, model.Date, cancellationToken);

                if (model.IsUndocumented)
                {
                    model.MsapBillingNumber = await unitOfWork.Billing.GenerateBillingNumber(model.Year, cancellationToken);
                }
                else if (string.IsNullOrWhiteSpace(model.MsapBillingNumber))
                {
                    return ServiceResult<int>.Failure("Billing Number is required.");
                }

                if (model.ToBillDispatchTickets == null || !model.ToBillDispatchTickets.Any())
                {
                    return ServiceResult<int>.Failure("At least one dispatch ticket must be selected.");
                }

                decimal total = 0, dispatch = 0, baf = 0;
                foreach (var ticketIdStr in model.ToBillDispatchTickets)
                {
                    var dt = await unitOfWork.DispatchTicket.GetAsync(t => t.DispatchTicketId == int.Parse(ticketIdStr), cancellationToken);
                    if (dt == null)
                    {
                        return ServiceResult<int>.Failure($"Dispatch ticket #{ticketIdStr} not found.");
                    }

                    if (model.JobOrderId.HasValue && dt.JobOrderId != model.JobOrderId)
                    {
                        return ServiceResult<int>.Failure($"Ticket #{dt.DispatchNumber} does not belong to the selected Job Order.");
                    }

                    if (model.BafRates != null && model.BafRates.TryGetValue(dt.DispatchTicketId, out var bafRate))
                    {
                        var chargeType = model.BafChargeTypes != null && model.BafChargeTypes.TryGetValue(dt.DispatchTicketId, out var ct)
                            ? ct
                            : null;
                        await ApplyBafRateAsync(dt, bafRate, chargeType, username, cancellationToken);
                    }

                    total += dt.TotalNetRevenue;
                    dispatch += dt.DispatchNetRevenue;
                    baf += dt.BAFNetRevenue;

                    dt.Billing = model;
                }

                model.Amount = model.Balance = model is { IsVatable: true, IsVatInclusive: false } ? total * VatMultiplier : total;
                model.DispatchAmount = dispatch;
                model.BAFAmount = baf;
                model.IsPaid = false;

                await unitOfWork.Billing.AddAsync(model, cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Created Billing #{model.MsapBillingNumber}", "Billing", model.MsapBillingId, model.MsapBillingNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult<int>.Success(model.MsapBillingId, "Billing created successfully. Status: For Posting");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create billing.");
                return ServiceResult<int>.Failure($"Failed to create billing: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        private async Task ApplyBafRateAsync(DispatchTicket dt, decimal newRate, string? chargeType, string username, CancellationToken cancellationToken)
        {
            var rateChanged = dt.BAFRate != newRate;
            var typeChanged = !string.IsNullOrWhiteSpace(chargeType) && dt.BAFChargeType != chargeType;
            if (!rateChanged && !typeChanged)
            {
                return;
            }

            var oldRate = dt.BAFRate;
            if (typeChanged)
            {
                dt.BAFChargeType = chargeType;
            }

            var hours = Math.Round(dt.TotalHours, 2);
            var discountAmount = newRate * (dt.BAFDiscount / 100);
            var perHour = dt.BAFChargeType == "Per hour";
            var bafBilling = perHour ? newRate * hours : newRate;
            var bafRevenue = perHour ? (newRate - discountAmount) * hours : newRate - discountAmount;

            dt.BAFRate = Math.Round(newRate, 2);
            dt.BAFBillingAmount = Math.Round(bafBilling, 2);
            dt.BAFNetRevenue = Math.Round(bafRevenue, 2);
            dt.TotalBilling = Math.Round(dt.DispatchBillingAmount + dt.BAFBillingAmount, 2);
            dt.TotalNetRevenue = Math.Round(dt.DispatchNetRevenue + dt.BAFNetRevenue, 2);

            var typeDesc = typeChanged ? $" ({dt.BAFChargeType})" : string.Empty;
            await unitOfWork.AuditTrail.AddAsync(
                new AuditTrail(username, $"BAF rate adjusted during billing on #{dt.DispatchNumber}: {oldRate:N2} → {newRate:N2}{typeDesc}", "Billing", dt.DispatchTicketId, dt.DispatchNumber),
                cancellationToken);
        }

        public async Task<ServiceResult> PostBillingAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var billing = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
                if (billing == null)
                {
                    return ServiceResult.Failure("Billing not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(billing.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var model = billing;

                    if (model.Status != SD.BillingStatus.ForPosting)
                    {
                        throw new InvalidOperationException($"Billing #{model.MsapBillingNumber} is already {model.Status}.");
                    }

                    var linkedTickets = await unitOfWork.DispatchTicket.GetAllAsync(dt => dt.BillingId == model.MsapBillingId, cancellationToken);
                    foreach (var dt in linkedTickets)
                    {
                        dt.Status = SD.DispatchTicketStatus.Billed;
                    }

                    var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);
                    if (customer == null)
                    {
                        throw new InvalidOperationException("Customer not found.");
                    }

                    model.Customer = customer;

                    if (model.PrincipalId.HasValue && model.PrincipalId != 0)
                    {
                        model.Principal = await unitOfWork.Principal.GetAsync(p => p.PrincipalId == model.PrincipalId.Value, cancellationToken);
                    }
                    model.Vessel = await unitOfWork.Vessel.GetAsync(v => v.VesselId == model.VesselId, cancellationToken) ?? null!;

                    var soldToName = (model.PrincipalId != null ? model.Principal?.PrincipalName : customer.CustomerName) ?? string.Empty;
                    var tinNo = (model.PrincipalId != null ? model.Principal?.TIN : customer.CustomerTin) ?? string.Empty;
                    var address = (model.PrincipalId != null ? model.Principal?.Address1 : customer.CustomerAddress) ?? string.Empty;

                    var salesBook = new SalesBook
                    {
                        TransactionDate = model.Date,
                        SerialNo = model.MsapBillingNumber,
                        SoldTo = soldToName,
                        TinNo = tinNo,
                        Address = address,
                        Description = model.Vessel.VesselName,
                        Amount = model.Amount - model.Discount
                    };

                    if (model.IsVatable)
                    {
                        salesBook.VatableSales = unitOfWork.Billing.ComputeNetOfVat(salesBook.Amount);
                        salesBook.VatAmount = unitOfWork.Billing.ComputeVatAmount(salesBook.VatableSales);
                        salesBook.NetSales = salesBook.VatableSales - salesBook.Discount;
                    }
                    else
                    {
                        salesBook.ZeroRated = salesBook.Amount;
                        salesBook.NetSales = salesBook.ZeroRated - salesBook.Discount;
                    }

                    salesBook.Discount = model.Discount;
                    salesBook.CreatedBy = username;
                    salesBook.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    salesBook.DueDate = model.DueDate;
                    salesBook.DocumentId = model.MsapBillingId;
                    salesBook.Company = model.Company;

                    var ledgers = new List<GeneralLedgerBook>();
                    var accountTitlesDto = await unitOfWork.Billing.GetListOfAccountTitleDto(cancellationToken);

                    var arTrade = accountTitlesDto.Find(c => c.AccountNumber == SD.MsapAccounts.ArTrade);
                    var revenue = accountTitlesDto.Find(c => c.AccountNumber == SD.MsapAccounts.MaritimeServiceRevenue);
                    var outputVat = accountTitlesDto.Find(c => c.AccountNumber == SD.MsapAccounts.OutputVat);

                    if (arTrade == null)
                    {
                        throw new InvalidOperationException($"Accounting setup incomplete: Account '{SD.MsapAccounts.ArTrade}' (AR Trade) not found in Chart of Accounts.");
                    }

                    if (revenue == null)
                    {
                        throw new InvalidOperationException($"Accounting setup incomplete: Account '{SD.MsapAccounts.MaritimeServiceRevenue}' (Service Revenue) not found in Chart of Accounts.");
                    }

                    if (model.IsVatable && outputVat == null)
                    {
                        throw new InvalidOperationException($"Accounting setup incomplete: Account '{SD.MsapAccounts.OutputVat}' (Output VAT) not found in Chart of Accounts.");
                    }

                    ledgers.Add(new GeneralLedgerBook
                    {
                        Date = model.Date,
                        Reference = model.MsapBillingNumber,
                        Description = $"Billing for {model.Vessel.VesselName}",
                        AccountId = arTrade.AccountId,
                        AccountNo = arTrade.AccountNumber,
                        AccountTitle = arTrade.AccountName,
                        Debit = salesBook.Amount,
                        Credit = 0,
                        Company = model.Company,
                        CreatedBy = username,
                        CreatedDate = salesBook.CreatedDate,
                        ModuleType = nameof(ModuleType.Sales),
                        SubAccountType = SubAccountType.Customer,
                        SubAccountId = model.CustomerId,
                        SubAccountName = customer.CustomerName
                    });

                    ledgers.Add(new GeneralLedgerBook
                    {
                        Date = model.Date,
                        Reference = model.MsapBillingNumber,
                        Description = $"Billing for {model.Vessel.VesselName}",
                        AccountId = revenue.AccountId,
                        AccountNo = revenue.AccountNumber,
                        AccountTitle = revenue.AccountName,
                        Debit = 0,
                        Credit = model.IsVatable ? salesBook.VatableSales : salesBook.ZeroRated,
                        Company = model.Company,
                        CreatedBy = username,
                        CreatedDate = salesBook.CreatedDate,
                        ModuleType = nameof(ModuleType.Sales)
                    });

                    if (model.IsVatable)
                    {
                        ledgers.Add(new GeneralLedgerBook
                        {
                            Date = model.Date,
                            Reference = model.MsapBillingNumber,
                            Description = $"Output VAT for {model.MsapBillingNumber}",
                            AccountId = outputVat!.AccountId,
                            AccountNo = outputVat.AccountNumber,
                            AccountTitle = outputVat.AccountName,
                            Debit = 0,
                            Credit = salesBook.VatAmount,
                            Company = model.Company,
                            CreatedBy = username,
                            CreatedDate = salesBook.CreatedDate,
                            ModuleType = nameof(ModuleType.Sales)
                        });
                    }

                    if (!unitOfWork.Billing.IsJournalEntriesBalanced(ledgers))
                    {
                        throw new InvalidOperationException("Accounting error: Journal entries are not balanced.");
                    }

                    model.Status = SD.BillingStatus.ForCollection;

                    await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Posted Billing #{model.MsapBillingNumber}", "Billing", model.MsapBillingId, model.MsapBillingNumber), cancellationToken);
                    await unitOfWork.SaveAsync(cancellationToken);

                    if (model.JobOrderId.HasValue)
                    {
                        await jobOrderService.TryAutoCloseAsync(model.JobOrderId.Value, username, cancellationToken);
                    }
                }, cancellationToken);

                return ServiceResult.Success($"Billing posted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to post billing {BillingId}", id);
                return ServiceResult.Failure($"Failed to post billing: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateBillingAsync(Billing model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var currentModel = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == model.MsapBillingId, cancellationToken);
                if (currentModel == null)
                {
                    return ServiceResult.Failure("Billing not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(currentModel.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                if (currentModel.Status != SD.BillingStatus.ForPosting)
                {
                    return ServiceResult.Failure("Only billings with 'For Posting' status can be edited.");
                }

                var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);
                if (customer == null)
                {
                    return ServiceResult.Failure("Customer not found.");
                }

                if (model.ToBillDispatchTickets != null)
                {
                    var oldTickets = await unitOfWork.DispatchTicket.GetAllAsync(dt => dt.BillingId == model.MsapBillingId, cancellationToken);
                    foreach (var dt in oldTickets)
                    {
                        dt.BillingId = null;
                    }
                }

                currentModel.IsVatable = model.IsVatable;
                currentModel.CustomerId = model.CustomerId;
                currentModel.PrincipalId = model.PrincipalId;
                currentModel.VoyageNumber = model.VoyageNumber;
                currentModel.COSNumber = model.COSNumber;
                currentModel.Date = model.Date;
                currentModel.Year = model.Date.Year;
                currentModel.PortId = model.PortId;
                currentModel.TerminalId = model.TerminalId;
                currentModel.VesselId = model.VesselId;
                currentModel.BilledTo = model.BilledTo;
                if (model.JobOrderId.HasValue)
                {
                    currentModel.JobOrderId = model.JobOrderId;
                }
                currentModel.IsVatInclusive = model.IsVatInclusive;
                currentModel.PrintWht = model.PrintWht;

                if (model.ToBillDispatchTickets != null)
                {
                    decimal total = 0, dispatch = 0, baf = 0;
                    foreach (var ticketIdStr in model.ToBillDispatchTickets)
                    {
                        var dt = await unitOfWork.DispatchTicket.GetAsync(t => t.DispatchTicketId == int.Parse(ticketIdStr), cancellationToken);
                        if (dt == null)
                        {
                            return ServiceResult.Failure($"Dispatch ticket #{ticketIdStr} not found.");
                        }

                        if (model.BafRates != null && model.BafRates.TryGetValue(dt.DispatchTicketId, out var bafRate))
                        {
                            await ApplyBafRateAsync(dt, bafRate, null, username, cancellationToken);
                        }

                        total += dt.TotalNetRevenue;
                        dispatch += dt.DispatchNetRevenue;
                        baf += dt.BAFNetRevenue;

                        dt.BillingId = currentModel.MsapBillingId;
                        dt.Billing = currentModel;
                    }

                    currentModel.Amount = currentModel.Balance = currentModel is { IsVatable: true, IsVatInclusive: false } ? total * VatMultiplier : total;
                    currentModel.DispatchAmount = dispatch;
                    currentModel.BAFAmount = baf;
                }

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Edit billing #{currentModel.MsapBillingNumber}", "Billing", currentModel.MsapBillingId, currentModel.MsapBillingNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Entry edited successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit billing.");
                return ServiceResult.Failure($"Failed to edit billing: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DeleteBillingAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
                if (model == null)
                {
                    return ServiceResult.Failure("Billing not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(model.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                var linkedTickets = await unitOfWork.DispatchTicket
                    .GetAllAsync(dt => dt.BillingId == id, cancellationToken);
                foreach (var dt in linkedTickets)
                {
                    dt.BillingId = null;
                }

                await unitOfWork.Billing.RemoveAsync(model, cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Deleted Billing #{model.MsapBillingNumber}", "Billing", model.MsapBillingId, model.MsapBillingNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                var jobOrderId = linkedTickets.FirstOrDefault()?.JobOrderId;
                if (jobOrderId.HasValue)
                {
                    var jobOrder = await unitOfWork.JobOrder.GetAsync(jo => jo.JobOrderId == jobOrderId.Value, cancellationToken);
                    if (jobOrder?.Status == SD.JobOrderStatus.Closed)
                    {
                        jobOrder.Status = SD.JobOrderStatus.Open;
                        await unitOfWork.SaveAsync(cancellationToken);
                    }
                }

                return ServiceResult.Success("Billing deleted successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete billing.");
                return ServiceResult.Failure($"Failed to delete billing: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }
        public async Task<ServiceResult> ReverseBillingAsync(int id, string username, string? remarks, CancellationToken cancellationToken)
        {
            try
            {
                var billing = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
                if (billing == null)
                {
                    return ServiceResult.Failure("Billing not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(billing.Date, cancellationToken);
                if (guard != null)
                {
                    return guard;
                }

                string billingNumber = billing.MsapBillingNumber;

                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    billing = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
                    billingNumber = billing!.MsapBillingNumber;

                    if (billing.Status != SD.BillingStatus.ForCollection)
                    {
                        throw new InvalidOperationException($"Only billings with '{SD.BillingStatus.ForCollection}' status can be reversed.");
                    }

                    if (billing.CollectionId.HasValue)
                    {
                        throw new InvalidOperationException($"Billing #{billingNumber} cannot be reversed because it is linked to a Collection (CR#{billing.CollectionNumber}).");
                    }

                    var linkedTickets = await unitOfWork.DispatchTicket.GetAllAsync(dt => dt.BillingId == billing.MsapBillingId, cancellationToken);
                    foreach (var dt in linkedTickets)
                    {
                        dt.Status = SD.DispatchTicketStatus.ForBilling;
                        dt.BillingId = null;
                    }

                    billing.Status = SD.BillingStatus.ForPosting;
                    billing.UnpostedBy = username;
                    billing.UnpostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    billing.UnpostRemarks = remarks;

                    await unitOfWork.AuditTrail.AddAsync(
                        new AuditTrail(username, $"Reversed (unposted) Billing #{billingNumber}", "Billing", billing.MsapBillingId, billingNumber),
                        cancellationToken);

                    if (billing.JobOrderId.HasValue)
                    {
                        var jobOrder = await unitOfWork.JobOrder.GetAsync(jo => jo.JobOrderId == billing.JobOrderId, cancellationToken);
                        if (jobOrder is { Status: SD.JobOrderStatus.Closed })
                        {
                            jobOrder.Status = SD.JobOrderStatus.Open;
                        }
                    }

                    await unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                return ServiceResult.Success($"Billing #{billingNumber} reversed successfully and is back to '{SD.BillingStatus.ForPosting}'.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reverse billing {BillingId}", id);
                return ServiceResult.Failure($"Failed to reverse billing: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<(IEnumerable<Billing> Data, int RecordsFiltered, int TotalRecords)> GetPagedBillingsAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            return await unitOfWork.Billing.GetPagedBillingsAsync(parameters, cancellationToken);
        }

        public async Task<List<object>> SearchPrincipalsAsync(string? term, int customerId, CancellationToken cancellationToken)
        {
            var result = await unitOfWork.Principal.SearchPrincipalsAsync(term ?? string.Empty, customerId, 10, cancellationToken);

            return result.Select(p => (object)new
            {
                value = p.PrincipalId,
                name = p.PrincipalName,
                address = p.Address1,
                tinNo = p.TIN,
                businessStyle = p.BusinessType,
                terms = p.Terms
            }).ToList();
        }

        public async Task<List<object>> GetBillableJobOrdersAsync(int customerId, CancellationToken cancellationToken)
        {
            var result = await unitOfWork.JobOrder.SearchBillableJobOrdersAsync("", customerId, 999, cancellationToken);

            return result.Select(j => (object)new
            {
                value = j.JobOrderId,
                name = j.JobOrderNumber,
                description = j.Remarks ?? ""
            }).ToList();
        }

        public async Task<ServiceResult<JobOrderBillingDto>> GetDispatchTicketsByJobOrderAsync(int jobOrderId, CancellationToken cancellationToken)
        {
            var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(jobOrderId, cancellationToken);
            if (jobOrder == null)
            {
                return ServiceResult<JobOrderBillingDto>.Failure("Job Order not found");
            }

            var tickets = jobOrder.DispatchTickets
                .Where(t => t is { Status: SD.DispatchTicketStatus.ForBilling, BillingId: null })
                .Select(t => new JobOrderTicketDto
                {
                    DispatchTicketId = t.DispatchTicketId,
                    DispatchNo = t.DispatchNumber,
                    Tugboat = t.Tugboat.TugboatName,
                    Service = t.Service.ServiceName,
                    Duration = t.TotalHours,
                    DispatchRate = t.DispatchRate,
                    DispatchAmount = t.DispatchBillingAmount,
                    BAFRate = t.BAFRate,
                    BAFDiscount = t.BAFDiscount,
                    BAFChargeType = t.BAFChargeType ?? string.Empty,
                    BAFAmount = t.BAFBillingAmount,
                    TotalAmount = t.TotalBilling,
                    Port = t.Terminal?.Port?.PortName,
                    Terminal = t.Terminal?.TerminalName
                }).ToList();

            return ServiceResult<JobOrderBillingDto>.Success(new JobOrderBillingDto
            {
                Header = new JobOrderHeaderDto
                {
                    VesselId = jobOrder.VesselId,
                    PortId = jobOrder.PortId,
                    TerminalId = jobOrder.TerminalId,
                    VoyageNumber = jobOrder.VoyageNumber,
                    COSNumber = jobOrder.COSNumber
                },
                Tickets = tickets
            });
        }

        public async Task<ServiceResult<JobOrderBillingDto>> GetDispatchTicketsByCustomerAsync(int customerId, CancellationToken cancellationToken)
        {
            var tickets = await unitOfWork.DispatchTicket.GetAllAsync(t =>
                t.CustomerId == customerId &&
                t.Status == SD.DispatchTicketStatus.ForBilling &&
                t.BillingId == null &&
                t.JobOrderId == null,
                cancellationToken);

            var ticketDtos = tickets.Select(t => new JobOrderTicketDto
            {
                DispatchTicketId = t.DispatchTicketId,
                DispatchNo = t.DispatchNumber,
                Tugboat = t.Tugboat.TugboatName,
                Service = t.Service.ServiceName,
                Duration = t.TotalHours,
                DispatchRate = t.DispatchRate,
                DispatchAmount = t.DispatchBillingAmount,
                BAFRate = t.BAFRate,
                BAFDiscount = t.BAFDiscount,
                BAFChargeType = t.BAFChargeType ?? string.Empty,
                BAFAmount = t.BAFBillingAmount,
                TotalAmount = t.TotalBilling,
                Port = t.Terminal?.Port?.PortName,
                Terminal = t.Terminal?.TerminalName
            }).ToList();

            return ServiceResult<JobOrderBillingDto>.Success(new JobOrderBillingDto
            {
                Header = new JobOrderHeaderDto(),
                Tickets = ticketDtos
            });
        }

        public async Task<List<SelectListItem>?> GetPrincipalsSelectListAsync(int customerId, CancellationToken cancellationToken)
        {
            var principals = await unitOfWork.Principal.GetAllAsync(t => t.CustomerId == customerId, cancellationToken);
            return principals.Select(t => new SelectListItem
            {
                Value = t.PrincipalId.ToString(),
                Text = t.PrincipalName
            }).ToList();
        }

        public async Task<List<SelectListItem>?> GetEditTicketsSelectListAsync(int? customerId, int billingId, CancellationToken cancellationToken)
        {
            var list = await unitOfWork.Billing.GetMsapUnbilledTicketsByCustomer(customerId, cancellationToken);
            if (billingId != 0)
            {
                var billedTickets = await unitOfWork.DispatchTicket.GetAllAsync(dt => dt.BillingId == billingId, cancellationToken);
                if (billedTickets.Any() && billedTickets.First().CustomerId == customerId)
                {
                    list?.AddRange(await unitOfWork.Billing.GetMsapBilledTicketsById(billingId, cancellationToken));
                }
            }
            return list;
        }

        public async Task<Billing> PopulateTicketListsAsync(Billing model, CancellationToken cancellationToken)
        {
            model.ToBillDispatchTickets = await unitOfWork.Billing
                .GetToBillDispatchTicketListAsync(model.MsapBillingId, cancellationToken);

            model.PaidDispatchTickets = await unitOfWork.Billing
                .GetPaidDispatchTicketsAsync(model.MsapBillingId, cancellationToken);

            model.UniqueTugboats = await unitOfWork.Billing
                .GetUniqueTugboatsListAsync(model.MsapBillingId, cancellationToken);

            unitOfWork.Billing.ProcessAddress(model, cancellationToken);
            return model;
        }

        public async Task<IEnumerable<DispatchTicket>> GetDispatchTicketsByIdsAsync(List<int> dispatchTicketIds, CancellationToken cancellationToken)
        {
            return await unitOfWork.DispatchTicket
                .GetAllAsync(t => dispatchTicketIds.Contains(t.DispatchTicketId), cancellationToken);
        }

        public async Task<Billing> PopulateBillingSelectListsAsync(Billing model, CancellationToken cancellationToken)
        {
            model.Vessels = await unitOfWork.Vessel.GetMsapVesselsSelectList(cancellationToken);
            model.Ports = await unitOfWork.Port.GetMsapPortsSelectList(cancellationToken);
            model.Customers = await unitOfWork.Billing.GetMsapCustomersWithBillablesSelectList(model.CustomerId, "", cancellationToken);

            if (model.PortId != 0)
            {
                model.Terminals = await unitOfWork.Terminal.GetMsapTerminalsSelectList(model.PortId, cancellationToken);
            }

            return model;
        }

        public async Task<ServiceResult<object>> GetCustomerDetailsAsync(int customerId, CancellationToken cancellationToken)
        {
            var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == customerId, cancellationToken);
            if (customer == null)
            {
                return ServiceResult<object>.Failure("Customer not found.", ServiceResultStatus.NotFound);
            }

            var principals = await unitOfWork.Principal.GetAllAsync(p => p.CustomerId == customerId, cancellationToken);
            var hasPrincipal = principals.Any();

            return ServiceResult<object>.Success(new
            {
                value = customer.CustomerId,
                name = customer.CustomerName,
                address = customer.CustomerAddress,
                tinNo = customer.CustomerTin,
                terms = customer.CustomerTerms,
                vatType = customer.VatType,
                isUndoc = customer.Type,
                businessStyle = customer.BusinessStyle ?? "-",
                withholdingTax = customer.WithHoldingTax,
                withholdingVat = customer.WithHoldingVat,
                hasPrincipal
            });
        }

        public async Task<List<object>> GetPrincipalsByCustomerAsync(int customerId, CancellationToken cancellationToken)
        {
            var principals = await unitOfWork.Principal.GetAllAsync(p => p.CustomerId == customerId, cancellationToken);
            return principals.Select(p => (object)new
            {
                value = p.PrincipalId,
                name = p.PrincipalName,
                address = p.Address1,
                tinNo = p.TIN,
                businessStyle = p.BusinessType,
                terms = p.Terms
            }).ToList();
        }

        public async Task<ServiceResult<(int DispatchBillingId, int BafBillingId)>> CreatePhilCebSplitAsync(
            Billing model, string bafBillingNumber, string username, string company, CancellationToken cancellationToken)
        {
            try
            {
                var guard = await GuardClosedPeriodAsync(model.Date, cancellationToken);
                if (guard != null)
                    return ServiceResult<(int, int)>.Failure(guard.Message!);

                if (string.IsNullOrWhiteSpace(model.MsapBillingNumber))
                    return ServiceResult<(int, int)>.Failure("Billing Number is required.");

                if (string.IsNullOrWhiteSpace(bafBillingNumber))
                    return ServiceResult<(int, int)>.Failure("BAF Billing Number is required.");

                var dupMain = await unitOfWork.Billing.GetAsync(b => b.MsapBillingNumber == model.MsapBillingNumber, cancellationToken);
                if (dupMain != null)
                    return ServiceResult<(int, int)>.Failure($"Billing number '{model.MsapBillingNumber}' already exists.");

                var dupBaf = await unitOfWork.Billing.GetAsync(b => b.MsapBillingNumber == bafBillingNumber, cancellationToken);
                if (dupBaf != null)
                    return ServiceResult<(int, int)>.Failure($"BAF Billing number '{bafBillingNumber}' already exists.");

                if (model.JobOrderId is 0) model.JobOrderId = null;

                if (model.ToBillDispatchTickets == null || !model.ToBillDispatchTickets.Any())
                    return ServiceResult<(int, int)>.Failure("At least one dispatch ticket must be selected.");

                var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);
                if (customer == null)
                    return ServiceResult<(int, int)>.Failure("Customer not found.");

                var now = DateTimeHelper.GetCurrentPhilippineTime();

                if (model.PrincipalId.HasValue && model.PrincipalId != 0)
                    model.Principal = await unitOfWork.Principal.GetAsync(p => p.PrincipalId == model.PrincipalId.Value, cancellationToken);

                var terms = (model.PrincipalId > 0 ? model.Principal?.Terms : customer.CustomerTerms) ?? "COD";
                var dueDate = await unitOfWork.Billing.ComputeDueDateAsync(terms, model.Date, cancellationToken);

                decimal dispatch = 0, baf = 0;
                var ticketEntities = new List<DispatchTicket>();
                foreach (var idStr in model.ToBillDispatchTickets)
                {
                    var dt = await unitOfWork.DispatchTicket.GetAsync(t => t.DispatchTicketId == int.Parse(idStr), cancellationToken);
                    if (dt == null)
                        return ServiceResult<(int, int)>.Failure($"Dispatch ticket #{idStr} not found.");

                    if (model.BafRates != null && model.BafRates.TryGetValue(dt.DispatchTicketId, out var bafRate))
                    {
                        await ApplyBafRateAsync(dt, bafRate, null, username, cancellationToken);
                    }

                    dispatch += dt.DispatchNetRevenue;
                    baf += dt.BAFNetRevenue;
                    ticketEntities.Add(dt);
                }

                var billing1 = new Billing
                {
                    MsapBillingNumber = model.MsapBillingNumber,
                    Date = model.Date,
                    DueDate = dueDate,
                    Year = model.Date.Year,
                    CustomerId = model.CustomerId,
                    Customer = customer,
                    PrincipalId = model.PrincipalId,
                    Principal = model.Principal,
                    BilledTo = model.BilledTo,
                    VesselId = model.VesselId,
                    PortId = model.PortId,
                    TerminalId = model.TerminalId,
                    VoyageNumber = model.VoyageNumber,
                    COSNumber = model.COSNumber,
                    JobOrderId = model.JobOrderId,
                    IsVatable = model.IsVatable,
                    IsVatInclusive = model.IsVatInclusive,
                    PrintWht = model.PrintWht,
                    Terms = terms,
                    Company = company,
                    Status = SD.BillingStatus.ForPosting,
                    CreatedBy = username,
                    CreatedDate = now,
                    IsPaid = false,
                    DispatchAmount = dispatch,
                    BAFAmount = 0,
                };
                billing1.Amount = billing1.Balance = billing1 is { IsVatable: true, IsVatInclusive: false }
                    ? dispatch * VatMultiplier : dispatch;

                var bafTickets = new List<DispatchTicket>();

                foreach (var dt in ticketEntities)
                {
                    var origBafRate = dt.BAFRate;
                    var origBafBillingAmt = dt.BAFBillingAmount;
                    var origBafNetRev = dt.BAFNetRevenue;

                    dt.Billing = billing1;
                    dt.BAFBillingAmount = 0;
                    dt.BAFNetRevenue = 0;
                    dt.BAFRate = 0;
                    dt.TotalBilling = dt.DispatchBillingAmount;
                    dt.TotalNetRevenue = dt.DispatchNetRevenue;

                    var bafTicket = new DispatchTicket
                    {
                        DispatchNumber = string.IsNullOrEmpty(dt.DispatchNumber) ? "BAF-A" : $"{dt.DispatchNumber}-A",
                        Date = dt.Date,
                        DateLeft = dt.DateLeft,
                        TimeLeft = dt.TimeLeft,
                        DateArrived = dt.DateArrived,
                        TimeArrived = dt.TimeArrived,
                        TotalHours = dt.TotalHours,
                        CustomerId = dt.CustomerId,
                        VesselId = dt.VesselId,
                        PortId = dt.PortId,
                        TerminalId = dt.TerminalId,
                        TugBoatId = dt.TugBoatId,
                        TugMasterId = dt.TugMasterId,
                        ServiceId = dt.ServiceId,
                        JobOrderId = dt.JobOrderId,
                        Status = dt.Status,
                        CreatedBy = username,
                        CreatedDate = now,
                        DispatchRate = 0,
                        DispatchBillingAmount = 0,
                        DispatchNetRevenue = 0,
                        BAFRate = origBafRate,
                        BAFBillingAmount = origBafBillingAmt,
                        BAFNetRevenue = origBafNetRev,
                        TotalBilling = origBafBillingAmt,
                        TotalNetRevenue = origBafNetRev
                    };
                    bafTickets.Add(bafTicket);
                }

                await unitOfWork.Billing.AddAsync(billing1, cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(
                    new AuditTrail(username, $"Created Billing #{billing1.MsapBillingNumber} (PHIL-CEB dispatch split)", "Billing", billing1.MsapBillingId, billing1.MsapBillingNumber),
                    cancellationToken);

                var billing2 = new Billing
                {
                    MsapBillingNumber = bafBillingNumber,
                    Date = model.Date,
                    DueDate = dueDate,
                    Year = model.Date.Year,
                    CustomerId = model.CustomerId,
                    PrincipalId = model.PrincipalId,
                    BilledTo = model.BilledTo,
                    VesselId = model.VesselId,
                    PortId = model.PortId,
                    TerminalId = model.TerminalId,
                    VoyageNumber = model.VoyageNumber,
                    COSNumber = model.COSNumber,
                    JobOrderId = model.JobOrderId,
                    IsVatable = model.IsVatable,
                    IsVatInclusive = model.IsVatInclusive,
                    PrintWht = model.PrintWht,
                    Terms = terms,
                    Company = company,
                    Status = SD.BillingStatus.ForPosting,
                    CreatedBy = username,
                    CreatedDate = now,
                    IsPaid = false,
                    DispatchAmount = 0,
                    BAFAmount = baf,
                };
                billing2.Amount = billing2.Balance = billing2 is { IsVatable: true, IsVatInclusive: false }
                    ? baf * VatMultiplier : baf;

                foreach (var bt in bafTickets)
                {
                    bt.Billing = billing2;
                    await unitOfWork.DispatchTicket.AddAsync(bt, cancellationToken);
                }

                await unitOfWork.Billing.AddAsync(billing2, cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(
                    new AuditTrail(username, $"Created Billing #{billing2.MsapBillingNumber} (PHIL-CEB BAF split)", "Billing", billing2.MsapBillingId, billing2.MsapBillingNumber),
                    cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult<(int, int)>.Success(
                    (billing1.MsapBillingId, billing2.MsapBillingId),
                    $"Billings created: #{billing1.MsapBillingNumber} (dispatch) and #{billing2.MsapBillingNumber} (BAF).");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create PHIL-CEB split billing.");
                return ServiceResult<(int, int)>.Failure($"Failed to create split billing: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        private async Task<ServiceResult?> GuardClosedPeriodAsync(DateOnly date, CancellationToken ct)
        {
            if (await unitOfWork.PostedPeriod.IsMonthClosedAsync(date.Year, date.Month, ct))
            {
                return ServiceResult.Failure($"Cannot modify: {date:MMMM yyyy} is closed.");
            }

            return null;
        }
    }
}
