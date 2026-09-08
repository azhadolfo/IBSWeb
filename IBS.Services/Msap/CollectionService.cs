using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap;
using IBS.Models.Msap.ViewModels;
using IBS.Utility.Msap;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using static IBS.Utility.Msap.TaxConstants;
using Microsoft.Extensions.Logging;

namespace IBS.Services.Msap
{
    public class CollectionService(
        IUnitOfWork unitOfWork,
        ILogger<CollectionService> logger)
    {

        public async Task<Collection?> GetCollectionByIdAsync(int id, CancellationToken cancellationToken)
        {
            var collection = await unitOfWork.Collection.GetAsync(c => c.MsapCollectionId == id, cancellationToken);
            if (collection != null)
            {
                collection.PaidBills = (await unitOfWork.Billing.GetAllAsync(b => b.CollectionId == collection.MsapCollectionId, cancellationToken)).ToList();
            }
            return collection;
        }

        public async Task<ServiceResult<int>> CreateCollectionAsync(CreateCollectionViewModel viewModel, string username, CancellationToken cancellationToken)
        {
            try
            {
                var guard = await GuardClosedPeriodAsync(viewModel.Date, cancellationToken);
                if (guard != null)
                {
                    return ServiceResult<int>.Failure(guard.Message!);
                }

                int collectionId = 0;
                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var model = await MapToEntityAsync(viewModel, cancellationToken);
                    model.CreatedBy = username;
                    model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    if (model.IsUndocumented)
                    {
                        model.MsapCollectionNumber = await unitOfWork.Collection.GenerateCollectionNumber(cancellationToken);
                    }
                    else
                    {
                        model.MsapCollectionNumber = viewModel.MsapCollectionNumber ?? string.Empty;
                    }

                    decimal totalAllocated = viewModel.BillingPayments?.Sum(p => p.AmountToPay) ?? 0;
                    if (totalAllocated != viewModel.Amount && !model.IsUndocumented)
                    {
                        throw new InvalidOperationException($"Collection amount ({viewModel.Amount:N2}) does not match the total allocated billing payments ({totalAllocated:N2}).");
                    }

                    await unitOfWork.Collection.AddAsync(model, cancellationToken);
                    await unitOfWork.SaveAsync(cancellationToken);
                    collectionId = model.MsapCollectionId;

                    if (viewModel.BillingPayments != null)
                    {
                        model.PaidBills = [];
                        foreach (var payment in viewModel.BillingPayments)
                        {
                            var billing = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == payment.BillingId, cancellationToken);
                            if (billing != null)
                            {
                                billing.Status = SD.BillingStatus.Collected;
                                billing.CollectionId = model.MsapCollectionId;
                                billing.CollectionNumber = model.MsapCollectionNumber;
                                await unitOfWork.Collection.UpdateBillingPayment(payment.BillingId, payment.AmountToPay, cancellationToken);
                                model.PaidBills.Add(billing);
                            }
                        }
                    }

                    await unitOfWork.Collection.PostAsync(model, new List<Offsettings>(), cancellationToken);

                    await unitOfWork.SaveAsync(cancellationToken);

                    var billIds = viewModel.BillingPayments?.Select(p => p.BillingId) ?? new List<int>();
                    var audit = new AuditTrail(username, $"Create collection #{model.MsapCollectionNumber} for billings #{string.Join(", #", billIds)}", "Collection", model.MsapCollectionId, model.MsapCollectionNumber);
                    await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);
                    await unitOfWork.SaveAsync(cancellationToken);

                }, cancellationToken);

