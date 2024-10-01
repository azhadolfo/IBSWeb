using System.ComponentModel.DataAnnotations;
using System.Net;

namespace IBS.Models.Filpride.Books
{
    public class FilprideAuditTrail
    {
        public Guid Id { get; set; }

        public string Username { get; set; }

        public DateTime Date { get; set; }

        [Display(Name = "Machine Name")]
        public string MachineName { get; set; }

        public string Activity { get; set; }

        [Display(Name = "Document Type")]
        public string DocumentType { get; set; }

        public string Company { get; set; }

        public FilprideAuditTrail()
        {
        }

        public FilprideAuditTrail(string username, string activity, string documentType, string ipAddress, string company)
        {
            Username = username;
            Date = DateTime.Now;

            // Attempt to resolve IP to hostname, fallback to IP if resolution fails
            try
            {
                var hostEntry = Dns.GetHostEntry(ipAddress);
                MachineName = hostEntry.HostName;
            }
            catch (Exception)
            {
                MachineName = ipAddress; // Fallback to IP address
            }

            Activity = activity;
            DocumentType = documentType;
            Company = company;
        }
    }
}