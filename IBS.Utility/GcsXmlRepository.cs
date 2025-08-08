using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace IBS.Utility
{
    public class GcsXmlRepository : IXmlRepository
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;
        private readonly string _objectName;

        public GcsXmlRepository(StorageClient storageClient, string bucketName, string objectName)
        {
            _storageClient = storageClient;
            _bucketName = bucketName;
            _objectName = objectName;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            try
            {
                using var stream = new MemoryStream();
                _storageClient.DownloadObject(_bucketName, _objectName, stream);
                stream.Position = 0;

                var doc = XDocument.Load(stream);
                return (doc.Root?.Elements() ?? []).ToList();
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // No keys yet — safe to ignore
                return new List<XElement>();
            }
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            var elements = GetAllElements().ToList();
            elements.Add(element);

            var doc = new XDocument(new XElement("root", elements));

            using var stream = new MemoryStream();
            doc.Save(stream);
            stream.Position = 0;

            _storageClient.UploadObject(_bucketName, _objectName, "application/xml", stream);
        }
    }
}
