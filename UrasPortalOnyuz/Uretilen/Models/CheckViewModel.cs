using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class CheckViewModel
    {
        public string CheckNum { get; set; }
        public string BankName { get; set; }
        public string OriginalDebtor { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public int CheckEntry { get; set; } // SQL sorgusundaki TransId'ye karşılık gelir.
        public string Note { get; set; } // Kullanıcının ekleyebileceği not için
    }
}