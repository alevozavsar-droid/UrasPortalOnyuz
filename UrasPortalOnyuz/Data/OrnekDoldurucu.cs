using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace WebApplication3
{
    /// <summary>
    /// ÖN YÜZ ÖRNEK VERİ ÜRETİCİSİ — SQL / SAP / dış servis bağlantısı YOKTUR.
    /// Otomatik üretilen denetleyiciler (Uretilen/) görünüm modellerini ve JSON cevaplarını buradan doldurur.
    /// Değerler ALAN ADINA göre seçilir (CardName → firma adı, Tutar → tutar, VadeTarihi → ileri tarih ...) ve
    /// sıra numarasına (i) göre değişir; her açılışta aynı (deterministik) örnekler gelir.
    /// </summary>
    public static class OrnekDoldurucu
    {
        // ------------------------------------------------------------------ örnek havuzlar
        static readonly string[] Firmalar =
        {
            "AKSA TEKSTİL SAN. VE TİC. A.Ş.", "DENİZ BOYA KİMYA LTD. ŞTİ.", "EGE MAKROSEL AMBALAJ A.Ş.", "KARADENİZ EMPRİME SAN. LTD. ŞTİ.",
            "MAVİ ÖRME KUMAŞ SAN. TİC. A.Ş.", "ANADOLU LOJİSTİK A.Ş.", "YILDIZ DİJİTAL BASKI LTD. ŞTİ.", "BORA KİMYA SANAYİ A.Ş.",
            "TOROS AMBALAJ SAN. VE TİC. LTD. ŞTİ.", "MARMARA TEKSTİL BOYA A.Ş.", "KUZEY PETROL ÜRÜNLERİ A.Ş.", "ATLAS MAKİNE SAN. LTD. ŞTİ.",
            "GÜNEŞ ENERJİ ELEKTRİK A.Ş.", "ÇINAR OFİS MALZEMELERİ LTD. ŞTİ.", "LİMAN GÜMRÜK MÜŞAVİRLİĞİ A.Ş.", "SEDEF DERİ KONFEKSİYON LTD. ŞTİ."
        };
        static readonly string[] Kisiler = { "Ahmet Yılmaz", "Ayşe Demir", "Mehmet Kaya", "Elif Şahin", "Mustafa Çelik", "Zeynep Arslan", "Emre Koç", "Selin Aydın", "Burak Öztürk", "Deniz Kurt", "Can Polat", "Merve Güneş" };
        static readonly string[] Bankalar = { "Türkiye Garanti Bankası A.Ş.", "Türkiye İş Bankası A.Ş.", "Yapı ve Kredi Bankası A.Ş.", "Akbank T.A.Ş.", "Türkiye Halk Bankası A.Ş.", "T.C. Ziraat Bankası A.Ş.", "QNB Bank A.Ş.", "Denizbank A.Ş." };
        static readonly string[] Subeler = { "MERKEZ ŞB.", "İKİTELLİ ŞB.", "BURSA OSB ŞB.", "ÇORLU ŞB.", "AVCILAR ŞB.", "HADIMKÖY ŞB." };
        static readonly string[] Sehirler = { "İstanbul", "Bursa", "İzmir", "Ankara", "Tekirdağ", "Kocaeli", "Denizli", "Gaziantep" };
        static readonly string[] Ilceler = { "Başakşehir", "Nilüfer", "Çiğli", "Ostim", "Çorlu", "Gebze", "Merkezefendi", "Şehitkamil" };
        static readonly string[] Durumlar = { "Açık", "Kapalı", "Onaylandı", "Beklemede" };
        static readonly string[] Turler = { "Fatura", "Tahsilat", "Ödeme", "İade" };
        static readonly string[] HesapAdlari = { "Alıcılar", "Satıcılar", "Bankalar", "Kasa", "Yurt İçi Satışlar", "Genel Yönetim Giderleri", "İndirilecek KDV", "Hesaplanan KDV" };
        static readonly string[] Kalemler = { "S 20 WHITE (40 Kg)", "DM 10 CLEAR (20 Kg)", "CYAN MASTER (1 Kg)", "MAGENTA MASTER (1 Kg)", "PV 2000 FOIL ADHESIVE (30 Kg)", "A 25 FIXATOR (1 Kg)", "DTF TRANSFER POWDER", "FLUOR RUBIN (5 Kg)" };
        static readonly string[] KalemKodlari = { "UR02.S0.W20.40", "UR03.D1.MC0.20", "T07.JTM.CYA.01", "T07.JTM.MAG.01", "UR02.VT.PV2.30", "URB01.FX.000.01", "T07.DTF.TPW.00", "UB02.RB.FLO.05" };
        static readonly string[] Aylar = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
        static readonly string[] ParaBirimleri = { "TRY", "TRY", "USD", "TRY", "EUR" };

        static string Sec(string[] a, int i) => a[((i % a.Length) + a.Length) % a.Length];
        static string Kucuk(string s) => (s ?? "").Replace("İ", "i").Replace("I", "ı").ToLowerInvariant().Replace("ı", "i").Replace("ş", "s").Replace("ğ", "g").Replace("ü", "u").Replace("ö", "o").Replace("ç", "c").Replace("_", "").Replace(" ", "");
        static bool Has(string ad, params string[] k) => k.Any(x => ad.Contains(x));
        static int Tohum(string ad, int i) { unchecked { int h = 17; foreach (var c in ad ?? "") h = h * 31 + c; return Math.Abs((h * 7 + i * 7919) % 1000003); } }

        // ------------------------------------------------------------------ alan adına göre değer
        public static T Deger<T>(string ad, int i) { var v = DegerTip(typeof(T), ad, i); return v is T t ? t : default; }

        public static object DegerTip(Type t, string ad, int i)
        {
            var u = Nullable.GetUnderlyingType(t); if (u != null) t = u;
            string a = Kucuk(ad);
            if (t == typeof(string)) return Metin(a, i);
            if (t == typeof(DateTime)) return Tarih(a, i);
            if (t == typeof(DateTimeOffset)) return new DateTimeOffset(Tarih(a, i));
            if (t == typeof(bool)) return Has(a, "iptal", "cancel", "hata", "error", "red", "sil", "kilit", "lock", "mukerrer") ? i % 7 == 3 : Has(a, "aktif", "active", "valid", "success", "basari", "var", "onay") ? true : i % 3 == 0;
            if (t == typeof(decimal)) return Sayi(a, i);
            if (t == typeof(double)) return (double)Sayi(a, i);
            if (t == typeof(float)) return (float)Sayi(a, i);
            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte))
            {
                long v = Has(a, "yil", "year") ? 2026 : Has(a, "ay", "month") && a.Length <= 6 ? (i % 12) + 1 : Has(a, "gun", "day") ? (i % 28) + 1
                       : Has(a, "line", "satir", "index", "sira") ? i : Has(a, "adet", "count", "sayi", "miktar", "qty") ? 3 + (Tohum(a, i) % 40)
                       : Has(a, "id", "entry", "num", "no", "key", "abs", "kod", "code") ? 1000 + i : i + 1;
                if (t == typeof(byte)) v = v % 250;
                return Convert.ChangeType(v, t);
            }
            if (t == typeof(Guid)) return new Guid(i + 1, 0x2026, 0x0918, 1, 2, 3, 4, 5, 6, 7, 8);
            if (t == typeof(TimeSpan)) return new TimeSpan(9 + i % 8, (i * 15) % 60, 0);
            if (t == typeof(char)) return 'A';
            if (t.IsEnum) { var d = Enum.GetValues(t); return d.Length > 0 ? d.GetValue(i % d.Length) : Activator.CreateInstance(t); }
            return null;
        }

        static DateTime Tarih(string a, int i)
        {
            var bugun = new DateTime(2026, 9, 18);
            if (Has(a, "vade", "due", "bitis", "end", "termin", "teslim")) return bugun.AddDays(7 + i * 9);
            if (Has(a, "baslangic", "start")) return new DateTime(2026, 1, 1).AddMonths(i % 9);
            return bugun.AddDays(-(i * 3) - (Tohum(a, i) % 2));
        }

        static decimal Sayi(string a, int i)
        {
            if (Has(a, "kur", "docrate", "exchange") || a == "rate") return Math.Round(41.2350m + i * 0.0137m, 4);
            if (Has(a, "oran", "yuzde", "percent", "prcnt", "vatrate")) return new[] { 20m, 10m, 1m, 0m }[i % 4];
            if (Has(a, "adet", "miktar", "qty", "quantity", "count", "sayi", "stok", "onhand", "kg", "kilo")) return 5 + Tohum(a, i) % 480;
            if (Has(a, "gun", "day")) return 15 + (i % 4) * 15;
            decimal tutar = Math.Round((decimal)(1000 + Tohum(a, i) % 249000) + (Tohum(a, i + 1) % 100) / 100m, 2);
            if (Has(a, "bakiye", "balance", "fark", "diff") && i % 4 == 3) tutar = -tutar;
            if (Has(a, "kdv", "vat", "tax", "vergi")) tutar = Math.Round(tutar * 0.2m, 2);
            return tutar;
        }

        static string Metin(string a, int i)
        {
            if (a.Length == 0) return "Örnek " + (i + 1);
            if (Has(a, "tarih", "date")) return Tarih(a, i).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            if (Has(a, "saat", "time")) return new TimeSpan(9 + i % 8, (i * 15) % 60, 0).ToString(@"hh\:mm");
            if (Has(a, "mail")) return "ornek" + (i + 1) + "@firma.com.tr";
            if (Has(a, "tel", "phone", "gsm", "cep")) return "0212 555 " + (10 + i % 90).ToString("00") + " " + (20 + i % 70).ToString("00");
            if (Has(a, "iban")) return "TR33 0006 1005 1978 6457 8413 " + (10 + i % 90);
            if (Has(a, "vkn", "tckn", "vergino", "lictrad", "taxid", "kimlik")) return (1234567000 + i * 37).ToString();
            if (Has(a, "vergidaire", "taxoffice")) return "Kozyatağı V.D.";
            if (Has(a, "ettn", "uuid", "guid")) return new Guid(i + 1, 0x2026, 0x0918, 1, 2, 3, 4, 5, 6, 7, 8).ToString().ToUpperInvariant();
            if (Has(a, "doviz", "currency", "curr", "parabirim") || a == "pb" || a == "cur") return Sec(ParaBirimleri, i);
            if (Has(a, "aktar", "islemtipi")) return i % 3 == 0 ? "X" : "R";
            if (Has(a, "durum", "status", "state")) return Sec(Durumlar, i);
            if (Has(a, "itemcode", "kalemkod", "stokkod", "urunkod", "itemkod")) return Sec(KalemKodlari, i);
            if (Has(a, "itemname", "kalemad", "urunad", "stokad", "dscription", "kalem", "urun", "item", "malzeme")) return Sec(Kalemler, i);
            if (Has(a, "cardcode", "carikod", "muhatapkod", "musterikod", "saticikod", "tedarikcikod", "bpcode")) return (i % 3 == 2 ? "T" : "M") + (1000 + i).ToString("0000");
            if (Has(a, "hesapad", "acctname", "accountname")) return Sec(HesapAdlari, i);
            if (Has(a, "acct", "hesapkod", "formatcode", "account", "hesap")) return new[] { "120", "320", "102", "100", "600", "770", "191", "391" }[i % 8] + ".01." + (1 + i).ToString("0000");
            if (Has(a, "banka", "bank")) return Sec(Bankalar, i);
            if (Has(a, "sube", "branch")) return Sec(Subeler, i);
            if (Has(a, "ilce", "county")) return Sec(Ilceler, i);
            if (Has(a, "sehir", "city") || a == "il") return Sec(Sehirler, i);
            if (Has(a, "adres", "address", "street", "cadde")) return "Organize Sanayi Bölgesi " + (i + 1) + ". Cad. No:" + (10 + i);
            if (Has(a, "cardname", "unvan", "cari", "musteri", "muhatap", "tedarik", "satici", "firma", "customer", "vendor", "supplier", "company", "borclu")) return Sec(Firmalar, i);
            if (Has(a, "kullanici", "user", "personel", "calisan", "sahip", "owner", "slpname", "satisci", "sofor", "adsoyad", "onaylayan", "yapan", "ekleyen", "atanan", "atayan", "gonderen", "alici", "employee", "isim")) return Sec(Kisiler, i);
            if (Has(a, "aciklama", "memo", "comment", "desc", "remark", "mesaj", "message", "sebep", "neden")) return "Örnek açıklama " + (i + 1);
            if (Has(a, "belgeno", "faturano", "docnum", "numatcard", "refno", "belge", "fatura", "evrak")) return "URS2026" + (1000 + i).ToString("000000000");
            if (Has(a, "sirketad", "veritaban", "database") || a == "db" || a == "sirket") return new[] { "URASKIMYA", "SELVI", "ALV_KIMYA", "DAF_KIMYA" }[i % 4];
            if (Has(a, "proje", "project")) return "PRJ-0" + (i % 5 + 1);
            if (Has(a, "depo", "whs", "warehouse", "ambar")) return "0" + (i % 3 + 1);
            if (Has(a, "birim", "uom", "unit")) return i % 2 == 0 ? "Adet" : "Kg";
            if (Has(a, "grup", "group", "kategori", "category")) return "Grup " + (char)('A' + i % 6);
            if ((Has(a, "ay", "month")) && a.Length <= 8) return Sec(Aylar, i);
            if (Has(a, "yil", "year")) return "2026";
            if (Has(a, "renk", "color")) return "#0d9488";
            if (Has(a, "url", "link", "path")) return "#";
            if (Has(a, "tip", "tur", "type", "kind")) return Sec(Turler, i);
            if (Has(a, "kod", "code")) return "K" + (i + 1).ToString("000");
            if (Has(a, "not")) return "Örnek not " + (i + 1);
            if (Has(a, "no", "num", "numara", "sira")) return (1000 + i).ToString();
            if (Has(a, "ad", "name", "baslik", "title", "etiket", "label")) return "Örnek Kayıt " + (i + 1);
            return "Örnek " + (i + 1);
        }

        // ------------------------------------------------------------------ nesne / liste üretimi
        public static T Yeni<T>() { var o = Olustur(typeof(T), 0, new HashSet<Type>(), 0); return o is T t ? t : default; }
        public static List<T> Liste<T>(int n) => Enumerable.Range(0, n).Select(i => Olustur(typeof(T), 1, new HashSet<Type>(), i, "")).Select(x => x is T t ? t : default).ToList();

        /// <summary>Dinamik (Dapper) satırlar: kolon adlarından sözlük satırları.</summary>
        public static List<Dictionary<string, object>> Satirlar(int n, string[] kolonlar) =>
            Enumerable.Range(0, n).Select(i => kolonlar.Distinct().ToDictionary(k => k, k => DinamikDeger(k, i))).ToList();

        static object DinamikDeger(string ad, int i)
        {
            string a = Kucuk(ad);
            if (a == "ay" || a == "yil" || a == "gun" || a == "month" || a == "year" || a == "day") return (int)DegerTip(typeof(int), ad, i);
            if (Has(a, "tarih", "date") && !Has(a, "ay")) return Tarih(a, i);
            if (Has(a, "tutar", "toplam", "total", "sum", "borc", "alacak", "bakiye", "balance", "fiyat", "price", "amount", "kdv", "vat", "miktar", "qty", "quantity", "adet", "oran", "kur", "rate", "maliyet", "cost", "debit", "credit", "net", "brut"))
                return Sayi(a, i);
            if ((a.EndsWith("id") && !Has(a, "uuid", "guid")) || a.EndsWith("entry") || Has(a, "docentry", "transid", "abs", "linenum", "sayisi", "count") || a == "no") return 1000 + i;
            return Metin(a, i);
        }

        /// <summary>Her görünüm açılışında ortak ViewBag değerleri.</summary>
        public static void Hazirla(Controller c)
        {
            string db = c.Request.Query["db"].ToString(); if (string.IsNullOrEmpty(db)) db = c.Request.Cookies["SelectedDatabase"] ?? "DefaultConnection";
            if (c.ViewData["CurrentDbDisplay"] == null || (c.ViewData["CurrentDbDisplay"] as string) == "") c.ViewData["CurrentDbDisplay"] = Data.OrnekVeri.SirketAdi(db);
            if (c.ViewData["CurrentDb"] as string == "") c.ViewData["CurrentDb"] = db;
        }

        static bool IlkelMi(Type t)
        {
            var u = Nullable.GetUnderlyingType(t) ?? t;
            return u.IsPrimitive || u.IsEnum || u == typeof(string) || u == typeof(decimal) || u == typeof(DateTime) || u == typeof(DateTimeOffset) || u == typeof(Guid) || u == typeof(TimeSpan);
        }

        static object Olustur(Type t, int derinlik, HashSet<Type> yol, int i, string ad = "")
        {
            if (t == typeof(object)) return new OrnekDinamik(i);   // tipi belirsiz (dynamic/object): istenen her alana adina uygun deger
            if (IlkelMi(t)) return DegerTip(t, ad, i);
            if (t.IsArray)
            {
                var e = t.GetElementType(); int n = derinlik <= 1 ? 12 : 3; if (derinlik > 3) n = 0;
                var arr = Array.CreateInstance(e, n); for (int k = 0; k < n; k++) arr.SetValue(Olustur(e, derinlik + 1, yol, k, ad), k); return arr;
            }
            if (t.IsGenericType)
            {
                var g = t.GetGenericTypeDefinition(); var a = t.GetGenericArguments();
                if (g == typeof(List<>) || g == typeof(IList<>) || g == typeof(IEnumerable<>) || g == typeof(ICollection<>) || g == typeof(IReadOnlyList<>) || g == typeof(IReadOnlyCollection<>))
                {
                    var liste = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(a));
                    int n = derinlik <= 1 ? 12 : 3; if (derinlik > 3 || yol.Contains(a[0])) n = 0;
                    for (int k = 0; k < n; k++) { var el = Olustur(a[0], derinlik + 1, yol, k, ad); if (el != null || !a[0].IsValueType) liste.Add(el); }
                    return liste;
                }
                if (g == typeof(Dictionary<,>) || g == typeof(IDictionary<,>) || g == typeof(IReadOnlyDictionary<,>))
                {
                    var sozluk = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(a));
                    int n = derinlik <= 1 ? 6 : 2; if (derinlik > 3) n = 0;
                    for (int k = 0; k < n; k++)
                    {
                        var anahtar = Olustur(a[0], derinlik + 1, yol, k, a[0] == typeof(string) ? "kod" : "no");
                        if (anahtar == null || sozluk.Contains(anahtar)) continue;
                        sozluk[anahtar] = Olustur(a[1], derinlik + 1, yol, k, "tutar");
                    }
                    return sozluk;
                }
                if (g == typeof(HashSet<>) || g == typeof(ISet<>)) return Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(a));
            }
            if (t == typeof(Microsoft.AspNetCore.Mvc.Rendering.SelectListItem)) return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(Metin(Kucuk(ad) + "ad", i), Metin("kod", i));
            if (t.IsInterface || t.IsAbstract) return null;
            if (t.IsValueType) return Activator.CreateInstance(t);
            if (t.Namespace != null && (t.Namespace.StartsWith("System") || t.Namespace.StartsWith("Microsoft") || t.Namespace.StartsWith("Newtonsoft"))) return null;
            if (t.GetConstructor(Type.EmptyTypes) == null) return null;
            object o; try { o = Activator.CreateInstance(t); } catch { return null; }
            if (!yol.Contains(t)) { yol.Add(t); Doldur(o, derinlik, yol, i); yol.Remove(t); }   // derin seviyede listeler bos (null degil), metinler dolu
            return o;
        }

        /// <summary>
        /// Dinamik alan degeri: (int)d.Ay, (string)d.Tip, d.Tutar.ToString("N2"), d.Odenen >= d.Toplam, @d.Ad ... hepsi calisir.
        /// Istenen ture alan adina uygun ornek deger olarak donusur.
        /// </summary>
        public sealed class SihirliDeger : System.Dynamic.DynamicObject, IComparable, IConvertible
        {
            readonly string _ad; readonly int _i;
            public SihirliDeger(string ad, int i) { _ad = ad; _i = i; }
            public object Dogal => DinamikDeger(_ad, _i);
            static bool SayiMi(object o) => o is decimal || o is int || o is long || o is double || o is float || o is short;
            public override bool TryConvert(System.Dynamic.ConvertBinder b, out object sonuc)
            {
                var hedef = Nullable.GetUnderlyingType(b.Type) ?? b.Type;
                if (hedef == typeof(object)) { sonuc = Dogal; return true; }
                sonuc = IlkelMi(hedef) ? DegerTip(hedef, _ad, _i) : null;
                if (sonuc == null && hedef.IsValueType) sonuc = Activator.CreateInstance(hedef);
                return true;
            }
            public override bool TryBinaryOperation(System.Dynamic.BinaryOperationBinder b, object arg, out object sonuc)
            {
                object sol = Dogal, sag = arg is SihirliDeger sd ? sd.Dogal : arg;
                var op = b.Operation;
                if ((SayiMi(sol) || SayiMi(sag)) && sag != null)
                {
                    decimal x = SayiMi(sol) ? Convert.ToDecimal(sol) : Sayi(Kucuk(_ad), _i), y = SayiMi(sag) ? Convert.ToDecimal(sag) : 0m;
                    switch (op)
                    {
                        case System.Linq.Expressions.ExpressionType.Add: sonuc = x + y; return true;
                        case System.Linq.Expressions.ExpressionType.Subtract: sonuc = x - y; return true;
                        case System.Linq.Expressions.ExpressionType.Multiply: sonuc = x * y; return true;
                        case System.Linq.Expressions.ExpressionType.Divide: sonuc = y == 0 ? 0 : x / y; return true;
                        case System.Linq.Expressions.ExpressionType.GreaterThan: sonuc = x > y; return true;
                        case System.Linq.Expressions.ExpressionType.GreaterThanOrEqual: sonuc = x >= y; return true;
                        case System.Linq.Expressions.ExpressionType.LessThan: sonuc = x < y; return true;
                        case System.Linq.Expressions.ExpressionType.LessThanOrEqual: sonuc = x <= y; return true;
                        case System.Linq.Expressions.ExpressionType.Equal: sonuc = x == y; return true;
                        case System.Linq.Expressions.ExpressionType.NotEqual: sonuc = x != y; return true;
                    }
                }
                string a1 = sol?.ToString(), a2 = sag?.ToString();
                switch (op)
                {
                    case System.Linq.Expressions.ExpressionType.Equal: sonuc = a1 == a2; return true;
                    case System.Linq.Expressions.ExpressionType.NotEqual: sonuc = a1 != a2; return true;
                    case System.Linq.Expressions.ExpressionType.Add: sonuc = a1 + a2; return true;
                    case System.Linq.Expressions.ExpressionType.AndAlso: case System.Linq.Expressions.ExpressionType.And: sonuc = sag is bool bb ? bb : true; return true;
                    case System.Linq.Expressions.ExpressionType.OrElse: case System.Linq.Expressions.ExpressionType.Or: sonuc = true; return true;
                }
                sonuc = false; return true;
            }
            public override bool TryUnaryOperation(System.Dynamic.UnaryOperationBinder b, out object sonuc)
            {
                switch (b.Operation)
                {
                    case System.Linq.Expressions.ExpressionType.Not: sonuc = false; return true;
                    case System.Linq.Expressions.ExpressionType.IsTrue: sonuc = true; return true;
                    case System.Linq.Expressions.ExpressionType.IsFalse: sonuc = false; return true;
                    case System.Linq.Expressions.ExpressionType.Negate: sonuc = -Sayi(Kucuk(_ad), _i); return true;
                }
                sonuc = Dogal; return true;
            }
            public override bool TryGetMember(System.Dynamic.GetMemberBinder b, out object sonuc)
            {
                var d = Dogal; var p = d?.GetType().GetProperty(b.Name);
                sonuc = p != null ? p.GetValue(d) : new SihirliDeger(b.Name, _i); return true;
            }
            public override bool TryInvokeMember(System.Dynamic.InvokeMemberBinder b, object[] args, out object sonuc)
            {
                var d = Dogal; sonuc = null; if (d == null) return true;
                var m = d.GetType().GetMethods().FirstOrDefault(x => x.Name == b.Name && x.GetParameters().Length == args.Length
                        && x.GetParameters().Select((pp, k) => args[k] == null || pp.ParameterType.IsInstanceOfType(args[k])).All(z => z));
                try { sonuc = m != null ? m.Invoke(d, args) : d.ToString(); } catch { sonuc = d.ToString(); }
                return true;
            }
            public override string ToString() => Dogal?.ToString() ?? "";
            public int CompareTo(object o) => string.Compare(ToString(), o?.ToString(), StringComparison.Ordinal);
            object Donustur(Type t) => DegerTip(t, _ad, _i) ?? (t.IsValueType ? Activator.CreateInstance(t) : null);
            TypeCode IConvertible.GetTypeCode() => TypeCode.Object;
            bool IConvertible.ToBoolean(IFormatProvider p) => (bool)Donustur(typeof(bool));
            byte IConvertible.ToByte(IFormatProvider p) => (byte)Donustur(typeof(byte));
            char IConvertible.ToChar(IFormatProvider p) => 'A';
            DateTime IConvertible.ToDateTime(IFormatProvider p) => (DateTime)Donustur(typeof(DateTime));
            decimal IConvertible.ToDecimal(IFormatProvider p) => (decimal)Donustur(typeof(decimal));
            double IConvertible.ToDouble(IFormatProvider p) => (double)Donustur(typeof(double));
            short IConvertible.ToInt16(IFormatProvider p) => (short)Donustur(typeof(short));
            int IConvertible.ToInt32(IFormatProvider p) => (int)Donustur(typeof(int));
            long IConvertible.ToInt64(IFormatProvider p) => (long)Donustur(typeof(long));
            sbyte IConvertible.ToSByte(IFormatProvider p) => 1;
            float IConvertible.ToSingle(IFormatProvider p) => (float)Donustur(typeof(float));
            string IConvertible.ToString(IFormatProvider p) => ToString();
            object IConvertible.ToType(Type t, IFormatProvider p) => Donustur(t);
            ushort IConvertible.ToUInt16(IFormatProvider p) => 1;
            uint IConvertible.ToUInt32(IFormatProvider p) => 1;
            ulong IConvertible.ToUInt64(IFormatProvider p) => 1;
        }

        /// <summary>Tipi belirsiz eleman (dynamic): istenen her alan icin adina uygun ornek deger dondurur (d.Value, d.Name, (int)d.PlanId ...).</summary>
        public sealed class OrnekDinamik : System.Dynamic.DynamicObject
        {
            readonly int _i; public OrnekDinamik(int i) { _i = i; }
            public override bool TryGetMember(System.Dynamic.GetMemberBinder b, out object sonuc) { sonuc = new SihirliDeger(b.Name, _i); return true; }
            public override bool TryConvert(System.Dynamic.ConvertBinder b, out object sonuc) { sonuc = b.Type == typeof(string) ? "Örnek " + (_i + 1) : (b.Type.IsValueType ? Activator.CreateInstance(b.Type) : null); return true; }
            public override string ToString() => "Örnek " + (_i + 1);
        }

        static void Doldur(object o, int derinlik, HashSet<Type> yol, int i)
        {
            foreach (var p in o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                // Sinifin kendi baslattigi dizi korunur (or. new decimal[13] ile 1..12 ay indeksi): yalnizca icerigi doldurulur
                if (p.CanRead && p.GetValue(o) is Array mevcut && mevcut.Length > 0) { var et = mevcut.GetType().GetElementType(); if (IlkelMi(et)) for (int k = 0; k < mevcut.Length; k++) { try { mevcut.SetValue(DegerTip(et, p.Name, k + i), k); } catch { } } continue; }
                if (!p.CanWrite || p.GetIndexParameters().Length > 0 || p.SetMethod == null || !p.SetMethod.IsPublic) continue;
                try { var d = Olustur(p.PropertyType, derinlik + 1, yol, i, p.Name); if (d != null) p.SetValue(o, d); } catch { }
            }
            foreach (var f in o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.IsInitOnly) continue;
                try { var d = Olustur(f.FieldType, derinlik + 1, yol, i, f.Name); if (d != null) f.SetValue(o, d); } catch { }
            }
        }
    }
}
