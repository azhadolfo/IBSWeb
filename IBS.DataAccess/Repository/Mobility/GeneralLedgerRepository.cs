using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using IBS.Utility.Enums;

namespace IBS.DataAccess.Repository.Mobility
{
    public class GeneralLedgerRepository : Repository<MobilityGeneralLedger>, IGeneralLedgerRepository
    {
        private ApplicationDbContext _db;

        public GeneralLedgerRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public byte[] ExportToExcel(IEnumerable<GeneralLedgerView> ledgers, DateOnly dateTo, DateOnly dateFrom, object accountNo, object accountName, string productCode)
        {
            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("GeneralLedger");

            // Set the column headers
            var mergedCells = worksheet.Cells["A1:C1"];
            mergedCells.Merge = true;
            mergedCells.Value = "GENERAL LEDGER BY ACCOUNT NUMBER";
            mergedCells.Style.Font.Size = 13;

            worksheet.Cells["A2"].Value = "Date Range:";
            worksheet.Cells["A3"].Value = "Account No:";
            worksheet.Cells["A4"].Value = "Account Title:";
            worksheet.Cells["A5"].Value = "Product Code:";

            worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
            worksheet.Cells["B3"].Value = $"{accountNo}";
            worksheet.Cells["B4"].Value = $"{accountName}";
            worksheet.Cells["B5"].Value = $"{productCode}";

            worksheet.Cells["A7"].Value = "Date";
            worksheet.Cells["B7"].Value = "Station Code";
            worksheet.Cells["C7"].Value = "Station Name";
            worksheet.Cells["D7"].Value = "Particular";
            worksheet.Cells["E7"].Value = "Account No";
            worksheet.Cells["F7"].Value = "Account Title";
            worksheet.Cells["G7"].Value = "Product Code";
            worksheet.Cells["H7"].Value = "Product Name";
            worksheet.Cells["I7"].Value = "Customer Code";
            worksheet.Cells["J7"].Value = "Customer Name";
            worksheet.Cells["K7"].Value = "Supplier Code";
            worksheet.Cells["L7"].Value = "Supplier Name";
            worksheet.Cells["M7"].Value = "Debit";
            worksheet.Cells["N7"].Value = "Credit";
            worksheet.Cells["O7"].Value = "Balance";

            // Apply styling to the header row
            using (var range = worksheet.Cells["A7:O7"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // Populate the data rows
            int row = 8;
            decimal balance = 0;
            string currencyFormat = "#,##0.0000";
            decimal debit = 0;
            decimal credit = 0;
            foreach (var journals in ledgers.OrderBy(j => j.AccountNumber).GroupBy(j => j.AccountTitle))
            {
                balance = 0;

                foreach (var journal in journals.OrderBy(j => j.TransactionDate))
                {
                    if (balance != 0)
                    {
                        if (journal.NormalBalance == nameof(NormalBalance.Debit))
                        {
                            balance += journal.Debit - journal.Credit;
                        }
                        else
                        {
                            balance -= journal.Debit - journal.Credit;
                        }
                    }
                    else
                    {
                        balance = journal.Debit > 0 ? journal.Debit : journal.Credit;
                    }

                    worksheet.Cells[row, 1].Value = journal.TransactionDate;
                    worksheet.Cells[row, 2].Value = journal.StationCode;
                    worksheet.Cells[row, 3].Value = journal.StationName;
                    worksheet.Cells[row, 4].Value = journal.Particular;
                    worksheet.Cells[row, 5].Value = journal.AccountNumber;
                    worksheet.Cells[row, 6].Value = journal.AccountTitle;
                    worksheet.Cells[row, 7].Value = journal.ProductCode;
                    worksheet.Cells[row, 8].Value = journal.ProductName;
                    worksheet.Cells[row, 9].Value = journal.CustomerCode;
                    worksheet.Cells[row, 10].Value = journal.CustomerName;
                    worksheet.Cells[row, 11].Value = journal.SupplierCode;
                    worksheet.Cells[row, 12].Value = journal.SupplierName;

                    worksheet.Cells[row, 13].Value = journal.Debit;
                    worksheet.Cells[row, 14].Value = journal.Credit;
                    worksheet.Cells[row, 15].Value = balance;

                    worksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;

                    row++;
                }

                debit = journals.Sum(j => j.Debit);
                credit = journals.Sum(j => j.Credit);
                balance = debit - credit;

                worksheet.Cells[row, 12].Value = "Total " + journals.Key;
                worksheet.Cells[row, 13].Value = debit;
                worksheet.Cells[row, 14].Value = credit;
                worksheet.Cells[row, 15].Value = balance;

                worksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;

                // Apply style to subtotal row
                using (var range = worksheet.Cells[row, 1, row, 15])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }

                row++;
            }

            using (var range = worksheet.Cells[row, 13, row, 15])
            {
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
            }

            debit = ledgers.Sum(j => j.Debit);
            credit = ledgers.Sum(j => j.Credit);
            balance = debit - credit;

            worksheet.Cells[row, 12].Value = "Total";
            worksheet.Cells[row, 12].Style.Font.Bold = true;
            worksheet.Cells[row, 13].Value = debit;
            worksheet.Cells[row, 14].Value = credit;
            worksheet.Cells[row, 15].Value = balance;

            worksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;

            // Auto-fit columns for better readability
            worksheet.Cells.AutoFitColumns();
            worksheet.View.FreezePanes(8, 1);

            // Convert the Excel package to a byte array
            return package.GetAsByteArray();
        }

        public async Task<IEnumerable<GeneralLedgerView>> GetLedgerViewByAccountNo(DateOnly dateFrom, DateOnly dateTo, string stationCode, string accountNo, string productCode, CancellationToken cancellationToken = default)
        {
            return await _db.GeneralLedgerViews
                .Where(g => g.TransactionDate >= dateFrom &&
                            g.TransactionDate <= dateTo &&
                            (accountNo == "ALL" || g.AccountNumber == accountNo) &&
                            (productCode == "ALL" || g.ProductCode == productCode) &&
                            (stationCode == "ALL" || g.StationCode == stationCode))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<GeneralLedgerView>> GetLedgerViewByJournal(DateOnly dateFrom, DateOnly dateTo, string stationCode, string journal, CancellationToken cancellationToken = default)
        {
            return await _db.GeneralLedgerViews
                .Where(g => g.TransactionDate >= dateFrom && g.TransactionDate <= dateTo && g.JournalReference == journal && (stationCode == "ALL" || g.StationCode == stationCode))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<GeneralLedgerView>> GetLedgerViewByTransaction(DateOnly dateFrom, DateOnly dateTo, string stationCode, CancellationToken cancellationToken = default)
        {
            return await _db.GeneralLedgerViews
                .Where(g => g.TransactionDate >= dateFrom && g.TransactionDate <= dateTo && (stationCode == "ALL" || g.StationCode == stationCode))
                .ToListAsync(cancellationToken);
        }
    }
}