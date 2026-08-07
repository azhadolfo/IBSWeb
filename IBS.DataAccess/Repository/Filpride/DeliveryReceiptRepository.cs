using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.DTOs;
using IBS.Models.Enums;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class DeliveryReceiptRepository : Repository<FilprideDeliveryReceipt>, IDeliveryReceiptRepository
    {
        private readonly ApplicationDbContext _db;

        public DeliveryReceiptRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string companyClaims, string documentType, CancellationToken cancellationToken = default)
        {
            return documentType switch
            {
                nameof(DocumentType.Documented) => await GenerateDocumentedCodeAsync(companyClaims, cancellationToken),
                nameof(DocumentType.Undocumented) => await GenerateUnDocumentedCodeAsync(companyClaims, cancellationToken),
                _ => throw new ArgumentException("Invalid type")
            };
        }

        private async Task<string> GenerateDocumentedCodeAsync(string companyClaims, CancellationToken cancellationToken = default)
        {
            var lastDr = await _db
                .FilprideDeliveryReceipts
                .AsNoTracking()
                .OrderByDescending(x => x.DeliveryReceiptNo.Length)
                .ThenByDescending(x => x.DeliveryReceiptNo)
                .FirstOrDefaultAsync(x =>
                    x.Company == companyClaims &&
                    x.Type == nameof(DocumentType.Documented) &&
                    !x.DeliveryReceiptNo.Contains("BEG"),
                    cancellationToken);

            if (lastDr == null)
            {
                return "DR0000000001";
            }

            var lastSeries = lastDr.DeliveryReceiptNo;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
        }

        private async Task<string> GenerateUnDocumentedCodeAsync(string companyClaims, CancellationToken cancellationToken = default)
        {
            var lastDr = await _db
                .FilprideDeliveryReceipts
                .AsNoTracking()
                .OrderByDescending(x => x.DeliveryReceiptNo.Length)
                .ThenByDescending(x => x.DeliveryReceiptNo)
                .FirstOrDefaultAsync(x =>
                        x.Company == companyClaims &&
                        x.Type == nameof(DocumentType.Undocumented) &&
                        !x.DeliveryReceiptNo.Contains("BEG"),
                    cancellationToken);

            if (lastDr == null)
            {
                return "DRU000000001";
            }

            var lastSeries = lastDr.DeliveryReceiptNo;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
        }

        public override async Task<IEnumerable<FilprideDeliveryReceipt>> GetAllAsync(Expression<Func<FilprideDeliveryReceipt, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideDeliveryReceipt> query = dbSet
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(po => po!.Product)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .Include(dr => dr.Hauler)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos!.PickUpPoint)
                .Include(dr => dr.Customer)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos!.Commissionee)
                .Include(dr => dr.PurchaseOrder).ThenInclude(po => po!.Product)
                .Include(dr => dr.AuthorityToLoad)
                .Include(dr => dr.Details).ThenInclude(d => d.CustomerOrderSlip).ThenInclude(cos => cos!.Product)
                .Include(dr => dr.Details).ThenInclude(d => d.CustomerOrderSlip).ThenInclude(cos => cos!.PickUpPoint)
                .Include(dr => dr.Details).ThenInclude(d => d.CustomerOrderSlip).ThenInclude(cos => cos!.Commissionee)
                .Include(dr => dr.Details).ThenInclude(d => d.PurchaseOrder).ThenInclude(po => po!.Product)
                .Include(dr => dr.Details).ThenInclude(d => d.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .Include(dr => dr.Details).ThenInclude(d => d.AuthorityToLoad);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override IQueryable<FilprideDeliveryReceipt> GetAllQuery(Expression<Func<FilprideDeliveryReceipt, bool>>? filter = null)
        {
            IQueryable<FilprideDeliveryReceipt> query = dbSet
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(po => po!.Product)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .Include(dr => dr.Hauler)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos!.PickUpPoint)
                .Include(dr => dr.Customer)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos!.Commissionee)
                .Include(dr => dr.PurchaseOrder).ThenInclude(po => po!.Product)
                .Include(dr => dr.AuthorityToLoad)
                .Include(dr => dr.Details).ThenInclude(d => d.CustomerOrderSlip).ThenInclude(cos => cos!.Product)
                .Include(dr => dr.Details).ThenInclude(d => d.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .Include(dr => dr.Details).ThenInclude(d => d.AuthorityToLoad)
                .AsSplitQuery()
                .AsNoTracking();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return query;
        }

        public override async Task<FilprideDeliveryReceipt?> GetAsync(Expression<Func<FilprideDeliveryReceipt, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(po => po!.Product)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .Include(dr => dr.Hauler)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos!.PickUpPoint)
                .Include(dr => dr.Customer)
                .Include(dr => dr.PurchaseOrder).ThenInclude(po => po!.Product)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos!.Commissionee)
                .Include(dr => dr.AuthorityToLoad)
                .Include(dr => dr.Details).ThenInclude(d => d.CustomerOrderSlip).ThenInclude(cos => cos!.Product)
                .Include(dr => dr.Details).ThenInclude(d => d.CustomerOrderSlip).ThenInclude(cos => cos!.PickUpPoint)
                .Include(dr => dr.Details).ThenInclude(d => d.CustomerOrderSlip).ThenInclude(cos => cos!.Commissionee)
                .Include(dr => dr.Details).ThenInclude(d => d.PurchaseOrder).ThenInclude(po => po!.Product)
                .Include(dr => dr.Details).ThenInclude(d => d.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .Include(dr => dr.Details).ThenInclude(d => d.AuthorityToLoad)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken = default)
        {
            var existingRecord = await GetAsync(dr => dr.DeliveryReceiptId == viewModel.DeliveryReceiptId,
                cancellationToken) ?? throw new NullReferenceException("DeliveryReceipt not found");

            var customerOrderSlip = await _db.FilprideCustomerOrderSlips
                .FirstOrDefaultAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId,
                    cancellationToken) ?? throw new NullReferenceException("CustomerOrderSlip not found");

            var hauler = await _db.FilprideSuppliers.FirstOrDefaultAsync(x => x.SupplierId == viewModel.HaulerId, cancellationToken);

            #region--Update COS

            await DeductTheVolumeToCos(existingRecord.CustomerOrderSlipId, existingRecord.Quantity, cancellationToken);

            if (viewModel.Volume > customerOrderSlip.BalanceQuantity)
            {
                throw new ArgumentException("The inputted balance exceeds the remaining balance of COS.");
            }

            customerOrderSlip.DeliveredQuantity += viewModel.Volume;
            customerOrderSlip.BalanceQuantity -= viewModel.Volume;

            if (customerOrderSlip.BalanceQuantity == 0)
            {
                customerOrderSlip.Status = nameof(CosStatus.Completed);
            }

            #endregion

            #region--Update Appointed PO

            await UpdatePreviousAppointedSupplierAsync(existingRecord);

            #endregion

            existingRecord.Date = viewModel.Date;
            existingRecord.CustomerOrderSlipId = viewModel.CustomerOrderSlipId;
            existingRecord.CustomerId = viewModel.CustomerId;
            existingRecord.Remarks = viewModel.Remarks;
            existingRecord.Quantity = viewModel.Volume;
            existingRecord.TotalAmount = viewModel.Volume * customerOrderSlip.DeliveredPrice;
            existingRecord.ManualDrNo = viewModel.ManualDrNo;
            existingRecord.Driver = viewModel.Driver;
            existingRecord.PlateNo = viewModel.PlateNo;
            existingRecord.HaulerId = viewModel.HaulerId ?? customerOrderSlip.HaulerId;
            existingRecord.ECC = viewModel.ECC;
            existingRecord.Freight = viewModel.Freight;
            existingRecord.FreightAmount = existingRecord.Quantity * (existingRecord.Freight + existingRecord.ECC);
            existingRecord.AuthorityToLoadNo = viewModel.ATLNo;
            existingRecord.CommissioneeId = customerOrderSlip.CommissioneeId;
            existingRecord.CommissionRate = customerOrderSlip.CommissionRate;
            existingRecord.CommissionAmount = existingRecord.Quantity * existingRecord.CommissionRate;
            existingRecord.CustomerAddress = customerOrderSlip.CustomerAddress;
            existingRecord.CustomerTin = customerOrderSlip.CustomerTin;
            existingRecord.HaulerName = hauler?.SupplierName;
            existingRecord.HaulerVatType = hauler?.VatType;
            existingRecord.HaulerTaxType = hauler?.TaxType;
            existingRecord.AuthorityToLoadId = viewModel.ATLId;
            existingRecord.PurchaseOrderId = viewModel.PurchaseOrderId;

            await AssignNewPurchaseOrderAsync(existingRecord);

            if (_db.ChangeTracker.HasChanges())
            {
                existingRecord.EditedBy = viewModel.CurrentUser;
                existingRecord.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                FilprideAuditTrail auditTrailBook = new(existingRecord.EditedBy!, $"Edit delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", existingRecord.Company);
                await _db.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        public async Task<List<SelectListItem>> GetDeliveryReceiptListAsync(string companyClaims, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideDeliveryReceipts
                .OrderBy(dr => dr.DeliveryReceiptId)
                .Where(dr => dr.DeliveredDate != null &&
                             dr.Company == companyClaims)
                .Select(dr => new SelectListItem
                {
                    Value = dr.DeliveryReceiptId.ToString(),
                    Text = dr.DeliveryReceiptNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetDeliveryReceiptListForSalesInvoice(string companyClaims, int cosId, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideDeliveryReceipts
                    .OrderBy(dr => dr.DeliveryReceiptId)
                    .Where(dr =>
                        dr.CustomerOrderSlipId == cosId &&
                        dr.DeliveredDate != null &&
                        !dr.HasAlreadyInvoiced &&
                        dr.Status == nameof(DRStatus.ForInvoicing) &&
                        dr.Company == companyClaims)
                    .Select(dr => new SelectListItem
                    {
                        Value = dr.DeliveryReceiptId.ToString(),
                        Text = dr.DeliveryReceiptNo
                    })
                    .ToListAsync(cancellationToken);
        }

        public async Task PostAsync(FilprideDeliveryReceipt deliveryReceipt, CancellationToken cancellationToken = default)
        {
            try
            {
                #region General Ledger Book Recording

                var ledgers = new List<FilprideGeneralLedgerBook>();
                var unitOfWork = new UnitOfWork(_db);
                var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
                var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException("Account title '101010100' not found.");
                var arTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020100") ?? throw new ArgumentException("Account title '101020100' not found.");
                var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account title '201030100' not found.");
                var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                var apHaulingPayableTitle = accountTitlesDto.Find(c => c.AccountNumber == "201010300") ?? throw new ArgumentException("Account title '201010300' not found.");
                var apCommissionPayableTitle = accountTitlesDto.Find(c => c.AccountNumber == "201010200") ?? throw new ArgumentException("Account title '201010200' not found.");
                var arTradeCwt = accountTitlesDto.Find(c => c.AccountNumber == "101020200") ?? throw new ArgumentException("Account title '101020200' not found.");
                var arTradeCwv = accountTitlesDto.Find(c => c.AccountNumber == "101020300") ?? throw new ArgumentException("Account title '101020300' not found.");

                var detailLines = deliveryReceipt.Details.Any()
                    ? deliveryReceipt.Details.ToList()
                    : new List<FilprideDeliveryReceiptDetail>
                    {
                        new()
                        {
                            CustomerOrderSlipId = deliveryReceipt.CustomerOrderSlipId,
                            PurchaseOrderId = deliveryReceipt.PurchaseOrderId ?? 0,
                            AuthorityToLoadId = deliveryReceipt.AuthorityToLoadId,
                            AuthorityToLoadNo = deliveryReceipt.AuthorityToLoadNo,
                            ProductId = deliveryReceipt.CustomerOrderSlip!.ProductId,
                            ProductName = deliveryReceipt.CustomerOrderSlip.ProductName,
                            Quantity = deliveryReceipt.Quantity,
                            UnitPrice = deliveryReceipt.CustomerOrderSlip!.DeliveredPrice,
                            TotalAmount = deliveryReceipt.TotalAmount,
                            CustomerOrderSlip = deliveryReceipt.CustomerOrderSlip,
                            PurchaseOrder = deliveryReceipt.PurchaseOrder,
                            AuthorityToLoad = deliveryReceipt.AuthorityToLoad
                        }
                    };

                var missingCosIds = detailLines
                    .Where(d => d.CustomerOrderSlip == null)
                    .Select(d => d.CustomerOrderSlipId)
                    .Distinct()
                    .ToList();

                var missingPoIds = detailLines
                    .Where(d => d.PurchaseOrder == null)
                    .Select(d => d.PurchaseOrderId)
                    .Distinct()
                    .ToList();

                var cosLookup = missingCosIds.Count == 0
                    ? new Dictionary<int, FilprideCustomerOrderSlip>()
                    : await _db.FilprideCustomerOrderSlips
                        .Include(c => c.Product)
                        .Include(c => c.Customer)
                        .Include(c => c.Commissionee)
                        .Include(c => c.PickUpPoint)
                        .Where(c => missingCosIds.Contains(c.CustomerOrderSlipId))
                        .ToDictionaryAsync(c => c.CustomerOrderSlipId, cancellationToken);

                var poLookup = missingPoIds.Count == 0
                    ? new Dictionary<int, IBS.Models.Filpride.AccountsPayable.FilpridePurchaseOrder>()
                    : await _db.FilpridePurchaseOrders
                        .Include(p => p.Product)
                        .Include(p => p.Supplier)
                        .Include(p => p.ActualPrices)
                        .Where(p => missingPoIds.Contains(p.PurchaseOrderId))
                        .ToDictionaryAsync(p => p.PurchaseOrderId, cancellationToken);

                decimal AllocateByQuantity(decimal unitAmount, decimal lineQuantity, bool isLastLine, ref decimal allocatedGrossAmount)
                {
                    var totalGrossAmount = unitAmount * deliveryReceipt.Quantity;
                    decimal lineGrossAmount;

                    if (isLastLine)
                    {
                        lineGrossAmount = totalGrossAmount - allocatedGrossAmount;
                    }
                    else
                    {
                        lineGrossAmount = deliveryReceipt.Quantity == 0
                            ? 0m
                            : RoundToFourDecimalPlaces(totalGrossAmount * (lineQuantity / deliveryReceipt.Quantity));
                    }

                    allocatedGrossAmount += lineGrossAmount;
                    return lineGrossAmount;
                }

                var allocatedFreight = 0m;
                var allocatedEcc = 0m;

                for (var index = 0; index < detailLines.Count; index++)
                {
                    var detail = detailLines[index];
                    var customerOrderSlip = detail.CustomerOrderSlip ?? cosLookup[detail.CustomerOrderSlipId];
                    var purchaseOrder = detail.PurchaseOrder ?? poLookup[detail.PurchaseOrderId];
                    var isLastLine = index == detailLines.Count - 1;
                    var description = $"{customerOrderSlip.DeliveryOption} by {deliveryReceipt.Hauler?.SupplierName ?? "Client"}";
                    var lineFreightGrossAmount = AllocateByQuantity(deliveryReceipt.Freight, detail.Quantity, isLastLine, ref allocatedFreight);
                    var lineEccGrossAmount = AllocateByQuantity(deliveryReceipt.ECC, detail.Quantity, isLastLine, ref allocatedEcc);

                    var (salesAcctNo, _) = GetSalesAccountTitle(customerOrderSlip.Product!.ProductCode);
                    var (cogsAcctNo, _) = GetCogsAccountTitle(customerOrderSlip.Product.ProductCode);
                    var (freightAcctNo, _) = GetFreightAccount(customerOrderSlip.Product.ProductCode);
                    var (commissionAcctNo, _) = GetCommissionAccount(customerOrderSlip.Product.ProductCode);
                    var (inventoryAcctNo, _) = GetInventoryAccountTitle(purchaseOrder.Product!.ProductCode);
                    var salesTitle = accountTitlesDto.Find(c => c.AccountNumber == salesAcctNo) ?? throw new ArgumentException($"Account title '{salesAcctNo}' not found.");
                    var cogsTitle = accountTitlesDto.Find(c => c.AccountNumber == cogsAcctNo) ?? throw new ArgumentException($"Account title '{cogsAcctNo}' not found.");
                    var freightTitle = accountTitlesDto.Find(c => c.AccountNumber == freightAcctNo) ?? throw new ArgumentException($"Account title '{freightAcctNo}' not found.");
                    var commissionTitle = accountTitlesDto.Find(c => c.AccountNumber == commissionAcctNo) ?? throw new ArgumentException($"Account title '{commissionAcctNo}' not found.");
                    var inventoryTitle = accountTitlesDto.Find(c => c.AccountNumber == inventoryAcctNo) ?? throw new ArgumentException($"Account title '{inventoryAcctNo}' not found.");

                    var netOfVatAmount = customerOrderSlip.VatType == SD.VatType_Vatable
                        ? ComputeNetOfVat(detail.TotalAmount)
                        : detail.TotalAmount;
                    var vatAmount = customerOrderSlip.VatType == SD.VatType_Vatable
                        ? ComputeVatAmount(netOfVatAmount)
                        : 0m;
                    var arTradeCwtAmount = customerOrderSlip.HasEWT ? ComputeEwtAmount(netOfVatAmount, 0.01m) : 0m;
                    var arTradeCwvAmount = customerOrderSlip.HasWVAT ? ComputeEwtAmount(netOfVatAmount, 0.05m) : 0m;
                    var netOfEwtAmount = arTradeCwtAmount > 0 || arTradeCwvAmount > 0
                        ? ComputeNetOfEwt(detail.TotalAmount, arTradeCwtAmount + arTradeCwvAmount)
                        : detail.TotalAmount;
                    var deliveredFreight = customerOrderSlip.DeliveryOption == SD.DeliveryOption_DirectDelivery
                        ? (decimal)customerOrderSlip.Freight!
                        : 0m;

                    if (arTradeCwtAmount > 0)
                    {
                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                            Reference = deliveryReceipt.DeliveryReceiptNo,
                            Description = description,
                            AccountId = arTradeCwt.AccountId,
                            AccountNo = arTradeCwt.AccountNumber,
                            AccountTitle = arTradeCwt.AccountName,
                            Debit = arTradeCwtAmount,
                            Credit = 0,
                            Company = deliveryReceipt.Company,
                            CreatedBy = deliveryReceipt.PostedBy!,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            ModuleType = nameof(ModuleType.Sales)
                        });
                    }

                    if (arTradeCwvAmount > 0)
                    {
                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                            Reference = deliveryReceipt.DeliveryReceiptNo,
                            Description = description,
                            AccountId = arTradeCwv.AccountId,
                            AccountNo = arTradeCwv.AccountNumber,
                            AccountTitle = arTradeCwv.AccountName,
                            Debit = arTradeCwvAmount,
                            Credit = 0,
                            Company = deliveryReceipt.Company,
                            CreatedBy = deliveryReceipt.PostedBy!,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            ModuleType = nameof(ModuleType.Sales)
                        });
                    }

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = description,
                        AccountId = customerOrderSlip.Terms == SD.Terms_Cod ? cashInBankTitle.AccountId : arTradeTitle.AccountId,
                        AccountNo = customerOrderSlip.Terms == SD.Terms_Cod ? cashInBankTitle.AccountNumber : arTradeTitle.AccountNumber,
                        AccountTitle = customerOrderSlip.Terms == SD.Terms_Cod ? cashInBankTitle.AccountName : arTradeTitle.AccountName,
                        Debit = netOfEwtAmount,
                        Credit = 0,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        SubAccountType = SubAccountType.Customer,
                        SubAccountId = customerOrderSlip.Terms != SD.Terms_Cod ? deliveryReceipt.CustomerId : null,
                        SubAccountName = customerOrderSlip.Terms != SD.Terms_Cod ? customerOrderSlip.CustomerName : null,
                        ModuleType = nameof(ModuleType.Sales)
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = description,
                        AccountId = salesTitle.AccountId,
                        AccountNo = salesTitle.AccountNumber,
                        AccountTitle = salesTitle.AccountName,
                        Debit = 0,
                        Credit = netOfVatAmount,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Sales)
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = description,
                        AccountId = vatOutputTitle.AccountId,
                        AccountNo = vatOutputTitle.AccountNumber,
                        AccountTitle = vatOutputTitle.AccountName,
                        Debit = 0,
                        Credit = vatAmount,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Sales)
                    });

                    var inventoryTransactions = await _db.FilprideInventories
                        .Where(i => i.Reference == deliveryReceipt.DeliveryReceiptNo
                                    && i.Company == deliveryReceipt.Company
                                    && i.ProductId == detail.ProductId
                                    && i.POId == detail.PurchaseOrderId)
                        .ToListAsync(cancellationToken);
                    decimal cogsNetOfVat;
                    if (inventoryTransactions.Any())
                    {
                        cogsNetOfVat = inventoryTransactions.Sum(i => i.Total);
                    }
                    else
                    {
                        var poPrice = RoundToFourDecimalPlaces(
                            await unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderCost(detail.PurchaseOrderId, cancellationToken) + deliveredFreight);
                        var cogsCost = purchaseOrder.VatType == SD.VatType_Vatable ? ComputeNetOfVat(poPrice) : poPrice;
                        cogsNetOfVat = detail.Quantity * cogsCost;
                    }

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = description,
                        AccountId = cogsTitle.AccountId,
                        AccountNo = cogsTitle.AccountNumber,
                        AccountTitle = cogsTitle.AccountName,
                        Debit = cogsNetOfVat,
                        Credit = 0,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Sales)
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = description,
                        AccountId = inventoryTitle.AccountId,
                        AccountNo = inventoryTitle.AccountNumber,
                        AccountTitle = inventoryTitle.AccountName,
                        Debit = 0,
                        Credit = cogsNetOfVat,
                        Company = deliveryReceipt.Company,
                        CreatedBy = deliveryReceipt.PostedBy!,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Sales)
                    });

                    if (lineFreightGrossAmount > 0)
                    {
                        var freightNetOfVat = deliveryReceipt.HaulerVatType == SD.VatType_Vatable
                            ? ComputeNetOfVat(lineFreightGrossAmount)
                            : lineFreightGrossAmount;
                        var freightVatAmount = deliveryReceipt.HaulerVatType == SD.VatType_Vatable
                            ? ComputeVatAmount(freightNetOfVat)
                            : 0m;

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                            Reference = deliveryReceipt.DeliveryReceiptNo,
                            Description = $"{description} for Freight",
                            AccountId = freightTitle.AccountId,
                            AccountNo = freightTitle.AccountNumber,
                            AccountTitle = freightTitle.AccountName,
                            Debit = freightNetOfVat,
                            Credit = 0,
                            Company = deliveryReceipt.Company,
                            CreatedBy = deliveryReceipt.PostedBy!,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            ModuleType = nameof(ModuleType.Sales)
                        });

                        if (freightVatAmount > 0)
                        {
                            ledgers.Add(new FilprideGeneralLedgerBook
                            {
                                Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                                Reference = deliveryReceipt.DeliveryReceiptNo,
                                Description = $"{description} for Freight",
                                AccountId = vatInputTitle.AccountId,
                                AccountNo = vatInputTitle.AccountNumber,
                                AccountTitle = vatInputTitle.AccountName,
                                Debit = freightVatAmount,
                                Credit = 0,
                                Company = deliveryReceipt.Company,
                                CreatedBy = deliveryReceipt.PostedBy!,
                                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                                ModuleType = nameof(ModuleType.Sales)
                            });
                        }
                    }

                    if (lineEccGrossAmount > 0)
                    {
                        var eccNetOfVat = deliveryReceipt.HaulerVatType == SD.VatType_Vatable
                            ? ComputeNetOfVat(lineEccGrossAmount)
                            : lineEccGrossAmount;
                        var eccVatAmount = deliveryReceipt.HaulerVatType == SD.VatType_Vatable
                            ? ComputeVatAmount(eccNetOfVat)
                            : 0m;

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                            Reference = deliveryReceipt.DeliveryReceiptNo,
                            Description = $"{description} for ECC",
                            AccountId = freightTitle.AccountId,
                            AccountNo = freightTitle.AccountNumber,
                            AccountTitle = freightTitle.AccountName,
                            Debit = eccNetOfVat,
                            Credit = 0,
                            Company = deliveryReceipt.Company,
                            CreatedBy = deliveryReceipt.PostedBy!,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            ModuleType = nameof(ModuleType.Sales)
                        });

                        if (eccVatAmount > 0)
                        {
                            ledgers.Add(new FilprideGeneralLedgerBook
                            {
                                Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                                Reference = deliveryReceipt.DeliveryReceiptNo,
                                Description = $"{description} for ECC",
                                AccountId = vatInputTitle.AccountId,
                                AccountNo = vatInputTitle.AccountNumber,
                                AccountTitle = vatInputTitle.AccountName,
                                Debit = eccVatAmount,
                                Credit = 0,
                                Company = deliveryReceipt.Company,
                                CreatedBy = deliveryReceipt.PostedBy!,
                                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                                ModuleType = nameof(ModuleType.Sales)
                            });
                        }
                    }

                    var lineHaulingGrossAmount = lineFreightGrossAmount + lineEccGrossAmount;
                    if (lineHaulingGrossAmount > 0)
                    {
                        var lineHaulingNetOfVat = deliveryReceipt.HaulerVatType == SD.VatType_Vatable ? ComputeNetOfVat(lineHaulingGrossAmount) : lineHaulingGrossAmount;
                        var lineHaulingEwtAmount = deliveryReceipt.HaulerTaxType == SD.TaxType_WithTax
                            ? ComputeEwtAmount(lineHaulingNetOfVat, deliveryReceipt.Hauler!.WithholdingTaxPercent ?? 0m)
                            : 0m;
                        var lineHaulingNetOfEwt = lineHaulingEwtAmount > 0
                            ? ComputeNetOfEwt(lineHaulingGrossAmount, lineHaulingEwtAmount)
                            : lineHaulingGrossAmount;
                        var haulingEwtTitle = lineHaulingEwtAmount > 0
                            ? accountTitlesDto.FirstOrDefault(c =>
                                  c.AccountNumber == (WithholdingTaxHelper.GetAccountNumberByPercent(deliveryReceipt.Hauler!.WithholdingTaxPercent ?? 0m)
                                      ?? throw new ArgumentException($"No EWT account mapping found for tax percentage '{deliveryReceipt.Hauler!.WithholdingTaxPercent ?? 0m}'.")))
                              ?? throw new ArgumentException("Mapped EWT account title not found.")
                            : null;

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                            Reference = deliveryReceipt.DeliveryReceiptNo,
                            Description = description,
                            AccountId = apHaulingPayableTitle.AccountId,
                            AccountNo = apHaulingPayableTitle.AccountNumber,
                            AccountTitle = apHaulingPayableTitle.AccountName,
                            Debit = 0,
                            Credit = lineHaulingNetOfEwt,
                            Company = deliveryReceipt.Company,
                            CreatedBy = deliveryReceipt.PostedBy!,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            SubAccountType = SubAccountType.Supplier,
                            SubAccountId = deliveryReceipt.HaulerId,
                            SubAccountName = deliveryReceipt.HaulerName,
                            ModuleType = nameof(ModuleType.Sales)
                        });

                        if (lineHaulingEwtAmount > 0)
                        {
                            ledgers.Add(new FilprideGeneralLedgerBook
                            {
                                Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                                Reference = deliveryReceipt.DeliveryReceiptNo,
                                Description = description,
                                AccountId = haulingEwtTitle!.AccountId,
                                AccountNo = haulingEwtTitle.AccountNumber,
                                AccountTitle = haulingEwtTitle.AccountName,
                                Debit = 0,
                                Credit = lineHaulingEwtAmount,
                                Company = deliveryReceipt.Company,
                                CreatedBy = deliveryReceipt.PostedBy!,
                                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                                ModuleType = nameof(ModuleType.Sales)
                            });
                        }
                    }

                    var commissionGrossAmount = detail.Quantity * customerOrderSlip.CommissionRate;
                    if (commissionGrossAmount > 0 && customerOrderSlip.CommissioneeId.HasValue && customerOrderSlip.Commissionee != null)
                    {
                        var commissionEwtAmount = customerOrderSlip.CommissioneeTaxType == SD.TaxType_WithTax
                            ? ComputeEwtAmount(commissionGrossAmount, customerOrderSlip.Commissionee.WithholdingTaxPercent ?? 0m)
                            : 0m;
                        var commissionNetOfEwt = commissionEwtAmount > 0 ? ComputeNetOfEwt(commissionGrossAmount, commissionEwtAmount) : commissionGrossAmount;
                        var commissionEwtTitle = commissionEwtAmount > 0
                            ? accountTitlesDto.FirstOrDefault(c =>
                                  c.AccountNumber == (WithholdingTaxHelper.GetAccountNumberByPercent(customerOrderSlip.Commissionee.WithholdingTaxPercent ?? 0m)
                                      ?? throw new ArgumentException($"No EWT account mapping found for tax percentage '{customerOrderSlip.Commissionee.WithholdingTaxPercent ?? 0m}'.")))
                              ?? throw new ArgumentException("Mapped EWT account title not found.")
                            : null;

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                            Reference = deliveryReceipt.DeliveryReceiptNo,
                            Description = $"{description}.",
                            AccountId = commissionTitle.AccountId,
                            AccountNo = commissionTitle.AccountNumber,
                            AccountTitle = commissionTitle.AccountName,
                            Debit = commissionGrossAmount,
                            Credit = 0,
                            Company = deliveryReceipt.Company,
                            CreatedBy = deliveryReceipt.PostedBy!,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            ModuleType = nameof(ModuleType.Sales)
                        });

                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                            Reference = deliveryReceipt.DeliveryReceiptNo,
                            Description = $"{description}.",
                            AccountId = apCommissionPayableTitle.AccountId,
                            AccountNo = apCommissionPayableTitle.AccountNumber,
                            AccountTitle = apCommissionPayableTitle.AccountName,
                            Debit = 0,
                            Credit = commissionNetOfEwt,
                            Company = deliveryReceipt.Company,
                            CreatedBy = deliveryReceipt.PostedBy!,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            SubAccountType = SubAccountType.Supplier,
                            SubAccountId = customerOrderSlip.CommissioneeId,
                            SubAccountName = customerOrderSlip.CommissioneeName,
                            ModuleType = nameof(ModuleType.Sales)
                        });

                        if (commissionEwtAmount > 0)
                        {
                            ledgers.Add(new FilprideGeneralLedgerBook
                            {
                                Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                                Reference = deliveryReceipt.DeliveryReceiptNo,
                                Description = $"{description}.",
                                AccountId = commissionEwtTitle!.AccountId,
                                AccountNo = commissionEwtTitle.AccountNumber,
                                AccountTitle = commissionEwtTitle.AccountName,
                                Debit = 0,
                                Credit = commissionEwtAmount,
                                Company = deliveryReceipt.Company,
                                CreatedBy = deliveryReceipt.PostedBy!,
                                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                                ModuleType = nameof(ModuleType.Sales)
                            });
                        }
                    }
                }

                if (!IsJournalEntriesBalanced(ledgers))
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

                await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                #endregion General Ledger Book Recording

                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        private async Task UpdateCosRemainingVolumeAsync(int cosId, decimal drVolume, CancellationToken cancellationToken)
        {
            var cos = await _db.FilprideCustomerOrderSlips
                .FirstOrDefaultAsync(po => po.CustomerOrderSlipId == cosId, cancellationToken)
                      ?? throw new InvalidOperationException("No record found.");

            cos.DeliveredQuantity += drVolume;
            cos.BalanceQuantity -= drVolume;

            if (cos.BalanceQuantity <= 0)
            {
                cos.Status = nameof(CosStatus.Completed);
            }
            else if (cos.BalanceQuantity >= 0 && cos.Status == nameof(CosStatus.Completed))
            {
                cos.Status = nameof(CosStatus.ForDR);
            }
        }

        public async Task DeductTheVolumeToCos(int cosId, decimal drVolume, CancellationToken cancellationToken = default)
        {
            var cos = await _db.FilprideCustomerOrderSlips
                .FirstOrDefaultAsync(po => po.CustomerOrderSlipId == cosId, cancellationToken)
                      ?? throw new InvalidOperationException("No record found.");

            if (cos.Status == nameof(CosStatus.Completed))
            {
                cos.Status = nameof(CosStatus.ForDR);
            }

            cos.DeliveredQuantity -= drVolume;
            cos.BalanceQuantity += drVolume;
            cos.IsDelivered = false;
        }

        public async Task UpdatePreviousAppointedSupplierAsync(FilprideDeliveryReceipt model)
        {
            var previousAppointedSupplier = await _db.FilprideBookAtlDetails
                .Include(x => x.AppointedSupplier)
                .FirstOrDefaultAsync(x => x.AuthorityToLoadId == model.AuthorityToLoadId
                                          && x.CustomerOrderSlipId == model.CustomerOrderSlipId
                                          && x.AppointedSupplier!.PurchaseOrderId == model.PurchaseOrderId)
                ?? throw new InvalidOperationException("Previous appointed supplier not found.");

            previousAppointedSupplier.UnservedQuantity += model.Quantity;
        }

        public async Task AssignNewPurchaseOrderAsync(FilprideDeliveryReceipt model)
        {
            var newAppointedSupplier = await _db.FilprideBookAtlDetails
                .Include(x => x.AppointedSupplier)
                .FirstOrDefaultAsync(x => x.AuthorityToLoadId == model.AuthorityToLoadId
                                          && x.CustomerOrderSlipId == model.CustomerOrderSlipId
                                          && x.AppointedSupplier!.PurchaseOrderId == model.PurchaseOrderId)
                ?? throw new InvalidOperationException("No atl detail found, contact the TNS.");

            newAppointedSupplier.UnservedQuantity -= model.Quantity;
        }

        public async Task AutoReversalEntryForInTransit(CancellationToken cancellationToken = default)
        {
            var today = DateTimeHelper.GetCurrentPhilippineTime();

            // Start of the current month
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // End of the previous month
            var endOfPreviousMonth = startOfMonth.AddDays(-1);

            var inTransits = await GetAllAsync(dr =>
                    dr.Date.Month == endOfPreviousMonth.Month &&
                    dr.Date.Year == endOfPreviousMonth.Year &&
                    dr.Status == nameof(DRStatus.PendingDelivery), cancellationToken);

            var poRepo = new PurchaseOrderRepository(_db);
            var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
            var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
            var apTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "201010100") ?? throw new ArgumentException("Account title '201010100' not found.");
            var ewtOnePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");

            foreach (var dr in inTransits.OrderBy(dr => dr.DeliveryReceiptNo))
            {
                var ledgers = new List<FilprideGeneralLedgerBook>();
                var purchaseOrderGroups = dr.Details.Any()
                    ? dr.Details
                        .Where(detail => detail.PurchaseOrder != null)
                        .GroupBy(detail => detail.PurchaseOrderId)
                        .Select(group => (PurchaseOrder: group.First().PurchaseOrder!, Quantity: group.Sum(detail => detail.Quantity)))
                        .ToList()
                    : dr.PurchaseOrder != null
                        ? new List<(IBS.Models.Filpride.AccountsPayable.FilpridePurchaseOrder PurchaseOrder, decimal Quantity)>
                        {
                            (dr.PurchaseOrder, dr.Quantity)
                        }
                        : new List<(IBS.Models.Filpride.AccountsPayable.FilpridePurchaseOrder PurchaseOrder, decimal Quantity)>();

                foreach (var purchaseOrderGroup in purchaseOrderGroups)
                {
                    var productCode = purchaseOrderGroup.PurchaseOrder.Product!.ProductCode;
                    var productCostGrossAmount = purchaseOrderGroup.Quantity * await poRepo.GetPurchaseOrderCost(purchaseOrderGroup.PurchaseOrder.PurchaseOrderId, cancellationToken);
                    var productCostNetOfVatAmount = ComputeNetOfVat(productCostGrossAmount);
                    var productCostVatAmount = ComputeVatAmount(productCostNetOfVatAmount);
                    var productCostEwtAmount = ComputeEwtAmount(productCostNetOfVatAmount, 0.01m);
                    var productCostNetOfEwt = ComputeNetOfEwt(productCostGrossAmount, productCostEwtAmount);
                    var (inventoryAcctNo, _) = GetInventoryAccountTitle(productCode);
                    var inventoryTitle = accountTitlesDto.Find(c => c.AccountNumber == inventoryAcctNo) ?? throw new ArgumentException($"Account title '{inventoryAcctNo}' not found.");

                    #region In-Transit Entries

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(endOfPreviousMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy}.",
                        AccountId = inventoryTitle.AccountId,
                        AccountNo = inventoryTitle.AccountNumber,
                        AccountTitle = inventoryTitle.AccountName,
                        Debit = productCostNetOfVatAmount,
                        Credit = 0,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(endOfPreviousMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy}.",
                        AccountId = vatInputTitle.AccountId,
                        AccountNo = vatInputTitle.AccountNumber,
                        AccountTitle = vatInputTitle.AccountName,
                        Debit = productCostVatAmount,
                        Credit = 0,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(endOfPreviousMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy}.",
                        AccountId = apTradeTitle.AccountId,
                        AccountNo = apTradeTitle.AccountNumber,
                        AccountTitle = apTradeTitle.AccountName,
                        Debit = 0,
                        Credit = productCostNetOfEwt,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId = purchaseOrderGroup.PurchaseOrder.SupplierId,
                        SubAccountName = purchaseOrderGroup.PurchaseOrder.SupplierName
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(endOfPreviousMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"In-Transit for the month of {endOfPreviousMonth:MMM yyyy}.",
                        AccountId = ewtOnePercent.AccountId,
                        AccountNo = ewtOnePercent.AccountNumber,
                        AccountTitle = ewtOnePercent.AccountName,
                        Debit = 0,
                        Credit = productCostEwtAmount,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    });

                    #endregion

                    #region Auto Reversal Entries

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(startOfMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                        AccountId = inventoryTitle.AccountId,
                        AccountNo = inventoryTitle.AccountNumber,
                        AccountTitle = inventoryTitle.AccountName,
                        Debit = 0,
                        Credit = productCostNetOfVatAmount,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(startOfMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                        AccountId = vatInputTitle.AccountId,
                        AccountNo = vatInputTitle.AccountNumber,
                        AccountTitle = vatInputTitle.AccountName,
                        Debit = 0,
                        Credit = productCostVatAmount,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(startOfMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                        AccountId = apTradeTitle.AccountId,
                        AccountNo = apTradeTitle.AccountNumber,
                        AccountTitle = apTradeTitle.AccountName,
                        Debit = productCostNetOfEwt,
                        Credit = 0,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId = purchaseOrderGroup.PurchaseOrder.SupplierId,
                        SubAccountName = purchaseOrderGroup.PurchaseOrder.SupplierName
                    });

                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = DateOnly.FromDateTime(startOfMonth),
                        Reference = dr.DeliveryReceiptNo,
                        Description = $"Auto reversal entries for the in-transit of {endOfPreviousMonth:MMM yyyy}.",
                        AccountId = ewtOnePercent.AccountId,
                        AccountNo = ewtOnePercent.AccountNumber,
                        AccountTitle = ewtOnePercent.AccountName,
                        Debit = productCostEwtAmount,
                        Credit = 0,
                        Company = dr.Company,
                        CreatedBy = "SYSTEM GENERATED",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    });

                    #endregion
                }

                if (!IsJournalEntriesBalanced(ledgers))
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

                await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> CheckIfManualDrNoExists(string manualDrNo)
        {
            return await _db.FilprideDeliveryReceipts
                .Where(dr => dr.CanceledBy == null && dr.VoidedBy == null)
                .AnyAsync(dr => dr.ManualDrNo == manualDrNo);
        }

        public async Task RecalculateDeliveryReceipts(
            int customerOrderSlipId,
            decimal updatedPrice,
            string userName,
            CancellationToken cancellationToken = default)
        {
            List<FilprideDeliveryReceipt> deliveryReceipts = await dbSet
                .Where(x => x.CustomerOrderSlipId == customerOrderSlipId
                            && x.VoidedBy == null
                            && x.CanceledBy == null)
                .Include(dr => dr.CustomerOrderSlip)
                    .ThenInclude(cos => cos!.Product)
                .ToListAsync(cancellationToken);

            foreach (FilprideDeliveryReceipt deliveryReceipt in deliveryReceipts)
            {
                decimal updatedAmount = deliveryReceipt.Quantity * updatedPrice;
                decimal difference = updatedAmount - deliveryReceipt.TotalAmount;
                deliveryReceipt.TotalAmount = updatedAmount;

                if (deliveryReceipt.DeliveredDate == null)
                {
                    continue;
                }

                await CreateEntriesForUpdatingPrice(deliveryReceipt, difference, userName, cancellationToken);
            }
        }

        private static (int? SupplierId, string? SupplierName) ResolveLockedPeriodSupplier(FilprideDeliveryReceipt deliveryReceipt)
        {
            var suppliers = deliveryReceipt.Details
                .Where(detail => detail.PurchaseOrder != null)
                .GroupBy(detail => detail.PurchaseOrder!.SupplierId)
                .Select(group => new
                {
                    SupplierId = group.Key,
                    SupplierName = group.First().PurchaseOrder!.SupplierName
                })
                .ToList();

            if (suppliers.Count == 1)
            {
                return (suppliers[0].SupplierId, suppliers[0].SupplierName);
            }

            if (suppliers.Count > 1)
            {
                return (null, "Multiple Suppliers");
            }

            return (deliveryReceipt.PurchaseOrder?.SupplierId, deliveryReceipt.PurchaseOrder?.SupplierName);
        }

        private async Task CreateEntriesForUpdatingPrice(FilprideDeliveryReceipt deliveryReceipt, decimal difference, string userName, CancellationToken cancellationToken = default)
        {
            try
            {
                #region General Ledger Book Recording

                var ledgers = new List<FilprideGeneralLedgerBook>();
                var (salesAcctNo, salesAcctTitle) = GetSalesAccountTitle(deliveryReceipt.CustomerOrderSlip!.Product!.ProductCode);
                var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
                var salesTitle = accountTitlesDto.Find(c => c.AccountNumber == salesAcctNo) ?? throw new ArgumentException($"Account title '{salesAcctNo}' not found.");
                var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException("Account title '101010100' not found.");
                var arTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020100") ?? throw new ArgumentException("Account title '101020100' not found.");
                var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account title '201030100' not found.");
                var arTradeCwt = accountTitlesDto.Find(c => c.AccountNumber == "101020200") ?? throw new ArgumentException("Account title '101020200' not found.");
                var arTradeCwv = accountTitlesDto.Find(c => c.AccountNumber == "101020300") ?? throw new ArgumentException("Account title '101020300' not found.");

                var unitOfWork = new UnitOfWork(_db);
                var deliveredDate = deliveryReceipt.DeliveredDate
                    ?? throw new InvalidOperationException($"Delivered date is required for DR#{deliveryReceipt.DeliveryReceiptNo}.");
                var firstDayOfTheMonth = DateTimeHelper.GetFirstDayOfCurrentPhilippineMonth();
                var isDeliveredPeriodPosted = await unitOfWork
                    .IsPeriodPostedAsync(Module.DeliveryReceipt, deliveredDate, cancellationToken);
                var postingDate = isDeliveredPeriodPosted
                    ? firstDayOfTheMonth
                    : deliveredDate;
                var signedDifference = difference;
                var particulars = $"Update Price on DR#{deliveryReceipt.DeliveryReceiptNo}. DR dated {deliveryReceipt.DeliveredDate}";
                var isIncremental = difference > 0;
                var (supplierId, supplierName) = ResolveLockedPeriodSupplier(deliveryReceipt);
                difference = Math.Abs(difference);

                var netOfVatAmount = deliveryReceipt.CustomerOrderSlip!.VatType == SD.VatType_Vatable
                    ? ComputeNetOfVat(difference)
                    : difference;
                var vatAmount = deliveryReceipt.CustomerOrderSlip.VatType == SD.VatType_Vatable
                    ? ComputeVatAmount(netOfVatAmount)
                    : 0m;
                var arTradeCwtAmount = deliveryReceipt.CustomerOrderSlip.HasEWT ? ComputeEwtAmount(netOfVatAmount, 0.01m) : 0m;
                var arTradeCwvAmount = deliveryReceipt.CustomerOrderSlip.HasWVAT ? ComputeEwtAmount(netOfVatAmount, 0.05m) : 0m;
                var netOfEwtAmount = arTradeCwtAmount > 0 || arTradeCwvAmount > 0
                    ? ComputeNetOfEwt(difference, (arTradeCwtAmount + arTradeCwvAmount))
                    : difference;

                if (arTradeCwtAmount > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = postingDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = particulars,
                        AccountId = arTradeCwt.AccountId,
                        AccountNo = arTradeCwt.AccountNumber,
                        AccountTitle = arTradeCwt.AccountName,
                        Debit = isIncremental ? arTradeCwtAmount : 0,
                        Credit = !isIncremental ? arTradeCwtAmount : 0,
                        Company = deliveryReceipt.Company,
                        CreatedBy = userName,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Sales)
                    });
                }

                if (arTradeCwvAmount > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = postingDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = particulars,
                        AccountId = arTradeCwv.AccountId,
                        AccountNo = arTradeCwv.AccountNumber,
                        AccountTitle = arTradeCwv.AccountName,
                        Debit = isIncremental ? arTradeCwvAmount : 0,
                        Credit = !isIncremental ? arTradeCwvAmount : 0,
                        Company = deliveryReceipt.Company,
                        CreatedBy = userName,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Sales)
                    });
                }

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = postingDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = particulars,
                    AccountId = deliveryReceipt.CustomerOrderSlip.Terms == SD.Terms_Cod ? cashInBankTitle.AccountId : arTradeTitle.AccountId,
                    AccountNo = deliveryReceipt.CustomerOrderSlip.Terms == SD.Terms_Cod ? cashInBankTitle.AccountNumber : arTradeTitle.AccountNumber,
                    AccountTitle = deliveryReceipt.CustomerOrderSlip.Terms == SD.Terms_Cod ? cashInBankTitle.AccountName : arTradeTitle.AccountName,
                    Debit = isIncremental ? netOfEwtAmount : 0,
                    Credit = !isIncremental ? netOfEwtAmount : 0,
                    Company = deliveryReceipt.Company,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    SubAccountType = SubAccountType.Customer,
                    SubAccountId = deliveryReceipt.CustomerOrderSlip.Terms != SD.Terms_Cod
                        ? deliveryReceipt.CustomerId
                        : null,
                    SubAccountName = deliveryReceipt.CustomerOrderSlip.Terms != SD.Terms_Cod
                        ? deliveryReceipt.CustomerOrderSlip.CustomerName
                        : null,
                    ModuleType = nameof(ModuleType.Sales)
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = postingDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = particulars,
                    AccountId = salesTitle.AccountId,
                    AccountNo = salesTitle.AccountNumber,
                    AccountTitle = salesTitle.AccountName,
                    Debit = !isIncremental ? netOfVatAmount : 0,
                    Credit = isIncremental ? netOfVatAmount : 0,
                    Company = deliveryReceipt.Company,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModuleType = nameof(ModuleType.Sales)
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = postingDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = particulars,
                    AccountId = vatOutputTitle.AccountId,
                    AccountNo = vatOutputTitle.AccountNumber,
                    AccountTitle = vatOutputTitle.AccountName,
                    Debit = !isIncremental ? vatAmount : 0,
                    Credit = isIncremental ? vatAmount : 0,
                    Company = deliveryReceipt.Company,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModuleType = nameof(ModuleType.Sales)
                });

                if (!IsJournalEntriesBalanced(ledgers))
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

                await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
                await unitOfWork.LockedPeriodAdjustment.AddIfPeriodPostedAsync(new LockedPeriodAdjustmentRequestDto
                {
                    Module = Module.DeliveryReceipt,
                    TransactionDate = deliveredDate,
                    EntityType = Module.DeliveryReceipt,
                    EntityNo = deliveryReceipt.DeliveryReceiptNo,
                    CustomerId = deliveryReceipt.CustomerId,
                    CustomerName = deliveryReceipt.CustomerOrderSlip?.CustomerName,
                    SupplierId = supplierId,
                    SupplierName = supplierName,
                    AdjustmentType = LockedPeriodAdjustmentType.SellingPrice,
                    OldValue = GetRoundedUnitValue(deliveryReceipt.TotalAmount - signedDifference, deliveryReceipt.Quantity),
                    NewValue = GetRoundedUnitValue(deliveryReceipt.TotalAmount, deliveryReceipt.Quantity),
                    AdjustmentValue = signedDifference,
                    AffectedQuantity = deliveryReceipt.Quantity,
                    Reason = "Update selling price in COS",
                    CreatedBy = userName
                }, cancellationToken);

                #endregion General Ledger Book Recording

                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        public async Task CreateEntriesForUpdatingCommission(FilprideDeliveryReceipt deliveryReceipt,
            decimal difference,
            string userName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var ledgers = new List<FilprideGeneralLedgerBook>();
                var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
                var (commissionAcctNo, commissionAcctTitle) = GetCommissionAccount(deliveryReceipt.CustomerOrderSlip!.Product!.ProductCode);
                var commissionTitle = accountTitlesDto.Find(c => c.AccountNumber == commissionAcctNo)
                                      ?? throw new ArgumentException($"Account title '{commissionAcctNo}' not found.");
                var apCommissionPayableTitle = accountTitlesDto.Find(c => c.AccountNumber == "201010200")
                                               ?? throw new ArgumentException("Account title '201010200' not found.");

                var unitOfWork = new UnitOfWork(_db);
                var deliveredDate = deliveryReceipt.DeliveredDate
                    ?? throw new InvalidOperationException($"Delivered date is required for DR#{deliveryReceipt.DeliveryReceiptNo}.");
                var firstDayOfMonth = DateTimeHelper.GetFirstDayOfCurrentPhilippineMonth();
                var isDeliveredPeriodPosted = await unitOfWork
                    .IsPeriodPostedAsync(Module.DeliveryReceipt, deliveredDate, cancellationToken);
                var postingDate = isDeliveredPeriodPosted
                    ? firstDayOfMonth
                    : deliveredDate;
                var signedDifference = difference;
                var particulars = $"Update commission rate on DR#{deliveryReceipt.DeliveryReceiptNo}. DR dated {deliveryReceipt.DeliveredDate}";
                var isIncremental = difference > 0;
                difference = Math.Abs(difference);

                var commissionGrossAmount = difference;
                var commissionEwtAmount = deliveryReceipt.CustomerOrderSlip!.CommissioneeTaxType == SD.TaxType_WithTax
                    ? ComputeEwtAmount(commissionGrossAmount, deliveryReceipt.Commissionee?.WithholdingTaxPercent ?? 0m)
                    : 0;
                var commissionNetOfEwt = commissionEwtAmount > 0 ?
                    ComputeNetOfEwt(commissionGrossAmount, commissionEwtAmount) : commissionGrossAmount;
                var ewtTitle = commissionEwtAmount > 0
                    ? accountTitlesDto.FirstOrDefault(c =>
                          c.AccountNumber == (WithholdingTaxHelper.GetAccountNumberByPercent(deliveryReceipt.Commissionee?.WithholdingTaxPercent ?? 0m)
                              ?? throw new ArgumentException($"No EWT account mapping found for tax percentage '{deliveryReceipt.Commissionee?.WithholdingTaxPercent ?? 0m}'.")))
                      ?? throw new ArgumentException("Mapped EWT account title not found.")
                    : null;

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = postingDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = particulars,
                    AccountId = commissionTitle.AccountId,
                    AccountNo = commissionTitle.AccountNumber,
                    AccountTitle = commissionTitle.AccountName,
                    Debit = isIncremental ? commissionGrossAmount : 0m,
                    Credit = !isIncremental ? commissionGrossAmount : 0m,
                    Company = deliveryReceipt.Company,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModuleType = nameof(ModuleType.Sales)
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = postingDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = particulars,
                    AccountId = apCommissionPayableTitle.AccountId,
                    AccountNo = apCommissionPayableTitle.AccountNumber,
                    AccountTitle = apCommissionPayableTitle.AccountName,
                    Debit = !isIncremental ? commissionNetOfEwt : 0m,
                    Credit = isIncremental ? commissionNetOfEwt : 0m,
                    Company = deliveryReceipt.Company,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    SubAccountType = SubAccountType.Supplier,
                    SubAccountId = deliveryReceipt.CommissioneeId,
                    SubAccountName = deliveryReceipt.CustomerOrderSlip.CommissioneeName,
                    ModuleType = nameof(ModuleType.Sales)
                });

                if (commissionEwtAmount > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = postingDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = particulars,
                        AccountId = ewtTitle!.AccountId,
                        AccountNo = ewtTitle.AccountNumber,
                        AccountTitle = ewtTitle.AccountName,
                        Debit = !isIncremental ? commissionEwtAmount : 0m,
                        Credit = isIncremental ? commissionEwtAmount : 0m,
                        Company = deliveryReceipt.Company,
                        CreatedBy = userName,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Sales)
                    });
                }

                if (!IsJournalEntriesBalanced(ledgers))
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

                await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
                await unitOfWork.LockedPeriodAdjustment.AddIfPeriodPostedAsync(new LockedPeriodAdjustmentRequestDto
                {
                    Module = Module.DeliveryReceipt,
                    TransactionDate = deliveredDate,
                    EntityType = Module.DeliveryReceipt,
                    EntityNo = deliveryReceipt.DeliveryReceiptNo,
                    CustomerId = deliveryReceipt.CustomerId,
                    CustomerName = deliveryReceipt.CustomerOrderSlip?.CustomerName,
                    SupplierId = deliveryReceipt.CommissioneeId,
                    SupplierName = deliveryReceipt.CustomerOrderSlip?.CommissioneeName,
                    AdjustmentType = LockedPeriodAdjustmentType.Commission,
                    OldValue = GetRoundedUnitValue(deliveryReceipt.CommissionAmount - signedDifference, deliveryReceipt.Quantity),
                    NewValue = deliveryReceipt.CommissionRate,
                    AdjustmentValue = signedDifference,
                    AffectedQuantity = deliveryReceipt.Quantity,
                    Reason = "Update commission",
                    CreatedBy = userName
                }, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        public async Task CreateEntriesForUpdatingFreight(FilprideDeliveryReceipt deliveryReceipt,
            decimal difference,
            string userName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var ledgers = new List<FilprideGeneralLedgerBook>();
                var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
                var (freightAcctNo, freightAcctTitle) = GetFreightAccount(deliveryReceipt.CustomerOrderSlip!.Product!.ProductCode);
                var freightTitle = accountTitlesDto.Find(c => c.AccountNumber == freightAcctNo)
                                   ?? throw new ArgumentException($"Account title '{freightAcctNo}' not found.");
                var apHaulingPayableTitle = accountTitlesDto.Find(c => c.AccountNumber == "201010300")
                                            ?? throw new ArgumentException("Account title '201010300' not found.");
                var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200")
                                    ?? throw new ArgumentException("Account title '101060200' not found.");

                var unitOfWork = new UnitOfWork(_db);
                var deliveredDate = deliveryReceipt.DeliveredDate
                    ?? throw new InvalidOperationException($"Delivered date is required for DR#{deliveryReceipt.DeliveryReceiptNo}.");
                var firstDayOfMonth = DateTimeHelper.GetFirstDayOfCurrentPhilippineMonth();
                var isDeliveredPeriodPosted = await unitOfWork
                    .IsPeriodPostedAsync(Module.DeliveryReceipt, deliveredDate, cancellationToken);
                var postingDate = isDeliveredPeriodPosted
                    ? firstDayOfMonth
                    : deliveredDate;
                var signedDifference = difference;
                var particulars = $"Update freight rate on DR#{deliveryReceipt.DeliveryReceiptNo}. DR dated {deliveryReceipt.DeliveredDate}";
                var isIncremental = difference > 0;
                difference = Math.Abs(difference);

                var freightGross = difference;
                var freightNetOfVat = deliveryReceipt.HaulerVatType == SD.VatType_Vatable
                    ? ComputeNetOfVat(freightGross)
                    : freightGross;
                var freightEwtAmount = deliveryReceipt.HaulerTaxType == SD.TaxType_WithTax
                    ? ComputeEwtAmount(freightNetOfVat, deliveryReceipt.Hauler?.WithholdingTaxPercent ?? 0m)
                    : 0m;
                var freightNetOfEwt = freightEwtAmount > 0
                    ? ComputeNetOfEwt(freightGross, freightEwtAmount)
                    : freightGross;
                var ewtTitle = freightEwtAmount > 0
                    ? accountTitlesDto.FirstOrDefault(c =>
                          c.AccountNumber == (WithholdingTaxHelper.GetAccountNumberByPercent(deliveryReceipt.Hauler?.WithholdingTaxPercent ?? 0m)
                              ?? throw new ArgumentException($"No EWT account mapping found for tax percentage '{deliveryReceipt.Hauler?.WithholdingTaxPercent ?? 0m}'.")))
                      ?? throw new ArgumentException("Mapped EWT account title not found.")
                    : null;

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = postingDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = particulars,
                    AccountId = freightTitle.AccountId,
                    AccountNo = freightTitle.AccountNumber,
                    AccountTitle = freightTitle.AccountName,
                    Debit = isIncremental ? freightNetOfVat : 0m,
                    Credit = !isIncremental ? freightNetOfVat : 0m,
                    Company = deliveryReceipt.Company,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModuleType = nameof(ModuleType.Sales)
                });

                var freightVatAmount = deliveryReceipt.HaulerVatType == SD.VatType_Vatable
                    ? ComputeVatAmount(freightNetOfVat)
                    : 0m;

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = postingDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = particulars,
                    AccountId = vatInputTitle.AccountId,
                    AccountNo = vatInputTitle.AccountNumber,
                    AccountTitle = vatInputTitle.AccountName,
                    Debit = isIncremental ? freightVatAmount : 0m,
                    Credit = !isIncremental ? freightVatAmount : 0m,
                    Company = deliveryReceipt.Company,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModuleType = nameof(ModuleType.Sales)
                });

                ledgers.Add(new FilprideGeneralLedgerBook
                {
                    Date = postingDate,
                    Reference = deliveryReceipt.DeliveryReceiptNo,
                    Description = particulars,
                    AccountId = apHaulingPayableTitle.AccountId,
                    AccountNo = apHaulingPayableTitle.AccountNumber,
                    AccountTitle = apHaulingPayableTitle.AccountName,
                    Debit = !isIncremental ? freightNetOfEwt : 0m,
                    Credit = isIncremental ? freightNetOfEwt : 0m,
                    Company = deliveryReceipt.Company,
                    CreatedBy = userName,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    SubAccountType = SubAccountType.Supplier,
                    SubAccountId = deliveryReceipt.HaulerId,
                    SubAccountName = deliveryReceipt.HaulerName,
                    ModuleType = nameof(ModuleType.Sales)
                });

                if (freightEwtAmount > 0)
                {
                    ledgers.Add(new FilprideGeneralLedgerBook
                    {
                        Date = postingDate,
                        Reference = deliveryReceipt.DeliveryReceiptNo,
                        Description = particulars,
                        AccountId = ewtTitle!.AccountId,
                        AccountNo = ewtTitle.AccountNumber,
                        AccountTitle = ewtTitle.AccountName,
                        Debit = !isIncremental ? freightEwtAmount : 0m,
                        Credit = isIncremental ? freightEwtAmount : 0m,
                        Company = deliveryReceipt.Company,
                        CreatedBy = userName,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        ModuleType = nameof(ModuleType.Sales)
                    });
                }

                if (!IsJournalEntriesBalanced(ledgers))
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

                await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
                await unitOfWork.LockedPeriodAdjustment.AddIfPeriodPostedAsync(new LockedPeriodAdjustmentRequestDto
                {
                    Module = Module.DeliveryReceipt,
                    TransactionDate = deliveredDate,
                    EntityType = Module.DeliveryReceipt,
                    EntityNo = deliveryReceipt.DeliveryReceiptNo,
                    CustomerId = deliveryReceipt.CustomerId,
                    CustomerName = deliveryReceipt.CustomerOrderSlip?.CustomerName,
                    SupplierId = deliveryReceipt.HaulerId,
                    SupplierName = deliveryReceipt.HaulerName,
                    AdjustmentType = LockedPeriodAdjustmentType.Freight,
                    OldValue = GetRoundedUnitValue(deliveryReceipt.FreightAmount - (deliveryReceipt.ECC * deliveryReceipt.Quantity) - signedDifference, deliveryReceipt.Quantity),
                    NewValue = deliveryReceipt.Freight,
                    AdjustmentValue = signedDifference,
                    AffectedQuantity = deliveryReceipt.Quantity,
                    Reason = "Update freight",
                    CreatedBy = userName
                }, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

    }
}
