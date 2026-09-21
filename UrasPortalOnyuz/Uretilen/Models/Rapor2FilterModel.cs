using System.Collections.Generic;

namespace WebApplication3.Models
{
    public class Rapor2FilterModel
    {
        public string SelectedDbKey { get; set; }


        public string InputFilter0 { get; set; }  // Çek Numarası (data-column="0")
        public string InputFilter3 { get; set; }  // Çek Tutarı (data-column="3")
        public string InputFilter5 { get; set; }  // Vade Tarihi (data-column="5")
        public string InputFilter6 { get; set; }  // Vade Ayı ve Yılı (data-column="6")
        public string InputFilter7 { get; set; }  // Portföye Giriş Tarihi (data-column="7")
        public string InputFilter8 { get; set; }  // İbraz Tarihi (data-column="8")
        public string InputFilter9 { get; set; }  // Tahsilat Belge No (data-column="9")
        public string InputFilter10 { get; set; } // İbraz Belge No (data-column="10")
        public string InputFilter16 { get; set; } // TL Karşılığı (eski data-column="17", şimdi "16")


        public List<string> MultiSelectFilter1 { get; set; }  // Çek Kimden Geldi (data-column="1")
        public List<string> MultiSelectFilter2 { get; set; }  // Asıl Borçlu (data-column="2")
        public List<string> MultiSelectFilter4 { get; set; }  // Para Birimi (data-column="4")
        public List<string> MultiSelectFilter11 { get; set; } // Hareket Durumu (data-column="11")
        public List<string> MultiSelectFilter12 { get; set; } // Belge Tipi (data-column="12")
        public List<string> MultiSelectFilter13 { get; set; } // Çek İşlem Türü (data-column="13")
        public List<string> MultiSelectFilter14 { get; set; } // İşlem Tipi Adı (data-column="14")

        public List<string> MultiSelectFilter15 { get; set; } // Çek Kime Verildi (eski data-column="16", şimdi "15")

        public int? SortColumn { get; set; }
        public string SortDirection { get; set; } // "asc", "desc", "none"
    }
}