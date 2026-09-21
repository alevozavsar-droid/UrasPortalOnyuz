using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class FilterDropdownsViewModel
    {
        public List<string> BelgeTarihiList { get; set; } = new List<string>();
        public List<string> BelgeAyiList { get; set; } = new List<string>();


        public List<string> VadeAyiList { get; set; } = new List<string>();

        public List<string> MusteriAdiList { get; set; } = new List<string>();
        public List<string> SatisTemsilcisiList { get; set; } = new List<string>();
        public List<string> BelgeParaBirimiList { get; set; } = new List<string>();
    }


}
