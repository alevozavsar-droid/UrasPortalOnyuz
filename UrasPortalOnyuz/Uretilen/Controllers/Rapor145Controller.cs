// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{





















    public static class IrsaliyeHataCozumleyici
    {
        public class Sonuc
        {
            public string Baslik { get; set; } = "";
            public string Ipucu { get; set; } = "";
            public string Grup { get; set; } = "";        // ozet sayimi icin
            public string KalemKodu { get; set; } = "";   // eslestirme ekrani icin
            public List<string> Etiketler { get; set; } = new List<string>();
        }

        private static string Yakala(string metin, string desen, int grup = 1)
        {
            var m = Regex.Match(metin, desen, RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[grup].Value.Trim() : null;
        }

        public static Sonuc Coz(string ham)
        {
            var s = new Sonuc();
            ham = (ham ?? "").Trim();

            if (ham.Length == 0)
            {
                s.Baslik = "Sebep kaydedilmemiş.";
                s.Grup = "Bilinmiyor";
                return s;
            }


            var kimlik = Yakala(ham, @"^\[(\d+)-(\d+)\]");
            if (kimlik != null)
            {
                var m = Regex.Match(ham, @"^\[(\d+)-(\d+)\]");
                s.Etiketler.Add("Belge " + m.Groups[1].Value + "/" + m.Groups[2].Value);
            }


            var stok = Yakala(ham, @"Kaynak Stok Kodu '([^']+)'")
                    ?? Yakala(ham, @"Hedef sistemde '([^']+)' kodlu stok bulunamadı");

            if (stok != null)
            {
                s.Baslik = "Kalem hedef şirkette bulunamadı: " + stok;
                s.Ipucu = "Kalem SELVİ'de hiç açılmamış ya da pasif durumda. Kalemi açıp aktifleştirin, sonra belgeyi kuyruğa geri koyun.";
                s.Grup = "Kalem bulunamadı";
                s.KalemKodu = stok;
                s.Etiketler.Add("Kalem " + stok);
                return s;
            }


            var depo = Yakala(ham, @"'([^']+)' kodlu depo bulunamadı");
            if (depo != null)
            {
                s.Baslik = "Depo hedef şirkette bulunamadı: " + depo;
                s.Ipucu = "SELVİ'de aynı kodlu depoyu tanımlayın.";
                s.Grup = "Depo bulunamadı";
                s.Etiketler.Add("Depo " + depo);
                return s;
            }


            if (ham.Contains("açık bir satır bulunamadı"))
            {
                var kalem = Yakala(ham, @"'([^']+)' kodlu");
                var miktar = Yakala(ham, @"kodlu,\s*([\d.,]+)\s*miktarlı");
                var entry = Yakala(ham, @"Entry:\s*(\d+)");

                s.Baslik = "Faturanın bağlanacağı irsaliyede açık satır kalmamış"
                         + (kalem != null ? " (" + kalem + ")" : "") + ".";
                s.Ipucu = "İrsaliye hedefte başka bir faturaya bağlanmış ya da miktarı değişmiş olabilir. "
                        + "SELVİ'deki irsaliyeyi kontrol edin.";
                s.Grup = "İrsaliyede açık satır yok";

                if (kalem != null) s.Etiketler.Add("Kalem " + kalem);
                if (miktar != null) s.Etiketler.Add("Miktar " + miktar);
                if (entry != null) s.Etiketler.Add("Hedef irsaliye " + entry);
                return s;
            }


            if (ham.Contains("Alt Belge") && ham.Contains("bulunamadı"))
            {
                var altTip = Yakala(ham, @"Kaynak ObjType:\s*(\d+)");
                var altEntry = Yakala(ham, @"DocEntry:\s*(\d+)");

                s.Baslik = "Faturanın bağlı olduğu irsaliye henüz hedefte yok.";
                s.Ipucu = "Önce irsaliye aktarılmalı. Yandaki düğmeyle alt belgeyi kuyruğa alabilirsiniz.";
                s.Grup = "İrsaliye önce aktarılmalı";

                if (altTip != null && altEntry != null)
                    s.Etiketler.Add("Önce: " + altTip + "/" + altEntry);
                return s;
            }


            if (ham.Contains("geçerli satır (stoklu) bulunamadı"))
            {
                s.Baslik = "Belgede aktarılabilir stok satırı yok.";
                s.Ipucu = "Servis yalnızca stok kalemi içeren satırları aktarır; bu belge tamamen hizmet/masraf satırlarından oluşuyor olabilir.";
                s.Grup = "Stok satırı yok";
                return s;
            }


            if (ham.Contains("UDF"))
            {
                var alan = Yakala(ham, @"\[([^\]]+)\] UDF alanına")
                        ?? Yakala(ham, @"Son uğraşılan alan:\s*([^,]+)");
                var deger = Yakala(ham, @"UDF alanına '([^']*)' değeri")
                         ?? Yakala(ham, @"Veri:\s*([^|]+)");

                s.Baslik = "Kullanıcı tanımlı alan (UDF) hedefte kabul edilmedi"
                         + (alan != null ? ": " + alan : "") + ".";
                s.Ipucu = "Alan SELVİ'de tanımlı olmayabilir, uzunluğu yetmiyor olabilir ya da geçerli değer listesinde bulunmuyordur.";
                s.Grup = "UDF alanı reddedildi";

                if (alan != null) s.Etiketler.Add("Alan " + alan);
                if (!string.IsNullOrWhiteSpace(deger)) s.Etiketler.Add("Değer " + deger.Trim());
                return s;
            }


            if (ham.Contains("E-Belge ID karakter"))
            {
                s.Baslik = "E-Belge numarası (ETTN / fatura ID) SAP'nin beklediği uzunlukta değil.";
                s.Ipucu = "Kaynak belgedeki e-belge alanı düzeltilmeden bu fatura hedefe geçemez.";
                s.Grup = "E-Belge numarası hatalı";
                return s;
            }


            if (ham.Contains("UoM specified is not assigned"))
            {
                var satir = Yakala(ham, @"in row\s*(\d+)");
                s.Baslik = "Satırdaki ölçü birimi, hedefte o kaleme tanımlı değil.";
                s.Ipucu = "Kalem kartındaki ölçü birimi grubuna bu birimi ekleyin.";
                s.Grup = "Ölçü birimi tanımsız";
                if (satir != null) s.Etiketler.Add("Satır " + satir);
                return s;
            }


            if (ham.Contains("BaseLine"))
            {
                var satir = Yakala(ham, @"line:\s*(\d+)");
                var analiz = Yakala(ham, @"Detaylı Analiz:\s*(.+)$");

                s.Baslik = "Fatura satırları, bağlı olduğu irsaliyenin satırlarıyla örtüşmüyor.";
                s.Ipucu = analiz ?? "Genellikle irsaliye hedefte elle değiştirilmişse olur.";
                s.Grup = "Satır eşleşmiyor (BaseLine)";
                if (satir != null) s.Etiketler.Add("Satır " + satir);
                return s;
            }


            if (ham.Contains("Otomatik Mal Girişi Başarısız"))
            {
                s.Baslik = "Hedefte otomatik mal girişi oluşturulamadı.";
                s.Ipucu = "SAP'nin yanıtı ham kayıtta. Genellikle stok/depo veya miktar kısıtı olur.";
                s.Grup = "Mal girişi oluşturulamadı";
                return s;
            }


            if (ham.Contains("zaten iptal edilmiş"))
            {
                s.Baslik = "Belge hedef sistemde zaten iptal edilmiş.";
                s.Grup = "Zaten iptal";
                return s;
            }
            if (ham.Contains("DI API İptal Hatası"))
            {
                s.Baslik = "SAP, belgenin iptalini reddetti.";
                s.Ipucu = "Belge başka bir belgeye bağlanmış olabilir.";
                s.Grup = "İptal reddedildi";
                return s;
            }


            if (ham.Contains("Hedef SAP şirketine bağlanılamadı"))
            {
                s.Baslik = "Servis hedef SAP şirketine bağlanamadı.";
                s.Ipucu = "Geçici bir bağlantı sorunu olabilir; belgeyi kuyruğa geri koyup tekrar deneyin.";
                s.Grup = "SAP bağlantısı yok";
                return s;
            }
            if (ham.Contains("kaynak sistemde bulunamadı"))
            {
                s.Baslik = "Belge kaynak sistemde bulunamadı.";
                s.Ipucu = "Belge silinmiş ya da henüz veritabanına işlenmemiş olabilir.";
                s.Grup = "Kaynakta bulunamadı";
                return s;
            }


            var tip = Yakala(ham, @"Desteklenmeyen belge tipi:\s*(\d+)");
            if (tip != null)
            {
                s.Baslik = "Servis bu belge tipini aktarmıyor.";
                s.Grup = "Desteklenmeyen belge tipi";
                s.Etiketler.Add("Tip " + tip);
                return s;
            }


            if (ham.Contains("Hizmet Faturası"))
            {
                s.Baslik = "Hizmet faturası olduğu için aktarılmadı.";
                s.Ipucu = "Servis yalnızca kalem (stok) içeren belgeleri aktarır - beklenen davranış.";
                s.Grup = "Hizmet faturası (atlandı)";
                return s;
            }
            if (ham.Contains("tekrar aktarılmayacak") || ham.Contains("durumu 'Kapalı'"))
            {
                var entry = Yakala(ham, @"Entry:\s*(\d+)");
                s.Baslik = "Hedefteki irsaliye kapalı; elle bir faturaya bağlanmış.";
                s.Ipucu = "Servis üzerine yazmamak için atladı - beklenen davranış.";
                s.Grup = "İrsaliye kapalı (atlandı)";
                if (entry != null) s.Etiketler.Add("Hedef " + entry);
                return s;
            }
            if (ham.Contains("Belge Carisi"))
            {
                s.Baslik = "Belgenin carisi aktarım kapsamında değil.";
                s.Grup = "Kapsam dışı cari (atlandı)";
                return s;
            }
            if (ham.Contains("başlangıç tarihi") || ham.Contains("tarihi") && ham.Contains("eski"))
            {
                s.Baslik = "Belge tarihi aktarım başlangıcından eski olduğu için atlandı.";
                s.Grup = "Milat öncesi (atlandı)";
                return s;
            }
            if (ham == "Başarılı")
            {
                s.Baslik = "Başarıyla aktarıldı.";
                s.Grup = "Başarılı";
                return s;
            }


            var kod = Yakala(ham, @"DI API Hatası\s*\[(-?\d+)\]");
            if (kod != null)
            {
                var sapMesaj = Yakala(ham, @"DI API Hatası\s*\[-?\d+\]:\s*([^\r\n(]+)");
                s.Baslik = "SAP belgeyi reddetti" + (sapMesaj != null ? ": " + sapMesaj.Trim() : ".");
                s.Grup = "SAP reddetti (" + kod + ")";
                s.Etiketler.Add("SAP " + kod);
                return s;
            }


            s.Baslik = ham;
            s.Grup = "Diğer";
            return s;
        }
    }
}
