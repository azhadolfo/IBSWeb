using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class Station
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StationId { get; set; }

        [Display(Name = "POS Code")]
        public string PosCode { get; set; }

        [Display(Name = "Station Code")]
        [Column(TypeName = "varchar(3)")]
        public string StationCode { get; set; }

        [Display(Name = "Station Name")]
        [Column(TypeName = "varchar(50)")]
        public string StationName { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string Initial { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Edited By")]
        [Column(TypeName = "varchar(50)")]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        public DateTime EditedDate { get; set; }
    }
}