// ============================================================
// PERSONEL MALİYET RAPORU — KALICI KAYIT (6 Kas 2026)
//   Sorun: tüm veri bellekteydi; uygulama yeniden başlayınca kullanıcının girdikleri (ör. Kamil Mahmudov ve Elmin Jabbarov'un Kira X tutarları) ve
//   "Yeni Bordro" ile kurulan dönem kayboluyordu ("tüm sekmelere yansımıyor" şikâyetinin nedeni).
//   Çözüm: Personel Maliyet uç noktalarındaki her başarılı değişiklik App_Data/personel-maliyet-kayit.jsonl dosyasına satır satır yazılır (olay günlüğü);
//   açılışta (OrnekIsinma) örnek veri tohumlandıktan sonra aynı sırayla yeniden uygulanır — dönem oluşturma dahil, böylece Ekim 2026 kendiliğinden kurulur.
//   Satır kaydında (Kaydet) ekranın gönderdiği "degisen" alan listesi varsa yalnız o alanlar mevcut kaydın üzerine yazılır; tohum verisi sonradan kodla
//   değişmişse (kimlik, resmi bordro tutarları) diğer alanlar korunur.
//   Kapsam: Kaydet · Ekle · Sil · DonemOlustur · TesvikKaydet · UretimKaydet / UretimSil · PrimKaydet / PrimSil · KesintiEkle (ek dosyası hariç) / KesintiGuncelle / KesintiSil ·
//           MuhasebeSimule / MuhasebeIslem / MuhasebeEsleme · Kimlik (Personel Dosyası'ndan telefon / e-posta / işe giriş; IkPersonelMaster.KimlikYaz, 9 Kas 2026). Kesinti planı ek dosyaları (PDF) kalıcı değildir; Zam / İşten Çıkış / Ödeme / Özel Rapor seçimleri zaten tarayıcıda saklanır.
//   Dosya yeri: içerik kökü (Startup: env.ContentRootPath; PM_KALICI_DIZIN ortam değişkeniyle değiştirilebilir) altında App_Data; git'e girmez (.gitignore).
// ============================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApplication3.Data
{
    public static class PersonelMaliyetKalici
    {
        /// <summary>İçerik kökü (Startup.Configure atar); null ise kalıcılık kapalı.</summary>
        public static string Dizin { get; set; }
        public static string Dosya => string.IsNullOrEmpty(Dizin) ? null : Path.Combine(Dizin, "App_Data", "personel-maliyet-kayit.jsonl");
        static readonly object Kilit = new object();
        static bool _yukleniyor;
        public static int YuklenenKayit { get; private set; }
        public static int AtlananKayit { get; private set; }
        static readonly JsonSerializerOptions Secenek = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        public class Kayit { public DateTime T { get; set; } public string Tip { get; set; } public string Kisi { get; set; } public string Yetki { get; set; } public JsonElement Veri { get; set; } }
        // günlükten okunan istek gövdeleri (denetleyicideki istek sınıflarıyla aynı alan adları)
        public class DonemKaydi { public string Donem { get; set; } public Dictionary<string, decimal> Kurlar { get; set; } }
        public class KaydetKaydi { public List<PmDuzenleme> Satirlar { get; set; } public string Donem { get; set; } }
        public class SilKaydi { public int Sira { get; set; } public string Donem { get; set; } }
        public class TesvikKaydi { public string Sirket { get; set; } public string Kod { get; set; } public bool Secili { get; set; } }
        public class IdKaydi { public int Id { get; set; } }
        public class KesintiEkleKaydi { public int PersonelSira { get; set; } public string Tur { get; set; } public string Kaynak { get; set; } public string IcraDairesi { get; set; } public string Alacakli { get; set; } public string DosyaNo { get; set; } public decimal Toplam { get; set; } public decimal Taksit { get; set; } public string Baslangic { get; set; } public string Aciklama { get; set; } }
        public class SimuleKaydi { public string Donem { get; set; } public string Kapsam { get; set; } }
        public class MuhasebeIslemKaydi { public int Id { get; set; } public string Islem { get; set; } public string Not { get; set; } }
        public class EslemeKaydi { public int? Id { get; set; } public string Sirket { get; set; } public string KarsiSirket { get; set; } public string IslemTuru { get; set; } public string KaynakKalem { get; set; } public string XR { get; set; } public string Yon { get; set; } public string HesapKodu { get; set; } public bool Varsayilan { get; set; } = true; public string GecerlilikBas { get; set; } public string GecerlilikBit { get; set; } public bool Aktif { get; set; } = true; }

        /// <summary>Başarılı bir değişikliği günlüğe ekler (yeniden yükleme sırasında yazılmaz; yazma hatası uygulamayı durdurmaz).</summary>
        public static void Yaz(string tip, object veri, string kisi, string yetki = null)
        {
            var dosya = Dosya; if (_yukleniyor || dosya == null) return;
            try
            {
                string satir = JsonSerializer.Serialize(new { t = DateTime.Now, tip, kisi, yetki, veri }, Secenek);
                lock (Kilit) { Directory.CreateDirectory(Path.GetDirectoryName(dosya)); File.AppendAllText(dosya, satir + Environment.NewLine); }
            }
            catch (Exception ex) { Console.WriteLine($"[Kalıcı] yazılamadı ({tip}): {ex.Message}"); }
        }

        /// <summary>Günlüğü baştan sona yeniden uygular (açılışta, örnek veri tohumlandıktan sonra); başarısız satır atlanır ve konsola yazılır.</summary>
        public static void Yukle()
        {
            var dosya = Dosya; if (dosya == null || !File.Exists(dosya)) { Console.WriteLine("[Kalıcı] Personel Maliyet: günlük yok" + (dosya == null ? " (dizin atanmadı)" : $" ({dosya})")); return; }
            _yukleniyor = true; YuklenenKayit = 0; AtlananKayit = 0;
            try
            {
                foreach (var satir in File.ReadLines(dosya))
                {
                    if (string.IsNullOrWhiteSpace(satir)) continue;
                    Kayit k = null;
                    try { k = JsonSerializer.Deserialize<Kayit>(satir, Secenek); Uygula(k); YuklenenKayit++; }
                    catch (Exception ex) { AtlananKayit++; Console.WriteLine($"[Kalıcı] atlandı ({k?.Tip ?? "?"} {k?.T:dd.MM HH:mm}): {ex.Message}"); }
                }
                PersonelMaliyetEkOrnek.UretimYansit();
                Console.WriteLine($"[Kalıcı] Personel Maliyet: {YuklenenKayit} kayıt yeniden uygulandı" + (AtlananKayit > 0 ? $", {AtlananKayit} atlandı" : "") + $" · {dosya}");
            }
            finally { _yukleniyor = false; }
        }

        static T Al<T>(JsonElement e) => JsonSerializer.Deserialize<T>(e.GetRawText(), Secenek);
        static void Hata(string h) { if (h != null) throw new InvalidOperationException(h); }

        static void Uygula(Kayit k)
        {
            var v = k.Veri; string kisi = string.IsNullOrWhiteSpace(k.Kisi) ? "Sistem" : k.Kisi;
            switch (k.Tip)
            {
                case "donem": { var d = Al<DonemKaydi>(v); PersonelMaliyetOrnek.DonemKur(d.Donem, d.Kurlar, out _); break; }
                case "kaydet": { var d = Al<KaydetKaydi>(v); var satirlar = (d.Satirlar ?? new List<PmDuzenleme>()).Select(s => KismiSatir(s, d.Donem)).ToList(); Hata(PersonelMaliyetOrnek.Kaydet(satirlar, kisi, d.Donem)); break; }
                case "ekle": { var d = Al<PmDuzenleme>(v); Hata(PersonelMaliyetOrnek.Ekle(d, kisi, out _, d.Donem)); break; }
                case "sil": { var d = Al<SilKaydi>(v); Hata(PersonelMaliyetOrnek.Sil(d.Sira, kisi, d.Donem)); break; }
                case "tesvik": { var d = Al<TesvikKaydi>(v); Hata(PersonelTesvikOrnek.Kaydet(d.Sirket, d.Kod, d.Secili)); break; }
                case "uretim": { var d = Al<PersonelMaliyetEkOrnek.UretimIstek>(v); Hata(PersonelMaliyetEkOrnek.UretimKaydet(d, out _)); break; }
                case "uretimSil": { var d = Al<IdKaydi>(v); Hata(PersonelMaliyetEkOrnek.UretimSil(d.Id)); break; }
                case "prim": { var d = Al<PersonelMaliyetEkOrnek.PrimIstek>(v); Hata(PersonelMaliyetEkOrnek.PrimKaydet(d, out _)); break; }
                case "primSil": { var d = Al<IdKaydi>(v); Hata(PersonelMaliyetEkOrnek.PrimSil(d.Id)); break; }
                case "kesintiEkle": { var d = Al<KesintiEkleKaydi>(v); Hata(PersonelKesintiOrnek.KesintiEkle(d.PersonelSira, d.Tur, d.Kaynak, d.IcraDairesi, d.Alacakli, d.DosyaNo, d.Toplam, d.Taksit, d.Baslangic, d.Aciklama, kisi, null, out _)); break; }
                case "kesinti": { var d = Al<PersonelKesintiOrnek.KesintiIstek>(v); Hata(PersonelKesintiOrnek.KesintiGuncelle(d, kisi, out _)); break; }
                case "kesintiSil": { var d = Al<IdKaydi>(v); Hata(PersonelKesintiOrnek.KesintiSil(d.Id)); break; }
                case "muhasebeSimule": { var d = Al<SimuleKaydi>(v); Hata(PersonelMuhasebeOrnek.Olustur(d.Donem, d.Kapsam, kisi, out _)); break; }
                case "muhasebeIslem": { var d = Al<MuhasebeIslemKaydi>(v); Hata(PersonelMuhasebeOrnek.Islem(d.Id, d.Islem, d.Not, kisi, k.Yetki ?? "", out _)); break; }
                case "muhasebeEsleme": { var d = Al<EslemeKaydi>(v); Hata(PersonelMuhasebeOrnek.EslemeKaydet(d.Id, d.Sirket, d.KarsiSirket, d.IslemTuru, d.KaynakKalem, d.XR, d.Yon, d.HesapKodu, d.Varsayilan, d.GecerlilikBas, d.GecerlilikBit, d.Aktif, kisi, out _)); break; }
                case "kimlik": { var d = Al<IkPersonelMaster.KimlikIstek>(v); Hata(IkPersonelMaster.KimlikYaz(d, kisi)); break; }   // Personel Dosyası'ndan telefon / e-posta / işe giriş (9 Kas 2026): kişinin tüm dönem kayıtlarına
                default: throw new InvalidOperationException("bilinmeyen kayıt türü: " + k.Tip);
            }
        }

        /// <summary>Günlükteki satır: "degisen" listesi varsa mevcut kaydın düzenleme görüntüsü üzerine yalnız o alanlar yazılır (tohum verisi sonradan değişmişse korunur); liste yoksa ya da kayıt bulunamazsa satır olduğu gibi uygulanır.</summary>
        static PmDuzenleme KismiSatir(PmDuzenleme s, string donem)
        {
            if (s.Degisen == null || s.Degisen.Count == 0 || s.Sira <= 0) return s;
            PmPersonel p; lock (PersonelMaliyetOrnek.Kilit) p = (PersonelMaliyetOrnek.ListeDonem(donem) ?? PersonelMaliyetOrnek.Liste).FirstOrDefault(x => x.Sira == s.Sira);
            if (p == null) return s;
            var d = PmDuzenleme.Of(p); d.Donem = s.Donem; d.Degisen = s.Degisen;
            foreach (var alan in s.Degisen)
            {
                var pi = typeof(PmDuzenleme).GetProperty(alan ?? "", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (pi != null && pi.CanWrite && pi.Name != "Degisen" && pi.Name != "Sira") pi.SetValue(d, pi.GetValue(s));
            }
            return d;
        }
    }
}
