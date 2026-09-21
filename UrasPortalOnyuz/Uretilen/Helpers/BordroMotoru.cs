using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Helpers
{

    public class BordroSonuc
    {
        public decimal BrutUcret { get; set; }
        public decimal NetUcret { get; set; }
        public decimal SGKIsci { get; set; }
        public decimal IssizlikIsci { get; set; }
        public decimal GelirVergisi { get; set; }
        public decimal DamgaVergisi { get; set; }
        public decimal SGKIsveren { get; set; }
        public decimal IssizlikIsveren { get; set; }
        public decimal ToplamMaliyet { get; set; }
    }


    public class VergiDilimiKurali
    {
        public decimal Limit { get; set; } // O dilimin üst limiti (Örn: 158.000)
        public decimal Oran { get; set; }  // O dilimin vergisi (Örn: 0.15)
    }

    public class DonemParametreleri
    {
        public int Yil { get; set; }
        public decimal AsgariUcretBrut { get; set; }
        public decimal SGKTavani { get; set; }
        public List<VergiDilimiKurali> VergiDilimleri { get; set; }
    }

    public static class BordroMotoru
    {



        private static DonemParametreleri GetParametreler(int yil)
        {

            if (yil == 2026)
            {
                return new DonemParametreleri
                {
                    Yil = 2026,
                    AsgariUcretBrut = 33030.00m,
                    SGKTavani = 247725.00m, // 33.030 * 7.5

                    VergiDilimleri = new List<VergiDilimiKurali>
                    {
                        new VergiDilimiKurali { Limit = 210000m, Oran = 0.15m },
                        new VergiDilimiKurali { Limit = 500000m, Oran = 0.20m },
                        new VergiDilimiKurali { Limit = 1450000m, Oran = 0.27m },
                        new VergiDilimiKurali { Limit = 5000000m, Oran = 0.35m },
                        new VergiDilimiKurali { Limit = decimal.MaxValue, Oran = 0.40m }
                    }
                };
            }
            else // Varsayılan 2025
            {
                return new DonemParametreleri
                {
                    Yil = 2025,
                    AsgariUcretBrut = 26005.50m,
                    SGKTavani = 195041.25m, // 26.005,50 * 7.5

                    VergiDilimleri = new List<VergiDilimiKurali>
                    {
                        new VergiDilimiKurali { Limit = 158000m, Oran = 0.15m },
                        new VergiDilimiKurali { Limit = 340000m, Oran = 0.20m },  // Mart geçişi için ayarlandı
                        new VergiDilimiKurali { Limit = 1150000m, Oran = 0.27m }, // Ekim geçişi için ayarlandı
                        new VergiDilimiKurali { Limit = 4300000m, Oran = 0.35m },
                        new VergiDilimiKurali { Limit = decimal.MaxValue, Oran = 0.40m }
                    }
                };
            }
        }




        public static BordroSonuc NettenBruteHesapla(decimal hedefNet, int yil)
        {

            var p = GetParametreler(yil);


            decimal tBrut = 0, tMaliyet = 0, tSgkIsci = 0, tIssizIsci = 0, tGv = 0, tDv = 0, tSgkIsv = 0, tIssizIsv = 0;


            decimal kumulatifPersonel = 0;
            decimal kumulatifAsgari = 0;


            for (int ay = 1; ay <= 12; ay++)
            {


                decimal auSgk = p.AsgariUcretBrut * 0.14m;
                decimal auIssiz = p.AsgariUcretBrut * 0.01m;
                decimal auMatrah = p.AsgariUcretBrut - (auSgk + auIssiz);



                decimal istisnaGv = VergiHesaplaDinamik(kumulatifAsgari, auMatrah, p.VergiDilimleri);
                decimal istisnaDv = p.AsgariUcretBrut * 0.00759m;


                var m = AyIcinBrutBul(hedefNet, kumulatifPersonel, p, istisnaGv, istisnaDv);


                tBrut += m.Brut;
                tMaliyet += m.Maliyet;
                tSgkIsci += m.SgkIsci;
                tIssizIsci += m.IssizIsci;
                tGv += m.NetGv;
                tDv += m.NetDv;
                tSgkIsv += m.SgkIsv;
                tIssizIsv += m.IssizIsv;


                kumulatifPersonel += m.GvMatrah;
                kumulatifAsgari += auMatrah;
            }

            return new BordroSonuc
            {
                BrutUcret = tBrut / 12,
                NetUcret = hedefNet,
                SGKIsci = tSgkIsci / 12,
                IssizlikIsci = tIssizIsci / 12,
                GelirVergisi = tGv / 12,
                DamgaVergisi = tDv / 12,
                SGKIsveren = tSgkIsv / 12,
                IssizlikIsveren = tIssizIsv / 12,
                ToplamMaliyet = tMaliyet / 12
            };
        }




        private static decimal VergiHesaplaDinamik(decimal kumulatifMatrah, decimal oAykiMatrah, List<VergiDilimiKurali> dilimler)
        {
            decimal toplamVergi = 0;
            decimal kalanIslemMatrahi = oAykiMatrah;
            decimal geciciKumulatif = kumulatifMatrah;

            foreach (var dilim in dilimler)
            {

                if (geciciKumulatif < dilim.Limit)
                {


                    decimal dilimdekiBosluk = dilim.Limit - geciciKumulatif;


                    decimal islenecekTutar = Math.Min(kalanIslemMatrahi, dilimdekiBosluk);


                    toplamVergi += islenecekTutar * dilim.Oran;


                    kalanIslemMatrahi -= islenecekTutar;
                    geciciKumulatif += islenecekTutar;
                }


                if (kalanIslemMatrahi <= 0) break;
            }

            return toplamVergi;
        }




        private static AyBordroModel AyIcinBrutBul(decimal hedefNet, decimal kumulatif, DonemParametreleri p, decimal istisnaGv, decimal istisnaDv)
        {
            decimal alt = hedefNet;
            decimal ust = hedefNet * 4.0m;
            AyBordroModel enIyi = new AyBordroModel();


            for (int i = 0; i < 50; i++)
            {
                decimal testBrut = (alt + ust) / 2;
                var m = HesaplaTekil(testBrut, kumulatif, p, istisnaGv, istisnaDv);

                if (Math.Abs(m.Net - hedefNet) < 0.005m)
                {
                    enIyi = m;
                    break;
                }

                if (m.Net < hedefNet) alt = testBrut;
                else ust = testBrut;
                enIyi = m;
            }
            return enIyi;
        }

        private static AyBordroModel HesaplaTekil(decimal brut, decimal kumulatif, DonemParametreleri p, decimal istisnaGv, decimal istisnaDv)
        {
            var m = new AyBordroModel();
            m.Brut = brut;


            decimal sgkMatrah = (brut > p.SGKTavani) ? p.SGKTavani : brut;
            m.SgkIsci = sgkMatrah * 0.14m;
            m.IssizIsci = sgkMatrah * 0.01m;


            m.GvMatrah = brut - (m.SgkIsci + m.IssizIsci);


            decimal hamGv = VergiHesaplaDinamik(kumulatif, m.GvMatrah, p.VergiDilimleri);


            m.NetGv = Math.Max(0, hamGv - istisnaGv);


            decimal hamDv = brut * 0.00759m;
            m.NetDv = Math.Max(0, hamDv - istisnaDv);


            m.Net = brut - (m.SgkIsci + m.IssizIsci + m.NetGv + m.NetDv);


            m.SgkIsv = sgkMatrah * 0.155m;
            m.IssizIsv = sgkMatrah * 0.02m;
            m.Maliyet = brut + m.SgkIsv + m.IssizIsv;

            return m;
        }

        private class AyBordroModel
        {
            public decimal Brut;
            public decimal Net;
            public decimal SgkIsci;
            public decimal IssizIsci;
            public decimal GvMatrah;
            public decimal NetGv;
            public decimal NetDv;
            public decimal SgkIsv;
            public decimal IssizIsv;
            public decimal Maliyet;
        }
    }
}