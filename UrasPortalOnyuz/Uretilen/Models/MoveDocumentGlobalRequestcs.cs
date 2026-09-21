using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class MoveDocumentGlobalRequest
    {
        public string DocNum { get; set; }
        public string ObjType { get; set; }
        public string MoveDirection { get; set; } // "top" veya "bottom"
        public string SelectedDatabase { get; set; } // Hangi veritabanında işlem yapılacağını belirtir
    }
}
