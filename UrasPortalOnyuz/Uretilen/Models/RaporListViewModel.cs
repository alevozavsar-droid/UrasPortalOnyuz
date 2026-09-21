using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{

    public class RaporListViewModel
    {
        public List<BelgeViewModel> Belgeler { get; set; }
        public int MevcutSayfa { get; set; }
        public int ToplamSayfa { get; set; }
        public int SayfaBoyutu { get; set; }
        public int ToplamKayitSayisi { get; set; }
    }
}
