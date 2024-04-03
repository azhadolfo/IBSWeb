using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IBS.DataAccess.Repository
{
    public class GeneralLedgerRepository : Repository<GeneralLedger>, IGeneralLedgerRepository
    {
        private ApplicationDbContext _db;

        public GeneralLedgerRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public byte[] ExportToExcel(IEnumerable<GeneralLedger> ledgers, DateOnly dateTo, DateOnly dateFrom, object accountNo, object accountName, string productCode)
        {
            // Create the Excel package
            using (var package = new ExcelPackage())
            {
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
                worksheet.Cells["B7"].Value = "Particulars";

                // Find the position to insert additional columns
                int columnOffset = 2; // Start after "Particulars" column
                if (worksheet.Cells["B7"].Value.ToString() == "Particulars")
                {
                    columnOffset++;
                }

                // Insert additional columns if needed
                if (accountNo.ToString().ToUpper() == "ALL")
                {
                    worksheet.InsertColumn(columnOffset, 2);
                    worksheet.Cells[7, columnOffset].Value = "Account No";
                    worksheet.Cells[7, columnOffset + 1].Value = "Account Title";
                    columnOffset += 2;
                }

                if (productCode.ToUpper() == "ALL")
                {
                    worksheet.InsertColumn(columnOffset, 1);
                    worksheet.Cells[7, columnOffset].Value = "Product Code";
                    columnOffset++;
                }

                // Set the remaining column headers
                worksheet.Cells[7, columnOffset].Value = "Debit";
                worksheet.Cells[7, columnOffset + 1].Value = "Credit";
                worksheet.Cells[7, columnOffset + 2].Value = "Balance";

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7"].Offset(0, 0, 1, columnOffset + 2))
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
                string currencyFormat = "#,##0.00";
                decimal debit = 0;
                decimal credit = 0;
                foreach (var journals in ledgers.OrderBy(j => j.AccountNumber).GroupBy(j => j.AccountTitle))
                {
                    foreach (var journal in journals.OrderBy(j => j.TransactionDate))
                    {
                        balance += journal.Debit - journal.Credit;

                        worksheet.Cells[row, 1].Value = journal.TransactionDate.ToString("MMM/dd/yyyy");
                        worksheet.Cells[row, 2].Value = journal.Particular;

                        if (accountNo.ToString().ToUpper() == "ALL")
                        {
                            worksheet.Cells[row, 3].Value = journal.AccountNumber;
                            worksheet.Cells[row, 4].Value = journal.AccountTitle;
                        }

                        if (productCode.ToUpper() == "ALL")
                        {
                            if (worksheet.Cells[7, 3].Value.ToString() == "Account No")
                                worksheet.Cells[row, 5].Value = journal.ProductCode;
                            else if (worksheet.Cells[7, 3].Value.ToString() == "Product Code")
                                worksheet.Cells[row, 3].Value = journal.ProductCode;
                        }

                        worksheet.Cells[row, columnOffset].Value = journal.Debit;
                        worksheet.Cells[row, columnOffset + 1].Value = journal.Credit;
                        worksheet.Cells[row, columnOffset + 2].Value = balance;

                        worksheet.Cells[row, columnOffset].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, columnOffset + 1].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, columnOffset + 2].Style.Numberformat.Format = currencyFormat;

                        row++;
                    }

                    debit = journals.Sum(j => j.Debit);
                    credit = journals.Sum(j => j.Credit);
                    balance = debit - credit;

                    worksheet.Cells[row, 2].Value = "Total " + journals.Key;
                    worksheet.Cells[row, columnOffset].Value = debit;
                    worksheet.Cells[row, columnOffset + 1].Value = credit;
                    worksheet.Cells[row, columnOffset + 2].Value = balance;

                    worksheet.Cells[row, columnOffset].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, columnOffset + 1].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, columnOffset + 2].Style.Numberformat.Format = currencyFormat;

                    // Apply style to subtotal row
                    using (var range = worksheet.Cells[row, 1, row, columnOffset + 2])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172,185,202));
                    }

                    row++;
                }

                debit = ledgers.Sum(j => j.Debit);
                credit = ledgers.Sum(j => j.Credit);
                balance = debit - credit;

                worksheet.Cells[row, 2].Value = "Total";
                worksheet.Cells[row, 2].Style.Font.Bold = true;
                worksheet.Cells[row, columnOffset].Value = debit;
                worksheet.Cells[row, columnOffset + 1].Value = credit;
                worksheet.Cells[row, columnOffset + 2].Value = balance;

                worksheet.Cells[row, columnOffset].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, columnOffset + 1].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, columnOffset + 2].Style.Numberformat.Format = currencyFormat;

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();

                // Convert the Excel package to a byte array
                return package.GetAsByteArray();
            }
        }
    }
}
