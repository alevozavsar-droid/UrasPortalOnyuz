using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class MultiSelectFilterModel
    {
        public string ColumnId { get; set; }
        public List<string> Values { get; set; }
    }
}