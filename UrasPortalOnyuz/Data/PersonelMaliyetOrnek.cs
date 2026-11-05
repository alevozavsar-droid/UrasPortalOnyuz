// ============================================================
// PERSONEL MALİYET RAPORU — veri, doğrulama ve bordro hesabı (Yönetim › Özel Raporlar ve İK › Personel Maliyet ve Bordro)
// Sütun düzeni: "Personel Bordro ve Ödeme Tablosu" (PersonelTakibi, 1 Kas 2026); tutarlar ekranda ₺ ile gösterilir.
// Kişi ve rakamlar: "Personel Maliyet Raporu 2026-09" (DUZELTILMIS_v5_2), 87 kişi. 13 SGK'sız üretim (X) kişisi rapor dışıdır ve Üretim Personeli'nde kalır.
//   Özel Kişiler (Ozel = true, 9 kişi; Alper Köse 30.09.2026'da İşten Ayrıldı, Aktif sayıda yer almaz): Alper Köse, Devrim / Hale Bisren, Emine Tanış, Erman Kaval, Medine Rozykulova, Murat Özavşar, Mustafa Savaş, Nükhet Özavşar, Orazgül Jumayeva.
//   Kalan 78 kişi Personel Maliyet Tablosu'nda bordrolu (Alev Özavşar Eren ve Bedirhan Genç dahil). Brüt ve SGK bilgisi raporda olmayanlarda (Emine Tanış, Murat Özavşar, Orazgül Jumayeva, Mesut Akıncı) brüt ve SGK hesaplanmaz (SgkDetayYok).
//   Sütun eşlemesi: 2026 R-NET → RNet · 2026 X-NET → XNet · YEMEK-R / X → Yemek / YemekX · YOL-R / X → Yol / YolX · FAZLA MESAİ-R / X → FazlaMesai / X · PRİM-R / X → Prim / X · KİRA DESTEĞİ → KiraDestegi · AVANS, İCRA, BORÇ, BES, ÖZEL SİGORTA KESİNTİSİ, SENDİKA KATKISI → kesinti sütunları · DİĞER EK ÖDEMELER-R → DigerEkOdemelerR · KESİNTİLER TOPLAMI-X → KesintilerToplamiX · İSTİHDAM HAZİNE İNDİRİMİ → HazineKaynak (yalnız gösterim)
//   TOPLAM BRÜT KAZANÇ → BrutKaynak · TOPLAM İŞVEREN → IsverenKaynak (satır düzenlenince silinir, model yeniden hesaplar) · ÖZEL DURUMU → Emekli / Gündelikçi · NOT → Not ("Ödeme Yapılmıyor" ödeme kaydını engeller)
// Hesaplanan alanlar (BordroParametre ile; kaynak değer varsa ona bağlanır):
//   Brüt: kaynakta Toplam Brüt Kazanç; gün toplamına bölünerek günlük ücret ve kazanç kalemleri çıkar (kazançlar toplamı brüte eşit kalır).
//   Toplam Brüt Kazanç = kazançlar + ek çalışma + sosyal yardım + prim + yan haklar + yasal ödemeler. SGK matrahı = kazançlar + ek çalışma + prim + sosyal yardımlar (servis ve SgkDisi hariç), tavanlı.
//   SGK işçi oranla (normal %15; emekli SGDP %7,5). DV = (brüt kazanç − yan haklar) × ‰7,59 − asgari ücret istisnası.
//   GV: kaynak varsa artık (brüt − bordro net − SGK işçi − DV); yoksa kümülatif dilim farkı − asgari ücret istisnası − indirimler.
//   İşveren: kaynak varsa İş Veren tutarı (kalemler oran payıyla dağıtılır); yoksa normal %23,75 − 4 puan hazine indirimi, emekli SGDP %22,5 + KVSK %2,25.
//   Bordro net = brüt kazanç − SGK işçi − GV − DV · Kalan Net (Banka) = bordro net − kesintiler · Net-R Bordro = Kalan Net (Banka) + Ek Ödemeler Toplam-R (Yol, Yemek, Fazla Mesai, Prim toplamı + bunların dışındaki tüm R ek ödemeler; bankadan yatacak net)
//   Dövizli maaş: kartta Para Birimi (USD / EUR / GBP) + Döviz Tutarı + Kur girilirse X-Net = tutar × kur (₺ karşılığı); X-Net ve X tarafı kalemler bordroya girmediğinden kaynak değerleri düşürmez.
//   Grup toplamları R / X ayrı: Toplam Ek Çalışma-R / -X, Toplam Sosyal Yardımlar-R / -X, Toplam Prim-R / -X (X toplamları = Ek Ödemeler Toplam-X).
//   Ödeme Özeti-R: R-Net, Ek Ödemeler Toplam-R, Kesintiler Toplamı-R, Kalan Net Tutar (Banka) · Ödeme Özeti-X: X-Net, Ek Ödemeler Toplam-X, Kesintiler Toplamı-X (girilir), Kalan Net Tutar (Elden) = Net-X Bordro − Kesintiler Toplamı-X
//   Net-X Bordro = X-Net + X tarafı kalemler (Yemek-X, Yol-X, Fazla Mesai-X, Prim-X; brüt ve vergiye girmez; X kesintisi maliyeti düşürmez) · Ara Toplam = Net-X Bordro + Net-R Bordro · Toplam Brüt Masraflar = brüt kazanç − Kalan Net (Banka) (vergi, SGK işçi, kesintiler)
//   Toplam İşveren = SGK işveren toplam · Genel Maliyet = Ara Toplam + Toplam Brüt Masraflar + Toplam İşveren (= kaynak tablodaki GENEL MALİYET)
// Personel Cari Kartı: 335.xxx cari kodu, durum (Aktif / İşten Ayrıldı), ayrılış tarihi, TC kimlik, telefon, e-posta kart çekmecesinden düzenlenir.
// Ekran: Controllers/PersonelMaliyetController.cs + Views/PersonelMaliyet/Index.cshtml + wwwroot/js/personel-maliyet.js
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace WebApplication3.Data
{
    /// <summary>2026 bordro parametreleri — tek yer; mevzuat değişince burası güncellenir.</summary>
    public static class BordroParametre
    {
        public const decimal AsgariBrut = 33030m;                       // 2026 brüt asgari ücret (aylık)
        public const decimal SgkTavanKati = 9m;
        public static decimal SgkTavan => AsgariBrut * SgkTavanKati;    // 297.270
        public const decimal IsciIssizlik = 0.01m, IsciMyo = 0.09m, IsciGss = 0.05m, IsciSgdp = 0.075m;
        public const decimal IsvIssizlik = 0.02m, IsvMyo = 0.12m, IsvGss = 0.075m, IsvSgdp = 0.225m, Kvsk = 0.0225m, HazineIndirimi = 0.04m;
        public const decimal Damga = 0.00759m;
        /// <summary>Gelir vergisi tarifesi (ücret gelirleri): dilim üst sınırı → oran.</summary>
        public static readonly (decimal Ust, decimal Oran)[] GvDilimleri = { (190000m, 0.15m), (400000m, 0.20m), (1500000m, 0.27m), (5300000m, 0.35m), (decimal.MaxValue, 0.40m) };
        public static int Ay => DateTime.Today.Month;
        public static decimal AsgariGvMatrahi => AsgariBrut * (1 - IsciIssizlik - IsciMyo - IsciGss);   // 28.075,50
        public static decimal AsgariDamga => Yuvarla(AsgariBrut * Damga);                               // 250,70
        public static decimal Yuvarla(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        public static decimal Tarife(decimal kumulatif)
        {
            decimal v = 0, alt = 0;
            foreach (var (ust, oran) in GvDilimleri) { if (kumulatif <= alt) break; v += (Math.Min(kumulatif, ust) - alt) * oran; alt = ust; }
            return v;
        }
        public static decimal AylikGv(decimal matrah, int ay) => matrah <= 0 ? 0 : Tarife(matrah * ay) - Tarife(matrah * (ay - 1));
        public static decimal AsgariIstisna(int ay) => Yuvarla(AylikGv(AsgariGvMatrahi, ay));
        public static object Paket => new
        {
            asgariBrut = AsgariBrut, sgkTavan = SgkTavan, isciIssizlik = IsciIssizlik, isciMyo = IsciMyo, isciGss = IsciGss, isciSgdp = IsciSgdp,
            isvIssizlik = IsvIssizlik, isvMyo = IsvMyo, isvGss = IsvGss, isvSgdp = IsvSgdp, kvsk = Kvsk, hazineIndirimi = HazineIndirimi, damga = Damga,
            dilimler = GvDilimleri.Select(d => new[] { d.Ust == decimal.MaxValue ? 1e15m : d.Ust, d.Oran }).ToArray(), ay = Ay, asgariGvMatrahi = AsgariGvMatrahi, asgariDamga = AsgariDamga
        };
    }

    /// <summary>Bir personelin bir aylık bordro hesabı (tüm hesaplanan sütunlar).</summary>
    public class PmBordro
    {
        public decimal BrutUcret { get; set; } public decimal Gunluk { get; set; }
        public decimal ToplamNet { get; set; }
        public decimal NormalKazanc { get; set; } public decimal HaftaSonuKazanc { get; set; } public decimal ResmiTatilKazanc { get; set; } public decimal BayramKazanc { get; set; } public decimal UcretliIzinKazanc { get; set; }
        public decimal ToplamEkCalisma { get; set; } public decimal ToplamSosyal { get; set; } public decimal ToplamPrim { get; set; } public decimal ToplamYanHaklar { get; set; } public decimal ToplamYasal { get; set; }
        public decimal ToplamBrutKazanc { get; set; } public decimal SgkMatrahi { get; set; }
        public decimal GvMatrahi { get; set; } public decimal KumulatifMatrah { get; set; } public decimal HesaplananGv { get; set; } public decimal AsgariIstisna { get; set; } public decimal GvTutari { get; set; } public decimal AsgariKumulatifMatrah { get; set; }
        public decimal DamgaMatrahi { get; set; } public decimal AsgariDamga { get; set; } public decimal DvTutari { get; set; }
        public decimal IsciIssizlik { get; set; } public decimal IsciMyo { get; set; } public decimal IsciGss { get; set; } public decimal IsciSgdp { get; set; } public decimal SgkIsciToplam { get; set; }
        public decimal IsvIssizlik { get; set; } public decimal IsvMyo { get; set; } public decimal IsvGss { get; set; } public decimal IsvSgdp { get; set; } public decimal Kvsk { get; set; } public decimal HazineIndirimi { get; set; } public decimal SgkIsverenToplam { get; set; }
        public decimal RNet2 { get; set; } public decimal KesintilerToplami { get; set; } public decimal KalanNetBanka { get; set; } public decimal DigerEkR { get; set; }
        /// <summary>X tarafı grup toplamları (rx sütunlarının alt satırları) ve bunların toplamı Ek Ödemeler Toplam-X (= Fazla Mesai-X + Yemek-X + Yol-X + Prim-X).</summary>
        public decimal ToplamEkCalismaX { get; set; } public decimal ToplamSosyalX { get; set; } public decimal ToplamPrimX { get; set; } public decimal DigerEkOdemelerX { get; set; }
        /// <summary>X-Net'in ₺ karşılığı (dövizli maaşta Döviz Tutarı × Kur, yoksa girilen X-Net) · Ödeme Özeti-X: X-Net, Ek Ödemeler Toplam-X, Kesintiler Toplamı-X (girilir), Kalan Net Tutar (Elden) = Net-X Bordro − Kesintiler Toplamı-X.</summary>
        public decimal XNetTl { get; set; } public decimal XNet2 { get; set; } public decimal KalanNetElden { get; set; }
        /// <summary>Net-X Bordro = X-Net + X tarafı kalemler (bordro dışı ödenen toplam) · Net-R Bordro = Kalan Net (Banka) + Ek Ödemeler Toplam-R (Yol, Yemek, Fazla Mesai, Prim toplamı ile bunların dışındaki tüm R ek ödemelerin toplamı; banka = R-NET + bu toplam − kesintiler.</summary>
        public decimal NetXBordro { get; set; } public decimal NetRBordro { get; set; } public decimal AraToplam { get; set; } public decimal ToplamBrutMasraflar { get; set; } public decimal ToplamIsveren { get; set; } public decimal GenelMaliyet { get; set; }
        /// <summary>Ödeme kanalı (maliyet değişmez): BankaOdeme = banka transferi (blokeli kişide 0); NakitRTutar = blokeli kişinin R tutarı (icra ve kesintiler düşülmüş), elden ödenir; EldenOdeme = Kalan Net (Elden) + NakitRTutar.</summary>
        public decimal BankaOdeme { get; set; } public decimal NakitRTutar { get; set; } public decimal EldenOdeme { get; set; }

        public static PmBordro Hesapla(PmPersonel p)
        {
            var R = (Func<decimal, decimal>)BordroParametre.Yuvarla;
            bool emekli = p.OzelDurum == "Emekli"; int ay = BordroParametre.Ay; decimal tavan = BordroParametre.SgkTavan;
            decimal isciOran = emekli ? BordroParametre.IsciSgdp : BordroParametre.IsciIssizlik + BordroParametre.IsciMyo + BordroParametre.IsciGss;
            decimal istisna = BordroParametre.AsgariIstisna(ay), asgDamga = BordroParametre.AsgariDamga;
            // dövizli maaş (4 Kas 2026): toplam ₺ = döviz tutarı × kur; "KalanX": R sabit, kalanı X (bordro dışı); "TamamiR" / "TamamiX": tamamı R ya da X
            decimal rNet = p.RNet, xNet = p.XNet;
            if (p.Dovizli)
            {
                decimal toplam = R(p.DovizTutar * p.Kur);
                if (p.DovizOdeme == PmPersonel.DovizTamamiR) { rNet = toplam; xNet = 0m; }
                else if (p.DovizOdeme == PmPersonel.DovizTamamiX) { rNet = 0m; xNet = toplam; }
                else { xNet = Math.Max(0m, R(toplam - p.RNet)); }
            }
            var h = new PmBordro { XNetTl = xNet, XNet2 = xNet, ToplamNet = xNet + rNet };
            // grup toplamları (bordrodaki ekler)
            h.ToplamEkCalisma = p.FazlaMesai + p.BayramCalisma + p.GeceFarki + p.PazarCalisma + p.ResmiTatilCalisma;
            h.ToplamSosyal = p.Yemek + p.Yol + p.Servis + p.KiraDestegi + p.Egitim + p.CocukYardimi;
            h.ToplamPrim = p.MaasFarki + p.Prim + p.OzelOdeme + p.KasaTazminati;
            h.ToplamYanHaklar = p.OzelSaglik + p.Kiyafet + p.KoruyucuEkipman + p.DogumYardimi + p.EvlilikYardimi;
            h.ToplamYasal = p.KidemTazminati + p.IhbarTazminati + p.IseIade + p.YillikIzinOdemesi + p.RaporFarki + p.DogumIzni;
            decimal ekler = h.ToplamEkCalisma + h.ToplamSosyal + h.ToplamPrim + h.ToplamYanHaklar + h.ToplamYasal;
            bool sgkYok = p.SgkDetayYok;
            bool kaynak = !sgkYok && p.BrutKaynak.HasValue && p.BrutKaynak.Value > 0;
            // 1) esas brüt: kaynak tablodan ya da R-Net'ten brütleştirme (net(B) artan olduğundan ikiye bölme)
            decimal Net(decimal b)
            {
                decimal isci = Math.Min(b, tavan) * isciOran;
                decimal gv = Math.Max(0, BordroParametre.AylikGv(b - isci, ay) - istisna);
                decimal dv = Math.Max(0, b * BordroParametre.Damga - asgDamga);
                return b - isci - gv - dv;
            }
            decimal brut = 0;
            if (kaynak) brut = R(Math.Max(0, p.BrutKaynak.Value - ekler));
            else if (!sgkYok && rNet > 0) { decimal lo = 0, hi = rNet * 3 + 50000; for (int i = 0; i < 80; i++) { decimal mid = (lo + hi) / 2; if (Net(mid) < rNet) lo = mid; else hi = mid; } brut = R(hi); }
            decimal gunSayisi = p.NormalGun + p.HaftaSonuGun + p.ResmiTatilGun + p.BayramGun + p.UcretliIzinGun;
            decimal gunluk = kaynak && gunSayisi > 0 ? brut / gunSayisi : brut / 30m;   // kaynak tabloda gün toplamı 30 değilse brüt o günlere bölünür; toplam brüt yine kaynakla aynı kalır
            h.BrutUcret = brut; h.Gunluk = R(gunluk);
            // 2) puantaj kazançları
            h.NormalKazanc = R(p.NormalGun * gunluk); h.HaftaSonuKazanc = R(p.HaftaSonuGun * gunluk); h.ResmiTatilKazanc = R(p.ResmiTatilGun * gunluk); h.BayramKazanc = R(p.BayramGun * gunluk); h.UcretliIzinKazanc = R(p.UcretliIzinGun * gunluk);
            decimal kazanclar = h.NormalKazanc + h.HaftaSonuKazanc + h.ResmiTatilKazanc + h.BayramKazanc + h.UcretliIzinKazanc;
            if (kaynak) { h.NormalKazanc += brut - kazanclar; kazanclar = brut; }   // kaynakta kazançlar brüte eşit; kuruş ve gün yuvarlama farkı normal çalışmaya
            // 3) brüt kazanç ve SGK matrahı
            h.ToplamBrutKazanc = sgkYok ? 0 : kazanclar + ekler;
            h.SgkMatrahi = sgkYok ? 0 : Math.Min(kazanclar + h.ToplamEkCalisma + h.ToplamPrim + (h.ToplamSosyal - p.Servis - p.SgkDisi), tavan);   // yol yardımı ve servis SGK matrahı dışında (kaynak tabloyla aynı)
            // 4) SGK işçi
            h.IsciIssizlik = emekli ? 0 : R(h.SgkMatrahi * BordroParametre.IsciIssizlik); h.IsciMyo = emekli ? 0 : R(h.SgkMatrahi * BordroParametre.IsciMyo);
            h.IsciGss = emekli ? 0 : R(h.SgkMatrahi * BordroParametre.IsciGss); h.IsciSgdp = emekli ? R(h.SgkMatrahi * BordroParametre.IsciSgdp) : 0;
            h.SgkIsciToplam = h.IsciIssizlik + h.IsciMyo + h.IsciGss + h.IsciSgdp;
            // 5) damga vergisi
            h.DamgaMatrahi = R(Math.Max(0, h.ToplamBrutKazanc - h.ToplamYanHaklar - p.SgkDisi)); h.AsgariDamga = h.DamgaMatrahi > 0 ? asgDamga : 0;   // yemek istisnası (SgkDisi) damga matrahından da düşer
            h.DvTutari = Math.Max(0, R(h.DamgaMatrahi * BordroParametre.Damga - h.AsgariDamga));
            // 6) gelir vergisi
            h.GvMatrahi = R(Math.Max(0, h.ToplamBrutKazanc - h.ToplamYasal - h.SgkIsciToplam - p.SgkDisi));
            h.KumulatifMatrah = R(h.GvMatrahi * ay); h.AsgariIstisna = h.GvMatrahi > 0 ? istisna : 0; h.AsgariKumulatifMatrah = sgkYok ? 0 : R(BordroParametre.AsgariGvMatrahi * ay);
            if (kaynak)
            {   // bordro net = R-Net + ekler; vergi artık olarak bulunur, böylece brüt − net kaynak tabloyla birebir tutar
                h.GvTutari = Math.Max(0, R(h.ToplamBrutKazanc - (rNet + ekler) - h.SgkIsciToplam - h.DvTutari));
                h.HesaplananGv = R(h.GvTutari + h.AsgariIstisna + p.GvIndirimleri + p.OzelSigortaIndirimi);
            }
            else
            {
                h.HesaplananGv = R(BordroParametre.AylikGv(h.GvMatrahi, ay));
                h.GvTutari = Math.Max(0, R(h.HesaplananGv - h.AsgariIstisna - p.GvIndirimleri - p.OzelSigortaIndirimi));
            }
            // 7) SGK işveren
            h.IsvIssizlik = emekli ? 0 : R(h.SgkMatrahi * BordroParametre.IsvIssizlik); h.IsvMyo = emekli ? 0 : R(h.SgkMatrahi * BordroParametre.IsvMyo);
            h.IsvGss = emekli ? 0 : R(h.SgkMatrahi * BordroParametre.IsvGss); h.IsvSgdp = emekli ? R(h.SgkMatrahi * BordroParametre.IsvSgdp) : 0;
            h.Kvsk = R(h.SgkMatrahi * BordroParametre.Kvsk); h.HazineIndirimi = emekli ? 0 : R(h.SgkMatrahi * BordroParametre.HazineIndirimi);
            if (p.IsverenKaynak.HasValue && p.IsverenKaynak.Value > 0)
            {   // kaynak tablodaki İş Veren tutarı kalemlere oran payıyla dağıtılır; hazine indirimi yalnız gösterilir, toplamdan düşülmez (rapor gibi)
                decimal z = p.IsverenKaynak.Value, top = h.IsvIssizlik + h.IsvMyo + h.IsvGss + h.IsvSgdp + h.Kvsk; h.HazineIndirimi = p.HazineKaynak;
                if (top > 0) { decimal k = z / top; h.IsvIssizlik = R(h.IsvIssizlik * k); h.IsvGss = R(h.IsvGss * k); h.Kvsk = R(h.Kvsk * k); h.IsvSgdp = R(h.IsvSgdp * k); h.IsvMyo = R(h.IsvMyo * k); }
                decimal fark = z - (h.IsvIssizlik + h.IsvMyo + h.IsvGss + h.IsvSgdp + h.Kvsk); if (emekli) h.IsvSgdp += fark; else h.IsvMyo += fark;
                h.SgkIsverenToplam = z;
            }
            else h.SgkIsverenToplam = h.IsvIssizlik + h.IsvMyo + h.IsvGss + h.IsvSgdp + h.Kvsk - h.HazineIndirimi;
            // 8) ödeme özeti ve maliyet (R bordrolu, X bordro dışı)
            // banka = anlaşılan net R-Net + R ek ödemeler (ekler) + Ek Ödemeler Toplam-R − kesintiler; brütten türetilmez (kaynak KALAN NET TUTAR (BANKA))
            h.RNet2 = rNet; h.KesintilerToplami = p.Avans + p.Icra + p.Borc + p.OzelSigortaKesintisi + p.SendikaKatkisi + p.Bes + p.MazotKesintisi + p.IhbarKesintisi;
            h.DigerEkR = R(ekler + p.DigerEkOdemelerR); h.KalanNetBanka = R(rNet + h.DigerEkR - h.KesintilerToplami);
            h.ToplamEkCalismaX = p.FazlaMesaiX; h.ToplamSosyalX = p.YemekX + p.YolX; h.ToplamPrimX = p.PrimX; h.DigerEkOdemelerX = h.ToplamEkCalismaX + h.ToplamSosyalX + h.ToplamPrimX;
            h.NetRBordro = h.KalanNetBanka; h.NetXBordro = xNet + h.DigerEkOdemelerX; h.KalanNetElden = R(h.NetXBordro - p.KesintilerToplamiX);   // X kesintisi maliyeti düşürmez, elden ödenen tutarı azaltır
            // ödeme kanalı (banka / elden): maliyet satırları (Ara Toplam, Genel Maliyet) bu kanaldan etkilenmez
            h.BankaOdeme = p.BankaBlokeli ? 0m : h.NetRBordro; h.NakitRTutar = p.BankaBlokeli ? h.NetRBordro : 0m; h.EldenOdeme = R(h.KalanNetElden + h.NakitRTutar);
            h.AraToplam = h.NetXBordro + h.NetRBordro; h.ToplamBrutMasraflar = sgkYok ? 0 : R(h.ToplamBrutKazanc - h.KalanNetBanka); h.ToplamIsveren = h.SgkIsverenToplam;
            h.GenelMaliyet = R(h.AraToplam + h.ToplamBrutMasraflar + h.ToplamIsveren);
            return h;
        }
    }

    public class PmPersonel
    {
        public int Sira { get; set; }
        public string CariKodu => "335." + Sira.ToString("000");
        // ---- kimlik
        public string SgkIsvereni { get; set; }
        public string FiiliGorevYeri { get; set; }
        public string AdSoyad { get; set; }
        public string Unvan { get; set; }
        public string Bolum { get; set; }
        public string Gorev { get; set; }
        public DateTime? IseGiris { get; set; }
        public string CatiDepartman { get; set; }
        public string OzelDurum { get; set; } = "";
        public string DepartmanKodu { get; set; } = "";
        public string DepartmanAciklamasi { get; set; } = "";
        public string SicilNo { get; set; } = "";
        public string SgkNo { get; set; } = "";
        // ---- personel cari kartı ek alanları
        public string Durum { get; set; } = "Aktif";        // Aktif | İşten Ayrıldı
        public DateTime? AyrilisTarihi { get; set; }
        public string TcKimlik { get; set; } = "";
        public string Telefon { get; set; } = "";
        public string Eposta { get; set; } = "";
        // ---- girilen tutarlar (aylık, TL) ve günler
        public decimal XNet { get; set; } public decimal RNet { get; set; }
        public decimal PrimGun { get; set; } = 30; public decimal NormalGun { get; set; } = 22; public decimal HaftaSonuGun { get; set; } = 8; public decimal ResmiTatilGun { get; set; } public decimal BayramGun { get; set; } public decimal UcretliIzinGun { get; set; }
        public decimal FazlaMesai { get; set; } public decimal BayramCalisma { get; set; } public decimal GeceFarki { get; set; } public decimal PazarCalisma { get; set; } public decimal ResmiTatilCalisma { get; set; }
        public decimal Yemek { get; set; } public decimal Yol { get; set; } public decimal Servis { get; set; } public decimal KiraDestegi { get; set; } public decimal Egitim { get; set; } public decimal CocukYardimi { get; set; }
        public decimal MaasFarki { get; set; } public decimal Prim { get; set; } public decimal OzelOdeme { get; set; }
        public decimal OzelSaglik { get; set; } public decimal Kiyafet { get; set; } public decimal KoruyucuEkipman { get; set; } public decimal DogumYardimi { get; set; } public decimal EvlilikYardimi { get; set; }
        public decimal KidemTazminati { get; set; } public decimal IhbarTazminati { get; set; } public decimal IseIade { get; set; } public decimal YillikIzinOdemesi { get; set; } public decimal RaporFarki { get; set; } public decimal DogumIzni { get; set; }
        public decimal GvIndirimleri { get; set; } public decimal OzelSigortaIndirimi { get; set; }
        public decimal Avans { get; set; } public decimal Icra { get; set; } public decimal Borc { get; set; } public decimal OzelSigortaKesintisi { get; set; } public decimal SendikaKatkisi { get; set; } public decimal Bes { get; set; }
        public decimal DigerEkOdemelerR { get; set; }
        public decimal KasaTazminati { get; set; } public decimal MazotKesintisi { get; set; } public decimal IhbarKesintisi { get; set; }
        /// <summary>X (bordro dışı) tarafı ödenen kalemler; tabloda aynı kalemin R değerinin altında gösterilir (rx sütunu). Brüt ve vergiye girmez, Net-X Bordro'ya eklenir. Kesintiler Toplamı-X elden ödenen tutardan düşülür.</summary>
        public decimal YemekX { get; set; } public decimal YolX { get; set; } public decimal FazlaMesaiX { get; set; } public decimal PrimX { get; set; } public decimal KesintilerToplamiX { get; set; }
        /// <summary>Dövizli maaş (kartta girilir): Para Birimi (USD / EUR / GBP; boş = ₺), aylık Döviz Tutarı ve Kur (1 birim = ? ₺). Doluysa X-Net = Döviz Tutarı × Kur.</summary>
        public string Doviz { get; set; } = ""; public decimal DovizTutar { get; set; } public decimal Kur { get; set; }
        public bool Dovizli => !string.IsNullOrEmpty(Doviz) && DovizTutar > 0 && Kur > 0;
        /// <summary>Dövizli maaş düzeni (4 Kas 2026): "KalanX" = R sabit tutar, toplam ₺ − R kadarı X (bordro dışı); "TamamiR" = toplam ₺ tamamı R (banka); "TamamiX" = toplam ₺ tamamı X (elden). Boş = KalanX.</summary>
        public string DovizOdeme { get; set; } = "";
        public const string DovizKalanX = "KalanX", DovizTamamiR = "TamamiR", DovizTamamiX = "TamamiX";
        /// <summary>Özel kişi: şirket kadrosu dışındaki kişiler (aile üyeleri, ev çalışanları vb.). Aynı bordro modeliyle ayrı "Özel Kişiler" sekmesinde izlenir, Personel Maliyet Tablosu'nda görünmez.</summary>
        public bool Ozel { get; set; }
        /// <summary>Banka hesabı blokeli (4 Kas 2026): R tutarı icra ve kesintiler düşüldükten sonra elden (X tarafı) ödenir; banka transferi yapılmaz. Maliyet ve bordro tutarları değişmez.</summary>
        public bool BankaBlokeli { get; set; }
        /// <summary>Dönem kopyası: kişi ve girilen tüm kalemler kopyalanır (hesap alanları her okumada yeniden hesaplanır).</summary>
        public PmPersonel Kopya() => (PmPersonel)MemberwiseClone();
        public string Not { get; set; } = "";
        /// <summary>Kaynak tablodan gelen Toplam Brüt Maliyet ve İş Veren tutarları (varsa hesap bunlara bağlanır; satır düzenlenince silinir).</summary>
        public decimal? BrutKaynak { get; set; } public decimal? IsverenKaynak { get; set; }
        /// <summary>SGK ve brüt detayı kaynakta yok (Özel kişiler, rapor 2026-09): brüt, SGK, vergi ve işveren maliyeti hesaplanmaz; bordro Net-R = R-Net + R ekleri − kesintiler; Genel Maliyet = Ara Toplam.</summary>
        public bool SgkDetayYok { get; set; }
        /// <summary>Yemek istisnası (rapor 2026-09: 7.500,22): SGK, damga ve gelir vergisi matrahından düşülür.</summary>
        public decimal SgkDisi { get; set; }
        /// <summary>Kaynak tablodaki İstihdam Hazine İndirimi (yalnız gösterim; İş Veren toplamı kaynak tutardır).</summary>
        public decimal HazineKaynak { get; set; }
        // ---- hesaplanan
        public PmBordro Hesap => PmBordro.Hesapla(this);
        /// <summary>Kıdem (yıl): işe girişten bugüne, ayrılanlarda ayrılış tarihine; planlanmış (gelecek tarihli) işe girişte 0.</summary>
        public decimal? KidemYil => IseGiris.HasValue ? Math.Max(0m, Math.Round((decimal)((AyrilisTarihi ?? DateTime.Today) - IseGiris.Value.Date).TotalDays / 365.25m, 1)) : (decimal?)null;
    }

    /// <summary>Ekrandan gelen personel satırı (tablo düzenlemesi, kart çekmecesi ve yeni personel için aynı yapı). Tutar / gün alan adları PmPersonel ile aynıdır.</summary>
    public class PmDuzenleme
    {
        public int Sira { get; set; }
        public string SgkIsvereni { get; set; } public string FiiliGorevYeri { get; set; } public string AdSoyad { get; set; } public string Unvan { get; set; } public string Bolum { get; set; } public string Gorev { get; set; }
        public string IseGiris { get; set; } public string CatiDepartman { get; set; } public string OzelDurum { get; set; }
        public string DepartmanKodu { get; set; } public string DepartmanAciklamasi { get; set; } public string SicilNo { get; set; } public string SgkNo { get; set; }
        public string Durum { get; set; } public string AyrilisTarihi { get; set; } public string TcKimlik { get; set; } public string Telefon { get; set; } public string Eposta { get; set; }
        public decimal XNet { get; set; } public decimal RNet { get; set; }
        public decimal PrimGun { get; set; } public decimal NormalGun { get; set; } public decimal HaftaSonuGun { get; set; } public decimal ResmiTatilGun { get; set; } public decimal BayramGun { get; set; } public decimal UcretliIzinGun { get; set; }
        public decimal FazlaMesai { get; set; } public decimal BayramCalisma { get; set; } public decimal GeceFarki { get; set; } public decimal PazarCalisma { get; set; } public decimal ResmiTatilCalisma { get; set; }
        public decimal Yemek { get; set; } public decimal Yol { get; set; } public decimal Servis { get; set; } public decimal KiraDestegi { get; set; } public decimal Egitim { get; set; } public decimal CocukYardimi { get; set; }
        public decimal MaasFarki { get; set; } public decimal Prim { get; set; } public decimal OzelOdeme { get; set; }
        public decimal OzelSaglik { get; set; } public decimal Kiyafet { get; set; } public decimal KoruyucuEkipman { get; set; } public decimal DogumYardimi { get; set; } public decimal EvlilikYardimi { get; set; }
        public decimal KidemTazminati { get; set; } public decimal IhbarTazminati { get; set; } public decimal IseIade { get; set; } public decimal YillikIzinOdemesi { get; set; } public decimal RaporFarki { get; set; } public decimal DogumIzni { get; set; }
        public decimal GvIndirimleri { get; set; } public decimal OzelSigortaIndirimi { get; set; }
        public decimal Avans { get; set; } public decimal Icra { get; set; } public decimal Borc { get; set; } public decimal OzelSigortaKesintisi { get; set; } public decimal SendikaKatkisi { get; set; } public decimal Bes { get; set; }
        public decimal DigerEkOdemelerR { get; set; }
        public decimal YemekX { get; set; } public decimal YolX { get; set; } public decimal FazlaMesaiX { get; set; } public decimal PrimX { get; set; } public decimal KesintilerToplamiX { get; set; }
        public decimal KasaTazminati { get; set; } public decimal MazotKesintisi { get; set; } public decimal IhbarKesintisi { get; set; }
        public string Doviz { get; set; } public decimal DovizTutar { get; set; } public decimal Kur { get; set; }
        public bool Ozel { get; set; }
        /// <summary>Kaydın ait olduğu dönem (yyyy-MM); boşsa güncel dönem.</summary>
        public string Donem { get; set; }
        public string Not { get; set; }
    }

    /// <summary>Tablo sütunu: başlık, JSON anahtarı (camelCase), tip ve düzenleme listesi.
    /// Tip: sira | metin | ad | tarih | secim | sayi (girilen tutar) | rx (girilen tutar, R üstte / X altta; X değeri Anahtar + "X") | gun (girilen gün) | hesap (hesaplanan) | genel (genel maliyet) | not | kart</summary>
    public class PmSutun
    {
        public string Ad { get; set; } public string Anahtar { get; set; } public string Tip { get; set; } public string Liste { get; set; } = "";
        public bool Sayisal => Tip == "sayi" || Tip == "rx" || Tip == "gun" || Tip == "hesap" || Tip == "genel";
        public bool Girdi => Tip == "sayi" || Tip == "rx" || Tip == "gun";
        /// <summary>Bu sütunun girilen alan anahtarları (rx sütununda R ve X).</summary>
        public IEnumerable<string> GirdiAnahtarlari => Tip == "rx" ? new[] { Anahtar, Anahtar + "X" } : Girdi ? new[] { Anahtar } : new string[0];
    }

    public static class PersonelMaliyetOrnek
    {
        public static readonly object Kilit = new object();
        public static readonly string Kaynak = "Personel Maliyet Raporu 2026-09 (DUZELTILMIS_v5_2)";
        public static (string Kisi, DateTime Tarih)? SonKayit { get; private set; }
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

        private static PmSutun S(string ad, string anahtar, string tip, string liste = "") => new PmSutun { Ad = ad, Anahtar = anahtar, Tip = tip, Liste = liste };
        /// <summary>Sütun grupları — "Personel Bordro ve Ödeme Tablosu" 1. satır (grup) ve 2. satır (sütun) ile aynı sırada; sonda Not ve Kart.</summary>
        public static readonly (string Grup, PmSutun[] Sutunlar)[] SutunGruplari =
        {
            ("Personel Kimlik Bilgileri", new[] { S("Sıra", "sira", "sira"), S("SGK İşvereni", "sgkIsvereni", "metin", "dlSgk"), S("Fiili Görev Yeri", "fiiliGorevYeri", "metin", "dlYer"), S("Ödeme Şirketi-R", "odemeSirketiR", "bilgi"), S("Ödeme Şirketi-X", "odemeSirketiX", "bilgi"), S("Adı-Soyadı", "adSoyad", "ad"), S("Unvan", "unvan", "metin", "dlUnvan"), S("Bölüm", "bolum", "metin", "dlBolum"), S("Görev", "gorev", "metin", "dlGorev"), S("İşe Giriş Tarihi", "iseGiris", "tarih"), S("Çatı Departman", "catiDepartman", "metin", "dlCati"), S("Özel Durumu", "ozelDurum", "secim"), S("Departman Kodu", "departmanKodu", "metin"), S("Departman Açıklaması", "departmanAciklamasi", "metin"), S("Sicil Numarası", "sicilNo", "metin"), S("SGK Numarası", "sgkNo", "metin") }),
            ("Net Ücret", new[] { S("R-Net", "rNet", "sayi"), S("X-Net", "xNet", "sayi"), S("Toplam Net", "toplamNet", "hesap") }),
            ("Puantaj ve Çalışma Kazançları", new[] { S("Prim Gün", "primGun", "gun"), S("Normal Çalışma (Gün)", "normalGun", "gun"), S("Normal Çalışma Kazancı", "normalKazanc", "hesap"), S("Hafta Sonu (Gün)", "haftaSonuGun", "gun"), S("Hafta Sonu Kazancı", "haftaSonuKazanc", "hesap"), S("Resmî Tatil (Gün)", "resmiTatilGun", "gun"), S("Resmî Tatil Kazancı", "resmiTatilKazanc", "hesap"), S("Bayram Tatili (Gün)", "bayramGun", "gun"), S("Bayram Tatili Kazancı", "bayramKazanc", "hesap"), S("Ücretli İzin (Gün)", "ucretliIzinGun", "gun"), S("Ücretli İzin Kazancı", "ucretliIzinKazanc", "hesap") }),
            ("Ek Çalışmalar", new[] { S("Fazla Mesai", "fazlaMesai", "rx"), S("Bayram", "bayramCalisma", "sayi"), S("Gece Farkı", "geceFarki", "sayi"), S("Pazar", "pazarCalisma", "sayi"), S("Resmi Tatil", "resmiTatilCalisma", "sayi"), S("Toplam Ek Çalışma-R", "toplamEkCalisma", "hesap"), S("Toplam Ek Çalışma-X", "toplamEkCalismaX", "hesap") }),
            ("Sosyal Yardımlar", new[] { S("Yemek", "yemek", "rx"), S("Yol", "yol", "rx"), S("Servis", "servis", "sayi"), S("Kira Desteği", "kiraDestegi", "sayi"), S("Eğitim", "egitim", "sayi"), S("Çocuk Yardımı", "cocukYardimi", "sayi"), S("Toplam Sosyal Yardımlar-R", "toplamSosyal", "hesap"), S("Toplam Sosyal Yardımlar-X", "toplamSosyalX", "hesap") }),
            ("Prim ve Ek Kazançlar", new[] { S("Maaş Farkı", "maasFarki", "sayi"), S("Prim", "prim", "rx"), S("Özel Ödeme", "ozelOdeme", "sayi"), S("Kasa Tazminatı", "kasaTazminati", "sayi"), S("Toplam Prim-R", "toplamPrim", "hesap"), S("Toplam Prim-X", "toplamPrimX", "hesap") }),
            ("Yan Haklar", new[] { S("Özel Sağlık", "ozelSaglik", "sayi"), S("Kıyafet", "kiyafet", "sayi"), S("Koruyucu Ekipman", "koruyucuEkipman", "sayi"), S("Doğum Yardımı", "dogumYardimi", "sayi"), S("Evlilik Yardımı", "evlilikYardimi", "sayi"), S("Toplam Yan Haklar", "toplamYanHaklar", "hesap") }),
            ("Yasal Ödemeler", new[] { S("Kıdem Tazminatı", "kidemTazminati", "sayi"), S("İhbar Tazminatı", "ihbarTazminati", "sayi"), S("İşe İade", "iseIade", "sayi"), S("Yıllık İzin Ödemesi", "yillikIzinOdemesi", "sayi"), S("Rapor Farkı", "raporFarki", "sayi"), S("Doğum İzni", "dogumIzni", "sayi"), S("Toplam Yasal Ödemeler", "toplamYasal", "hesap") }),
            ("Brüt Kazanç ve SGK Matrahı", new[] { S("Toplam Brüt Kazanç", "toplamBrutKazanc", "hesap"), S("SGK Matrahı", "sgkMatrahi", "hesap") }),
            ("Gelir Vergisi", new[] { S("Gelir Vergisi Matrahı", "gvMatrahi", "hesap"), S("Kümülatif Vergi Matrahı", "kumulatifMatrah", "hesap"), S("Hesaplanan Gelir Vergisi", "hesaplananGv", "hesap"), S("Asgari Ücret Vergi İstisnası", "asgariIstisna", "hesap"), S("Gelir Vergisi İndirimleri", "gvIndirimleri", "sayi"), S("Özel Sigorta İndirimi", "ozelSigortaIndirimi", "sayi"), S("Gelir Vergisi Tutarı", "gvTutari", "hesap"), S("Asgari Ücret Kümülatif Matrahı", "asgariKumulatifMatrah", "hesap") }),
            ("Damga Vergisi", new[] { S("Damga Vergisi Matrahı", "damgaMatrahi", "hesap"), S("Asgari Ücret Damga Vergisi", "asgariDamga", "hesap"), S("Damga Vergisi Tutarı", "dvTutari", "hesap") }),
            ("SGK İşçi Payları", new[] { S("İşsizlik Sigortası Primi (İşçi)", "isciIssizlik", "hesap"), S("Malullük, Yaşlılık ve Ölüm Primi (İşçi)", "isciMyo", "hesap"), S("Genel Sağlık Sigortası Primi (İşçi)", "isciGss", "hesap"), S("SGDP Primi (İşçi)", "isciSgdp", "hesap"), S("SGK İşçi Toplam Prim", "sgkIsciToplam", "hesap") }),
            ("İşveren Payları", new[] { S("İşsizlik Sigortası Primi (İşveren)", "isvIssizlik", "hesap"), S("Malullük, Yaşlılık ve Ölüm Primi (İşveren)", "isvMyo", "hesap"), S("Genel Sağlık Sigortası Primi (İşveren)", "isvGss", "hesap"), S("SGDP Primi (İşveren)", "isvSgdp", "hesap"), S("Kısa Vadeli Sigorta Kolu Primi", "kvsk", "hesap"), S("İstihdam Hazine İndirimi", "hazineIndirimi", "hesap"), S("SGK İşveren Toplam Prim", "sgkIsverenToplam", "hesap") }),
            ("Diğer Kesintiler", new[] { S("Avans", "avans", "sayi"), S("İcra", "icra", "sayi"), S("Borç", "borc", "sayi"), S("Özel Sigorta Kesintisi", "ozelSigortaKesintisi", "sayi"), S("Sendika Katkısı", "sendikaKatkisi", "sayi"), S("Bireysel Emeklilik Sistemi", "bes", "sayi"), S("Mazot Kesintisi", "mazotKesintisi", "sayi"), S("İhbar Kesintisi", "ihbarKesintisi", "sayi") }),
            ("Ödeme Özeti-R", new[] { S("R-Net", "rNet2", "hesap"), S("Ek Ödemeler Toplam-R", "digerEkR", "hesap"), S("Kesintiler Toplamı-R", "kesintilerToplami", "hesap"), S("Kalan Net Tutar (Banka)", "kalanNetBanka", "hesap") }),
            ("Ödeme Özeti-X", new[] { S("X-Net", "xNet2", "hesap"), S("Ek Ödemeler Toplam-X", "digerEkOdemelerX", "hesap"), S("Kesintiler Toplamı-X", "kesintilerToplamiX", "sayi"), S("Kalan Net Tutar (Elden)", "kalanNetElden", "hesap") }),
            ("Maliyet ve Not", new[] { S("Net-X Bordro", "netXBordro", "hesap"), S("Net-R Bordro", "netRBordro", "hesap"), S("Ara Toplam", "araToplam", "hesap"), S("Toplam Brüt Masraflar", "toplamBrutMasraflar", "hesap"), S("Toplam İşveren", "toplamIsveren", "hesap"), S("Genel Maliyet", "genelMaliyet", "genel"), S("Not", "not", "not") }),
            ("Kart", new[] { S("Personel Kartı", "kart", "kart") })
        };
        public static IEnumerable<PmSutun> TumSutunlar => SutunGruplari.SelectMany(g => g.Sutunlar);
        public static object SutunPaketi => SutunGruplari.Select(g => new { grup = g.Grup, sutunlar = g.Sutunlar.Select(s => new { ad = s.Ad, anahtar = s.Anahtar, tip = s.Tip, liste = s.Liste }) });

        public static readonly string[] OzelDurumlar = { "Emekli", "Engelli", "Gündelikçi" };
        /// <summary>Dövizli maaş para birimleri (boş = ₺).</summary>
        public static readonly string[] ParaBirimleri = { "USD", "EUR", "GBP" };
        public static readonly string[] Durumlar = { "Aktif", "İşten Ayrıldı" };

        // ---- dönem (ay): tek dönem Eylül 2026; kaynak tablo bu dönemin bordrosudur (diğer aylar yok)
        public const string GuncelDonem = "2026-09";
        private static readonly Lazy<SortedDictionary<string, List<PmPersonel>>> _donemler = new Lazy<SortedDictionary<string, List<PmPersonel>>>(DonemleriKur);
        public static SortedDictionary<string, List<PmPersonel>> Donemler => _donemler.Value;
        /// <summary>Eylül 2026 bordro listesi (Personel Maliyet Raporu 2026-09). Diğer ekranlar (yansıtma, muhasebe) bunu kullanır.</summary>
        public static List<PmPersonel> Liste => Donemler[GuncelDonem];
        public static List<PmPersonel> ListeDonem(string donem) => !string.IsNullOrEmpty(donem) && Donemler.TryGetValue(donem, out var l) ? l : null;
        /// <summary>En yeni dönem: dönem verileri açılırken otomatik seçilir ve "güncel" sayılır (4 Kas 2026).</summary>
        public static string EnYeniDonem => Donemler.Keys.Aggregate((a, b) => string.CompareOrdinal(a, b) >= 0 ? a : b);
        private static readonly string[] AyAdlari = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
        public static string DonemAd(string kod) => kod != null && kod.Length == 7 && int.TryParse(kod.Substring(5, 2), out int ay) && ay >= 1 && ay <= 12 ? AyAdlari[ay - 1] + " " + kod.Substring(0, 4) : kod;
        public static object DonemPaketi => Donemler.Keys.OrderByDescending(k => k).Select(k => new { kod = k, ad = DonemAd(k), guncel = k == EnYeniDonem, kisi = Donemler[k].Count }).ToList();
        private static SortedDictionary<string, List<PmPersonel>> DonemleriKur()
        {
            var guncel = Tohumla();
            foreach (var p in guncel) p.BankaBlokeli = Array.IndexOf(BankaBlokeliKisiler, p.AdSoyad) >= 0;
            return new SortedDictionary<string, List<PmPersonel>> { [GuncelDonem] = guncel };
        }
        /// <summary>Banka hesabı blokeli kişiler (4 Kas 2026): Cafer İpek ve Nükhet Özavşar; R tutarları elden ödenir.</summary>
        private static readonly string[] BankaBlokeliKisiler = { "Cafer İpek", "Nükhet Özavşar" };
        /// <summary>Yeni bordro dönemi (ör. 2026-10): güncel dönemin kopyası; kesinti taksitleri çağıran tarafından o aya göre yazılır. Dönem zaten varsa dokunulmaz (yeni = false).</summary>
        public static List<PmPersonel> DonemOlustur(string donem, Dictionary<string, decimal> kurlar, out bool yeni)
        {
            if (string.IsNullOrEmpty(donem) || !System.Text.RegularExpressions.Regex.IsMatch(donem, @"^\d{4}-(0[1-9]|1[0-2])$")) throw new ArgumentException("Dönem yyyy-aa biçiminde olmalı (örn. 2026-10).");
            if (string.CompareOrdinal(donem, GuncelDonem) <= 0) throw new ArgumentException("Yeni bordro güncel dönemden (" + DonemAd(GuncelDonem) + ") sonraki bir ay olmalı.");
            lock (Kilit)
            {
                yeni = !Donemler.ContainsKey(donem);
                if (!yeni) return Donemler[donem];
                decimal eur = kurlar != null && kurlar.TryGetValue("EUR", out var e) ? e : 0m;
                decimal usd = kurlar != null && kurlar.TryGetValue("USD", out var u) ? u : 0m;
                if (eur <= 0 || usd <= 0) throw new ArgumentException("Yeni bordro için EUR ve USD kuru (₺) girilmeli.");
                var liste = Donemler[GuncelDonem].Where(p => !(DonemdeYok.TryGetValue(donem, out var yok) && yok.Contains(p.AdSoyad))).Select(p => p.Kopya()).ToList();
                foreach (var p in liste)
                {
                    var d = Array.Find(DovizliDuzen, x => x.Ad == p.AdSoyad);
                    if (d.Ad == null) continue;
                    decimal yeniKur = d.Pb == "EUR" ? eur : usd;
                    p.Doviz = d.Pb; p.DovizTutar = d.Tutar; p.Kur = yeniKur; p.DovizOdeme = d.Odeme; p.RNet = d.SabitR; p.XNet = 0m;
                    if (d.Odeme == PmPersonel.DovizTamamiR)
                    {   // R ₺ kura göre değişir: kaynak brüt ve işveren tutarı kur oranında ölçeklenir (kur aynıysa Eylül maliyeti aynen kalır)
                        decimal f = yeniKur / (d.Pb == "EUR" ? EylulEur : EylulUsd);
                        if (p.BrutKaynak.HasValue) p.BrutKaynak = Math.Round(p.BrutKaynak.Value * f, 2);
                        if (p.IsverenKaynak.HasValue) p.IsverenKaynak = Math.Round(p.IsverenKaynak.Value * f, 2);
                    }
                }
                Donemler[donem] = liste;
                return liste;
            }
        }
        /// <summary>Eylül 2026 kaynak kurları (R ve X tutarlarından): USD 48,9008 (Sevgi, Berkan, Orazgül) · EUR 55,6306 (Vu Thi, Joao yaklaşık).</summary>
        private const decimal EylulEur = 55.6306m, EylulUsd = 48.9008m;
        /// <summary>Dövizli maaş düzeni (kullanıcı, 4 Kas 2026): Joao ve Vu Thi R sabit + kalanı X; Sevgi ve Berkan tamamı R; Orazgül tamamı X. Kur yeni bordro girdisidir.</summary>
        private static readonly (string Ad, string Pb, decimal Tutar, string Odeme, decimal SabitR)[] DovizliDuzen =
        {
            ("Joao Paulo Araujo Fernandes", "EUR", 5000m, PmPersonel.DovizKalanX, 67157m),
            ("Vu Thi Thuy Ha", "EUR", 2000m, PmPersonel.DovizKalanX, 46931m),
            ("Orazgül Jumayeva", "USD", 1300m, PmPersonel.DovizTamamiX, 0m),
            ("Sevgi Kaya", "USD", 2500m, PmPersonel.DovizTamamiR, 0m),
            ("Berkan Özkan", "USD", 2000m, PmPersonel.DovizTamamiR, 0m),
        };
        /// <summary>Dönemde yok (kullanıcı, 4 Kas 2026): Ekim 2026 bordrosuna alınmaz.</summary>
        private static readonly Dictionary<string, string[]> DonemdeYok = new Dictionary<string, string[]> { ["2026-10"] = new[] { "Emine Tanış", "Medine Rozykulova" } };
        private static readonly StringComparer TrSira = StringComparer.Create(new CultureInfo("tr-TR"), true);
        private static string[] Ayrik(Func<PmPersonel, string> f) => Liste.Select(f).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x, TrSira).ToArray();
        public static string[] SgkIsverenleri => Ayrik(p => p.SgkIsvereni);
        public static string[] GorevYerleri => Ayrik(p => p.FiiliGorevYeri);
        public static string[] CatiDepartmanlar => Ayrik(p => p.CatiDepartman);
        public static string[] Bolumler => Ayrik(p => p.Bolum);
        public static string[] Unvanlar => Ayrik(p => p.Unvan);
        public static string[] Gorevler => Ayrik(p => p.Gorev);

        private static readonly string[] TarihBicimleri = { "yyyy-MM-dd", "dd.MM.yyyy", "d.MM.yyyy", "dd.M.yyyy", "d.M.yyyy" };
        private static DateTime? Tarih(string s) => DateTime.TryParseExact((s ?? "").Trim(), TarihBicimleri, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : (DateTime?)null;
        private static string Temiz(string s) => (s ?? "").Trim();

        // ---- girilen sayısal alanlar: PmDuzenleme → PmPersonel aynı adlı özellikler (sütun tanımından türetilir)
        private static readonly PropertyInfo[] DuzenlemeSayisal = TumSutunlar.SelectMany(s => s.GirdiAnahtarlari).Select(k => typeof(PmDuzenleme).GetProperty(Pascal(k))).Where(p => p != null).ToArray();
        /// <summary>X tarafı alanlar (rx sütunlarının alt satırı): brüt, vergi ve SGK'ya girmez; değişmesi kaynak tablo değerlerini düşürmez.</summary>
        private static readonly HashSet<string> XAlanlari = new HashSet<string>(TumSutunlar.Where(s => s.Tip == "rx").Select(s => Pascal(s.Anahtar) + "X").Concat(new[] { "XNet", "KesintilerToplamiX", "Avans", "Icra", "Borc", "OzelSigortaKesintisi", "SendikaKatkisi", "Bes", "MazotKesintisi", "IhbarKesintisi", "DigerEkOdemelerR" }));   // kesintiler ve R ek ödemeleri de brüte girmez
        private static string Pascal(string k) => char.ToUpperInvariant(k[0]) + k.Substring(1);
        private static readonly Dictionary<string, PropertyInfo> PersonelOzellik = typeof(PmPersonel).GetProperties().ToDictionary(p => p.Name, p => p);
        private static readonly Dictionary<string, PropertyInfo> BordroOzellik = typeof(PmBordro).GetProperties().ToDictionary(p => p.Name, p => p);
        private static readonly HashSet<string> GunAnahtarlari = new HashSet<string>(TumSutunlar.Where(s => s.Tip == "gun").Select(s => s.Anahtar));

        /// <summary>Ekrana giden satır: tüm sütun anahtarları (girilen + hesaplanan) + kart alanları + kaynak değerler.</summary>
        public static Dictionary<string, object> Dto(PmPersonel p)
        {
            var h = p.Hesap;
            var d = new Dictionary<string, object> { ["sira"] = p.Sira, ["cariKodu"] = p.CariKodu, ["durum"] = p.Durum, ["tcKimlik"] = p.TcKimlik, ["telefon"] = p.Telefon, ["eposta"] = p.Eposta, ["kidemYil"] = p.KidemYil, ["brutUcret"] = h.BrutUcret, ["gunluk"] = h.Gunluk,
                ["brutKaynak"] = p.BrutKaynak, ["isverenKaynak"] = p.IsverenKaynak, ["bankaBlokeli"] = p.BankaBlokeli, ["bankaOdeme"] = h.BankaOdeme, ["nakitR"] = h.NakitRTutar, ["eldenOdeme"] = h.EldenOdeme, ["dovizOdeme"] = p.DovizOdeme,
                ["xNet"] = h.XNetTl, ["xNetGirilen"] = p.XNet, ["doviz"] = p.Doviz, ["dovizTutar"] = p.DovizTutar, ["kur"] = p.Kur, ["ozel"] = p.Ozel, ["odemeSirketiR"] = h.NetRBordro > 0 ? PersonelMuhasebeOrnek.OdemeSirketiR : "", ["odemeSirketiX"] = h.KalanNetElden > 0 ? PersonelMuhasebeOrnek.OdemeSirketiX(p) : "",   // xNet ekranda ₺ karşılığı (dövizli maaşta tutar × kur)
                ["iseGiris"] = p.IseGiris?.ToString("yyyy-MM-dd"), ["iseGirisTr"] = p.IseGiris?.ToString("dd.MM.yyyy"), ["ayrilisTarihi"] = p.AyrilisTarihi?.ToString("yyyy-MM-dd"), ["ayrilisTr"] = p.AyrilisTarihi?.ToString("dd.MM.yyyy") };
            foreach (var k in TumSutunlar.Where(s => s.Tip != "kart").SelectMany(s => s.Tip == "rx" ? s.GirdiAnahtarlari : new[] { s.Anahtar }))
            {
                if (d.ContainsKey(k)) continue;
                string ad = Pascal(k);
                if (PersonelOzellik.TryGetValue(ad, out var pp)) d[k] = pp.GetValue(p);
                else if (BordroOzellik.TryGetValue(ad, out var bp)) d[k] = bp.GetValue(h);
            }
            return d;
        }

        /// <summary>Satır doğrulaması; hata metni döner (null = geçerli).</summary>
        private static string Dogrula(PmDuzenleme d, string kim)
        {
            if (string.IsNullOrWhiteSpace(d.AdSoyad)) return $"{kim}: Adı-Soyadı zorunludur.";
            if (string.IsNullOrWhiteSpace(d.SgkIsvereni)) return $"{kim}: SGK İşvereni zorunludur.";
            if (string.IsNullOrWhiteSpace(d.FiiliGorevYeri)) return $"{kim}: Fiili Görev Yeri zorunludur.";
            if (string.IsNullOrWhiteSpace(d.Unvan)) return $"{kim}: Unvan zorunludur.";
            if (string.IsNullOrWhiteSpace(d.Bolum)) return $"{kim}: Bölüm zorunludur.";
            if (string.IsNullOrWhiteSpace(d.Gorev)) return $"{kim}: Görev zorunludur.";
            if (string.IsNullOrWhiteSpace(d.CatiDepartman)) return $"{kim}: Çatı Departman zorunludur.";
            if (!string.IsNullOrWhiteSpace(d.IseGiris) && Tarih(d.IseGiris) == null) return $"{kim}: İşe Giriş Tarihi geçersiz.";
            if (!string.IsNullOrWhiteSpace(d.OzelDurum) && !OzelDurumlar.Contains(d.OzelDurum.Trim())) return $"{kim}: Özel Durumu yalnız {string.Join(" / ", OzelDurumlar)} olabilir.";
            string durum = string.IsNullOrWhiteSpace(d.Durum) ? "Aktif" : d.Durum.Trim();
            if (!Durumlar.Contains(durum)) return $"{kim}: Durum yalnız {string.Join(" / ", Durumlar)} olabilir.";
            if (durum == "İşten Ayrıldı")
            {
                if (string.IsNullOrWhiteSpace(d.AyrilisTarihi)) return $"{kim}: İşten Ayrıldı için ayrılış tarihi zorunludur.";
                var at = Tarih(d.AyrilisTarihi); if (at == null) return $"{kim}: ayrılış tarihi geçersiz.";
                if (at > DateTime.Today) return $"{kim}: ayrılış tarihi bugünden sonra olamaz.";
                if (Tarih(d.IseGiris) != null && at < Tarih(d.IseGiris)) return $"{kim}: ayrılış tarihi işe giriş tarihinden önce olamaz.";
            }
            string tc = Temiz(d.TcKimlik); if (tc.Length > 0 && (tc.Length != 11 || !tc.All(char.IsDigit))) return $"{kim}: TC Kimlik No 11 haneli rakam olmalıdır.";
            string ep = Temiz(d.Eposta); if (ep.Length > 0 && (!ep.Contains("@") || !ep.Contains("."))) return $"{kim}: e-posta biçimi geçersiz.";
            string dv = Temiz(d.Doviz);
            if (dv.Length > 0 && !ParaBirimleri.Contains(dv)) return $"{kim}: Para Birimi yalnız {string.Join(" / ", ParaBirimleri)} olabilir.";
            if (dv.Length > 0 && (d.DovizTutar <= 0 || d.Kur <= 0)) return $"{kim}: dövizli maaşta Döviz Tutarı ve Kur sıfırdan büyük olmalıdır.";
            if (d.DovizTutar < 0 || d.Kur < 0) return $"{kim}: Döviz Tutarı ve Kur negatif olamaz.";
            foreach (var pi in DuzenlemeSayisal)
            {
                decimal v = (decimal)pi.GetValue(d); string anahtar = char.ToLowerInvariant(pi.Name[0]) + pi.Name.Substring(1);
                if (v < 0) return $"{kim}: tutarlar ve günler negatif olamaz.";
                if (GunAnahtarlari.Contains(anahtar) && v > 31) return $"{kim}: gün alanları 31'den büyük olamaz.";
            }
            if (d.NormalGun + d.HaftaSonuGun + d.ResmiTatilGun + d.BayramGun + d.UcretliIzinGun > 31) return $"{kim}: çalışma, tatil ve izin günleri toplamı 31'i aşamaz.";
            return null;
        }
        /// <summary>Aynı kişi aynı SGK işvereninde ikinci kez aktif olamaz (farklı işverenlerde bölünmüş kayıt — ör. avukat — serbesttir).</summary>
        private static bool AyniKisi(PmPersonel x, string ad, string sgk) => TrSira.Compare(x.AdSoyad, ad) == 0 && TrSira.Compare(x.SgkIsvereni, sgk) == 0;
        private static void Uygula(PmPersonel p, PmDuzenleme d)
        {
            bool hesapDegisti = p.OzelDurum != Temiz(d.OzelDurum) || DuzenlemeSayisal.Any(pi => !XAlanlari.Contains(pi.Name) && (decimal)pi.GetValue(d) != (decimal)PersonelOzellik[pi.Name].GetValue(p));
            p.AdSoyad = Temiz(d.AdSoyad); p.SgkIsvereni = Temiz(d.SgkIsvereni); p.FiiliGorevYeri = Temiz(d.FiiliGorevYeri); p.Unvan = Temiz(d.Unvan); p.Bolum = Temiz(d.Bolum); p.Gorev = Temiz(d.Gorev);
            p.IseGiris = Tarih(d.IseGiris); p.CatiDepartman = Temiz(d.CatiDepartman); p.OzelDurum = Temiz(d.OzelDurum);
            p.DepartmanKodu = Temiz(d.DepartmanKodu); p.DepartmanAciklamasi = Temiz(d.DepartmanAciklamasi); p.SicilNo = Temiz(d.SicilNo); p.SgkNo = Temiz(d.SgkNo);
            p.Durum = string.IsNullOrWhiteSpace(d.Durum) ? "Aktif" : d.Durum.Trim(); p.AyrilisTarihi = p.Durum == "İşten Ayrıldı" ? Tarih(d.AyrilisTarihi) : null;
            p.TcKimlik = Temiz(d.TcKimlik); p.Telefon = Temiz(d.Telefon); p.Eposta = Temiz(d.Eposta);
            foreach (var pi in DuzenlemeSayisal) PersonelOzellik[pi.Name].SetValue(p, pi.GetValue(d));
            p.Doviz = Temiz(d.Doviz); p.DovizTutar = p.Doviz.Length > 0 ? d.DovizTutar : 0; p.Kur = p.Doviz.Length > 0 ? d.Kur : 0;
            if (p.Dovizli) p.XNet = BordroParametre.Yuvarla(p.DovizTutar * p.Kur);   // dövizli maaşta X-Net ₺ karşılığıdır; ekrandan gelen X-Net yok sayılır
            p.Ozel = d.Ozel;   // özel kişi bayrağı: ekran mevcut değeri geri gönderir (yeni kartta açıldığı sekmeye göre)
            p.Not = Temiz(d.Not);
            if (hesapDegisti) { p.BrutKaynak = null; p.IsverenKaynak = null; }   // kaynak tablo değerleri artık geçerli değil; model hesaplar
        }

        /// <summary>Düzenlenen satırları uygular (önce hepsi doğrulanır); hata metni döner (null = başarılı).</summary>
        public static string Kaydet(List<PmDuzenleme> satirlar, string kisi, string donem = null)
        {
            if (satirlar == null || satirlar.Count == 0) return "Kaydedilecek değişiklik yok.";
            var liste = ListeDonem(donem) ?? Liste;
            lock (Kilit)
            {
                var gorulenSira = new HashSet<int>(); var gorulen = new HashSet<string>(TrSira);
                var gelenSiralar = new HashSet<int>(satirlar.Select(s => s.Sira));
                foreach (var d in satirlar)
                {
                    if (!gorulenSira.Add(d.Sira)) return $"Sıra {d.Sira} istekte birden fazla kez geçiyor.";
                    var p = liste.FirstOrDefault(x => x.Sira == d.Sira); if (p == null) return $"Sıra {d.Sira} bulunamadı.";
                    var h = Dogrula(d, $"Sıra {d.Sira} ({p.AdSoyad})"); if (h != null) return h;
                    string ad = Temiz(d.AdSoyad), sgk = Temiz(d.SgkIsvereni), durum = string.IsNullOrWhiteSpace(d.Durum) ? "Aktif" : d.Durum.Trim();
                    if (durum == "Aktif")
                    {
                        if (!gorulen.Add(ad + "|" + sgk)) return $"Sıra {d.Sira}: {ad} ({sgk}) istekte birden fazla aktif satırda geçiyor.";
                        if (liste.Any(x => x.Sira != d.Sira && !gelenSiralar.Contains(x.Sira) && x.Durum == "Aktif" && AyniKisi(x, ad, sgk))) return $"Sıra {d.Sira}: {ad} adlı aktif bir personel {sgk} işvereninde zaten var.";
                    }
                }
                foreach (var d in satirlar) Uygula(liste.First(x => x.Sira == d.Sira), d);
                SonKayit = (kisi, DateTime.Now);
            }
            return null;
        }

        /// <summary>Yeni personel kartı açar; hata metni döner (null = başarılı), kayıt out parametresiyle döner.</summary>
        public static string Ekle(PmDuzenleme d, string kisi, out PmPersonel kayit, string donem = null)
        {
            kayit = null; if (d == null) return "Veri yok.";
            var h = Dogrula(d, "Yeni personel"); if (h != null) return h;
            var liste = ListeDonem(donem) ?? Liste;
            lock (Kilit)
            {
                if (liste.Any(x => x.Durum == "Aktif" && AyniKisi(x, Temiz(d.AdSoyad), Temiz(d.SgkIsvereni)))) return $"{Temiz(d.AdSoyad)} adlı aktif bir personel {Temiz(d.SgkIsvereni)} işvereninde zaten var.";
                var p = new PmPersonel { Sira = Donemler.Values.SelectMany(l => l).Select(x => x.Sira).DefaultIfEmpty(0).Max() + 1 };   // sıra (cari kodu) tüm dönemlerde tekil
                Uygula(p, d); liste.Add(p); kayit = p; SonKayit = (kisi, DateTime.Now);
            }
            return null;
        }

        /// <summary>Satırı kalıcı olarak siler (hatalı giriş için); işten ayrılanlar silinmez, Durum = İşten Ayrıldı yapılır.</summary>
        public static string Sil(int sira, string kisi, string donem = null)
        {
            var liste = ListeDonem(donem) ?? Liste;
            lock (Kilit)
            {
                var p = liste.FirstOrDefault(x => x.Sira == sira); if (p == null) return $"Sıra {sira} bulunamadı.";
                liste.Remove(p); SonKayit = (kisi, DateTime.Now);
            }
            return null;
        }

        // ---------- Tohum: Personel Maliyet Raporu 2026-09 (DUZELTILMIS_v5_2, 87 satır, tablo sırasıyla) ----------
        private static readonly Dictionary<string, string> BolumKodu = new Dictionary<string, string>
        {
            ["İdari İşler"] = "D10", ["Muhasebe"] = "D20", ["Finans"] = "D21", ["Cari İşler"] = "D22", ["Üretim"] = "D30", ["Ar-Ge"] = "D31", ["Kalite Güvence"] = "D32", ["Kalite Kontrol"] = "D33",
            ["Teknik Hizmetler"] = "D40", ["Depo ve Sevkiyat"] = "D50", ["Satınalma"] = "D51", ["Satış"] = "D60", ["İhracat"] = "D61", ["Yurtdışı Şube"] = "D62", ["İnsan Kaynakları"] = "D70", ["Hukuk"] = "D80", ["Üst Yönetim"] = "D90", ["Yönetim Özel"] = "D91"
        };
        private static readonly Dictionary<string, string> SgkKisaltma = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ALV Kimya"] = "ALV", ["Uras Kimya"] = "URS", ["Selvi Kimya"] = "SLV", ["Uras Holding"] = "HLD", ["Avrupa Paper"] = "AVP", ["Alv Filo"] = "FLO"
        };
        private const string ODEME_YOK = "Ödeme Yapılmıyor", ALV = "ALV Kimya", URS = "Uras Kimya", SLV = "Selvi Kimya", HLD = "Uras Holding", AVP = "Avrupa Paper", COR = "ALV Kimya Çorlu", IST = "ALV Kimya İst.";
        private static int _sira;
        /// <summary>Kaynak satırı (Personel Maliyet Raporu 2026-09 sütunlarıyla birebir): r = 2026 R-NET, x = 2026 X-NET; yemek / yol (R) ve yemekX / yolX (X) = YEMEK ve YOL; mesai / mesaiX = FAZLA MESAİ; prim / primX = PRİM; kira = KİRA DESTEĞİ; avans / icra / borc / bes / ozelSigorta / sendika = Diğer Kesintiler; digerR = DİĞER EK ÖDEMELER-R; kesintiX = KESİNTİLER TOPLAMI-X; brut = TOPLAM BRÜT KAZANÇ; isveren = TOPLAM İŞVEREN; hazine = İSTİHDAM HAZİNE İNDİRİMİ (yalnız gösterim); dk / da / sicil / sgkNo = departman kodu ve açıklaması, sicil, SGK numarası (verilmezse üretilir); sgkYok = SGK / brüt detayı yok.</summary>
        private static PmPersonel P(string sgk, string yer, string ad, string unvan, string bolum, string gorev, string cati, string ozel = "", string odeme = "",
            decimal r = 0, decimal x = 0, decimal yemek = 0, decimal yemekX = 0, decimal yol = 0, decimal yolX = 0, decimal mesai = 0, decimal mesaiX = 0, decimal prim = 0, decimal primX = 0, decimal kira = 0, decimal avans = 0, decimal icra = 0, decimal borc = 0, decimal bes = 0, decimal ozelSigorta = 0, decimal sendika = 0, decimal digerR = 0, decimal kesintiX = 0, decimal sgkDisi = 0, decimal brut = 0, decimal isveren = 0, decimal primGun = 30, decimal normalGun = 22, decimal haftaSonuGun = 8, bool sgkYok = false, string giris = "", bool ozelKisi = false, decimal hazine = 0, string dk = null, string da = null, string sicil = null, string sgkNo = null, string durum = "Aktif", string ayrilis = null)
        {
            int sira = ++_sira;
            return new PmPersonel
            {
                Sira = sira, SgkIsvereni = sgk, FiiliGorevYeri = yer, AdSoyad = ad, Unvan = unvan, Bolum = bolum, Gorev = gorev, CatiDepartman = cati, OzelDurum = ozel ?? "", IseGiris = Tarih(giris), Ozel = ozelKisi,
                RNet = r, Yemek = yemek, Yol = yol, FazlaMesai = mesai, XNet = x, YemekX = yemekX, YolX = yolX, FazlaMesaiX = mesaiX, Prim = prim, PrimX = primX, Avans = avans, Icra = icra, Borc = borc, Bes = bes, OzelSigortaKesintisi = ozelSigorta, SendikaKatkisi = sendika, KiraDestegi = kira, DigerEkOdemelerR = digerR, KesintilerToplamiX = kesintiX, SgkDisi = sgkDisi, SgkDetayYok = sgkYok, HazineKaynak = hazine, PrimGun = primGun, NormalGun = normalGun, HaftaSonuGun = haftaSonuGun,
                BrutKaynak = brut > 0 ? brut : (decimal?)null, IsverenKaynak = isveren > 0 ? isveren : (decimal?)null, Not = odeme ?? "", Durum = durum, AyrilisTarihi = Tarih(ayrilis),
                DepartmanKodu = dk ?? (BolumKodu.TryGetValue(bolum, out var bk) ? bk : "D00"), DepartmanAciklamasi = da ?? bolum + " Departmanı",
                SicilNo = sicil ?? ((SgkKisaltma.TryGetValue(sgk, out var ks) ? ks : "PRS") + "-" + sira.ToString("0000")), SgkNo = sgkNo ?? (2600000000000L + sira * 1234567L + 8191L * (sira % 7)).ToString()
            };
        }

        private static List<PmPersonel> Tohumla() => TohumListe();
        /// <summary>Kesinti planları (Diğer Kesintiler sekmesi) değişince kişinin dönemdeki kesinti sütunu plan toplamına eşitlenir (ozellik: Avans | Icra | Borc | Bes | OzelSigortaKesintisi | SendikaKatkisi). Kesintiler brüte girmez; kaynak değerler korunur.</summary>
        public static void KesintiAyarla(string donem, int sira, string ozellik, decimal tutar)
        {
            if (!PersonelOzellik.TryGetValue(ozellik, out var pi)) return;
            lock (Kilit) { var p = ListeDonem(donem)?.FirstOrDefault(x => x.Sira == sira); if (p != null) pi.SetValue(p, Math.Round(tutar, 2)); }
        }
        private static List<PmPersonel> TohumListe()
        {
            _sira = 0;
            return new List<PmPersonel>
            {
                P(ALV, IST, "Aksel Güler", "Yönetici", "İhracat", "İhracat Operasyon Yöneticisi", "Tedarik Zinciri ve Operasyonlar", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 85000.00m, yol: 3650.10m, avans: 14000.00m, bes: 4120.00m, brut: 137357.98m, isveren: 32622.52m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "01.03.2022"),
                P(ALV, HLD, "Alev Özavşar Eren", "Yönetim Özel", "Yönetim Özel", "Yönetim Temsilcisi", "Genel Yönetim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. X-NET \"ALEV ÖZAVŞAR\" (Selvi), R-NET \"ALEV ÖZAVŞAR EREN\" (ALV Kimya); aynı kişi olduğu teyit edilmeli.", r: 60000.01m, x: 201209.12m, bes: 2505.00m, brut: 83507.76m, isveren: 19833.09m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "19.03.2020", dk: "D90", da: "Üst Yönetim Departmanı", sicil: "R-001", sgkNo: "3401202004885"),
                P(ALV, COR, "Arslan Çoşkun", "Personel", "İdari İşler", "Temizlik Görevlisi", "Kurumsal Hizmetler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Mesai: bordro 2,776.08 / Personel Kazanç 1,866.67 farklı; Personel Kazanç kullanıldı.", r: 35000.01m, mesai: 1866.67m, brut: 46104.12m, isveren: 10949.73m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "23.03.2019"),
                P(ALV, COR, "Aslan Huseynov", "Personel", "Üretim", "Üretim Personeli", "Üretim", "Gündelikçi", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Ödeme Yapılmıyor", r: 39180.35m, brut: 49545.00m, isveren: 11766.93m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m),
                P(ALV, URS, "Berk Yenilmez", "Uzman", "Üretim", "Üretim Planlama Uzmanı", "Üretim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 65000.00m, yol: 3650.10m, brut: 104726.76m, isveren: 24872.61m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "01.07.2025"),
                P(URS, URS, "Ayşegül Zent Ula", "Uzman", "Cari İşler", "Cari Hesap Uzmanı", "Mali İşler", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 3,946.05 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 4266.67m, x: 46733.33m, yol: 3650.10m, brut: 8558.67m, isveren: 2118.27m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "11.12.2023"),
                P(ALV, URS, "Burak Sergen Ada", "Uzman", "İdari İşler", "İdari İşler Uzmanı", "Kurumsal Hizmetler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Mesai: bordro 5,408.62 / Personel Kazanç 3,866.67 farklı; Personel Kazanç kullanıldı.", r: 40000.00m, mesai: 3866.67m, brut: 55118.35m, isveren: 13090.60m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "27.07.2026"),
                P(ALV, COR, "Burhan Evvel", "Personel", "İdari İşler", "Temizlik Görevlisi", "Kurumsal Hizmetler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 35000.01m, brut: 43328.04m, isveren: 10290.41m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "30.11.2020"),
                P(ALV, URS, "Burhan Gün", "Personel", "İdari İşler", "Sevkiyat Şoförü", "Kurumsal Hizmetler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 46000.00m, bes: 2033.00m, brut: 67771.76m, isveren: 16095.79m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "29.11.2021"),
                P(ALV, URS, "Dilber Güney", "Yönetici", "İnsan Kaynakları", "Bordro ve Özlük İşleri Yöneticisi", "Kurumsal Hizmetler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 80000.00m, yol: 3650.10m, avans: 13000.00m, bes: 3876.00m, brut: 129200.18m, isveren: 30685.05m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "23.11.2020"),
                P(ALV, IST, "Dilek Güden", "Yönetici", "Muhasebe", "Muhasebe Yöneticisi", "Mali İşler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 90000.00m, yol: 3650.10m, brut: 143225.42m, isveren: 34016.04m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "01.09.2022"),
                P(ALV, URS, "Berkan Özkan", "Müdür", "İhracat", "İhracat Müdürü", "Satış ve Pazarlama", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 97801.60m, x: 0.01m, yol: 3650.10m, primX: 66793.28m, brut: 157711.98m, isveren: 37456.59m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "01.08.2022"),
                P(ALV, URS, "Efe Akçakale", "Uzman Yardımcısı", "Teknik Hizmetler", "Teknik Servis Personeli", "Satış ve Pazarlama", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,428.37 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 35000.00m, yemek: 7500.22m, yol: 3650.10m, bes: 1525.00m, sgkDisi: 7500.22m, brut: 58344.31m, isveren: 12075.46m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "24.06.2024"),
                P(ALV, COR, "Emre Can Yavuz", "Uzman", "Üretim", "Otomasyon Uzmanı", "Üretim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 54000.01m, kira: 9100.00m, bes: 2801.00m, brut: 93381.07m, isveren: 22177.99m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "18.10.2021"),
                P(ALV, URS, "Erman Kaval", "Uzman", "Yurtdışı Şube", "Yurtdışı Şube - Genel Müdür", "Satış ve Pazarlama", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 28100.00m, bes: 1040.00m, brut: 34668.66m, isveren: 8233.80m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "01.02.2021", ozelKisi: true, dk: "D60", da: "Satış Departmanı", sicil: "R-395", sgkNo: "2601200610125"),
                P(ALV, IST, "Eser Şenürek", "Uzman", "Ar-Ge", "Ar-Ge Uzmanı", "Üretim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 70000.00m, yol: 3650.10m, brut: 110594.21m, isveren: 26266.13m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "11.06.2019"),
                P(ALV, COR, "Görkem Akbulut", "Uzman", "Ar-Ge", "Ar-Ge Uzmanı", "Üretim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 47000.00m, brut: 67112.95m, isveren: 15939.33m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "04.07.2024"),
                P(ALV, URS, "Cafer İpek", "Müdür", "Teknik Hizmetler", "İnşaat Mühendisi", "Satış ve Pazarlama", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Ödeme Yapılmıyor", r: 5333.33m, x: 114666.67m, brut: 5765.76m, isveren: 1427.03m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "03.05.2025"),
                P(ALV, COR, "Kamil Mahmudov", "Personel", "Üretim", "Üretim Personeli", "Üretim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 51000.01m, yol: 3650.10m, brut: 79594.54m, isveren: 18903.70m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m),
                P(ALV, URS, "Mustafa Budak", "Uzman", "İhracat", "İhracat Operasyon Uzmanı", "Tedarik Zinciri ve Operasyonlar", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 58500.00m, yol: 3650.10m, brut: 93589.02m, isveren: 22227.39m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "04.08.2025"),
                P(ALV, URS, "Mustafa Savaş", "Uzman", "Yurtdışı Şube", "Yurtdışı Şube - Sorumlusu", "Satış ve Pazarlama", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 28100.00m, bes: 1040.00m, brut: 34668.66m, isveren: 8233.80m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "01.04.2022", ozelKisi: true, dk: "D21", da: "Finans Departmanı", sicil: "R-400", sgkNo: "3401201908589"),
                P(ALV, IST, "Nevruz Sarıtaş", "Uzman", "Muhasebe", "Muhasebe Uzmanı", "Mali İşler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 52000.00m, yol: 3650.10m, brut: 81226.12m, isveren: 19291.21m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "25.10.2021"),
                P(ALV, URS, "Dinçer Özdemir", "Personel", "İdari İşler", "Sevkiyat Şoförü", "Kurumsal Hizmetler", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 4266.67m, x: 41733.32m, yolX: 1095.03m, mesaiX: 613.33m, icra: 1066.67m, brut: 4612.62m, isveren: 1141.62m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "24.03.2023"),
                P(ALV, COR, "Savaş Özcan", "Uzman", "Üretim", "Üretim ve Proses Kontrol Sorumlusu", "Üretim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 68000.01m, kira: 25000.00m, brut: 142164.75m, isveren: 33764.13m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "02.04.2019"),
                P(ALV, COR, "Semih Evirgen", "Yönetici", "Kalite Güvence", "Uyumluluk ve Çevre Yöneticisi", "Üretim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 65000.01m, brut: 96481.05m, isveren: 22914.24m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "04.01.2021"),
                P(ALV, COR, "Soner İkizoğlu", "Teknisyen", "Teknik Hizmetler", "Elektrik Teknisyeni", "Satış ve Pazarlama", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 58500.00m, brut: 85875.91m, isveren: 20395.53m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "04.07.2024"),
                P(ALV, COR, "Şirin Tunç", "Personel", "İdari İşler", "Servis Personeli", "Kurumsal Hizmetler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 32000.00m, brut: 38866.47m, isveren: 9230.79m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "24.06.2024"),
                P(URS, URS, "Erdoğan Polat", "Uzman", "Satış", "İç Piyasa Satış Uzmanı – Emprime", "Satış ve Pazarlama", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 4133.33m, x: 26866.67m, yemek: 7500.22m, primX: 139037.06m, sgkDisi: 7500.22m, brut: 11968.69m, isveren: 1105.95m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "31.08.2023"),
                P(ALV, URS, "Yalçın Avcı", "Uzman", "Depo ve Sevkiyat", "Stok Kontrol Uzmanı", "Tedarik Zinciri ve Operasyonlar", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı. Mesai: bordro 19,660.30 / Personel Kazanç 12,050.00 farklı; Personel Kazanç kullanıldı.", r: 45000.00m, yol: 3650.10m, mesai: 12050.00m, avans: 10000.00m, brut: 91755.84m, isveren: 21792.02m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "18.10.2021"),
                P(URS, HLD, "Ertan Yavuz", "Teknisyen", "İdari İşler", "Elektrik Teknisyeni", "Kurumsal Hizmetler", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 4266.67m, x: 55733.32m, mesaiX: 9600.00m, brut: 4612.62m, isveren: 1141.62m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "30.03.2023"),
                P(URS, URS, "Ayfer Şahin", "Yönetici", "Muhasebe", "Muhasebe Yöneticisi", "Mali İşler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 90000.00m, yol: 3650.10m, brut: 143225.42m, isveren: 34016.04m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "01.04.2023"),
                P(ALV, URS, "Devrim / Hale Bisren", "Yönetim Özel", "Yönetim Özel", "Yönetim Temsilcisi", "Genel Yönetim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. X-NET Devrim Bisren (Selvi), R-NET ve bordro Hale Bisren (ALV Kimya) ait; iki kişi tek satırda.", r: 28100.00m, x: 286900.00m, brut: 34668.66m, isveren: 8233.80m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, ozelKisi: true, dk: "D90", da: "Üst Yönetim Departmanı", sicil: "R-398", sgkNo: "3501201256042"),
                P(URS, URS, "Azad Shirinov", "Personel", "Üretim", "Üretim Personeli", "Üretim", "Gündelikçi", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Ödeme Yapılmıyor", r: 39180.35m, brut: 49545.00m, isveren: 11766.93m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m),
                // Cüneyt Arda Ceylan: kaynak tabloda ALV Kimya (7.000) + Uras Kimya (46.666,68) + Uras Holding (4.666,67) olarak üç satırdı; kullanıcı isteğiyle (1 Kas 2026) tek satırda toplandı: R-Net 58.333,35 · Brüt Masraflar 24.554,82 · İş Veren 19.685,93
                P(URS, HLD, "Cüneyt Arda Ceylan", "Yönetici", "Hukuk", "Avukat", "Kurumsal Hizmetler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Üç firmadan bordro: gün ve tutarlar toplandı; kümülatif matrah boş bırakıldı.", r: 58333.35m, brut: 82888.17m, isveren: 19685.93m, primGun: 25.00m, normalGun: 25.00m, haftaSonuGun: 0.00m, giris: "21.03.2019"),
                P(URS, URS, "Elman Abbaslı", "Personel", "Üretim", "Üretim Personeli", "Üretim", "Gündelikçi", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Ödeme Yapılmıyor", r: 39180.35m, brut: 49545.00m, isveren: 11766.93m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m),
                P(URS, URS, "Elmin Jabbarov", "Personel", "Üretim", "Üretim Personeli", "Üretim", "Gündelikçi", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Ödeme Yapılmıyor", r: 39180.35m, brut: 49545.00m, isveren: 11766.93m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m),
                P(ALV, URS, "İsmail Duman", "Personel", "İdari İşler", "Sevkiyat Şoförü", "Kurumsal Hizmetler", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 4266.67m, x: 41733.33m, brut: 4612.62m, isveren: 1141.62m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "23.01.2024"),
                P(URS, "URS Makine", "Joao Paulo Araujo Fernandes", "Genel Müdür", "Üst Yönetim", "Genel Müdür", "Genel Yönetim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 67156.80m, x: 211001.50m, brut: 100000.00m, isveren: 23750.00m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "11.02.2023"),
                P(URS, "Diğer", "Medine Rozykulova", "Yönetim Özel", "Yönetim Özel", "Ev Çalışanı", "Genel Yönetim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 28500.00m, x: 63571.04m, yolX: 5000.00m, bes: 1121.00m, brut: 37378.15m, isveren: 8877.31m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "26.09.2025", ozelKisi: true, dk: "D90", da: "Üst Yönetim Departmanı", sicil: "R-608", sgkNo: "3402201501787"),
                P(URS, URS, "Mehman Babayev", "Personel", "Üretim", "Üretim Personeli", "Üretim", "Gündelikçi", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Ödeme Yapılmıyor", r: 39180.35m, brut: 49545.00m, isveren: 11766.93m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m),
                P(URS, URS, "Murad Hudaybergenov", "Personel", "Üretim", "Üretim Personeli", "Üretim", "Gündelikçi", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Ödeme Yapılmıyor", r: 39180.35m, brut: 49545.00m, isveren: 11766.93m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m),
                P(SLV, "Asia", "Mesut Akıncı", "Uzman", "Satış", "İç Piyasa Satış Uzmanı", "Satış ve Pazarlama", "", "Bordro kaydı yok: puantaj, brüt, vergi ve SGK alanları boş; maliyet yalnızca net ödemeler kadar (alt sınır). Ödeme Yapılmıyor", x: 15000.00m, primGun: 0.00m, normalGun: 0.00m, haftaSonuGun: 0.00m, sgkYok: true, giris: "16.02.2022"),
                P(URS, URS, "Rahman Babazade", "Personel", "Üretim", "Üretim Personeli", "Üretim", "Gündelikçi", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Ödeme Yapılmıyor", r: 39180.35m, brut: 49545.00m, isveren: 11766.93m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m),
                P(URS, URS, "Ramin Musayev", "Personel", "Üretim", "Üretim Personeli", "Üretim", "Gündelikçi", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Ödeme Yapılmıyor", r: 39180.35m, brut: 49545.00m, isveren: 11766.93m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m),
                P(SLV, SLV, "Murat Özavşar", "Yönetim Özel", "Yönetim Özel", "Yönetim Temsilcisi", "Genel Yönetim", "", "Bordro kaydı yok: puantaj, brüt, vergi ve SGK alanları boş; maliyet yalnızca net ödemeler kadar (alt sınır).", x: 277000.00m, primGun: 0.00m, normalGun: 0.00m, haftaSonuGun: 0.00m, sgkYok: true, giris: "06.01.2023", ozelKisi: true, dk: "D90", da: "Üst Yönetim Departmanı", sicil: "", sgkNo: ""),
                P(URS, URS, "Tural Abbasov", "Personel", "Üretim", "Üretim Personeli", "Üretim", "Gündelikçi", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Ödeme Yapılmıyor", r: 39180.35m, brut: 49545.00m, isveren: 11766.93m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m),
                P(URS, URS, "Vu Thi Thuy Ha", "Kıdemli Uzman", "Satınalma", "Satınalma Kıdemli Uzmanı", "Tedarik Zinciri ve Operasyonlar", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 46930.78m, x: 64330.40m, brut: 67000.00m, isveren: 15912.50m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "08.03.2023"),
                P(AVP, URS, "Ahmet Özkan", "Uzman", "Finans", "Finans Uzmanı", "Mali İşler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 75000.00m, yol: 3650.10m, bes: 3631.00m, brut: 121042.36m, isveren: 28747.56m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "02.10.2024"),
                P(AVP, URS, "Bahar Seyrekoğlu", "Uzman Yardımcısı", "İdari İşler", "Danışma Personeli", "Kurumsal Hizmetler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı. Mesai: bordro 7,396.40 / Personel Kazanç 4,533.33 farklı; Personel Kazanç kullanıldı.", r: 40000.00m, yol: 3650.10m, mesai: 4533.33m, bes: 2066.00m, brut: 68868.37m, isveren: 16356.24m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "04.07.2023"),
                P(AVP, URS, "Berk Berberoğlu", "Uzman", "Finans", "Finans Uzmanı", "Mali İşler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 65000.00m, yol: 3650.10m, avans: 5000.00m, bes: 3141.00m, brut: 104726.76m, isveren: 24872.61m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "25.09.2023"),
                P(AVP, URS, "Burak Kaya", "Personel", "İdari İşler", "Finans Ulaşım", "Kurumsal Hizmetler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 53000.00m, yol: 3650.10m, avans: 13300.00m, bes: 2554.00m, brut: 85148.01m, isveren: 20222.65m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "01.07.2025"),
                P(AVP, URS, "Can Gürsel", "Uzman", "Depo ve Sevkiyat", "Sevkiyat Uzmanı", "Tedarik Zinciri ve Operasyonlar", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,915.26 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 37000.00m, yol: 3650.10m, bes: 1629.00m, brut: 54305.34m, isveren: 12897.52m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "15.08.2022"),
                P(AVP, URS, "Eda Bozkurt", "Uzman", "Satınalma", "İthalat Uzmanı", "Tedarik Zinciri ve Operasyonlar", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 67000.00m, yol: 3650.10m, bes: 3239.00m, brut: 107989.87m, isveren: 25647.60m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "09.05.2022"),
                P(AVP, URS, "Emrah Ateş", "Uzman", "Depo ve Sevkiyat", "Sevkiyat Uzmanı", "Tedarik Zinciri ve Operasyonlar", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı. Mesai: bordro 1,359.63 / Personel Kazanç 833.33 farklı; Personel Kazanç kullanıldı.", r: 50000.00m, yol: 3650.10m, mesai: 833.33m, bes: 2448.00m, brut: 81612.97m, isveren: 19383.08m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "01.06.2022"),
                P(AVP, URS, "Emre Keskin", "Uzman", "Teknik Hizmetler", "Teknik Servis Renk Uzmanı", "Satış ve Pazarlama", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 70000.00m, yemek: 7500.22m, bes: 3207.00m, sgkDisi: 7500.22m, brut: 114429.43m, isveren: 25395.69m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "21.03.2019"),
                P(AVP, URS, "Ezgi Demir", "Kıdemli Uzman", "Kalite Kontrol", "Kalite Kontrol Kıdemli Uzmanı", "Üretim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı. Mesai: bordro 5,373.26 / Personel Kazanç 3,293.33 farklı; Personel Kazanç kullanıldı.", r: 52000.00m, yol: 3650.10m, mesai: 3293.33m, bes: 2666.00m, brut: 88889.71m, isveren: 21111.31m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "07.05.2019"),
                P(AVP, URS, "Hacı Mustafa Korkmaz", "Uzman", "Ar-Ge", "Ar-Ge Uzmanı", "Üretim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 55000.00m, yol: 3650.10m, bes: 2652.00m, brut: 88411.14m, isveren: 20997.66m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "04.07.2022"),
                P(AVP, URS, "Hakan Haydut", "Uzman", "Muhasebe", "Muhasebe Uzmanı", "Mali İşler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 55000.00m, yol: 3650.10m, bes: 2652.00m, brut: 88411.14m, isveren: 20997.66m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "22.07.2024"),
                P(AVP, URS, "İdil Akel", "Uzman", "İnsan Kaynakları", "İnsan Kaynakları Uzmanı", "Kurumsal Hizmetler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 55000.00m, yol: 3650.10m, bes: 2652.00m, brut: 88411.14m, isveren: 20997.66m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "16.06.2025"),
                P(AVP, URS, "Melike Ural Kaya", "Uzman", "Cari İşler", "Cari Hesap Uzmanı", "Mali İşler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,428.37 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 50000.00m, yol: 3650.10m, bes: 2088.00m, brut: 69610.83m, isveren: 16532.58m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "07.11.2022"),
                P(AVP, URS, "Mustafa Özdeniz", "Uzman", "Teknik Hizmetler", "Teknik Servis Personeli", "Satış ve Pazarlama", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 60000.00m, yemek: 7500.22m, icra: 15000.00m, bes: 2718.00m, sgkDisi: 7500.22m, brut: 98113.82m, isveren: 21520.73m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "21.03.2019"),
                P(AVP, URS, "Olgun Küskü", "Uzman", "Kalite Kontrol", "Kalite Kontrol Uzmanı", "Üretim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı. Mesai: bordro 3,083.64 / Personel Kazanç 1,890.00 farklı; Personel Kazanç kullanıldı.", r: 40500.00m, yol: 3650.10m, mesai: 1890.00m, bes: 2035.00m, brut: 67837.15m, isveren: 16111.32m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "19.08.2020"),
                P(AVP, URS, "Ömer Kantar", "Uzman", "Teknik Hizmetler", "Teknik Servis Personeli", "Satış ve Pazarlama", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 60000.00m, yemek: 7500.22m, bes: 2718.00m, sgkDisi: 7500.22m, brut: 98113.82m, isveren: 21520.73m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "04.12.2020"),
                P(AVP, URS, "Ömer Yıldırım", "Uzman", "Teknik Hizmetler", "Teknik Servis Personeli", "Satış ve Pazarlama", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 60000.00m, yemek: 7500.22m, bes: 2718.00m, sgkDisi: 7500.22m, brut: 98113.82m, isveren: 21520.73m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "05.10.2020"),
                P(AVP, URS, "Özge İsen", "Kıdemli Uzman", "Üst Yönetim", "Proje Kıdemli Uzmanı", "Genel Yönetim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 60000.00m, bes: 2718.00m, brut: 90613.60m, isveren: 21520.73m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "24.11.2023"),
                P(ALV, URS, "Mustafa Ulaş Erikli", "Uzman", "Depo ve Sevkiyat", "Sevkiyat Uzmanı", "Tedarik Zinciri ve Operasyonlar", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 3,946.05 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 4133.33m, x: 38866.67m, yol: 3650.10m, mesaiX: 7740.00m, brut: 8414.52m, isveren: 2082.60m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "20.02.2023"),
                P(AVP, URS, "Serkan Genç", "Yönetici", "Satınalma", "Satınalma Yöneticisi", "Tedarik Zinciri ve Operasyonlar", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 85000.00m, yol: 3650.10m, bes: 4120.00m, brut: 137357.98m, isveren: 32622.52m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "24.01.2023"),
                P(AVP, URS, "Yeşim Karakütük", "Personel", "İdari İşler", "Temizlik Görevlisi", "Kurumsal Hizmetler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,428.37 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 35000.00m, yol: 3650.10m, bes: 1525.00m, brut: 50844.09m, isveren: 12075.46m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "18.11.2024"),
                P(SLV, SLV, "Beyza Gizem Zazoğlu", "Uzman", "Muhasebe", "Muhasebe Uzmanı", "Mali İşler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 68000.00m, yol: 3650.10m, hazine: 5366.55m, brut: 107331.08m, isveren: 25491.14m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "28.08.2024"),
                P(SLV, SLV, "Birol Şeker", "Uzman", "Depo ve Sevkiyat", "Mal Kabul Uzmanı", "Tedarik Zinciri ve Operasyonlar", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,955.35 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 52000.01m, yol: 3650.10m, hazine: 4061.31m, brut: 81226.12m, isveren: 19291.21m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "18.05.2019"),
                P(SLV, SLV, "Seda Saraç", "Personel", "İdari İşler", "Servis Personeli", "Kurumsal Hizmetler", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,428.37 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 30000.00m, yol: 3650.10m, hazine: 2066.63m, brut: 41332.70m, isveren: 9816.52m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m),
                P(URS, URS, "Müjgan Yetgin", "Uzman", "Satış", "İç Piyasa Satış Uzmanı – Emprime", "Satış ve Pazarlama", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 4133.33m, x: 26866.67m, yemek: 7500.22m, primX: 50308.64m, sgkDisi: 7500.22m, brut: 11968.69m, isveren: 1105.95m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "15.04.2022"),
                P(ALV, URS, "Neslihan Uludağ", "Direktör", "Üst Yönetim", "YK Üyesi", "Genel Yönetim", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 5333.33m, x: 384666.67m, brut: 5765.76m, isveren: 1427.03m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "29.03.2023"),
                P("Alv Filo", URS, "Nükhet Özavşar", "Yönetim Özel", "Yönetim Özel", "Yönetim Temsilcisi", "Genel Yönetim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 180000.00m, x: 470000.00m, icra: 45000.00m, brut: 318366.47m, isveren: 70601.63m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "11.01.2023", ozelKisi: true, dk: "D90", da: "Üst Yönetim Departmanı", sicil: "R-003", sgkNo: "3501199571976"),
                P(ALV, COR, "Özgür Pamir", "Direktör", "Üretim", "Dijital Üretim Direktörü", "Üretim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 100000.01m, x: 140000.00m, brut: 153585.69m, isveren: 36476.60m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "03.08.2016"),
                P(ALV, URS, "Rabiye Doğan", "Personel", "İdari İşler", "Temizlik Görevlisi", "Kurumsal Hizmetler", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 3,946.05 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 4266.67m, x: 30733.33m, yol: 3650.10m, brut: 8558.67m, isveren: 2118.27m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "22.01.2024"),
                P(URS, URS, "Refik Polat", "Yönetici", "Depo ve Sevkiyat", "Depo Yöneticisi", "Tedarik Zinciri ve Operasyonlar", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 4133.33m, x: 70866.67m, mesaiX: 23083.33m, brut: 4468.47m, isveren: 1105.95m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "29.03.2023"),
                P(ALV, COR, "Selçuk Mert", "Müdür", "Üretim", "Üretim Müdürü", "Üretim", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 5333.34m, x: 129666.67m, brut: 5765.76m, isveren: 1427.03m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "06.09.2024"),
                P(AVP, URS, "Serhat Mutlu", "Uzman", "Satış", "İç Piyasa Satış Uzmanı – Dijital", "Satış ve Pazarlama", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 60000.00m, x: 0.01m, yemek: 7500.22m, primX: 41397.86m, bes: 2718.00m, sgkDisi: 7500.22m, brut: 98113.82m, isveren: 21520.73m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "21.03.2019"),
                P(AVP, URS, "Sevgi Kaya", "Müdür", "İhracat", "İhracat Müdürü", "Satış ve Pazarlama", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 5,710.63 / Personel Kazanç 3,500.10 farklı; Personel Kazanç kullanıldı.", r: 122252.00m, x: 0.01m, yol: 3500.10m, primX: 14906.00m, brut: 197892.16m, isveren: 46999.38m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "19.11.2022"),
                P(ALV, COR, "Ümit Özdemir", "Uzman", "Depo ve Sevkiyat", "Sevkiyat Uzmanı", "Tedarik Zinciri ve Operasyonlar", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 4266.67m, x: 50733.33m, brut: 4612.62m, isveren: 1141.62m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "20.11.2024"),
                P(ALV, URS, "Yusuf Öztürk", "Uzman", "Satış", "İç Piyasa Satış Uzmanı – Emprime", "Satış ve Pazarlama", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi.", r: 31000.00m, x: 0.01m, yemek: 7500.22m, primX: 48120.57m, sgkDisi: 7500.22m, brut: 46481.73m, isveren: 9258.11m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "20.08.2022"),
                P(ALV, URS, "Zahit Ercan", "Personel", "İdari İşler", "Temizlik Görevlisi", "Kurumsal Hizmetler", "Emekli", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 3,946.05 / Personel Kazanç 3,650.10 farklı; Personel Kazanç kullanıldı.", r: 4266.67m, x: 35733.32m, yol: 3650.10m, brut: 8558.67m, isveren: 2118.27m, primGun: 4.00m, normalGun: 4.00m, haftaSonuGun: 0.00m, giris: "01.09.2021"),
                P(SLV, "Diğer", "Orazgül Jumayeva", "Yönetim Özel", "Yönetim Özel", "Ev Çalışanı", "Genel Yönetim", "", "Bordro kaydı yok: puantaj, brüt, vergi ve SGK alanları boş; maliyet yalnızca net ödemeler kadar (alt sınır).", x: 63571.04m, yolX: 2000.00m, primGun: 0.00m, normalGun: 0.00m, haftaSonuGun: 0.00m, sgkYok: true, giris: "13.08.2025", ozelKisi: true, dk: "D90", da: "Üst Yönetim Departmanı", sicil: "", sgkNo: ""),
                P(SLV, "Diğer", "Alper Köse", "Yönetim Özel", "Yönetim Özel", "Yatırım", "Genel Yönetim", "", "Bordro kaydı yok: puantaj, brüt, vergi ve SGK alanları boş; maliyet yalnızca net ödemeler kadar (alt sınır). Kaynak kazanç/kesinti raporundan eklendi; organizasyon bilgileri kullanıcı talebine göre güncellendi.", x: 389414.91m, primGun: 0.00m, normalGun: 0.00m, haftaSonuGun: 0.00m, sgkYok: true, ozelKisi: true, dk: "D90", da: "Üst Yönetim Departmanı", sicil: "", sgkNo: "", durum: "İşten Ayrıldı", ayrilis: "30.09.2026"),
                P(AVP, URS, "Bedirhan Genç", "Kıdemli Uzman", "Kalite Güvence", "Kalite Sistem Kıdemli Uzmanı", "Üretim", "", "Bordro ve Personel Kazanç raporu ile kontrol edildi. Yol: bordro 8,157.96 / Personel Kazanç 5,000.10 farklı; Personel Kazanç kullanıldı.", r: 78500.00m, yol: 5000.10m, bes: 3868.00m, brut: 128955.45m, isveren: 30626.91m, primGun: 30.00m, normalGun: 30.00m, haftaSonuGun: 0.00m, giris: "24.11.2025", dk: "D33", da: "Kalite Kontrol Departmanı", sicil: "R-2", sgkNo: "1701201203599"),
                P(SLV, "Diğer", "Emine Tanış", "SMMM", "Muhasebe", "Mali Müşavir", "Mali İşler", "", "Bordro kaydı yok: puantaj, brüt, vergi ve SGK alanları boş; maliyet yalnızca net ödemeler kadar (alt sınır). Kaynak kazanç/kesinti raporundan eklendi; SMMM rolüne uygun organizasyon bilgileri kullanıcı talebine göre güncellendi.", x: 4000.00m, primGun: 0.00m, normalGun: 0.00m, haftaSonuGun: 0.00m, sgkYok: true, ozelKisi: true, dk: "D90", da: "Üst Yönetim Departmanı", sicil: "", sgkNo: "")
            };
        }
    }
}
