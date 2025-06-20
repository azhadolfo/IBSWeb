using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Cloud.Storage.V1;
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
    }

    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly GCSConfigOptions _options;
        private readonly ILogger<GoogleDriveService> _logger;
        private readonly DriveService _driveService;
        private readonly GoogleCredential _googleCredential;

        private const string ApplicationName = "IBS Application";
        private readonly string[] _scopes;

        public GoogleDriveService(GoogleCredential googleCredential, IOptions<GCSConfigOptions> options, ILogger<GoogleDriveService> logger)
        {
            _options = options.Value;
            _googleCredential = googleCredential;
            _logger = logger;
            _scopes = new[] { DriveService.Scope.DriveFile };

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

            // Load credentials
            using var stream = new FileStream(_options.GCPStorageAuthFile!, FileMode.Open, FileAccess.Read);
            var credential = _googleCredential.CreateScoped(DriveService.ScopeConstants.Drive);

            // Initialize Drive API service
            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential.CreateScoped(_scopes),
                ApplicationName = ApplicationName
            });
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderId, string mimeType)
        {
            // error handling
            if (fileStream == null || !fileStream.CanRead)
            {
                throw new ArgumentException("File stream is invalid or not readable.", nameof(fileStream));
            }
            if (string.IsNullOrEmpty(fileName))
            {            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

            }
            if (string.IsNullOrEmpty(mimeType))
            {
                throw new ArgumentException("MIME type cannot be empty.", nameof(mimeType));
            }

            try
            {
                // convert the file into meta data
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = string.IsNullOrEmpty(folderId) ? null : new List<string> { folderId }
                };

                var request = _driveService.Files.Create(fileMetadata, fileStream, mimeType); // create a file instance into the drive
                request.Fields = "id";
                var result = await request.UploadAsync(); // receives the result of the process

                // if there is exception, throw
                if (result.Exception != null)
                {
                    throw result.Exception;
                }

                return request.ResponseBody.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload file '{fileName}' to Google Drive: {ex.Message}", ex);
            }
        }

        public async Task<GoogleDriveFileViewModel> DownloadFileAsync(string fileId)
        {
            // error handling
            if (string.IsNullOrEmpty(fileId))
                throw new ArgumentException("File ID cannot be empty.", nameof(fileId));

            try
            {
                var fileRequest = _driveService.Files.Get(fileId); // Get file request
                fileRequest.Fields = "id, name, webViewLink";
                var file = await fileRequest.ExecuteAsync(); // Execute file request

                using var stream = new MemoryStream(); // Create stream instance
                await fileRequest.DownloadAsync(stream); // Download file content

                // Return the model containing the file data
                return new GoogleDriveFileViewModel
                {
                    FileName = file.Name,
                    FileLink = file.WebViewLink,
                    FileContent = stream.ToArray()
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download file with ID '{fileId}' from Google Drive: {ex.Message}", ex);
            }
        }
    }
}
