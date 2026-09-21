using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models // Projenizin ana ad alanı (namespace)
{
    public class MoveDocumentRequest
    {
        public string DocNum { get; set; }
        public string ObjType { get; set; }
        public string NewOrder { get; set; } // Bu alan, JavaScript'ten gelen yeni sıralama bilgisini taşıyacak
    }
}