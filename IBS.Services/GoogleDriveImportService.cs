using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models;
using IBS.Models.Mobility.ViewModels;
using IBS.Utility;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IBS.Services
{
    public interface IGoogleDriveImportService
    {
        Task<List<GoogleDriveFileViewModel>> GetFileFromDriveAsync(string stationName, string folderId);
    }

    public class GoogleDriveImportService : IGoogleDriveImportService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly GCSConfigOptions _options;
        private readonly ILogger<GoogleDriveImportService> _logger;
        private readonly GoogleCredential _googleCredential;

        public GoogleDriveImportService(IOptions<GCSConfigOptions> options, ILogger<GoogleDriveImportService> logger,
            ApplicationDbContext dbContext, IUnitOfWork unitOfWork)
        {
            _options = options.Value;
            _logger = logger;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;

            try
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (environment == Environments.Production)
                {
                    _googleCredential = GoogleCredential.GetApplicationDefault();
                }
                else
                {
                    // Log for debugging purposes
                    _logger.LogInformation($"Environment: {environment}, Auth File: {_options.GCPStorageAuthFile}");

                    if (!File.Exists(_options.GCPStorageAuthFile))
                    {
                        throw new FileNotFoundException($"Auth file not found: {_options.GCPStorageAuthFile}");
                    }

                    _googleCredential = GoogleCredential.FromFile(_options.GCPStorageAuthFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize Google Cloud Storage client: {ex.Message}");
                throw;
            }
        }

        public async Task Execute()
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var sessionStartDate = DateTimeHelper.GetCurrentPhilippineTime();
            string sessionCode = sessionStartDate.ToString("yyyyddMMHHmmss");

            try
            {
                _logger.LogInformation($"==========Import process started in {sessionStartDate}==========");

                LogMessage logMessage = new("Information", "Start",
                    sessionCode);
                await _dbContext.LogMessages.AddAsync(logMessage);
                logMessage = new("Information", "GoogleDriveImportService",
                    $"Import process started in {sessionStartDate}.");
                await _dbContext.LogMessages.AddAsync(logMessage);

                var posSalesModel = await ImportPosSales();
                var purchasesModel = await ImportPurchases();
                var fmsSalesModel = await ImportFmsSales();

                foreach (var fms in fmsSalesModel)
                {
                    var purchase = purchasesModel
                        .FirstOrDefault(s => s.StationName == fms.StationName);

                    var posSales = posSalesModel
                        .FirstOrDefault(s => s.StationName == fms.StationName);

                    var message = $"{posSales.Message} {purchase.Message} || {fms.Message}";
                    logMessage = new("Information", $"{fms.StationName}", message);

                    if (!string.IsNullOrEmpty(purchase.OpeningFileStatus) ||
                        !string.IsNullOrEmpty(posSales.OpeningFileStatus) ||
                        !string.IsNullOrEmpty(fms.OpeningFileStatus) ||
                        !string.IsNullOrEmpty(purchase.CsvStatus) ||
                        !string.IsNullOrEmpty(posSales.CsvStatus) ||
                        !string.IsNullOrEmpty(fms.CsvStatus))
                    {
                        logMessage.LogLevel = "Warning";
                    }

                    if (!string.IsNullOrEmpty(purchase.Error) || !string.IsNullOrEmpty(posSales.Error) || !string.IsNullOrEmpty(fms.Error))
                    {
                        logMessage.LogLevel = "Error";
                    }

                    await _dbContext.LogMessages.AddAsync(logMessage);
                }

                _logger.LogInformation(
                    $"==========Import process finished in {DateTimeHelper.GetCurrentPhilippineTime()}==========");

                logMessage = new("Information", "GoogleDriveImportService",
                    $"Import process finished in {DateTimeHelper.GetCurrentPhilippineTime()}.");
                await _dbContext.LogMessages.AddAsync(logMessage);

                logMessage = new("Information", "End",
                    sessionCode);
                await _dbContext.LogMessages.AddAsync(logMessage);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogInformation("==========GoogleDriveImportService.Execute - EXCEPTION: " + ex.Message + "==========");

                LogMessage startMessage = new("Information", "Start",
                    sessionCode);
                await _dbContext.LogMessages.AddAsync(startMessage);
                startMessage = new("Information", "GoogleDriveImportService",
                    $"Import process started in {sessionStartDate}.");
                await _dbContext.LogMessages.AddAsync(startMessage);

                LogMessage endMessage = new("Error", "GoogleDriveImportService",
                    $"IMPORT SERVICE EXCEPTION {DateTimeHelper.GetCurrentPhilippineTime()}. Error: {ex.Message}.");
                await _dbContext.LogMessages.AddAsync(endMessage);
                endMessage = new("Information", "End",
                    sessionCode);
                await _dbContext.LogMessages.AddAsync(endMessage);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<List<GoogleDriveFileViewModel>> GetFileFromDriveAsync(string stationName, string folderId)
        {
            // get credential
            var serviceCredential = _googleCredential.CreateScoped(DriveService.ScopeConstants.Drive);
            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = serviceCredential,
                ApplicationName = "IBS Application"
            });

            var request = service.Files.List();
            request.Q = $"'{folderId}' in parents and trashed=false";
            request.Fields = "nextPageToken, files(id, name, webViewLink)";
            var filesModel = new List<GoogleDriveFileViewModel>();

            do
            {
                var result = await request.ExecuteAsync();

                foreach (var file in result.Files)
                {
                    var fileVm = new GoogleDriveFileViewModel
                    {
                        FileName = file.Name,
                        FileLink = file.WebViewLink
                    };

                    using (var stream = new MemoryStream())
                    {
                        var downloadRequest = service.Files.Get(file.Id);
                        await downloadRequest.DownloadAsync(stream);
                        fileVm.FileContent = stream.ToArray();
                    }

                    filesModel.Add(fileVm);
                }

                request.PageToken = result.NextPageToken;
            } while (request.PageToken != null);

            return filesModel;
        }

        private async Task<List<LogMessageDto>> ImportPosSales()
        {
            List<LogMessageDto> logList = new();
            _logger.LogInformation("==========IMPORTING SALES==========");

            int fuelsCount;
            int lubesCount;
            int safedropsCount;
            bool hasPoSales = false;

            var stations = await _dbContext.MobilityStations
                .Where(s => s.IsActive)
                .ToListAsync();

            foreach (var station in stations)
            {
                LogMessageDto model = new LogMessageDto();
                model.StationName = station.StationName;

                if (!string.IsNullOrEmpty(station.FolderPath))
                {
                    var fileList = await GetFileFromDriveAsync(station.StationName, station.FolderPath);

                    try
                    {
                        var currentYear = DateTime.UtcNow.ToString("yyyy");

                        var files = fileList.Where(f =>
                                (f.FileName.Contains("fuels", StringComparison.CurrentCultureIgnoreCase) ||
                                 f.FileName.Contains("lubes", StringComparison.CurrentCultureIgnoreCase) ||
                                 f.FileName.Contains("safedrops", StringComparison.CurrentCultureIgnoreCase)) &&
                                Path.GetFileNameWithoutExtension(f.FileName).EndsWith(currentYear))
                            .ToList();

                        if (!files.Any())
                        {
                            _logger.LogWarning($"==========NO CSV FILES IN '{station.StationName}' FOR IMPORT SALES(POS).==========");

                            // LogMessage logMessage = new("Warning", $"ImportSales - {station.StationName}",
                            //     $"No csv files found for station '{station.StationName}'.");

                            // await _dbContext.LogMessages.AddAsync(logMessage);
                            // await _dbContext.SaveChangesAsync();

                            model.CsvStatus = " NO CSV ";

                            model.Message =
                                $"<strong>SALES(POS):</strong> {model.CsvStatus} {model.OpeningFileStatus} {model.Error} {model.HowManyImported} ";

                            logList.Add(model);

                            continue;
                        }

                        fuelsCount = 0;
                        lubesCount = 0;
                        safedropsCount = 0;
                        hasPoSales = false;

                        foreach (var file in files)
                        {
                            _logger.LogInformation($"==========IMPORTING {station.StationName} SALES(POS) FROM: {file.FileName}==========");
                            var fileName = file.FileName;
                            bool fileOpened = false;
                            while (!fileOpened)
                            {
                                if (fileName.Contains("fuels"))
                                {
                                    (fuelsCount, hasPoSales) =
                                        await _unitOfWork.MobilitySalesHeader.ProcessFuelGoogleDrive(file);
                                }
                                else if (fileName.Contains("lubes"))
                                {
                                    (lubesCount, hasPoSales) =
                                        await _unitOfWork.MobilitySalesHeader.ProcessLubeGoogleDrive(file);
                                }
                                else if (fileName.Contains("safedrops"))
                                {
                                    safedropsCount =
                                        await _unitOfWork.MobilitySalesHeader.ProcessSafeDropGoogleDrive(file);
                                }

                                fileOpened = true; // File opened successfully, exit the loop
                            }

                            if (!fileOpened)
                            {
                                // Log a warning or handle the situation where the file could not be opened after retrying
                                _logger.LogWarning($"==========Failed to open file '{file.FileName}' after multiple retries.==========");

                                // LogMessage logMessage = new("Warning", $"ImportSales - {station.StationName}",
                                //     $"Failed to open file '{file.FileName}' after multiple retries.");
                                //
                                // await _dbContext.LogMessages.AddAsync(logMessage);
                                // await _dbContext.SaveChangesAsync();

                                model.OpeningFileStatus = "Can't open ";

                                model.Message =
                                    $" SALES(POS): {model.CsvStatus} {model.OpeningFileStatus} {model.Error} {model.HowManyImported} ";

                                logList.Add(model);
                            }
                        }

                        if (fuelsCount != 0 || lubesCount != 0 || safedropsCount != 0)
                        {
                            await _unitOfWork.MobilitySalesHeader.ComputeSalesPerCashier(hasPoSales);

                            // LogMessage logMessage = new("Information", $"ImportSales - {station.StationName}",
                            //     $"Sales imported successfully in the station '{station.StationName}', Fuels: '{fuelsCount}' record(s), Lubes: '{lubesCount}' record(s), Safe drops: '{safedropsCount}' record(s).");

                            _logger.LogInformation("==========" + station.StationName + " SALES(POS) IMPORTED==========");

                            // await _dbContext.LogMessages.AddAsync(logMessage);
                            // await _dbContext.SaveChangesAsync();

                            model.HowManyImported = $"FUELS: '{fuelsCount:N0}', LUBES: '{lubesCount:N0}', SAFE DROPS: '{safedropsCount:N0}'.";
                        }
                        else
                        {
                            // Import this message to your message box
                            _logger.LogInformation("==========You're up to date.==========");

                            // LogMessage logMessage = new("Information", $"ImportSales - {station.StationName}",
                            //     $"No new record found in the station '{station.StationName}'.");

                            // await _dbContext.LogMessages.AddAsync(logMessage);
                            // await _dbContext.SaveChangesAsync();

                            model.HowManyImported = "YOU'RE UP TO DATE.";
                        }
                    }
                    catch (Exception ex)
                    {
                        // LogMessage logMessage = new("Error", $"ImportSales - {station.StationName}",
                        //     $"Error: {ex.Message} in '{station.StationName}'.");
                        // _logger.LogInformation("EXCEPTION - ImportSales("  + station.StationName + "): " + ex.Message);

                        // await _dbContext.LogMessages.AddAsync(logMessage);
                        // await _dbContext.SaveChangesAsync();

                        model.Error = $"ERROR: {ex.Message} in '{station.StationName}'.";
                        _logger.LogError(ex, $"Failed to import sales(POS): {ex.Message} in '{station.StationName}'.");
                        throw;
                    }
                }
                else
                {
                    model.Error =
                        $" ERROR: NO SALESTEXT.";
                }
                model.Message =
                    $"SALES(POS): {model.CsvStatus} {model.OpeningFileStatus} {model.Error} {model.HowManyImported} ";

                logList.Add(model);
            }
            _logger.LogInformation($"==========SALES(POS) IMPORT COMPLETED==========");

            return logList;
        }

        private async Task<List<LogMessageDto>> ImportPurchases()
        {
            List<LogMessageDto> logList = new();
            _logger.LogInformation("==========IMPORTING PURCHASES==========");

            var stations = await _dbContext.MobilityStations
                .Where(s => s.IsActive)
                .ToListAsync();

            int fuelsCount;
            int lubesCount;
            int poSalesCount;

            foreach (var station in stations)
            {
                LogMessageDto model = new LogMessageDto();
                model.StationName = station.StationName;

                if (!string.IsNullOrEmpty(station.FolderPath))
                {
                    var fileList = await GetFileFromDriveAsync(station.StationName, station.FolderPath);

                    try
                    {
                        var currentYear = DateTime.UtcNow.ToString("yyyy");
                        var files = fileList.Where(f =>
                                (f.FileName.Contains("FUEL_DELIVERY", StringComparison.CurrentCulture) ||
                                f.FileName.Contains("LUBE_DELIVERY", StringComparison.CurrentCulture)) &&
                                Path.GetFileNameWithoutExtension(f.FileName).EndsWith(DateTime.UtcNow.ToString(currentYear)));

                        if (!files.Any())
                        {
                            // Import this message to your message box
                            // _logger.LogWarning($"NO CSV FILES IN '{station.StationName}' FOR IMPORT PURCHASE.");

                            // LogMessage logMessage = new("Warning", $"ImportPurchases - {station.StationName}",
                            //     $"No csv files found in station '{station.StationName}'.");

                            // await _dbContext.LogMessages.AddAsync(logMessage);
                            // await _dbContext.SaveChangesAsync();

                            model.CsvStatus = " NO CSV ";
                            model.Message =
                                $" <strong>PURCHASES:</strong> {model.CsvStatus} {model.OpeningFileStatus} {model.Error} {model.HowManyImported} ";
                            logList.Add(model);

                            continue;
                        }

                        fuelsCount = 0;
                        lubesCount = 0;
                        poSalesCount = 0;

                        foreach (var file in files)
                        {
                            _logger.LogInformation($"==========IMPORTING {station.StationName} PURCHASES FROM: {file.FileName}==========");
                            string fileName = Path.GetFileName(file.FileName).ToLower();

                            bool fileOpened = false;
                            while (!fileOpened)
                            {
                                if (fileName.Contains("fuel"))
                                {
                                    fuelsCount =
                                        await _unitOfWork.MobilityFuelPurchase.ProcessFuelDeliveryGoogleDrive(
                                            file);
                                }
                                else if (fileName.Contains("lube"))
                                {
                                    lubesCount =
                                        await _unitOfWork.MobilityLubePurchaseHeader
                                            .ProcessLubeDeliveryGoogleDrive(file);
                                }

                                fileOpened = true; // File opened successfully, exit the loop
                            }

                            if (!fileOpened)
                            {
                                // Log a warning or handle the situation where the file could not be opened after retrying
                                _logger.LogWarning(
                                $"==========Failed to open file '{file.FileName}' after multiple retries.==========");

                                // LogMessage logMessage = new("Warning", $"ImportPurchases - {station.StationName}",
                                //     $"Failed to open file '{file.FileName}' after multiple retries.");

                                // await _dbContext.AddAsync(logMessage);
                                // await _dbContext.SaveChangesAsync();

                                model.OpeningFileStatus = $" (CAN'T OPEN FILE'{file.FileName}') ";
                            }
                        }

                        if (fuelsCount != 0 || lubesCount != 0 || poSalesCount != 0)
                        {
                            // LogMessage logMessage = new("Information", $"ImportPurchases - {station.StationName}",
                            //     $"Purchases imported successfully in the station '{station.StationName}', Fuel Delivery: '{fuelsCount}' record(s), Lubes Delivery: '{lubesCount}' record(s), PO Sales: '{poSalesCount}' record(s).");

                            // _logger.LogInformation($"Imported successfully in the station '{station.StationName}', Fuel Delivery: '{fuelsCount}' record(s), Lubes Delivery: '{lubesCount}' record(s), PO Sales: '{poSalesCount}' record(s).");

                            // await _dbContext.LogMessages.AddAsync(logMessage);
                            // await _dbContext.SaveChangesAsync();

                            model.HowManyImported = $"FUELS: '{fuelsCount:N0}', LUBES: '{lubesCount:N0}', PO SALES: '{poSalesCount:N0}'.";
                        }
                        else
                        {
                            // Import this message to your message box
                            // _logger.LogInformation("==========You're up to date.==========");

                            // LogMessage logMessage = new("Information", $"ImportPurchases - {station.StationName}",
                            //     $"No new record found in the station '{station.StationName}'.");

                            // await _dbContext.LogMessages.AddAsync(logMessage);
                            // await _dbContext.SaveChangesAsync();

                            model.HowManyImported = "YOU'RE UP TO DATE.";
                        }
                    }
                    catch (Exception ex)
                    {
                        // LogMessage logMessage = new("Error", $"ImportPurchase - {station.StationName}",
                        //     $"Error: {ex.Message} in '{station.StationName}'.");
                        // _logger.LogInformation("==========GoogleDriveImportService.Purchases - EXCEPTION: " + ex.Message + " " + station.StationName +
                        //                        " SALES==========");

                        // await _dbContext.LogMessages.AddAsync(logMessage);
                        // await _dbContext.SaveChangesAsync();

                        model.Error = $"ERROR: {ex.Message} in '{station.StationName}'.";
                        _logger.LogError(ex, $"Failed to import purchases: {ex.Message} in '{station.StationName}'.");
                        throw;
                    }

                    _logger.LogInformation("==========" + station.StationName + " PURCHASES IMPORTED==========");
                }
                else
                {
                    model.Error =
                        $" ERROR: NO SALESTEXT.";
                }

                model.Message =
                    $" PURCHASES: {model.CsvStatus} {model.OpeningFileStatus} {model.Error} {model.HowManyImported} ";

                logList.Add(model);
            }
            _logger.LogInformation($"==========PURCHASE IMPORT COMPLETE==========");

            return logList;
        }

        private async Task<List<LogMessageDto>> ImportFmsSales()
        {
            List<LogMessageDto> logList = new();
            _logger.LogInformation("==========IMPORTING SALES==========");

            int fuelsCount;
            int lubesCount;
            int depositCount;

            var stations = await _dbContext.MobilityStations
                .Where(s => s.IsActive)
                .ToListAsync();

            foreach (var station in stations)
            {
                LogMessageDto model = new LogMessageDto();
                model.StationName = station.StationName;

                if (!string.IsNullOrEmpty(station.FolderPath))
                {
                    var fileList = await GetFileFromDriveAsync(station.StationName, station.FolderPath);

                    try
                    {
                        var currentYear = DateTime.UtcNow.ToString("yyyy");

                        var files = fileList.Where(f =>
                                (f.FileName.Contains("FMS_CALIBRATION", StringComparison.CurrentCultureIgnoreCase) ||
                                 f.FileName.Contains("FMS_CASHIER_SHIFT", StringComparison.CurrentCultureIgnoreCase) ||
                                 f.FileName.Contains("FMS_FUEL_SALES", StringComparison.CurrentCultureIgnoreCase) ||
                                 f.FileName.Contains("FMS_LUBE_SALES", StringComparison.CurrentCultureIgnoreCase) ||
                                 f.FileName.Contains("FMS_PO_SALES", StringComparison.CurrentCultureIgnoreCase) ||
                                 f.FileName.Contains("FMS_DEPOSIT", StringComparison.CurrentCultureIgnoreCase)) &&
                                Path.GetFileNameWithoutExtension(f.FileName).EndsWith(currentYear))
                            .ToList();

                        if (!files.Any())
                        {
                            _logger.LogWarning($"==========NO CSV FILES IN '{station.StationName}' FOR IMPORT SALES(FMS).==========");

                            model.CsvStatus = " NO CSV ";

                            model.Message =
                                $"<strong>SALES(FMS):</strong> {model.CsvStatus} {model.OpeningFileStatus} {model.Error} {model.HowManyImported} ";

                            logList.Add(model);

                            continue;
                        }

                        fuelsCount = 0;
                        lubesCount = 0;
                        depositCount = 0;

                        foreach (var file in files)
                        {
                            _logger.LogInformation($"==========IMPORTING {station.StationName} SALES(FMS) FROM: {file.FileName}==========");
                            var fileName = file.FileName;
                            bool fileOpened = false;
                            while (!fileOpened)
                            {
                                if (fileName.Contains("FUEL"))
                                {
                                    fuelsCount =
                                        await _unitOfWork.MobilitySalesHeader.ProcessFmsFuelSalesGoogleDrive(file);
                                }
                                else if (fileName.Contains("LUBE"))
                                {
                                    lubesCount =
                                        await _unitOfWork.MobilitySalesHeader.ProcessFmsLubeSalesGoogleDrive(file);
                                }
                                else if (fileName.Contains("CALIBRATION"))
                                {
                                    await _unitOfWork.MobilitySalesHeader.ProcessFmsCalibrationGoogleDrive(file);
                                }
                                else if (fileName.Contains("CASHIER"))
                                {
                                    await _unitOfWork.MobilitySalesHeader.ProcessFmsCashierShiftGoogleDrive(file);
                                }
                                else if (fileName.Contains("PO_SALES"))
                                {
                                   await _unitOfWork.MobilitySalesHeader.ProcessFmsPoSalesGoogleDrive(file);
                                }
                                else if (fileName.Contains("DEPOSIT"))
                                {
                                   depositCount = await _unitOfWork.MobilitySalesHeader.ProcessFmsDepositGoogleDrive(file);
                                }

                                fileOpened = true; // File opened successfully, exit the loop
                            }

                            if (!fileOpened)
                            {
                                // Log a warning or handle the situation where the file could not be opened after retrying
                                _logger.LogWarning($"==========Failed to open file '{file.FileName}' after multiple retries.==========");

                                model.OpeningFileStatus = "Can't open ";

                                model.Message =
                                    $" SALES(FMS): {model.CsvStatus} {model.OpeningFileStatus} {model.Error} {model.HowManyImported} ";

                                logList.Add(model);
                            }
                        }

                        if (fuelsCount != 0 || lubesCount != 0 || depositCount != 0)
                        {
                            await _unitOfWork.MobilitySalesHeader.ComputeSalesReportForFms();

                            _logger.LogInformation("==========" + station.StationName + " SALES(FMS) IMPORTED==========");

                            model.HowManyImported = $"FUELS: '{fuelsCount:N0}', LUBES: '{lubesCount:N0}', DEPOSIT: '{depositCount:N0}' .";
                        }
                        else
                        {
                            // Import this message to your message box
                            _logger.LogInformation("==========You're up to date.==========");

                            model.HowManyImported = "YOU'RE UP TO DATE.";
                        }
                    }
                    catch (Exception ex)
                    {
                        model.Error = $"ERROR: {ex.Message} in '{station.StationName}'.";
                        _logger.LogError(ex, $"Failed to import sales(FMS): {ex.Message} in '{station.StationName}'.");
                        throw;
                    }
                }
                else
                {
                    model.Error =
                        $" ERROR: NO SALESTEXT.";
                }
                model.Message =
                    $"SALES(FMS): {model.CsvStatus} {model.OpeningFileStatus} {model.Error} {model.HowManyImported} ";

                logList.Add(model);
            }
            _logger.LogInformation($"==========SALES(FMS) IMPORT COMPLETED==========");

            return logList;
        }
    }
}
