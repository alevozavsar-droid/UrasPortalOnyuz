using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class ExportRequestModel
    {
        public List<FilterModel> Filters { get; set; }
        public SortModel Sort { get; set; }


        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> TransactionTypes { get; set; }
    }

    public class FilterModel
    {
        public int ColumnIndex { get; set; }
        public string ColumnName { get; set; }
        public List<string> Values { get; set; }
    }

    public class SortModel
    {
        public int ColumnIndex { get; set; }
        public string ColumnName { get; set; }
        public string Direction { get; set; }
    }
}