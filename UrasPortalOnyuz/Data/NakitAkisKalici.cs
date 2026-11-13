// ============================================================
// KASA NAKİT AKIŞI — KALICI KAYIT (13 Kas 2026)
//   Sorun: Nakit Akış Planlama verisi bellekteydi; uygulama her yeniden başladığında kullanıcının girdikleri (durum, planlanan tarih, öncelik, muafiyet, kasa / banka seçimi,
//   yeni ödeme kayıtları, yüklenen dekont ve belge asılları, grup içi tür seçimi, nakit onayı) kayboluyordu.
//   Çözüm (Personel Maliyet günlüğüyle aynı kalıp): ekrandan yapılan her başarılı değişiklik App_Data/kasa-nakit-kayit.jsonl dosyasına satır satır yazılır; açılışta (OrnekIsinma,
//   örnek veri tohumlandıktan ve öbür modüller satırlarını açtıktan sonra) aynı sırayla yeniden uygulanır. Yüklenen dosyalar App_Data/kasa-nakit-ekler altında saklanır.
//   Kapsam: alan (satır içi / çekmece alanı: AlanKaydet) · muafiyet · kaydet (çekmece Kaydet) · ekle (Yeni Ödeme) · sil · grupTur (Mahsup / Temlik / Yansıtma) · nakitOnay ·
//           ek (dekont, belge aslı, imzalı makbuz … dosyalı ya da dosyasız) · oncelikSira (Öncelik Planı sürükle-sırala).
//   Kapsam dışı (öbür modüllerin bellek içi durumuna bağlı; yeniden başlatmada sıfırlanır): talimat, makbuz, çek / senet planlama, dolaşım, ibraz, GMY çek çıkış onayı,
//           kısmi ödeme / taksit, departman talebini akışa ekleme, modül kaydı bağlama.
//   Güvenlik: her kayıt hedef satırın izini (kurum | tutar) taşır; yeniden uygulamada satır bulunamazsa ya da iz tutmuyorsa (tohum değişmiş, satır başka modülden gelmişti) kayıt atlanır.
//   "ekle" ile açılan satırın numarası yeniden uygulamada değişebilir → eski no → yeni no eşlemesi tutulur, sonraki kayıtlar yeni numaraya uygulanır.
//   Dosya yeri: PersonelMaliyetKalici.Dizin (içerik kökü; PM_KALICI_DIZIN ortam değişkeniyle değiştirilebilir) altında App_Data.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinansRaporlama.Controllers;

namespace WebApplication3.Data
{
    public static class NakitAkisKalici
    {
        static string Kok => string.IsNullOrEmpty(PersonelMaliyetKalici.Dizin) ? null : Path.Combine(PersonelMaliyetKalici.Dizin, "App_Data");
        public static string Dosya => Kok == null ? null : Path.Combine(Kok, "kasa-nakit-kayit.jsonl");
        public static string EkDizini => Kok == null ? null : Path.Combine(Kok, "kasa-nakit-ekler");
        static readonly object Kilit = new object();
        static bool _yukleniyor;
        public static bool Yukleniyor => _yukleniyor;
        public static int YuklenenKayit { get; private set; }
        public static int AtlananKayit { get; private set; }
        static readonly JsonSerializerOptions Secenek = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        public class Kayit { public DateTime T { get; set; } public string Tip { get; set; } public string Kisi { get; set; } public string Db { get; set; } public int Doc { get; set; } public string Iz { get; set; } public JsonElement Veri { get; set; } }
        public class AlanVeri { public string Alan { get; set; } public string Deger { get; set; } }
        public class MetinVeri { public string Deger { get; set; } }
        public class OnayVeri { public string Karar { get; set; } public string Onaylayan { get; set; } public string Not { get; set; } }
        public class EkVeri { public string Ad { get; set; } public string Tur { get; set; } public string Aciklama { get; set; } public string IcerikTipi { get; set; } public string Dosya { get; set; } }
        public class SiraSatiri { public string DbName { get; set; } public int DocEntry { get; set; } public int? Oncelik { get; set; } public string Iz { get; set; } }
        public class SiraVeri { public List<SiraSatiri> Satirlar { get; set; } }

