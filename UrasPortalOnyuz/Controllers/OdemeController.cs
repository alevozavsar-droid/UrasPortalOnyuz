namespace WebApplication3.Controllers
{
    /// <summary>
    /// YAPILAN ÖDEMELER › YENİ ÖDEME — Tahsilat motorunun tedarikçi yönü ("Cari İşler › Tahsilat mantığı, bu sefer ödemeyi biz yapıyoruz").
    /// TahsilatController'dan türer; uçlar /Odeme/Veri · Faturalar · Kur · Onizle · Kaydet · IslemeAl · Sil · OtomatikKapat · EkstrePdf / EkstreExcel · SimulasyonPdf / SimulasyonExcel · TahsilatPdf / TahsilatExcel (makbuz).
    /// Fark: cariler TEDARİKÇİ, açık faturalar ALIŞ faturaları, kayıtlar ÖD-… (TahsilatOrnek.Odemeler); işlenince cari hesabına BORÇ, her satır Kasa Nakit Akışı'na ÖDENDİ satır, Yapılan Ödemeler listesinde "Ödeme Ekranı" kaynağı.
    /// Ekran: Views/YapilanOdemeler/Index.cshtml › "Yeni Ödeme Ekle" sekmesi (wwwroot/js/tahsilat.js, data-mod="odeme").
    /// </summary>
    public class OdemeController : TahsilatController
    {
        protected override bool OdemeMi => true;
    }
}
