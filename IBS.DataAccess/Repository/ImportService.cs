using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace IBS.DataAccess.Repository
{
    public class ImportService : IHostedService, IDisposable
    {
        private readonly ILogger<ImportService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;

        public ImportService(ILogger<ImportService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sales import service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(2000)); // Adjust the interval as needed

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            try
            {
                await ImportSales();
                await ImportPurchases();

                _logger.LogInformation("Import completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during import.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sales import service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async Task ImportSales()
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var stations = await db.Stations.ToListAsync();
            int fuelsCount;
            int lubesCount;
            int safedropsCount;
            bool hasPoSales = false;


            foreach (var station in stations.Where(s => s.StationName == "TARLAC"))
            {

                var importFolder = Path.Combine(station.FolderPath, "SALESTEXT");

                if (!Directory.Exists(importFolder))
                {
                    // Import this message to your message box
                    _logger.LogWarning($"The directory for station '{station.StationName}' was not found.");

                    LogMessage logMessage = new("Warning", "Importing Sales", $"The directory '{importFolder}' for station '{station.StationName}' was not found.");

                    await db.LogMessages.AddAsync(logMessage);
                    await db.SaveChangesAsync();

                    continue;
                }

                try
                {
                    var files = Directory.GetFiles(importFolder, "*.csv")
                                        .Where(f =>
                                            f.Contains("fuels", StringComparison.CurrentCultureIgnoreCase) ||
                                            f.Contains("lubes", StringComparison.CurrentCultureIgnoreCase) ||
                                            f.Contains("safedrops", StringComparison.CurrentCultureIgnoreCase) &&
                                            Path.GetFileNameWithoutExtension(f).EndsWith(DateTime.Now.ToString("yyyy")));

                    if (!files.Any())
                    {
                        // Import this message to your message box
                        _logger.LogWarning($"No csv files found in station '{station.StationName}'.");

                        LogMessage logMessage = new("Warning", "Importing Sales", $"No csv files found in station '{station.StationName}'.");

                        await db.LogMessages.AddAsync(logMessage);
                        await db.SaveChangesAsync();

                        continue;
                    }

                    fuelsCount = 0;
                    lubesCount = 0;
                    safedropsCount = 0;
                    hasPoSales = false;

                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file).ToLower();

                        await using var stream = new FileStream(file, FileMode.Open);
                        using var reader = new StreamReader(stream);
                        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            HeaderValidated = null,
                            MissingFieldFound = null,
                        });

                        var newRecords = new List<object>();

                        if (fileName.Contains("fuels"))
                        {
                            var records = csv.GetRecords<Fuel>();
                            var existingRecords = await db.Set<Fuel>().ToListAsync();
                            foreach (var record in records)
                            {
                                if (!existingRecords.Exists(existingRecord => existingRecord.nozdown == record.nozdown))
                                {
                                    if (!String.IsNullOrEmpty(record.cust) && !String.IsNullOrEmpty(record.plateno) && !String.IsNullOrEmpty(record.pono))
                                    {
                                        hasPoSales = true;
                                    }

                                    newRecords.Add(record);
                                    fuelsCount++;
                                }
                            }
                        }
                        else if (fileName.Contains("lubes"))
                        {
                            var records = csv.GetRecords<Lube>();
                            var existingRecords = await db.Set<Lube>().ToListAsync();
                            foreach (var record in records)
                            {
                                if (!existingRecords.Exists(existingRecord => existingRecord.xStamp == record.xStamp))
                                {
                                    if (!String.IsNullOrEmpty(record.cust) && !String.IsNullOrEmpty(record.plateno) && !String.IsNullOrEmpty(record.pono))
                                    {
                                        hasPoSales = true;
                                    }

                                    newRecords.Add(record);
                                    lubesCount++;
                                }
                            }
                        }
                        else if (fileName.Contains("safedrops"))
                        {
                            var records = csv.GetRecords<SafeDrop>();
                            var existingRecords = await db.Set<SafeDrop>().ToListAsync();
                            foreach (var record in records)
                            {
                                if (!existingRecords.Exists(existingRecord => existingRecord.xSTAMP == record.xSTAMP))
                                {
                                    newRecords.Add(record);
                                    safedropsCount++;
                                }
                            }
                        }

                        await db.AddRangeAsync(newRecords);
                        await db.SaveChangesAsync();
                    }

                    if (fuelsCount != 0 || lubesCount != 0 || safedropsCount != 0)
                    {
                        await unitOfWork.SalesHeader.ComputeSalesPerCashier(hasPoSales);
                    }
                    else
                    {
                        // Import this message to your message box
                        _logger.LogInformation("You're up to date.");

                        LogMessage logMessage = new("Information", "Importing Sales", $"No new record found in the station '{station.StationName}'.");

                        await db.LogMessages.AddAsync(logMessage);
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    LogMessage logMessage = new("Error", "Importing Sales", $"Error: {ex.Message} in '{station.StationName}'.");

                    await db.LogMessages.AddAsync(logMessage);
                    await db.SaveChangesAsync();
                }
            }
        }

        private async Task ImportPurchases()
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stations = await db.Stations.ToListAsync();

            int fuelsCount;
            int lubesCount;
            int poSalesCount;

            foreach (var station in stations.Where(s => s.StationName == "TARLAC"))
            {
                if (!Directory.Exists(station.FolderPath))
                {
                    // Import this message to your message box
                    _logger.LogWarning($"The directory for station '{station.StationName}' was not found.");

                    LogMessage logMessage = new("Warning", "Importing Purchases", $"The directory for station '{station.StationName}' was not found.");

                    await db.LogMessages.AddAsync(logMessage);
                    await db.SaveChangesAsync();

                    continue;
                }

                var importFolder = Path.Combine(station.FolderPath, "CSV");
                var files = Directory.GetFiles(importFolder, "*.csv")
                                     .Where(f =>
                                     f.Contains("FUEL_DELIVERY", StringComparison.CurrentCulture) ||
                                     f.Contains("LUBE_DELIVERY", StringComparison.CurrentCulture) ||
                                     (f.Contains("PO_SALES", StringComparison.CurrentCulture) &&
                                     Path.GetFileNameWithoutExtension(f).Contains(DateTime.Now.ToString("yyyy"))));

                if (!files.Any())
                {
                    // Import this message to your message box
                    _logger.LogWarning($"No csv files found in station '{station.StationName}'.");

                    LogMessage logMessage = new("Warning", "Importing Purchases", $"No csv files found in station '{station.StationName}'.");

                    await db.LogMessages.AddAsync(logMessage);
                    await db.SaveChangesAsync();

                    continue;
                }

                fuelsCount = 0;
                lubesCount = 0;
                poSalesCount = 0;

                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file).ToLower();

                    if (fileName.Contains("fuel"))
                    {
                        fuelsCount = await unitOfWork.FuelPurchase.ProcessFuelDelivery(file);
                    }
                    else if (fileName.Contains("lube"))
                    {
                        lubesCount = await unitOfWork.LubePurchaseHeader.ProcessLubeDelivery(file);
                    }
                    else if (fileName.Contains("po_sales"))
                    {
                        poSalesCount = await unitOfWork.PurchaseOrder.ProcessPOSales(file);
                    }
                }

                if (fuelsCount == 0 && lubesCount == 0 && poSalesCount == 0)
                {
                    // Import this message to your message box
                    _logger.LogInformation("You're up to date.");

                    LogMessage logMessage = new("Information", "Importing Purchases", $"No new record found in the station '{station.StationName}'.");

                    await db.LogMessages.AddAsync(logMessage);
                    await db.SaveChangesAsync();
                }
            }
        }
    }

}