                return ServiceResult<int>.Success(collectionId, "Collection created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create collection.");
                return ServiceResult<int>.Failure($"Failed to create collection: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateCollectionAsync(CreateCollectionViewModel viewModel, string username, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var currentModel = await unitOfWork.Collection.GetAsync(c => c.MsapCollectionId == viewModel.MsapCollectionId, cancellationToken);
                    if (currentModel == null)
                    {
                        throw new InvalidOperationException("Collection not found.");
                    }

                    var guard = await GuardClosedPeriodAsync(currentModel.Date, cancellationToken);
                    if (guard != null)
                    {
                        throw new InvalidOperationException(guard.Message);
                    }

                    if (currentModel.CustomerId != viewModel.CustomerId)
                    {
                        throw new InvalidOperationException("Customer cannot be changed on an existing collection.");
                    }

                    if (currentModel.IsPrinted)
                    {
                        throw new InvalidOperationException("Cannot edit a collection that has already been printed.");
                    }

                    var oldBillings = await unitOfWork.Billing.GetAllAsync(b => b.CollectionId == currentModel.MsapCollectionId, cancellationToken);
                    foreach (var billing in oldBillings)
                    {
                        billing.Status = SD.BillingStatus.ForCollection;
                        billing.CollectionId = 0;
                        billing.CollectionNumber = null;
                        await unitOfWork.Collection.RemoveBillingPayment(billing.MsapBillingId, billing.AmountPaid, 0, cancellationToken);
                    }

                    decimal totalAllocated = viewModel.BillingPayments?.Sum(p => p.AmountToPay) ?? 0;
                    if (totalAllocated != viewModel.Amount && !currentModel.IsUndocumented)
                    {
                        throw new InvalidOperationException($"Collection amount (₱{viewModel.Amount:N2}) does not match the total allocated billing payments (₱{totalAllocated:N2}).");
                    }

                    if (viewModel.BillingPayments != null)
                    {
                        foreach (var payment in viewModel.BillingPayments)
                        {
                            var billing = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == payment.BillingId, cancellationToken);
                            if (billing != null)
                            {
                                billing.Status = SD.BillingStatus.Collected;
                                billing.CollectionId = currentModel.MsapCollectionId;
                                billing.CollectionNumber = currentModel.MsapCollectionNumber;
                                await unitOfWork.Collection.UpdateBillingPayment(payment.BillingId, payment.AmountToPay, cancellationToken);
                            }
                        }
                    }

                    var audit = new AuditTrail(username, $"Edit collection #{currentModel.MsapCollectionNumber}", "Collection");
                    await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                    currentModel.Date = viewModel.Date;
                    currentModel.CustomerId = viewModel.CustomerId;
                    currentModel.IsUndocumented = viewModel.IsUndocumented;
                    if (!currentModel.IsUndocumented)
                    {
                        currentModel.MsapCollectionNumber = viewModel.MsapCollectionNumber ?? currentModel.MsapCollectionNumber;
                    }
                    currentModel.ReferenceNo = viewModel.ReferenceNo;
                    currentModel.Remarks = viewModel.Remarks;
                    currentModel.CashAmount = viewModel.CashAmount;
                    currentModel.CheckAmount = viewModel.CheckAmount;
                    currentModel.CheckNumber = viewModel.CheckNumber;
                    currentModel.CheckDate = viewModel.CheckDate;
                    currentModel.CheckBank = viewModel.CheckBank;
                    currentModel.CheckBranch = viewModel.CheckBranch;
                    currentModel.BankId = viewModel.BankId;
                    currentModel.DepositDate = viewModel.DepositDate;
                    currentModel.Amount = viewModel.Amount;
                    currentModel.EWT = viewModel.EWT;
                    currentModel.WVAT = viewModel.WVAT;
                    currentModel.Total = viewModel.Amount + viewModel.EWT + viewModel.WVAT;

                    if (viewModel.BankId.HasValue)
                    {
                        var bank = await unitOfWork.BankAccount.GetAsync(b => b.BankAccountId == viewModel.BankId.Value, cancellationToken);
                        if (bank != null)
                        {
                            currentModel.BankAccountNumber = bank.AccountNo;
                            currentModel.BankAccountName = bank.AccountName;
                        }
                    }

                    currentModel.EditedBy = username;
                    currentModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    await unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                return ServiceResult.Success("Collection modified successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit collection.");
                return ServiceResult.Failure($"Failed to edit collection: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }


        public async Task<(IEnumerable<Collection> Data, int RecordsFiltered, int TotalRecords)> GetPagedCollectionsAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            return await unitOfWork.Collection.GetPagedCollectionsAsync(parameters, cancellationToken);
        }

