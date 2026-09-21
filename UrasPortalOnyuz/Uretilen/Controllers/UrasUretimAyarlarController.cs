// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{






























    [Authorize]
    [Route("[controller]")]
    public class UrasUretimAyarlarController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UrasUretimAyarlarController> _logger;
        private readonly EmailService _emailService;

        private bool TamYetkiliMi()  {return default;
}

        private JsonResult Veri(Func<object> f)
 {return default;
}

        private JsonResult Islem(Func<string> f)
 {return default;
}



        private record IzinTanim(string Key, string Grup, string Ad, string Aciklama);

        private static readonly List<IzinTanim> Katalog = new()
        {
            new("ANASAYFA","Ekranlar","Anasayfa","Üretim emri listesi ve onay ekranı"),
            new("URUNAGACI","Ekranlar","Ürün Ağacı","Ürün ağacı görüntüleme/düzenleme"),
            new("URETIMSIPARISI","Ekranlar","Üretim Siparişi","Üretim siparişi ekleme ekranı"),
            new("URUNDONUSUMU","Ekranlar","Ürün Dönüşümü","Ürün dönüşüm işlemleri"),
            new("URUNAYRISTIRMA","Ekranlar","Ürün Ayrıştırma","Ürün ayrıştırma işlemleri"),
            new("NUMUNE","Ekranlar","Numune","Mamülden demonte ile numune üretimi"),
            new("LOT_YAPILANDIRMA","Ekranlar","Seri/Parti Tanımları","Lot numara tanımları ve kalem bazlı sayaç sıfırlama"),
            new("STOK","Ekranlar","Stok","Mal girişi/çıkışı, depo stok raporu"),
            new("KALITEKONTROL","Ekranlar","Kalite Kontrol","KK formları (Ürün KK, Yaşlandırma, Su Bazlı, pH)"),
            new("GIRISKK","Ekranlar","Giriş Kalite Kontrol Formu (URS-F09-01)","Karantina depo hammadde giriş KK ve transfer"),
            new("SATINALMAKK","Ekranlar","Satınalma Kalite Kontrol","Kimyasal envanter (CIL) tanımları ve satınalma siparişi değerlendirmesi"),
            new("TUTANAK","Ekranlar","Karantina/Red/İmha Tutanağı","URS-F10-01 tutanağı oluşturma ve defteri görme"),
            new("TUTANAK_ONAY","Ekranlar","Tutanak Onay Yetkilisi","Bekleyen tutanakları onaylar/reddeder (varsayılan yalnız admin)"),
            new("TUTANAK_DUZENLE","Ekranlar","Tutanak Düzenleme","Tutanak defterindeki geçmiş kayıtları düzenler (varsayılan yalnız admin)"),
            new("MUSTERISIKAYET","Ekranlar","Müşteri Şikayet Formu","Talep ve Şikayet Formu (varsayılan yalnız admin)"),
            new("KALEMANAVERILERI","Ekranlar","Kalem Ana Verileri","Kalem kartı ekleme/güncelleme"),
            new("ETIKET","Ekranlar","Etiket Yazdırma","Stoktaki kalem+lot için etiket basma"),
            new("STOKSAYIM","Ekranlar","Stok Sayım","Sayım belgeleri, yeni sayım açma, satır/parti ekleme, kapatma"),
            new("STOKSAYIM_KAYIT","Ekranlar","Sayımı Stok Kaydına Çevir","Sayım farklarını stok kaydına dönüştürür (varsayılan yalnız admin)"),
            new("SAHA","Ekranlar","Saha Ekranı","Uras Kimya Saha (tablet) ekranı"),
            new("SILO","Ekranlar","Silo Dolum","Silo dolum takibi: etiket okutarak dolum/boşaltma"),
            new("SILO_TANIM","Ekranlar","Silo Tanımları","Silo ekleme/düzenleme/pasife alma"),
            new("RAPOR","Ekranlar","Raporlar","Raporlar menüsü ve Rapor Merkezi"),
            new("BAKIM","Ekranlar","Arıza & Bakım","Arıza kayıtları, periyodik bakım planı"),
            new("BAKIM_YONETIM","Ekranlar","Bakım Yönetimi","Arıza/bakım silme ve erteleme (varsayılan yalnız admin)"),
            new("BAKIM_ISLEM","Ekranlar","Bakım İşlemleri","Teknisyen yetkisi: atama, başlatma, işlem/malzeme girişi"),
            new("BAKIM_ONAY","Ekranlar","Bakım Onayı","Onaya gönderilen iş emirlerini onaylar/reddeder (varsayılan yalnız admin)"),
            new("ARIZA_BILDIR","Ekranlar","Arıza Bildirimi","Yeni arıza kaydı açabilir (varsayılan tüm rollerde açık)"),
            new("YETKILENDIRME","Ekranlar","Yetkilendirme","Bu ekran; kullanıcı yetkilerini yönetir"),
            new("RAPOR_URETIM","Raporlar","Üretim Raporu","Günlük/haftalık/aylık üretim raporu"),
            new("RAPOR_IZLENEBILIRLIK","Raporlar","Ürün İzlenebilirlik","Parti detayı, bileşen partileri, harita"),
            new("RAPOR_HAMMADDE","Raporlar","Hammadde Tüketim","Kalem/parti/ürün/gün bazında hammadde tüketimi"),
            new("RAPOR_PERFORMANS","Raporlar","İş Emri Performans","Süre, gerçekleşme, saatlik hız, termin sapması"),
            new("RAPOR_STOKYASI","Raporlar","Parti Yaşlandırma","Depodaki partilerin yaşı"),
            new("RAPOR_SILO","Raporlar","Silo Raporu","Silo dolum/tüketim/boşaltma özetleri"),
            new("RAPOR_LOTSIRA","Raporlar","Lot Sıra Kontrol","Kazan/lot numaralarında atlama/mükerrer denetimi"),
            new("RAPOR_SAYIM","Raporlar","Stok Sayım Raporu","Sayım belgeleri, sistem-sayım farkları"),
            new("SAHA_BASLATBITIR","Saha İşlemleri","Üretim Başlat / Bitir","Sahada işi başlatma ve bitirme butonları"),
            new("SAHA_URETIMTAMAMLA","Saha İşlemleri","Üretimi Tamamla","Tek adımda üretim tamamlama"),
            new("SAHA_KALITEONAY","Saha İşlemleri","Kalite Onayı","Sahada kalite onayı verme"),
            new("KAPSAM_HAMMADDE","Kalem Kapsamı","Hammadde (H)","Hammadde kalemlerini görür"),
            new("KAPSAM_YARIMAMUL","Kalem Kapsamı","Yarı Mamul (YM)","YM ile başlayan kalemleri görür"),
            new("KAPSAM_MAMUL","Kalem Kapsamı","Mamul (U)","U ile başlayan kalemleri görür"),
            new("KALEM_GERCEKAD","Kalem Kapsamı","Gerçek Kalem Adı","Kapalıysa kalem adları gizli tanımla görünür"),
            new("RECETE","Kalem Kapsamı","Reçete Görüntüleme","Üretim siparişi reçete satırlarını görür (varsayılan yalnız admin)"),
            new("RECETEMALIYET","Kalem Kapsamı","Reçete Birim Maliyet","Reçete satırlarında birim maliyet görür (varsayılan yalnız admin)"),
            new("MOBIL_URETIMSIPARISI","Mobil Ekranlar","Üretim Siparişleri","Mobil uygulamada üretim siparişleri modülü"),
            new("MOBIL_URUNAGAC","Mobil Ekranlar","Üretim Siparişi Ürün Ağacı","Mobil detayda bileşen satırları (varsayılan yalnız admin)"),
            new("MOBIL_STOKRAPORU","Mobil Ekranlar","Depo Stok Raporu","Mobil uygulamada depo stok raporu"),
            new("MOBIL_TESLIMAT","Mobil Ekranlar","Teslimat","Mobil uygulamada teslimat modülü"),
            new("MOBIL_SATINALMARPR","Mobil Ekranlar","Satın Alma Raporu","Mobil detaylı satın alma raporu"),
            new("MOBIL_RECETEMALIYET","Mobil Ekranlar","Reçete Birim Maliyet","Mobil reçete birim maliyet raporu (varsayılan kapalı)"),
            new("MOBIL_STOKSAYIM","Mobil Ekranlar","Stok Sayım","Mobil stok sayım ekranı (varsayılan yalnız admin)"),
        };

        private static readonly (string Kod, string Ad, string Kisa)[] RolSablonlari = new[]
        {
            ("1", "Yönetici (Tam Yetki)", "Yönetici"),
            ("2", "Süpervizör", "Süpervizör"),
            ("3", "Saha - Başlat/Bitir", "Saha B/B"),
            ("4", "Saha - Tek Adım Tamamla", "Saha Tek Adım"),
            ("5", "Saha - Kalite Onaylı", "Saha Kalite"),
            ("", "Standart Operatör", "Operatör"),
        };


        private static Dictionary<string, bool> VarsayilanlarRol(string webCode)
 {return default;
}



        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("MailAyarlari")]
        public IActionResult MailAyarlari()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("MailAyarlari");
}



        [HttpGet("GetUsers")]
        public JsonResult GetUsers()
 {return new JsonResult(WebApplication3.Data.UretimOrnek.Kullanicilar(), WebApplication3.Data.UretimOrnek.PascalCase);
}

        [HttpGet("GetTanimlar")]
        public JsonResult GetTanimlar()
 {return new JsonResult(WebApplication3.Data.UretimOrnek.Tanimlar(), WebApplication3.Data.UretimOrnek.PascalCase);
}

        [HttpGet("GetUserYetki")]
        public JsonResult GetUserYetki(string code, string webCode)
 {return new JsonResult(WebApplication3.Data.UretimOrnek.KullaniciYetki(code, webCode), WebApplication3.Data.UretimOrnek.PascalCase);
}

        public class YetkiKayitModel
        {
            public string Code { get; set; }
            public string Username { get; set; }
            public string WebCode { get; set; }
            public Dictionary<string, bool> Perms { get; set; }
        }

        [HttpPost("SaveUserYetki")]
        public JsonResult SaveUserYetki([FromBody] YetkiKayitModel model)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpPost("ResetUserYetki")]
        public JsonResult ResetUserYetki([FromBody] YetkiKayitModel model)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpPost("UpdateWebCode")]
        public JsonResult UpdateWebCode([FromBody] YetkiKayitModel model)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}



        public class MailAliciModel
        {
            public int Id { get; set; }
            public string Ad { get; set; }
            public string Email { get; set; }
            public string GirisKK { get; set; }
            public string SatinalmaKK { get; set; }
            public string Tutanak { get; set; }
            public string Bakim { get; set; }
            public string UretimOnay { get; set; }
        }

        [HttpGet("GetMailAlicilar")]
        public JsonResult GetMailAlicilar()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpPost("SaveMailAlici")]
        public JsonResult SaveMailAlici([FromBody] MailAliciModel model)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpPost("DeleteMailAlici")]
        public JsonResult DeleteMailAlici([FromBody] MailAliciModel model)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}



        [HttpGet("GetMailAyar")]
        public JsonResult GetMailAyar()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        public class TestMailModel { public string Alici { get; set; } }

        [HttpPost("SendTestMail")]
        public JsonResult SendTestMail([FromBody] TestMailModel model)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}
    }
}
