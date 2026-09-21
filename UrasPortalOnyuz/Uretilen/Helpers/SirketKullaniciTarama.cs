using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Helpers
{

    public class SirketKullanicisi
    {
        public string Sirket { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }

        public string eMail { get; set; }
        public string U_BE1_PASSWORD { get; set; }
        public string U_BE1_YETKI { get; set; }
        public string PersonelAd { get; set; }
        public string PersonelTel { get; set; }

        public List<string> Sirketler { get; set; } = new List<string>();
    }






    public static class SirketKullaniciTarama
    {

        public static List<(string Db, string ConnectionString)> Sirketler(IConfiguration cfg, string haricDb = null)
 {return default;
}


        public static string MerkezDb(IConfiguration cfg)
 {return default;
}





        public static async Task<List<SirketKullanicisi>> DigerSirketKullanicilariAsync(IConfiguration cfg, ILogger logger, string sadeceKod = null)
        {
            string merkez = MerkezDb(cfg);
            var sirketler = Sirketler(cfg, merkez);
            var gorevler = sirketler.Select(s => SirketKullanicilariAsync(s.Db, s.ConnectionString, logger, sadeceKod)).ToList();
            var sonuclar = await Task.WhenAll(gorevler);

            var birlesik = new Dictionary<string, SirketKullanicisi>(StringComparer.OrdinalIgnoreCase);

            foreach (var liste in sonuclar)
            {
                foreach (var k in liste)
                {
                    if (string.IsNullOrWhiteSpace(k.UserCode)) continue;
                    if (!birlesik.TryGetValue(k.UserCode.Trim(), out var mevcut))
                    {
                        k.Sirketler.Add(k.Sirket);
                        birlesik[k.UserCode.Trim()] = k;
                        continue;
                    }
                    mevcut.Sirketler.Add(k.Sirket);
                    if (string.IsNullOrWhiteSpace(mevcut.eMail)) mevcut.eMail = k.eMail;
                    if (string.IsNullOrWhiteSpace(mevcut.UserName) || mevcut.UserName.Trim().Equals(mevcut.UserCode.Trim(), StringComparison.OrdinalIgnoreCase)) mevcut.UserName = k.UserName;
                    if (string.IsNullOrWhiteSpace(mevcut.U_BE1_PASSWORD)) mevcut.U_BE1_PASSWORD = k.U_BE1_PASSWORD;
                    if (string.IsNullOrWhiteSpace(mevcut.U_BE1_YETKI)) mevcut.U_BE1_YETKI = k.U_BE1_YETKI;
                    if (string.IsNullOrWhiteSpace(mevcut.PersonelAd)) mevcut.PersonelAd = k.PersonelAd;
                    if (string.IsNullOrWhiteSpace(mevcut.PersonelTel)) mevcut.PersonelTel = k.PersonelTel;
                }
            }
            return birlesik.Values.OrderBy(x => x.UserCode, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static async Task<List<SirketKullanicisi>> SirketKullanicilariAsync(string db, string cs, ILogger logger, string sadeceKod)
 {return default;
}
    }
}
