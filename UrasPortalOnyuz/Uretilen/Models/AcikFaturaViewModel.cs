using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class AcikFaturaViewModel
    {
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DueDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public decimal DocTotal { get; set; }
        public string DocCurrency { get; set; }
        public decimal BalanceDue { get; set; }

        public string ApprovalStatus { get; set; }
    }














}