        public async Task<CreateCollectionViewModel> PopulateCreateViewModelAsync(CancellationToken cancellationToken)
        {
            return new CreateCollectionViewModel
            {
                Customers = await unitOfWork.Collection.GetMsapCustomersWithCollectiblesSelectList(0, string.Empty, cancellationToken),
                BankAccounts = await unitOfWork.GetBankAccountListById(cancellationToken)
            };
        }

        public async Task<CreateCollectionViewModel?> PopulateEditViewModelAsync(int id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.Collection.GetAsync(c => c.MsapCollectionId == id, cancellationToken);
            if (model == null)
            {
                return null;
            }

            var viewModel = MapToViewModel(model);
            var billings = await unitOfWork.Billing.GetBillingsByCollectionIdAsync(id, cancellationToken);
            viewModel.ToCollectBillings = billings
                .Select(b => b.MsapBillingId.ToString())
                .ToList();

            viewModel.Customers = await unitOfWork.Collection.GetMsapCustomersWithCollectiblesSelectList(id, model.Customer.Type, cancellationToken);
            viewModel.Billings = await GetEditBillingsAsync(model.CustomerId, model.MsapCollectionId, cancellationToken);
            viewModel.BankAccounts = await unitOfWork.GetBankAccountListById(cancellationToken);

            return viewModel;
        }

