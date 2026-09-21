using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class OPCH
    {
        [Key]
        public int DocNum { get; set; }

        public int DocEntry { get; set; }
        public int? U_BE1_SIRA { get; set; }

    }
}
