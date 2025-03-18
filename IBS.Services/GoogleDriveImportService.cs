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
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace IBS.Services
{
    public interface IGoogleDriveImportService
    {
        Task<List<GoogleDriveFile>> GetFileFromDriveAsync(string stationName, string folderId);
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
                LogMessage logMessage = new("Information", "ImportStart",
                    sessionCode);
                await _dbContext.LogMessages.AddAsync(logMessage);

                _logger.LogInformation($"==========Import process started in {sessionStartDate}==========");

                logMessage = new("Information", "GoogleDriveImportService",
                    $"Import process started in {DateTimeHelper.GetCurrentPhilippineTime()}.");
                await _dbContext.LogMessages.AddAsync(logMessage);

                var salesModel = await ImportSales();
                var purchasesModel = await ImportPurchases();

                foreach (var sales in salesModel)
                {
                    var purchase = purchasesModel
                        .FirstOrDefault(s => s.StationName == sales.StationName);
                    var message = $"{sales.Message} /// {purchase.Message}";
                    logMessage = new("Information", $"ImportService-{sales.StationName}",
                        message);
                    if (!string.IsNullOrEmpty(purchase.OpeningFileStatus) || !string.IsNullOrEmpty(sales.OpeningFileStatus))
                    {
                        logMessage.LogLevel = "Warning";
                    }
                    if (!string.IsNullOrEmpty(purchase.Error) || !string.IsNullOrEmpty(sales.Error))
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

                logMessage = new("Information", "ImportEnd",
                    sessionCode);
                await _dbContext.LogMessages.AddAsync(logMessage);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogInformation("==========GoogleDriveImportService.Execute - EXCEPTION: " + ex.Message + "==========");

                LogMessage logMessage = new("Error", "GoogleDriveImportService",
                    $"Importing service exception {DateTimeHelper.GetCurrentPhilippineTime()}.");
                await _dbContext.LogMessages.AddAsync(logMessage);
                logMessage = new("Information", "ImportEnd",
                    sessionCode);
                await _dbContext.LogMessages.AddAsync(logMessage);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<List<GoogleDriveFile>> GetFileFromDriveAsync(string stationName, string folderId)
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
            var filesModel = new List<GoogleDriveFile>();

            do
            {
                var result = await request.ExecuteAsync();

                foreach (var file in result.Files)
                {
                    var fileVM = new GoogleDriveFile
                    {
                        FileName = file.Name,
                        FileLink = file.WebViewLink
                    };

                    using (var stream = new MemoryStream())
                    {
                        var downloadRequest = service.Files.Get(file.Id);
                        await downloadRequest.DownloadAsync(stream);
                        fileVM.FileContent = stream.ToArray();
                    }

                    filesModel.Add(fileVM);
                }

                request.PageToken = result.NextPageToken;
            } while (request.PageToken != null);

            return filesModel;
        }

        public async Task<List<LogMessageDto>> ImportSales()
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

                if (station.FolderPath != " ")
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
                            _logger.LogWarning($"==========NO CSV FILES IN '{station.StationName}' FOR IMPORT SALES.==========");

                            // LogMessage logMessage = new("Warning", $"ImportSales - {station.StationName}",
                            //     $"No csv files found for station '{station.StationName}'.");

                            // await _dbContext.LogMessages.AddAsync(logMessage);
                            // await _dbContext.SaveChangesAsync();

                            model.CsvStatus = " (NO CSV) ";

                            model.Message =
                                $" ImportSales({model.StationName}): {model.CsvStatus} {model.OpeningFileStatus} {model.Error} {model.HowManyImported} ";

                            logList.Add(model);

                            continue;
                        }

                        fuelsCount = 0;
                        lubesCount = 0;
                        safedropsCount = 0;
                        hasPoSales = false;

                        foreach (var file in files)
                        {
                            _logger.LogInformation($"==========IMPORTING {station.StationName} SALES FROM: {file.FileName}==========");
                            var fileName = file.FileName;
                            bool fileOpened = false;
                            int retryCount = 0;
                            while (!fileOpened && retryCount < 5)
                            {
                                try
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
                                catch (IOException)
                                {
                                    // File is locked, wait for 100 milliseconds before retrying
                                    await Task.Delay(100);
                                    retryCount++;
                                }
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
                                    $" ImportSales({model.StationName}): {model.CsvStatus}/{model.OpeningFileStatus}/{model.Error}/{model.HowManyImported} ";

                                logList.Add(model);
                            }
                        }

                        if (fuelsCount != 0 || lubesCount != 0 || safedropsCount != 0)
                        {
                            await _unitOfWork.MobilitySalesHeader.ComputeSalesPerCashier(hasPoSales);

                            // LogMessage logMessage = new("Information", $"ImportSales - {station.StationName}",
                            //     $"Sales imported successfully in the station '{station.StationName}', Fuels: '{fuelsCount}' record(s), Lubes: '{lubesCount}' record(s), Safe drops: '{safedropsCount}' record(s).");

                            _logger.LogInformation("==========" + station.StationName + " SALES IMPORTED==========");

                            // await _dbContext.LogMessages.AddAsync(logMessage);
                            // await _dbContext.SaveChangesAsync();

                            model.HowManyImported = $"Sales for: '{station.StationName}', Fuels: '{fuelsCount}', Lubes: '{lubesCount}', Safe drops: '{safedropsCount}'.";
                        }
                        else
                        {
                            // Import this message to your message box
                            _logger.LogInformation("==========You're up to date.==========");

                            // LogMessage logMessage = new("Information", $"ImportSales - {station.StationName}",
                            //     $"No new record found in the station '{station.StationName}'.");

                            // await _dbContext.LogMessages.AddAsync(logMessage);
                            // await _dbContext.SaveChangesAsync();

                            model.HowManyImported = "You're up to date.";
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
                    }
                }
                else
                {
                    model.Error =
                        $" ERROR: NO SALESTEXT.";
                }
                model.Message =
                    $" ImportSales({model.StationName}): {model.CsvStatus}/{model.OpeningFileStatus}/{model.Error}/{model.HowManyImported} ";

                logList.Add(model);
            }
            _logger.LogInformation($"==========SALES IMPORT COMPLETED==========");

            return logList;
        }

        public async Task<List<LogMessageDto>> ImportPurchases()
        {
            List<LogMessageDto> logList = new();
            _logger.LogInformation("==========IMPORTING PURCHASES==========");

            var stations = await _dbContext.MobilityStations.Where(s => s.IsActive)
                .ToListAsync();

            int fuelsCount;
            int lubesCount;
            int poSalesCount;

            foreach (var station in stations)
            {
                LogMessageDto model = new LogMessageDto();
                model.StationName = station.StationName;

                if (station.FolderPath != " ")
                {
                    var fileList = await GetFileFromDriveAsync(station.StationName, station.FolderPath);

                    try
                    {
                        var currentYear = DateTime.UtcNow.ToString("yyyy");
                        var files = fileList.Where(f =>
                                (f.FileName.Contains("FUEL_DELIVERY", StringComparison.CurrentCulture) ||
                                f.FileName.Contains("LUBE_DELIVERY", StringComparison.CurrentCulture) ||
                                f.FileName.Contains("PO_SALES", StringComparison.CurrentCulture)) &&
                                Path.GetFileNameWithoutExtension(f.FileName).EndsWith(DateTime.UtcNow.ToString(currentYear)));

                        if (!files.Any())
                        {
                            // Import this message to your message box
                            // _logger.LogWarning($"NO CSV FILES IN '{station.StationName}' FOR IMPORT PURCHASE.");

                            // LogMessage logMessage = new("Warning", $"ImportPurchases - {station.StationName}",
                            //     $"No csv files found in station '{station.StationName}'.");

                            // await _dbContext.LogMessages.AddAsync(logMessage);
                            // await _dbContext.SaveChangesAsync();

                            model.CsvStatus = " (NO CSV) ";
                            model.Message =
                                $" ImportPurchases({model.StationName}): {model.CsvStatus} {model.OpeningFileStatus} {model.Error} {model.HowManyImported} ";
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
                            int retryCount = 0;
                            while (!fileOpened && retryCount < 5)
                            {
                                try
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
                                    else if (fileName.Contains("po_sales"))
                                    {
                                        poSalesCount =
                                            await _unitOfWork.MobilityPOSales.ProcessPOSalesGoogleDrive(file);
                                    }

                                    fileOpened = true; // File opened successfully, exit the loop
                                }
                                catch (Exception ex)
                                {
                                    // File is locked, wait for 100 milliseconds before retrying
                                    await Task.Delay(100);
                                    retryCount++;
                                }
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

                            model.HowManyImported = $"Purchases for: '{station.StationName}', Fuels: '{fuelsCount}', Lubes: '{lubesCount}', PO Sales: '{poSalesCount}'.";
                        }
                        else
                        {
                            // Import this message to your message box
                            // _logger.LogInformation("==========You're up to date.==========");

                            // LogMessage logMessage = new("Information", $"ImportPurchases - {station.StationName}",
                            //     $"No new record found in the station '{station.StationName}'.");

                            // await _dbContext.LogMessages.AddAsync(logMessage);
                            // await _dbContext.SaveChangesAsync();

                            model.HowManyImported = "You're up to date.";
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
                    }

                    _logger.LogInformation("==========" + station.StationName + " PURCHASES IMPORTED==========");
                }
                else
                {
                    model.Error =
                        $" ERROR: NO SALESTEXT.";
                }

                model.Message =
                    $" ImportPurchases({model.StationName}): {model.CsvStatus} {model.OpeningFileStatus} {model.Error}/{model.HowManyImported} ";

                logList.Add(model);
            }
            _logger.LogInformation($"==========PURCHASE IMPORT COMPLETE==========");

            return logList;
        }
    }
}
