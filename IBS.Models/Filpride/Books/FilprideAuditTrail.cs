using System.ComponentModel.DataAnnotations;
using System.Net;
using IBS.Utility;
using IBS.Utility.Helpers;

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

        public FilprideAuditTrail(string username, string activity, string documentType, string company)
        {
            Username = username;
            Date = DateTimeHelper.GetCurrentPhilippineTime();
            MachineName = Environment.MachineName;
            Activity = activity;
            DocumentType = documentType;
            Company = company;
        }
    }
}