        public async Task<ServiceResult<object>> GetUncollectedBillingsForTableAsync(int customerId, int? collectionId, CancellationToken cancellationToken)
        {
            try
            {
                var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == customerId, cancellationToken);
                if (customer == null)
                {
                    return ServiceResult<object>.Failure("Customer not found.");
                }

                var billings = await unitOfWork.Collection.GetMsapUncollectedBillingsByCustomerList(customerId, cancellationToken);
                if (collectionId.HasValue && collectionId.Value != 0)
                {
                    var alreadyCollected = await unitOfWork.Billing.GetAllAsync(b => b.CollectionId == collectionId.Value, cancellationToken);
                    billings.AddRange(alreadyCollected);
                }

                var result = billings
                    .DistinctBy(b => b.MsapBillingId)
                    .Select(b =>
                    {
                        decimal ewt = 0;
                        if (customer.WithHoldingTax && b.BilledTo == SD.BilledToLocal)
                        {
                            ewt = b.IsVatable ? (b.Amount / VatMultiplier) * EwtRate : b.Amount * EwtRate;
                        }

                        decimal wvat = 0;
                        if (customer.WithHoldingVat && b.BilledTo == SD.BilledToLocal)
                        {
                            wvat = b.IsVatable ? (b.Amount / VatMultiplier) * WvatRate : 0;
                        }

                        return new
                        {
                            msapBillingId = b.MsapBillingId,
                            msapBillingNumber = b.MsapBillingNumber,
                            date = b.Date,
                            amount = b.Amount,
                            balance = b.Balance,
                            ewt = Math.Round(ewt, 2),
                            wvat = Math.Round(wvat, 2),
                            net = Math.Round(b.Amount - ewt - wvat, 2),
                            isVatable = b.IsVatable,
                            isSelected = collectionId.HasValue && b.CollectionId == collectionId.Value
                        };
                    });

                return ServiceResult<object>.Success(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get billings for table.");
                return ServiceResult<object>.Failure($"Failed to get billings: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult<IEnumerable<Billing>>> GetSelectedBillingsAsync(List<string> billingIds, CancellationToken cancellationToken)
        {
            var ids = billingIds.Select(int.Parse).ToList();
            var billings = await unitOfWork.Billing.GetAllAsync(b => ids.Contains(b.MsapBillingId), cancellationToken);
            return ServiceResult<IEnumerable<Billing>>.Success(billings);
        }

        public async Task<bool> IsCustomerVatableAsync(int customerId, CancellationToken cancellationToken)
        {
            var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == customerId, cancellationToken);
            return customer?.VatType == SD.VatType_Vatable;
        }

        public async Task<ServiceResult<object>> GetBankAccountDetailsAsync(int bankId, CancellationToken cancellationToken)
        {
            var bank = await unitOfWork.BankAccount.GetAsync(b => b.BankAccountId == bankId, cancellationToken);
            if (bank == null)
            {
                return ServiceResult<object>.Failure("Bank not found.", ServiceResultStatus.NotFound);
            }

            return ServiceResult<object>.Success(new { bank = bank.Bank, accountNo = bank.AccountNo, accountName = bank.AccountName });
        }

        public async Task<List<SelectListItem>?> GetUncollectedBillingsSelectListAsync(int? customerId, CancellationToken cancellationToken)
        {
            return await unitOfWork.Collection.GetMsapUncollectedBillingsByCustomer(customerId, cancellationToken);
        }

        private async Task<Collection> MapToEntityAsync(CreateCollectionViewModel viewModel, CancellationToken cancellationToken)
        {
            var model = new Collection
            {
                MsapCollectionId = viewModel.MsapCollectionId ?? 0,
                IsUndocumented = viewModel.IsUndocumented,
                Date = viewModel.Date,
                CustomerId = viewModel.CustomerId,
                Amount = viewModel.Amount,
                EWT = viewModel.EWT,
                WVAT = viewModel.WVAT,
                Total = viewModel.Amount + viewModel.EWT + viewModel.WVAT,
                CashAmount = viewModel.CashAmount,
                CheckAmount = viewModel.CheckAmount,
                CheckNumber = viewModel.CheckNumber,
                CheckDate = viewModel.CheckDate,
                CheckBank = viewModel.CheckBank,
                CheckBranch = viewModel.CheckBranch,
                BankId = viewModel.BankId,
                ReferenceNo = viewModel.ReferenceNo,
                Remarks = viewModel.Remarks,
                DepositDate = viewModel.DepositDate,
                Customer = (await unitOfWork.Customer.GetAsync(c => c.CustomerId == viewModel.CustomerId, cancellationToken))!,
                Company = SD.Company_MMSI
            };

            if (viewModel.BankId.HasValue)
            {
                var bank = await unitOfWork.BankAccount.GetAsync(b => b.BankAccountId == viewModel.BankId.Value, cancellationToken);
                if (bank != null)
                {
                    model.BankAccountNumber = bank.AccountNo;
                    model.BankAccountName = bank.AccountName;
                }
            }

            return model;
        }

        private CreateCollectionViewModel MapToViewModel(Collection model)
        {
            return new CreateCollectionViewModel
            {
                MsapCollectionId = model.MsapCollectionId,
                MsapCollectionNumber = model.MsapCollectionNumber,
                IsUndocumented = model.IsUndocumented,
                Date = model.Date,
                CustomerId = model.CustomerId,
                Amount = model.Amount,
                EWT = model.EWT,
                WVAT = model.WVAT,
                CashAmount = model.CashAmount,
                CheckAmount = model.CheckAmount,
                CheckNumber = model.CheckNumber,
                CheckDate = model.CheckDate,
                CheckBank = model.CheckBank,
                CheckBranch = model.CheckBranch,
                BankId = model.BankId,
                ReferenceNo = model.ReferenceNo,
                Remarks = model.Remarks,
                DepositDate = model.DepositDate,
            };
        }

        public async Task<List<SelectListItem>> GetCustomerSelectListAsync(int? collectionId, int customerId, CancellationToken cancellationToken)
        {
            var cust = await unitOfWork.Customer.GetAsync(c => c.CustomerId == customerId, cancellationToken);
            return await unitOfWork.Collection.GetMsapCustomersWithCollectiblesSelectList(
                collectionId ?? 0,
                cust?.Type ?? string.Empty,
                cancellationToken);
        }

        private async Task<List<SelectListItem>?> GetEditBillingsAsync(int? customerId, int? collectionId, CancellationToken cancellationToken)
        {
            var list = await unitOfWork.Collection.GetMsapUncollectedBillingsByCustomer(customerId, cancellationToken);
            if (collectionId.HasValue && collectionId.Value != 0)
            {
                var model = await unitOfWork.Collection.GetAsync(c => c.MsapCollectionId == collectionId.Value, cancellationToken);
                if (model?.CustomerId == customerId)
                {
                    list?.AddRange(await unitOfWork.Collection.GetMsapCollectedBillsById(collectionId.Value, cancellationToken));
                }
            }
            return list;
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
