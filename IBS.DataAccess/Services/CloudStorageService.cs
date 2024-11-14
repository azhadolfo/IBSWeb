using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using IBS.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IBS.DataAccess.Services
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
                _googleCredential = environment == Environments.Production
                    ? GoogleCredential.FromJson(_options.GCPStorageAuthFile)
                    : GoogleCredential.FromFile(_options.GCPStorageAuthFile);

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
                var serviceCredential = _googleCredential.UnderlyingCredential as ServiceAccountCredential;
                if (serviceCredential == null)
                {
                    throw new InvalidOperationException("Service account credentials are required to sign URLs.");
                }

                var urlSigner = UrlSigner.FromServiceAccountCredential(serviceCredential);
                var signedUrl = await urlSigner.SignAsync(_options.GoogleCloudStorageBucketName, fileNameToRead, TimeSpan.FromMinutes(timeOutInMinutes));
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
    }
}
