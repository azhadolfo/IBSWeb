using IBS.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IBS.Services
{
    public class LocalFileStorageService : ICloudStorageService
    {
        private readonly GCSConfigOptions _options;
        private readonly ILogger<LocalFileStorageService> _logger;
        private readonly string _storagePath;

        public LocalFileStorageService(IOptions<GCSConfigOptions> options, ILogger<LocalFileStorageService> logger, IWebHostEnvironment environment)
        {
            _options = options.Value;
            _logger = logger;

            // Use a local folder within the project for development storage
            // Cross-platform: works on Windows and Linux
            _storagePath = Path.Combine(environment.ContentRootPath, "App_Data", "LocalStorage");

            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }

            _logger.LogInformation("╔════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║  LOCAL STORAGE MODE (Development)                      ║");
            _logger.LogInformation("║  Storage Path: {StoragePath}                           ║", _storagePath.PadRight(43));
            _logger.LogInformation("║  Works on: Windows & Linux                             ║");
            _logger.LogInformation("║  No GCP credentials required                           ║");
            _logger.LogInformation("╚════════════════════════════════════════════════════════╝");
        }

        public Task DeleteFileAsync(string fileNameToDelete)
        {
            try
            {
                var filePath = Path.Combine(_storagePath, fileNameToDelete);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation($"File deleted: {filePath}");
                }
                else
                {
                    _logger.LogWarning($"File not found for deletion: {filePath}");
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting file: {ex.Message}");
                throw;
            }
        }

        public Task<string> GetSignedUrlAsync(string fileNameToRead, int timeOutInMinutes = 30)
        {
            // For local development, return a direct URL to the file
            var filePath = Path.Combine(_storagePath, fileNameToRead);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}", fileNameToRead);
            }

            // Return a URL that can be used to access the file
            // In development, this will be a relative path
            var relativePath = $"/local-storage/{fileNameToRead.Replace("\\", "/")}";
            _logger.LogInformation($"Local URL generated for file '{fileNameToRead}': {relativePath}");
            return Task.FromResult(relativePath);
        }

        public async Task<string> UploadFileAsync(IFormFile fileToUpload, string fileNameToSave)
        {
            if (fileToUpload == null || fileToUpload.Length == 0)
            {
                _logger.LogError("File upload failed: No file provided or file is empty.");
                throw new ArgumentException("File is either null or empty.", nameof(fileToUpload));
            }

            try
            {
                // Create subdirectory based on filename structure (e.g., folder/filename.ext)
                var fileNameParts = fileNameToSave.Split('/');
                string targetDirectory = _storagePath;

                if (fileNameParts.Length > 1)
                {
                    for (int i = 0; i < fileNameParts.Length - 1; i++)
                    {
                        targetDirectory = Path.Combine(targetDirectory, fileNameParts[i]);
                    }
                }

                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                var filePath = Path.Combine(targetDirectory, fileNameParts[^1]);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await fileToUpload.CopyToAsync(stream);
                }

                _logger.LogInformation($"File uploaded successfully: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while uploading file: {ex.Message}");
                throw;
            }
        }

        public Task<Stream> DownloadFileAsync(string fileNameToDownload)
        {
            try
            {
                var filePath = Path.Combine(_storagePath, fileNameToDownload);
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}", fileNameToDownload);
                }

                var memoryStream = new MemoryStream();
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fileStream.CopyTo(memoryStream);
                }
                memoryStream.Seek(0, SeekOrigin.Begin);

                _logger.LogInformation($"File {fileNameToDownload} downloaded successfully");
                return Task.FromResult<Stream>(memoryStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while downloading file: {ex.Message}");
                throw;
            }
        }

        public async Task<IFormFile?> GetFileAsFormFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("File name is required.");
            }

            try
            {
                var fileStream = await DownloadFileAsync(fileName);

                if (fileStream == null || fileStream.Length == 0)
                {
                    throw new FileNotFoundException("File not found.", fileName);
                }

                var formFile = new FormFile(fileStream, 0, fileStream.Length, "file", fileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/octet-stream",
                };

                return formFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing file: {ex.Message}");
                throw;
            }
        }
    }
}