        /// <summary>Satırın izi: yeniden uygulamada aynı satır olduğunu doğrulamak için (kurum | tutar).</summary>
        public static string Iz(NakitAkisViewModel m) => m == null ? null : $"{m.U_Kurum}|{(m.U_Tutar ?? 0).ToString("0.##", CultureInfo.InvariantCulture)}";

        /// <summary>Başarılı bir değişikliği günlüğe ekler (yeniden uygulama sırasında yazılmaz; yazma hatası uygulamayı durdurmaz).</summary>
        public static void Yaz(string tip, string db, int doc, string iz, object veri, string kisi)
        {
            var dosya = Dosya; if (_yukleniyor || dosya == null) return;
            try
            {
                string satir = JsonSerializer.Serialize(new { t = DateTime.Now, tip, kisi, db, doc, iz, veri }, Secenek);
                lock (Kilit) { Directory.CreateDirectory(Path.GetDirectoryName(dosya)); File.AppendAllText(dosya, satir + Environment.NewLine); }
            }
            catch (Exception ex) { Console.WriteLine($"[Kalıcı] Kasa Nakit yazılamadı ({tip}): {ex.Message}"); }
        }

        /// <summary>Yüklenen dosyayı App_Data/kasa-nakit-ekler altına kaydeder; günlüğe yazılacak dosya adını döndürür (kalıcılık kapalıysa ya da dosya yoksa null).</summary>
        public static string EkSakla(byte[] veri, string ad)
        {
            var dizin = EkDizini; if (_yukleniyor || dizin == null || veri == null || veri.Length == 0) return null;
            try
            {
                Directory.CreateDirectory(dizin);
                string uzanti = Path.GetExtension(ad ?? ""); if (uzanti.Length > 8 || uzanti.Any(c => !(char.IsLetterOrDigit(c) || c == '.'))) uzanti = "";
                string dosya = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8)}{uzanti.ToLowerInvariant()}";
                File.WriteAllBytes(Path.Combine(dizin, dosya), veri); return dosya;
            }
            catch (Exception ex) { Console.WriteLine($"[Kalıcı] Kasa Nakit eki saklanamadı ({ad}): {ex.Message}"); return null; }
        }

        /// <summary>Günlüğü baştan sona yeniden uygular (açılışta, örnek veri ve öbür modüllerin satırları kurulduktan sonra); uygulanamayan satır atlanır ve konsola yazılır.</summary>
        public static void Yukle()
        {
            var dosya = Dosya; if (dosya == null || !File.Exists(dosya)) { Console.WriteLine("[Kalıcı] Nakit Akış Planlama: günlük yok" + (dosya == null ? " (dizin atanmadı)" : $" ({dosya})")); return; }
            _yukleniyor = true; YuklenenKayit = 0; AtlananKayit = 0; var esle = new Dictionary<int, int>();
            try
            {
                foreach (var satir in File.ReadLines(dosya))
                {
                    if (string.IsNullOrWhiteSpace(satir)) continue;
                    Kayit k = null;
                    try { k = JsonSerializer.Deserialize<Kayit>(satir, Secenek); if (Uygula(k, esle)) YuklenenKayit++; else AtlananKayit++; }
                    catch (Exception ex) { AtlananKayit++; Console.WriteLine($"[Kalıcı] Kasa Nakit atlandı ({k?.Tip ?? "?"} #{k?.Doc} {k?.T:dd.MM HH:mm}): {ex.Message}"); }
                }
                Console.WriteLine($"[Kalıcı] Nakit Akış Planlama: {YuklenenKayit} kayıt yeniden uygulandı" + (AtlananKayit > 0 ? $", {AtlananKayit} atlandı" : "") + $" · {dosya}");
            }
            finally { _yukleniyor = false; }
        }

        static T Coz<T>(JsonElement e) => JsonSerializer.Deserialize<T>(e.GetRawText(), Secenek);
        static NakitAkisViewModel Hedef(string db, int doc, string iz, Dictionary<int, int> esle, out int yeniDoc)
        {
            yeniDoc = esle.TryGetValue(doc, out var y) ? y : doc;
            var m = NakitAkisOrnek.Bul(db, yeniDoc); if (m == null) return null;
            return string.IsNullOrEmpty(iz) || Iz(m) == iz ? m : null;   // iz tutmuyorsa başka bir satır: dokunma
        }

        static bool Uygula(Kayit k, Dictionary<int, int> esle)
        {
            if (k == null || string.IsNullOrEmpty(k.Tip)) return false;
            string db = k.Db ?? "", kisi = k.Kisi ?? "";
            if (k.Tip == "ekle")
            {
                var model = Coz<NakitAkisViewModel>(k.Veri); if (model == null) return false; model.DocEntry = 0; model.DekontListesi = new List<DekontKaydi>(); model.Belgeler = new List<NakitAkisBelge>();
                var yeni = NakitAkisOrnek.Ekle(db, model, kisi); if (yeni == null) return false; esle[k.Doc] = yeni.DocEntry; return true;
            }
            if (k.Tip == "oncelikSira")
            {
                var v = Coz<SiraVeri>(k.Veri); int n = 0;
                foreach (var s in v?.Satirlar ?? new List<SiraSatiri>()) { var hedef = Hedef(s.DbName ?? "", s.DocEntry, s.Iz, esle, out _); if (hedef == null) continue; hedef.U_Oncelik = s.Oncelik; n++; }
                return n > 0;
            }
            var m = Hedef(db, k.Doc, k.Iz, esle, out int doc); if (m == null) { Console.WriteLine($"[Kalıcı] Kasa Nakit atlandı ({k.Tip} #{k.Doc}): satır bulunamadı ya da değişmiş ({k.Iz})"); return false; }
            switch (k.Tip)
            {
                case "alan": { var v = Coz<AlanVeri>(k.Veri); var r = NakitAkisOrnek.AlanKaydet(db, doc, v.Alan, v.Deger, kisi); if (r.ok && v.Alan == "U_Oncelik") NakitAkisOrnek.OncelikKaydir(db, doc); return r.ok; }
                case "muafiyet": m.MuafiyetSebebi = Coz<MetinVeri>(k.Veri)?.Deger ?? ""; return true;
                case "kaydet": { var model = Coz<NakitAkisViewModel>(k.Veri); if (model == null) return false; model.DocEntry = doc; return NakitAkisOrnek.Guncelle(db, model, kisi); }
                case "sil": return NakitAkisOrnek.Sil(db, doc, kisi);
                case "grupTur": return NakitAkisOrnek.GrupIciTurDegistir(db, doc, Coz<MetinVeri>(k.Veri)?.Deger, kisi).ok;
                case "nakitOnay": { var v = Coz<OnayVeri>(k.Veri); return (v.Karar == "Reddet" ? NakitAkisOrnek.NakitReddet(db, doc, v.Onaylayan, v.Not, kisi) : NakitAkisOrnek.NakitOnayla(db, doc, v.Onaylayan, v.Not, kisi)).ok; }
                case "ek":
                    {
                        var v = Coz<EkVeri>(k.Veri); byte[] veri = null;
                        if (!string.IsNullOrEmpty(v.Dosya) && EkDizini != null) { var yol = Path.Combine(EkDizini, Path.GetFileName(v.Dosya)); if (File.Exists(yol)) veri = File.ReadAllBytes(yol); }
                        var r = NakitAkisOrnek.EkEkle(db, doc, v.Ad, v.Tur, v.Aciklama, v.IcerikTipi, veri, kisi);
                        if (r.ok && r.satir != null) lock (NakitAkisOrnek.Kilit) { var son = r.satir.DekontListesi.LastOrDefault(); if (son != null) son.Tarih = k.T; }   // yükleme zamanı ilk yüklemedeki gibi kalır
                        return r.ok;
                    }
                default: return false;
            }
        }
    }
}
