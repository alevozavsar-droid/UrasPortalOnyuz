// ============================================================
// BELGE GÖRÜNTÜSÜ (PDF) — Kasa Nakit Akışı › belge no tıklanınca açılan pencerenin sağ yanı (13 Kas 2026: "fatura numarasına tıkladığımda hem kayıt detaylarını hem de fatura PDF görüntüsünü göreyim").
//   Satıra belge aslı ("Fatura" eki) yüklendiyse /Rapor79/FaturaPdf onu döndürür; yüklenmediyse bu sınıf sistem kaydından (NakitAkisOrnek.BelgeDetay: başlık, kalemler, toplamlar, süreç)
//   A4 dikey bir belge görüntüsü üretir (QuestPDF, Community lisansı). Görüntünün altında "sistem kaydından üretilmiştir, belge aslı değildir" notu yer alır.
//   Fatura / Sipariş: kalem tablosu + KDV + genel toplam · Cari Bakiye: açık kalemler dökümü · Mahsup / Temlik / Yansıtma / Avans: tek kalemli belge özeti.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebApplication3.Data
{
    public static class FaturaGoruntu
    {
        static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        const string Koyu = "#0f172a", Gri = "#64748b", Cizgi = "#cbd5e1", Zemin = "#f1f5f9", Mavi = "#1d4ed8";
        static FaturaGoruntu() { QuestPDF.Settings.License = LicenseType.Community; QuestPDF.Settings.EnableDebugging = false; }

        static string S(object o) => o == null ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);
        static decimal D(object o) { try { return o == null ? 0 : Convert.ToDecimal(o, CultureInfo.InvariantCulture); } catch { return 0; } }
        static string Para(decimal v) => v.ToString("N2", Tr);
        static string Tarih(string s) => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t.ToString(s != null && s.Length > 10 ? "dd.MM.yyyy HH:mm" : "dd.MM.yyyy", Tr) : "—";
        static string PbIsaret(string pb) => pb == "USD" ? "$" : pb == "EUR" || pb == "EURO" ? "€" : pb == "GBP" ? "£" : "₺";

        /// <summary><paramref name="detay"/> = NakitAkisOrnek.BelgeDetay sonucu (anonim nesne; alanlar dinamik okunur).</summary>
        public static byte[] Uret(object detay)
        {
            dynamic d = detay; dynamic s = d.surec;
            string tip = S(d.tip), no = S(d.no), kurum = S(d.kurum), sahip = S(d.faturaSahibi), odeyen = S(d.odeyenSirket), urun = S(d.urun), satinalmaci = S(d.satinalmaci), pb = S(d.pb), sirket = S(d.sirket);
            string tarih = Tarih(S(d.tarih)), sonOdeme = Tarih(S(d.sonOdeme)); int docEntry = Convert.ToInt32((object)d.docEntry);
            decimal net = D(d.net), kdv = D(d.kdv), toplam = D(d.belgeTutari), toplamTL = D(d.belgeTutariTL), kur = D(d.kur); string isaret = PbIsaret(pb); bool dovizli = !string.IsNullOrEmpty(pb) && pb != "TRY" && pb != "TL";
            var kalemler = new List<(string kod, string ad, decimal miktar, string birim, decimal fiyat, decimal tutar, decimal kdv)>();
            foreach (object k in (IEnumerable<object>)d.kalemler) { dynamic x = k; string kod = S(x.kod), ad = S(x.ad), birim = S(x.birim); decimal miktar = D(x.miktar), fiyat = D(x.birimFiyat), tutar = D(x.tutar), kk = D(x.kdv); kalemler.Add((kod, ad, miktar, birim, fiyat, tutar, kk)); }
            var acik = new List<(string belge, string no, string tarih, string vade, decimal tutar, decimal odenen, decimal kalan)>();
            if (d.acikKalemler != null) foreach (object k in (IEnumerable<object>)d.acikKalemler) { dynamic x = k; string belge = S(x.belge), bno = S(x.no), bt = Tarih(S(x.tarih)), bv = Tarih(S(x.vade)); decimal tutar = D(x.tutar), odenen = D(x.odenen), kalan = D(x.kalan); acik.Add((belge, bno, bt, bv, tutar, odenen, kalan)); }
            decimal cariBakiye = D(d.cariBakiye);
            string talepEden = S(s.talepEden), yukleyen = S(s.yukleyen), yuklemeT = Tarih(S(s.yuklemeTarihi)), onaylayan = S(s.belgeOnaylayan), onayT = Tarih(S(s.belgeOnayTarihi)), onayDurum = S(s.belgeOnayDurum), odemeOnay = S(s.odemeOnaylayan), odemeOnayDurum = S(s.odemeOnayDurum);
            bool ornek = (bool)s.ornek;
            string baslik = tip == "Fatura" ? "FATURA" : tip == "Sipariş" ? "SATINALMA SİPARİŞİ" : tip == "Cari Bakiye" ? "CARİ BAKİYE DÖKÜMÜ" : tip == "Avans" ? "AVANS BELGESİ" : string.IsNullOrEmpty(tip) ? "BELGE" : tip.ToUpper(Tr) + " BELGESİ";

            return Document.Create(c => c.Page(p =>
            {
                p.Size(PageSizes.A4); p.Margin(28); p.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(Koyu));
                p.Header().Column(h =>
                {
                    h.Item().Row(r =>
                    {
                        r.RelativeItem().Column(k => { k.Item().Text(kurum).FontSize(14).Bold(); k.Item().Text(tip == "Cari Bakiye" ? "Cari hesap" : "Düzenleyen (ödenecek cari)").FontSize(7.5f).FontColor(Gri); });
                        r.ConstantItem(190).Border(1).BorderColor(Cizgi).Padding(6).Column(k =>
                        {
                            k.Item().Text(baslik).FontSize(12).Bold().FontColor(Mavi);
                            Satir(k, "Belge No", string.IsNullOrEmpty(no) ? "—" : no); Satir(k, "Belge Tarihi", tarih); Satir(k, "Son Ödeme Tarihi", sonOdeme); Satir(k, "Para Birimi", string.IsNullOrEmpty(pb) ? "TRY" : pb); if (dovizli) Satir(k, "Kur", kur.ToString("N4", Tr));
                        });
                    });
                    h.Item().PaddingTop(8).LineHorizontal(1).LineColor(Cizgi);
                });
                p.Content().PaddingTop(10).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Background(Zemin).Padding(7).Column(k => { k.Item().Text("SAYIN (FATURA SAHİBİ)").FontSize(7).Bold().FontColor(Gri); k.Item().Text(sahip).FontSize(11).Bold(); if (!string.IsNullOrEmpty(odeyen) && odeyen != sahip) k.Item().Text("Ödeyen şirket: " + odeyen).FontSize(8).FontColor(Mavi); });
                        r.ConstantItem(10);
                        r.RelativeItem().Background(Zemin).Padding(7).Column(k => { k.Item().Text("KONU").FontSize(7).Bold().FontColor(Gri); k.Item().Text(string.IsNullOrEmpty(urun) ? "—" : urun).FontSize(9.5f); if (!string.IsNullOrEmpty(satinalmaci)) k.Item().Text("Satınalma temsilcisi: " + satinalmaci).FontSize(8).FontColor(Gri); });
                    });
                    if (acik.Count > 0)
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(2); cd.RelativeColumn(2); cd.RelativeColumn(2); cd.RelativeColumn(2); cd.RelativeColumn(2); cd.RelativeColumn(2); });
                            t.Header(hd => { foreach (var b in new[] { "Belge", "No", "Tarih", "Vade", "Tutar (₺)", "Ödenen (₺)", "Kalan (₺)" }) { var h = hd.Cell().Element(Bas); (b.EndsWith(")") ? h.AlignRight() : h).Text(b); } });
                            foreach (var a in acik) { t.Cell().Element(Hucre).Text(a.belge); t.Cell().Element(Hucre).Text(a.no); t.Cell().Element(Hucre).Text(a.tarih); t.Cell().Element(Hucre).Text(a.vade); t.Cell().Element(HucreSag).Text(Para(a.tutar)); t.Cell().Element(HucreSag).Text(Para(a.odenen)); t.Cell().Element(HucreSag).Text(Para(a.kalan)); }
                        });
                    else
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.ConstantColumn(58); cd.RelativeColumn(5); cd.ConstantColumn(52); cd.ConstantColumn(44); cd.ConstantColumn(64); cd.ConstantColumn(74); cd.ConstantColumn(62); });
                            t.Header(hd => { foreach (var b in new[] { "Kod", "Açıklama", "Miktar", "Birim", "Birim Fiyat", "Tutar", "KDV" }) { var h = hd.Cell().Element(Bas); (b == "Kod" || b == "Açıklama" || b == "Birim" ? h : h.AlignRight()).Text(b); } });
                            foreach (var k in kalemler) { t.Cell().Element(Hucre).Text(k.kod); t.Cell().Element(Hucre).Text(k.ad); t.Cell().Element(HucreSag).Text(k.miktar.ToString("N0", Tr)); t.Cell().Element(Hucre).Text(k.birim); t.Cell().Element(HucreSag).Text(k.fiyat.ToString("N4", Tr)); t.Cell().Element(HucreSag).Text(Para(k.tutar)); t.Cell().Element(HucreSag).Text(Para(k.kdv)); }
                            if (kalemler.Count == 0) t.Cell().ColumnSpan(7).Element(Hucre).Text("Kalem yok.").FontColor(Gri);
                        });
                    col.Item().AlignRight().Width(230).Border(1).BorderColor(Cizgi).Padding(7).Column(k =>
                    {
                        if (acik.Count > 0) Satir(k, "Cari Bakiye (₺)", Para(cariBakiye), true);
                        else
                        {
                            Satir(k, "Ara Toplam (KDV Hariç)", Para(net) + " " + isaret); Satir(k, "KDV", Para(kdv) + " " + isaret); Satir(k, "GENEL TOPLAM", Para(toplam) + " " + isaret, true);
                            if (dovizli) Satir(k, "TL Karşılığı", Para(toplamTL) + " ₺");
                        }
                    });
                    col.Item().Border(1).BorderColor(Cizgi).Padding(7).Column(k =>
                    {
                        k.Item().Text("BELGE SÜRECİ").FontSize(7).Bold().FontColor(Gri);
                        Satir(k, "Talep Eden", string.IsNullOrEmpty(talepEden) ? "—" : talepEden);
                        if (!string.IsNullOrEmpty(yukleyen)) Satir(k, "Sisteme Yükleyen", yukleyen + " · " + yuklemeT + (ornek ? " (örnek)" : ""));
                        if (!string.IsNullOrEmpty(onaylayan)) Satir(k, "Belge Onayı", onaylayan + " · " + onayDurum + (onayT == "—" ? "" : " · " + onayT) + (ornek ? " (örnek)" : ""));
                        if (!string.IsNullOrEmpty(odemeOnayDurum)) Satir(k, "Ödeme Onayı", (string.IsNullOrEmpty(odemeOnay) ? "—" : odemeOnay) + " · " + odemeOnayDurum);
                    });
                });
                p.Footer().Column(f =>
                {
                    f.Item().LineHorizontal(1).LineColor(Cizgi);
                    f.Item().PaddingTop(4).Text($"Bu görüntü Kasa Nakit Akışı kaydından üretilmiştir; belge aslı değildir. Belge aslı yüklendiğinde bu pencerede o gösterilir. · {sirket} · kayıt #{docEntry} · {DateTime.Now.ToString("dd.MM.yyyy HH:mm", Tr)}").FontSize(7).FontColor(Gri);
                });
            })).GeneratePdf();
        }

        static void Satir(ColumnDescriptor k, string etiket, string deger, bool kalin = false) => k.Item().PaddingTop(2).Row(r => { r.RelativeItem().Text(etiket).FontSize(kalin ? 9.5f : 8).FontColor(kalin ? Koyu : Gri); var t = r.RelativeItem().AlignRight().Text(deger).FontSize(kalin ? 10.5f : 8.5f); if (kalin) t.Bold(); });
        static IContainer Bas(IContainer c) => c.Background(Koyu).PaddingVertical(4).PaddingHorizontal(5).DefaultTextStyle(x => x.FontColor("#ffffff").FontSize(7.5f).Bold());
        static IContainer Hucre(IContainer c) => c.BorderBottom(1).BorderColor(Cizgi).PaddingVertical(4).PaddingHorizontal(5);
        static IContainer HucreSag(IContainer c) => Hucre(c).AlignRight();
    }
}
