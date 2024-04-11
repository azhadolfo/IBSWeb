using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class CsvFile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FileId { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string FileName { get; set; }

        [Column(TypeName = "varchar(3)")]
        public string StationCode { get; set; }

        public bool IsUploaded { get; set; }
    }
}
