using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
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

        public byte[] ExportToExcel(IEnumerable<GeneralLedger> ledgers, DateOnly dateTo, DateOnly dateFrom, string accountNo, string accountName, string productCode)
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
                worksheet.Cells["C7"].Value = "Debit";
                worksheet.Cells["D7"].Value = "Credit";
                worksheet.Cells["E7"].Value = "Balance";

                // Populate the data rows
                int row = 8;
                decimal balance = 0;
                foreach (var journal in ledgers.OrderBy(j => j.AccountNumber))
                {
                    balance += journal.Debit + journal.Credit;

                    worksheet.Cells[row, 1].Value = journal.TransactionDate;
                    worksheet.Cells[row, 2].Value = journal.Particular;
                    worksheet.Cells[row, 3].Value = journal.Debit;
                    worksheet.Cells[row, 4].Value = journal.Credit;
                    worksheet.Cells[row, 5].Value = balance;

                    // Format Debit, Credit, and Balance columns with commas
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00";
                    worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";

                    row++;
                }

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return excelBytes;
            }
        }
    }
}
