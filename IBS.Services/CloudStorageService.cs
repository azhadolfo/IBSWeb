using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using IBS.Utility;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IBS.Services
{
    public interface ICloudStorageService
    {
        Task<string> GetSignedUrlAsync(string fileNameToRead, int timeOutInMinutes = 30);

        Task<string> UploadFileAsync(IFormFile fileToUpload, string fileNameToSave);

        Task DeleteFileAsync(string fileNameToDelete);
    }

    public class CloudStorageService : ICloudStorageService
    {
        private readonly GCSConfigOptions _options;
        private readonly ILogger<CloudStorageService> _logger;
        private readonly GoogleCredential _googleCredential;
        private readonly StorageClient _storageClient;

        public CloudStorageService(IOptions<GCSConfigOptions> options, ILogger<CloudStorageService> logger)
        {
            _options = options.Value;
            _logger = logger;

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

                _storageClient = StorageClient.Create(_googleCredential);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize Google Cloud Storage client: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteFileAsync(string fileNameToDelete)
        {
            try
            {
                await _storageClient.DeleteObjectAsync(_options.GoogleCloudStorageBucketName, fileNameToDelete);
                _logger.LogInformation($"File '{fileNameToDelete}' successfully deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while deleting file '{fileNameToDelete}': {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetSignedUrlAsync(string fileNameToRead, int timeOutInMinutes = 30)
        {
            try
            {
                var bucketName = _options.GoogleCloudStorageBucketName;
                var urlSigner = UrlSigner.FromCredential(_googleCredential);

                var signedUrl = await urlSigner.SignAsync(bucketName, fileNameToRead, TimeSpan.FromMinutes(timeOutInMinutes));

                _logger.LogInformation($"Signed URL obtained for file '{fileNameToRead}'");
                return signedUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while obtaining signed URL for file '{fileNameToRead}': {ex.Message}");
                throw;
            }
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
                _logger.LogInformation($"Uploading file '{fileNameToSave}' to bucket '{_options.GoogleCloudStorageBucketName}'.");

                using (var memoryStream = new MemoryStream())
                {
                    await fileToUpload.CopyToAsync(memoryStream);
                    memoryStream.Position = 0; // Reset stream position after copying

                    var uploadedFile = await _storageClient.UploadObjectAsync(
                        _options.GoogleCloudStorageBucketName,
                        fileNameToSave,
                        fileToUpload.ContentType ?? "application/octet-stream",
                        memoryStream
                    );

                    _logger.LogInformation($"File '{fileNameToSave}' uploaded successfully. Media link: {uploadedFile.MediaLink}");
                    return uploadedFile.MediaLink;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while uploading file '{fileNameToSave}': {ex.Message}");
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(string fileNameToDownload)
        {
            try
            {
                _logger.LogInformation($"Downloading file {fileNameToDownload} from storage {_options.GoogleCloudStorageBucketName}");

                using (var storageClient = StorageClient.Create(_googleCredential))
                {
                    var memoryStream = new MemoryStream();
                    await storageClient.DownloadObjectAsync(_options.GoogleCloudStorageBucketName, fileNameToDownload, memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin); // Reset stream position to the beginning for reading
                    _logger.LogInformation($"File {fileNameToDownload} downloaded successfully");
                    return memoryStream;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while downloading file {fileNameToDownload}: {ex.Message}");
                throw;
            }
        }
    }
}
