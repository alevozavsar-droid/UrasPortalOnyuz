// ============================================================
// ORTAK CARİ SÜZGECİ — Müşteri / Tedarikçi Cari Kartları ekranındaki 14 süzgeç (Ara, Cari Kart Tipi, Bölge, Ülke, Sınıf, İlişki Durumu, Rol, Durum, Faaliyet Ana Grubu,
// Faaliyet Grubu, Satış / Satınalma Temsilcisi, Şirket (ERP), Limit Durumu, Risk Durumu) diğer cari ekranlarında da aynı davranır:
// Otomatik Kapatma, Adat Hesaplama, Tahsilat, Tedarikçi Fatura ve Ödeme Takip, Tedarikçi Konsolide Ödeme Takip. Sunucu: süzgeç alanları (Alanlar) + seçenekler (Secenekler); istemci: wwwroot/js/cari-suzgec.js
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    public static class CariSuzgec
    {
        /// <summary>Kartın süzgeç alanları (istemcideki CariSuzgec.uyuyor(k) bu adları bekler).</summary>
        public static object Alanlar(CariKart k, CariRisk r = null, CariLimit l = null)
        {
            r ??= CariKartOrnek.RiskHesapla(k); l ??= CariKartOrnek.LimitHesapla(k);
            return new
            {
                kod = k.Kod, kisaAd = k.KisaAd, resmiUnvan = k.ResmiUnvan ?? "", vergiNo = k.VergiNo ?? "", yetkili = k.Yetkili ?? "", eposta = k.Eposta ?? "", sehir = k.Sehir ?? "", telefon = k.Telefon ?? "",
                kartTipi = k.KartTipi, bolge = k.Bolge ?? "", ulke = k.Ulke ?? "", sinif = k.Sinif ?? "", iliskiDurumu = k.IliskiDurumu ?? "", rol = k.Rol ?? "", durum = k.Durum, sirket = k.Sirket ?? "", vadeSarti = k.VadeSarti ?? "",
                anaGruplar = k.Faaliyetler.Select(f => f.AnaGrup).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray(),
                gruplar = k.Faaliyetler.Select(f => f.AnaGrup + "|" + f.Grup).Distinct().ToArray(),
                temsilciler = k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray(),
                temsilci = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct()),
                limitDurum = l.Durum, limit = l.Limit, limitKullanim = l.KullanimYuzde, risk = r.Durum, riskNeden = r.Neden, maxGecikme = r.MaxGecikme
            };
        }
        /// <summary>Süzgeç seçenekleri (tüm ekranlar aynı listeyi kullanır).</summary>
        public static object Secenekler() => new
        {
            kartTipleri = CariKartOrnek.KartTipleri, bolgeler = CariKartOrnek.Bolgeler, ulkeler = CariKartOrnek.Ulkeler, sinifMatrisi = CariKartOrnek.SinifMatrisi, iliskiDurumlari = CariKartOrnek.IliskiDurumlari, roller = CariKartOrnek.Roller, durumlar = CariKartOrnek.Durumlar,
            faaliyetHiyerarsisi = CariKartOrnek.FaaliyetHiyerarsisi, temsilciler = CariKartOrnek.SatisTemsilcileri.Values.SelectMany(x => x).Concat(AlimOnayOrnek.Satinalmacilar.Select(s => s.Ad)).Distinct().OrderBy(x => x, StringComparer.Create(CariKartOrnek.Tr, false)).ToArray(),
            sirketler = CariKartOrnek.Sirketler, limitDurumlari = new[] { "Limit İçinde", "Limite Yakın", "Limit Aşıldı", "Limit Yok" }, riskDurumlari = new[] { "Riskli", "İzlemede", "Risksiz" }
        };
    }
}
