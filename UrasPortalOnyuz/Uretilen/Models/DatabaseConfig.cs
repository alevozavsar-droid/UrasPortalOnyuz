using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class DatabaseConfig
    {
        public string Key { get; set; }
        public string Display { get; set; }
        public List<string> RequiredAuthorities { get; set; } 
    }
}
