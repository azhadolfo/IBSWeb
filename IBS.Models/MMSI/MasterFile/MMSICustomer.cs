using System.ComponentModel.DataAnnotations;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSICustomer
    {
        [Key]
        public int MMSICustomerId { get; set; }

        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        public string CustomerTIN {  get; set; }

        public string CustomerBusinessStyle { get; set; }

        public string CustomerTerms { get; set; }

        public string? Landline1 { get; set; }

        public string? Landline2 { get; set; }

        public string? Mobile1 { get; set; }

        public string? Mobile2 { get; set; }

        public bool IsActive { get; set; }

        public bool IsVatable { get; set; }
    }
}
