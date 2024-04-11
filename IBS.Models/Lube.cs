﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class Lube : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column(TypeName = "date")]
        public DateOnly INV_DATE { get; set; }

        public int xYEAR { get; set; }

        public int xMONTH { get; set; }

        public int xDAY { get; set; }

        public int xCORPCODE { get; set; }

        public int xSITECODE { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Price { get; set; }

        //AmountDB = Price * Volume
        [Column(TypeName = "numeric(18,2)")]
        public decimal AmountDB { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        //Volume = Amount / Price
        public int LubesQty { get; set; }

        [Column(TypeName = "varchar(16)")]
        public string ItemCode { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Particulars { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string xOID { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Cashier { get; set; }

        public int Shift { get; set; }

        public long xTRANSACTION { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string xStamp { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? plateno { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? pono { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? cust { get; set; }

        public bool IsProcessed { get; set; }
    }
}