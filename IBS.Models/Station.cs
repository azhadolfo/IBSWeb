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
        public int PosCode { get; set; }

        [Display(Name = "Customer Address")]
        [Column(TypeName = "varchar(5)")]
        public string StationCode { get; set; }

        [Display(Name = "Station Name")]
        [Column(TypeName = "varchar(50)")]
        public string StationName { get; set; }
    }
}