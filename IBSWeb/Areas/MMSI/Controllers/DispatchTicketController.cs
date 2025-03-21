using System.Globalization;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI;
using IBS.Models.MMSI;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.MMSI
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class DispatchTicketController : Controller
    {
        public readonly ApplicationDbContext _db;
        private readonly DispatchTicketRepository _dispatchRepo;

        public DispatchTicketController(ApplicationDbContext db, DispatchTicketRepository dispatchRepo)
        {
            _db = db;
            _dispatchRepo = dispatchRepo;
        }

        public async Task<IActionResult> Index()
        {
            var dispatchTickets = await _db.MMSIDispatchTickets
                .Where(dt => dt.Status != "For Posting" && dt.Status != "Cancelled")
                .Include(a => a.ActivityService)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .ToListAsync();

            return View(dispatchTickets);
        }

        public void ReadXLS(string FilePath)
        {
            FileInfo existingFile = new FileInfo(FilePath);
            using (ExcelPackage package = new ExcelPackage(existingFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                int colCount = worksheet.Dimension.End.Column;
                int rowCount = worksheet.Dimension.End.Row;

                for (int row = 1; row <= rowCount; row++)
                {
                    for (int col = 1; col <= colCount; col++)
                    {
                        Console.WriteLine(" Row:" + row + " column:" + col + " Value:" + worksheet.Cells[row, col].Value?.ToString().Trim());
                    }
                }
            }
        }

        [HttpGet]
        public IActionResult UploadExcel()
        {
            return View();
        }

        [HttpPost]
        public async Task <IActionResult> UploadExcel(IFormFile excelFile, CancellationToken cancellationToken)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tempFiles");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fullPath = Path.Combine(uploadsFolder, excelFile.FileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await excelFile.CopyToAsync(stream);
            }

            string name = excelFile.FileName;
            FileInfo existingFile = new FileInfo(fullPath);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(existingFile);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null || worksheet.Dimension == null)
            {
                return BadRequest("The uploaded Excel file is null.");
            }

            int colCount = worksheet.Dimension.End.Column;
            int rowCount = worksheet.Dimension.End.Row;

            #region == Excel Reading ==

            string dateFormat = "MM/dd/yyyy";
            string timeFormat = "HH:mm";

            for (int row = 9; row <= rowCount; row++)
            {
                string dateFromExcel = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                DateTime datePart = DateTime.ParseExact(dateFromExcel, dateFormat, CultureInfo.InvariantCulture);
                var dispatchNumber = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                var cosNumber = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                var dateTimeLeft = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                var dataTimeTimeArrived = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                var tugboat = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                var tugboatName = worksheet.Cells[row, 7].Value?.ToString()?.Trim();
                var tugMaster = worksheet.Cells[row, 8].Value?.ToString()?.Trim();
                var tugMasterName = worksheet.Cells[row, 9].Value?.ToString()?.Trim();
                var vessel = worksheet.Cells[row, 10].Value?.ToString()?.Trim();
                var vesselName = worksheet.Cells[row, 11].Value?.ToString()?.Trim();
                var port = worksheet.Cells[row, 12].Value?.ToString()?.Trim();
                var portName = worksheet.Cells[row, 13].Value?.ToString()?.Trim();
                var berminal = worksheet.Cells[row, 14].Value?.ToString()?.Trim();
                var berminalName = worksheet.Cells[row, 15].Value?.ToString()?.Trim();
                var baseOrStation = worksheet.Cells[row, 16].Value?.ToString()?.Trim();
                var bctivityService = worksheet.Cells[row, 17].Value?.ToString()?.Trim();
                var remarks = worksheet.Cells[row, 18].Value?.ToString()?.Trim();
                var voyageNumber = worksheet.Cells[row, 19].Value?.ToString()?.Trim();

                if (string.IsNullOrEmpty(dispatchNumber) && string.IsNullOrEmpty(dateTimeLeft) && string.IsNullOrEmpty(dataTimeTimeArrived))
                {
                    continue; // Skip empty rows
                }

                // var model = new ExcelTestModel
                // {
                //     // equate the values gathered to the model instance here
                // };
                //
                // await _db.ExcelTestModels.AddAsync(model, cancellationToken);
            }

            #endregion

            await _db.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Preview (int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIDispatchTickets.Where(dt => dt.DispatchTicketId == id)
                .Include(a => a.ActivityService)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .FirstOrDefaultAsync();

            return View(model);

        }

        [HttpGet]
        public async Task<IActionResult> Tariffing(int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIDispatchTickets
                .Where(dt => dt.DispatchTicketId == id)
                .Include(a => a.ActivityService)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .FirstOrDefaultAsync(cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Tariffing(MMSIDispatchTicket model, string chargeType, CancellationToken cancellationToken)
        {
            try
            {
                var currentModel = await _db.MMSIDispatchTickets.FindAsync(model.DispatchTicketId, cancellationToken);
                TimeSpan timeDifference = model.DateArrived.ToDateTime(model.TimeArrived) - model.DateLeft.ToDateTime(model.TimeLeft);

                currentModel.Status = "Tariff Pending";
                currentModel.DispatchRate = model.DispatchRate;
                currentModel.DispatchBillingAmount = model.DispatchBillingAmount;
                currentModel.DispatchDiscount = model.DispatchDiscount;
                currentModel.DispatchNetRevenue = model.DispatchNetRevenue;
                currentModel.BAFRate = model.BAFRate;
                currentModel.BAFBillingAmount = model.BAFBillingAmount;
                currentModel.BAFDiscount = model.BAFDiscount;
                currentModel.BAFNetRevenue = model.BAFNetRevenue;
                currentModel.DispatchChargeType = chargeType;
                currentModel.TotalBilling = model.TotalBilling;
                currentModel.TotalNetRevenue = model.TotalNetRevenue;

                await _db.SaveChangesAsync();
                TempData["success"] = "Tariff entered successfully!";

                return RedirectToAction(nameof(Index), new { id = currentModel.DispatchTicketId });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                model = await _db.MMSIDispatchTickets
                .Where(dt => dt.DispatchTicketId == model.DispatchTicketId)
                .Include(a => a.ActivityService)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .FirstOrDefaultAsync(cancellationToken);

                return View(model);
            }
        }

        public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                model.Status = "For Billing";
                await _db.SaveChangesAsync();
                TempData["success"] = "Entry Approved!";

                return RedirectToAction(nameof(Index), new { id = id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { id = id });
            }
        }

        public async Task<IActionResult> Disapprove(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                model.Status = "Disapproved";
                await _db.SaveChangesAsync();
                TempData["success"] = "Entry Disapproved!";

                return RedirectToAction(nameof(Index), new { id = id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { id = id });
            }
        }

        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);

                if (model != null)
                {
                    //if (model.UploadName != null)
                    //{
                    //    string filePath = Path.Combine("wwwroot/Dispatch_Ticket_Uploads", model.UploadName);

                    //    if (System.IO.File.Exists(filePath))
                    //    {
                    //        System.IO.File.Delete(filePath);
                    //    }
                    //}

                    model.Status = "Cancelled";
                    _db.SaveChanges();
                    TempData["success"] = "Service Request cancelled.";

                    return RedirectToAction(nameof(Index));
                }

                else
                {
                    TempData["error"] = "Can't find entry, please try again.";

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var terminals = await _db
                .MMSITerminals
                .Where(t => t.PortId == portId)
                .OrderBy(t => t.TerminalId)
                .ToListAsync(cancellationToken);

            var terminalsList = terminals.Select(t => new SelectListItem
            {
                Value = t.TerminalId.ToString(),
                Text = t.TerminalNumber + " " + t.TerminalName
            }).ToList();

            return Json(terminalsList);
        }

        [HttpGet]
        public async Task<IActionResult> GetDispatchTicketList(string status, CancellationToken cancellationToken)
        {
            var item = new List<MMSIDispatchTicket>();
            if (status == "All" || status == null)
            {
                item = await _db.MMSIDispatchTickets
                    .Where(dt => dt.Status != "Cancelled" && dt.Status != "For Posting")
                    .Include(a => a.ActivityService)
                    .Include(a => a.Terminal).ThenInclude(t => t.Port)
                    .Include(a => a.Tugboat)
                    .Include(a => a.TugMaster)
                    .Include(a => a.Vessel)
                    .ToListAsync();
            }
            else
            {
                item = await _db.MMSIDispatchTickets
                    .Where(dt => dt.Status == status)
                    .Include(a => a.ActivityService)
                    .Include(a => a.Terminal).ThenInclude(t => t.Port)
                    .Include(a => a.Tugboat)
                    .Include(a => a.TugMaster)
                    .Include(a => a.Vessel)
                    .ToListAsync(cancellationToken);
            }

            return Json(item);
        }

        public async Task<IActionResult> DeleteImage(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                string filePath = Path.Combine("wwwroot/Dispatch_Ticket_Uploads", model.UploadName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                model.UploadName = null;
                await _db.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Image Deleted Successfully!";

                return RedirectToAction(nameof(Index), new { id = model.DispatchTicketId });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { id = id });
            }
        }
    }
}
