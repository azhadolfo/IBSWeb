﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Utility;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IBS.DataAccess.Repository.Mobility
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
            DateTime now = DateTimeHelper.GetCurrentPhilippineTime();
            DateTime nextRuntime = new DateTime(now.Year, now.Month, now.Day, 08, 00, 00); // It will automatically run every 8:00 AM
            TimeSpan dueTime;

            if (nextRuntime <= now)
            {
                nextRuntime = nextRuntime.AddDays(1);
            }

            dueTime = nextRuntime - now; // next - 05/11/2024 08:00 now - 05/10/2024 17:11 ???

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromDays(1)); // Adjust the interval as needed

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                LogMessage logMessage = new("Information", "ImportService", $"Importing service is starting in {DateTime.Now}.");

                await db.LogMessages.AddAsync(logMessage);
                await db.SaveChangesAsync();

                await ImportSales();
                await ImportPurchases();

                _logger.LogInformation("Import completed successfully.");
            }
            catch (Exception ex)
            {
                LogMessage logMessage = new("Error", "ImportService", $"Error: {ex.Message}.");

                await db.LogMessages.AddAsync(logMessage);
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

            var stations = await db.MobilityStations.Where(s => s.StationCode == "S19").ToListAsync();
            int fuelsCount;
            int lubesCount;
            int safedropsCount;
            bool hasPoSales = false;

            foreach (var station in stations)
            {
                var importFolder = Path.Combine(station.FolderPath);

                if (!Directory.Exists(importFolder))
                {
                    // Import this message to your message box
                    _logger.LogWarning($"The directory for station '{station.StationName}' was not found.");

                    LogMessage logMessage = new("Warning", "ImportSales", $"The directory '{importFolder}' for station '{station.StationName}' was not found.");

                    await db.LogMessages.AddAsync(logMessage);
                    await db.SaveChangesAsync();

                    continue;
                }

                await using var transaction = await db.Database.BeginTransactionAsync();

                try
                {
                    var files = Directory.GetFiles(importFolder, "*.csv")
                                        .Where(f =>
                                            f.Contains("fuels", StringComparison.CurrentCultureIgnoreCase) ||
                                            f.Contains("lubes", StringComparison.CurrentCultureIgnoreCase) ||
                                            f.Contains("safedrops", StringComparison.CurrentCultureIgnoreCase) &&
                                            Path.GetFileNameWithoutExtension(f).EndsWith(DateTime.UtcNow.ToString("yyyy")));

                    if (!files.Any())
                    {
                        // Import this message to your message box
                        _logger.LogWarning($"No csv files found in station '{station.StationName}'.");

                        LogMessage logMessage = new("Warning", "ImportSales", $"No csv files found in station '{station.StationName}'.");

                        await db.LogMessages.AddAsync(logMessage);
                        await db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        continue;
                    }

                    fuelsCount = 0;
                    lubesCount = 0;
                    safedropsCount = 0;
                    hasPoSales = false;

                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file).ToLower();
                        bool fileOpened = false;
                        int retryCount = 0;
                        while (!fileOpened && retryCount < 5)
                        {
                            try
                            {
                                if (fileName.Contains("fuels"))
                                {
                                    (fuelsCount, hasPoSales) = await unitOfWork.MobilitySalesHeader.ProcessFuel(file);
                                }
                                else if (fileName.Contains("lubes"))
                                {
                                    (lubesCount, hasPoSales) = await unitOfWork.MobilitySalesHeader.ProcessLube(file);
                                }
                                else if (fileName.Contains("safedrops"))
                                {
                                    safedropsCount = await unitOfWork.MobilitySalesHeader.ProcessSafeDrop(file);
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
                            _logger.LogWarning($"Failed to open file '{file}' after multiple retries.");

                            LogMessage logMessage = new("Warning", "ImportSales", $"Failed to open file '{file}' after multiple retries.");

                            await db.LogMessages.AddAsync(logMessage);
                            await db.SaveChangesAsync();
                        }
                    }

                    if (fuelsCount != 0 || lubesCount != 0 || safedropsCount != 0)
                    {
                        await unitOfWork.MobilitySalesHeader.ComputeSalesPerCashier(hasPoSales);

                        LogMessage logMessage = new("Information", "ImportSales", $"Imported successfully in the station '{station.StationName}', Fuels: '{fuelsCount}' record(s), Lubes: '{lubesCount}' record(s), Safe drops: '{safedropsCount}' record(s).");

                        await db.LogMessages.AddAsync(logMessage);
                        await db.SaveChangesAsync();
                    }
                    else
                    {
                        // Import this message to your message box
                        _logger.LogInformation("You're up to date.");

                        LogMessage logMessage = new("Information", "ImportSales", $"No new record found in the station '{station.StationName}'.");

                        await db.LogMessages.AddAsync(logMessage);
                        await db.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    LogMessage logMessage = new("Error", "ImportSales", $"Error: {ex.Message} in '{station.StationName}'.");

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

            var stations = await db.MobilityStations.Where(s => s.StationCode == "S19").ToListAsync();
            int fuelsCount;
            int lubesCount;
            int poSalesCount;

            foreach (var station in stations)
            {
                var importFolder = Path.Combine(station.FolderPath);

                if (!Directory.Exists(importFolder))
                {
                    // Import this message to your message box
                    _logger.LogWarning($"The directory for station '{station.StationName}' was not found.");

                    LogMessage logMessage = new("Warning", "ImportPurchases", $"The directory '{importFolder}' for station '{station.StationName}' was not found.");

                    await db.LogMessages.AddAsync(logMessage);
                    await db.SaveChangesAsync();

                    continue;
                }

                await using var transaction = await db.Database.BeginTransactionAsync();

                try
                {
                    var files = Directory.GetFiles(importFolder, "*.csv")
                                     .Where(f =>
                                     f.Contains("FUEL_DELIVERY", StringComparison.CurrentCulture) ||
                                     f.Contains("LUBE_DELIVERY", StringComparison.CurrentCulture) ||
                                     f.Contains("PO_SALES", StringComparison.CurrentCulture) &&
                                     Path.GetFileNameWithoutExtension(f).EndsWith(DateTime.UtcNow.ToString("yyyy")));

                    if (!files.Any())
                    {
                        // Import this message to your message box
                        _logger.LogWarning($"No csv files found in station '{station.StationName}'.");

                        LogMessage logMessage = new("Warning", "ImportPurchases", $"No csv files found in station '{station.StationName}'.");

                        await db.LogMessages.AddAsync(logMessage);
                        await db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        continue;
                    }

                    fuelsCount = 0;
                    lubesCount = 0;
                    poSalesCount = 0;

                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileName(file).ToLower();

                        bool fileOpened = false;
                        int retryCount = 0;
                        while (!fileOpened && retryCount < 5)
                        {
                            try
                            {
                                if (fileName.Contains("fuel"))
                                {
                                    fuelsCount = await unitOfWork.MobilityFuelPurchase.ProcessFuelDelivery(file);
                                }
                                else if (fileName.Contains("lube"))
                                {
                                    lubesCount = await unitOfWork.MobilityLubePurchaseHeader.ProcessLubeDelivery(file);
                                }
                                else if (fileName.Contains("po_sales"))
                                {
                                    poSalesCount = await unitOfWork.MobilityPOSales.ProcessPOSales(file);
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
                            _logger.LogWarning($"Failed to open file '{file}' after multiple retries.");

                            LogMessage logMessage = new("Warning", "ImportPurchases", $"Failed to open file '{file}' after multiple retries.");

                            await db.LogMessages.AddAsync(logMessage);
                            await db.SaveChangesAsync();

                            return;
                        }
                    }

                    if (fuelsCount != 0 || lubesCount != 0 || poSalesCount != 0)
                    {
                        LogMessage logMessage = new("Information", "ImportPurchases", $"Imported successfully in the station '{station.StationName}', Fuel Delivery: '{fuelsCount}' record(s), Lubes Delivery: '{lubesCount}' record(s), PO Sales: '{poSalesCount}' record(s).");

                        await db.LogMessages.AddAsync(logMessage);
                        await db.SaveChangesAsync();
                    }
                    else
                    {
                        // Import this message to your message box
                        _logger.LogInformation("You're up to date.");

                        LogMessage logMessage = new("Information", "ImportPurchases", $"No new record found in the station '{station.StationName}'.");

                        await db.LogMessages.AddAsync(logMessage);
                        await db.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    LogMessage logMessage = new("Error", "ImportPurchase", $"Error: {ex.Message} in '{station.StationName}'.");

                    await db.LogMessages.AddAsync(logMessage);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}