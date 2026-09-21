// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using WebApplication3.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Globalization;
using System.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Routing;
using ClosedXML.Excel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{

    public class ChildFirstHesapComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (string.Equals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            string xKey = x.Trim() + "~";
            string yKey = y.Trim() + "~";

            return string.Compare(xKey, yKey, StringComparison.Ordinal);
        }
    }
}