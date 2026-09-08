using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace IBS.Services.Msap
{
    public class ChartOfAccountService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<ChartOfAccountService> logger)
        : IChartOfAccountService
    {
        public async Task<IEnumerable<ChartOfAccount>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await unitOfWork.ChartOfAccount.GetAllAsync(null, cancellationToken);
        }

        public async Task<(IEnumerable<object> Data, int TotalRecords)> GetPagedListAsync(DataTablesParameters parameters, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default)
        {
            System.Linq.Expressions.Expression<Func<ChartOfAccount, bool>>? filter = null;
            if (dateFrom.HasValue || dateTo.HasValue)
            {
                var dateToInclusive = dateTo?.AddDays(1);
                filter = s => (!dateFrom.HasValue || s.CreatedDate >= dateFrom.Value) &&
                              (!dateTo.HasValue || s.CreatedDate < dateToInclusive!.Value);
            }

            var chartOfAccounts = await unitOfWork.ChartOfAccount.GetAllAsync(filter, cancellationToken);

            if (!string.IsNullOrEmpty(parameters.Search.Value))
            {
                var searchValue = parameters.Search.Value.ToLower();
                chartOfAccounts = chartOfAccounts.Where(s =>
                    (s.AccountNumber != null && s.AccountNumber.ToLower().Contains(searchValue)) ||
                    (s.AccountName.ToLower().Contains(searchValue)) ||
                    (s.AccountType != null && s.AccountType.ToLower().Contains(searchValue)) ||
                    (s.NormalBalance != null && s.NormalBalance.ToLower().Contains(searchValue)) ||
                    s.Level.ToString().Contains(searchValue) ||
                    s.CreatedDate.ToString("MMM dd, yyyy").ToLower().Contains(searchValue)
                ).ToList();
            }

            var totalRecords = chartOfAccounts.Count();

            IEnumerable<ChartOfAccount> pagedChartOfAccounts = parameters.Length == -1
                ? chartOfAccounts
                : chartOfAccounts.Skip(parameters.Start).Take(parameters.Length);

            var pagedData = pagedChartOfAccounts.Select(x => new
            {
                x.AccountId,
                x.AccountNumber,
                x.AccountName,
                x.AccountType,
                x.NormalBalance,
                x.Level,
                CreatedDate = x.CreatedDate.ToString("MMM dd, yyyy")
            }).ToList();

            return (pagedData, totalRecords);
        }

        public async Task<ServiceResult> CreateAsync(int parentId, string accountName, string createdBy, string company, CancellationToken cancellationToken = default)
        {
            try
            {
                var parentAccount = await unitOfWork.ChartOfAccount.GetAsync(c => c.AccountId == parentId, cancellationToken);
                if (parentAccount == null)
                {
                    return ServiceResult.Failure("Parent Account not found");
                }

                var lastAccount = (await unitOfWork.ChartOfAccount.GetAllAsync(c => c.ParentAccountId == parentId, cancellationToken))
                    .OrderByDescending(c => c.AccountNumber)
                    .FirstOrDefault();

                var lastSeries = int.Parse(lastAccount?.AccountNumber ?? parentAccount.AccountNumber!);
                var levelToCreate = parentAccount.Level + 1;

                var newAccount = new ChartOfAccount
                {
                    IsMain = false,
                    AccountType = parentAccount.AccountType,
                    NormalBalance = parentAccount.NormalBalance ?? "",
                    AccountName = accountName,
                    ParentAccountId = parentId,
                    CreatedBy = createdBy,
                    Level = levelToCreate,
                    FinancialStatementType = parentAccount.FinancialStatementType,
                    AccountNumber = levelToCreate switch
                    {
                        4 => (lastSeries + 100).ToString(),
                        5 => (lastSeries + 1).ToString(),
                        _ => throw new InvalidOperationException("Unsupported COA level for automatic numbering")
                    }
                };

                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    await unitOfWork.ChartOfAccount.AddAsync(newAccount, cancellationToken);

                    AuditTrail auditTrail = new(createdBy, $"Created new Account #{newAccount.AccountNumber}", "Chart of Accounts");
                    await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                    await unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                await cacheService.RemoveAsync($"coa:{company}", cancellationToken);
                return ServiceResult.Success($"Account #{newAccount.AccountNumber} Created Successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create COA for {Company}", company);
                return ServiceResult.Failure($"Failed to create account: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateAsync(int accountId, string accountName, string editedBy, string company, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingAccount = await unitOfWork.ChartOfAccount.GetAsync(x => x.AccountId == accountId, cancellationToken);
                if (existingAccount == null)
                {
                    return ServiceResult.Failure("Account not found", ServiceResultStatus.NotFound);
                }

                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    existingAccount.AccountName = accountName;
                    existingAccount.EditedBy = editedBy;
                    existingAccount.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    AuditTrail auditTrail = new(editedBy, $"Edited Account #{existingAccount.AccountNumber}", "Chart of Accounts");
                    await unitOfWork.AuditTrail.AddAsync(auditTrail, cancellationToken);

                    await unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                await cacheService.RemoveAsync($"coa:{company}", cancellationToken);
                return ServiceResult.Success("Account Edited Successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit COA for {Company}", company);
                return ServiceResult.Failure($"Failed to edit account: {ex.Message}");
            }
        }

        public async Task<byte[]> ExportToExcelAsync(string selectedRecordIds, CancellationToken cancellationToken = default)
        {
            var recordIds = selectedRecordIds.Split(',').Select(int.Parse).ToList();
            var selectedList = (await unitOfWork.ChartOfAccount.GetAllAsync(coa => recordIds.Contains(coa.AccountId), cancellationToken))
                .OrderBy(coa => coa.AccountId)
                .ToList();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("ChartOfAccount");

            string[] headers = ["IsMain", "AccountNumber", "AccountName", "AccountType", "NormalBalance", "Level", "CreatedBy", "CreatedDate", "EditedBy", "EditedDate", "HasChildren", "ParentAccountId", "OriginalChartOfAccount"
            ];
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            var row = 2;
            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.IsMain;
                worksheet.Cells[row, 2].Value = item.AccountNumber;
                worksheet.Cells[row, 3].Value = item.AccountName;
                worksheet.Cells[row, 4].Value = item.AccountType;
                worksheet.Cells[row, 5].Value = item.NormalBalance;
                worksheet.Cells[row, 6].Value = item.Level;
                worksheet.Cells[row, 7].Value = item.CreatedBy;
                worksheet.Cells[row, 8].Value = item.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[row, 9].Value = item.EditedBy;
                worksheet.Cells[row, 10].Value = item.EditedDate?.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[row, 11].Value = item.HasChildren;
                worksheet.Cells[row, 12].Value = item.ParentAccountId;
                worksheet.Cells[row, 13].Value = item.AccountId;
                row++;
            }

            foreach (var ws in package.Workbook.Worksheets)
            {
                ws.Protection.SetPassword("mis123");
            }

            package.Workbook.Protection.SetPassword("mis123");

            return await package.GetAsByteArrayAsync(cancellationToken);
        }

        public async Task<IEnumerable<int>> GetAllIdsAsync(CancellationToken cancellationToken = default)
        {
            var coas = await unitOfWork.ChartOfAccount.GetAllAsync(null, cancellationToken);
            return coas.Select(c => c.AccountId);
        }
    }
}
