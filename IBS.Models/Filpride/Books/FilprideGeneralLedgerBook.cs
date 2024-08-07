﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.Books
{
    public class FilprideGeneralLedgerBook
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GeneralLedgerBookId { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        public string Reference { get; set; }

        [Display(Name = "Account Number")]
        public string AccountNo { get; set; }

        [Display(Name = "Account Title")]
        public string AccountTitle { get; set; }

        public string Description { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Debit { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Credit { get; set; }

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsPosted { get; set; } = true;

        public string Company { get; set; } = string.Empty;
    }
}