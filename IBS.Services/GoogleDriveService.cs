using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IBS.Services
{
    public interface IGoogleDriveService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderId, string mimeType);
        Task<GoogleDriveFileViewModel> DownloadFileAsync(string fileId);
        Task<GoogleDriveFileViewModel> GetFileByNameAsync(string fileName, string folderId);
        Task DeleteFileAsync(string? fileId);
    }

    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly GCSConfigOptions _options;
        private readonly ILogger<GoogleDriveService> _logger;
        private readonly DriveService _driveService;
        private readonly string[] _scopes = new[] { DriveService.Scope.DriveFile };
        private const string ApplicationName = "IBS Application";

        public GoogleDriveService(IOptions<GCSConfigOptions> options, ILogger<GoogleDriveService> logger)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                GoogleCredential credential;
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                if (environment == Environments.Production)
                {
                    credential = GoogleCredential.GetApplicationDefault();
                    _logger.LogInformation("Using application default credentials for Google Drive in production.");
                }
                else
                {
                    _logger.LogInformation($"Environment: {environment}, Auth File: {_options.GCPStorageAuthFile}");

                    if (!File.Exists(_options.GCPStorageAuthFile))
                    {
                        throw new FileNotFoundException($"Auth file not found: {_options.GCPStorageAuthFile}");
                    }

                    credential = GoogleCredential.FromFile(_options.GCPStorageAuthFile);
                }

                // Create scoped credential for Drive API
                credential = credential.CreateScoped(_scopes);

                // Initialize Drive API service
                _driveService = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize Google Drive client: {ex.Message}");
                throw;
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderId, string mimeType)
        {
            if (fileStream == null || !fileStream.CanRead)
            {
                throw new ArgumentException("File stream is invalid or not readable.", nameof(fileStream));
            }
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("File name cannot be empty.", nameof(fileName));
            }
            if (string.IsNullOrEmpty(mimeType))
            {
                throw new ArgumentException("MIME type cannot be empty.", nameof(mimeType));
            }

            try
            {
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = string.IsNullOrEmpty(folderId) ? null : new List<string> { folderId }
                };

                var request = _driveService.Files.Create(fileMetadata, fileStream, mimeType);
                request.Fields = "id";
                var result = await request.UploadAsync();

                if (result.Exception != null)
                {
                    throw result.Exception;
                }

                return request.ResponseBody.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to upload file '{fileName}' to Google Drive: {ex.Message}");
                throw new Exception($"Failed to upload file '{fileName}' to Google Drive: {ex.Message}", ex);
            }
        }

        public async Task<GoogleDriveFileViewModel> DownloadFileAsync(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
            }

            try
            {
                var fileRequest = _driveService.Files.Get(fileId);
                fileRequest.Fields = "id, name, webViewLink";
                var file = await fileRequest.ExecuteAsync();

                using var stream = new MemoryStream();
                await fileRequest.DownloadAsync(stream);

                return new GoogleDriveFileViewModel
                {
                    FileName = file.Name,
                    FileLink = file.WebViewLink,
                    FileContent = stream.ToArray()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to download file with ID '{fileId}' from Google Drive: {ex.Message}");
                throw new Exception($"Failed to download file with ID '{fileId}' from Google Drive: {ex.Message}", ex);
            }
        }

        public async Task<GoogleDriveFileViewModel> GetFileByNameAsync(string fileName, string folderId)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("File name cannot be empty.", nameof(fileName));
            }
            if (string.IsNullOrEmpty(folderId))
            {
                throw new ArgumentException("Folder ID cannot be empty.", nameof(folderId));
            }

            try
            {
                string query = $"name = '{fileName}' and '{folderId}' in parents and trashed = false";
                var listRequest = _driveService.Files.List();
                listRequest.Q = query;
                listRequest.Fields = "files(id, name, webViewLink)";
                listRequest.Spaces = "drive";
                var result = await listRequest.ExecuteAsync();
                var files = result.Files;

                if (files == null || !files.Any())
                {
                    return new GoogleDriveFileViewModel
                    {
                        FileName = string.Empty,
                        FileLink = string.Empty,
                        FileContent = null,
                        FileId = string.Empty,
                        DoesExist = false
                    };
                }

                if (files.Count > 1)
                {
                    _logger.LogWarning($"Multiple files found with name '{fileName}' in folder ID '{folderId}'. Returning the first match.");
                }

                var file = files.First();

                // Downloading
                var fileRequest = _driveService.Files.Get(file.Id);
                fileRequest.Fields = "id, name, webViewLink";
                var fileMetadata = await fileRequest.ExecuteAsync();
                using var stream = new MemoryStream();
                await fileRequest.DownloadAsync(stream);

                return new GoogleDriveFileViewModel
                {
                    FileName = fileMetadata.Name,
                    FileLink = fileMetadata.WebViewLink,
                    FileContent = stream.ToArray(),
                    FileId = file.Id,
                    DoesExist = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to retrieve file '{fileName}' from folder ID '{folderId}' in Google Drive: {ex.Message}");
                throw new Exception($"Failed to retrieve file '{fileName}' from folder ID '{folderId}' in Google Drive: {ex.Message}", ex);
            }
        }

        public async Task DeleteFileAsync(string? fileId)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
            }

            try
            {
                // Execute the delete request
                await _driveService.Files.Delete(fileId).ExecuteAsync();
                _logger.LogInformation($"Successfully deleted file with ID '{fileId}' from Google Drive.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to delete file with ID '{fileId}' from Google Drive: {ex.Message}");
                throw new Exception($"Failed to delete file with ID '{fileId}' from Google Drive: {ex.Message}", ex);
            }
        }
    }
}
