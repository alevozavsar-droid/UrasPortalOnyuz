// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using WebApplication3.Models; // KrediTeminatViewModel, TeminatDetayViewModel, KefaletDetayViewModel burada olmalı
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Data;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text.Json;

namespace WebApplication3.Controllers
{


    public class GlobalConfigRequest
    {
        public string ConfigKey { get; set; } // U_AyarAdi'ye karşılık gelir (Örn: ColumnTitle_FirmaAdi veya Formula_Anapara)
        public string ConfigValue { get; set; } // U_AyarDegeri'ye karşılık gelir
    }


   


    public static class ModelExtensions
    {
        private static readonly CultureInfo INV_Culture = CultureInfo.InvariantCulture;

        public static object ToDbValue(this object value, SqlDbType type)
        {
            if (value == null || value is string s && string.IsNullOrWhiteSpace(s)) return DBNull.Value;

            try
            {

                string stringValue = value.ToString().Replace(',', '.').Trim();

                if (type == SqlDbType.DateTime)
                {
                    if (DateTime.TryParse(stringValue, INV_Culture, DateTimeStyles.None, out DateTime dateValue))
                        return dateValue;
                }
                else if (type == SqlDbType.Decimal)
                {

                    if (decimal.TryParse(stringValue, NumberStyles.Any, INV_Culture, out decimal decValue))
                        return decValue;
                }
                else if (type == SqlDbType.Int)
                {
                    if (int.TryParse(stringValue, out int intValue))
                        return intValue;
                }
                else
                {
                    return stringValue;
                }
            }
            catch { }

            return DBNull.Value;
        }

        public static SqlDbType ToSqlDbType(this Type propertyType)
        {

            Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (underlyingType == typeof(DateTime)) return SqlDbType.DateTime;
            if (underlyingType == typeof(decimal)) return SqlDbType.Decimal;
            if (underlyingType == typeof(int)) return SqlDbType.Int;
            return SqlDbType.NVarChar;
        }

        public static object ToDbDateValue(this string value)
        {
            if (string.IsNullOrEmpty(value)) return DBNull.Value;
            return DateTime.TryParse(value, INV_Culture, DateTimeStyles.None, out DateTime dateValue) ? (object)dateValue : DBNull.Value;
        }

        public static object ToDbDecimalValue(this string value)
        {
            string cleanValue = (value ?? "").Replace(',', '.').Trim();
            if (string.IsNullOrEmpty(cleanValue)) return DBNull.Value;
            return decimal.TryParse(cleanValue, NumberStyles.Any, INV_Culture, out decimal decValue) ? (object)decValue : DBNull.Value;
        }

        public static object ToDbIntValue(this string value)
        {
            if (string.IsNullOrEmpty(value)) return DBNull.Value;
            return int.TryParse(value, out int intValue) ? (object)intValue : DBNull.Value;
        }

        public static object SafeConvertFromDb(this object rawValue, Type propertyType, string propertyName)
        {
            if (rawValue == null || rawValue == DBNull.Value)
            {
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return null;
                if (propertyType == typeof(string))
                    return string.Empty;
                if (propertyType.IsValueType)
                    return Activator.CreateInstance(propertyType);
                return null;
            }

            Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            try
            {
                if (underlyingType == typeof(DateTime))
                {
                    return Convert.ChangeType(rawValue, underlyingType, INV_Culture);
                }
                else if (underlyingType == typeof(decimal))
                {
                    return Convert.ChangeType(rawValue, underlyingType, INV_Culture);
                }
                else if (underlyingType == typeof(int))
                {
                    return Convert.ChangeType(rawValue, underlyingType, INV_Culture);
                }
                else if (underlyingType == typeof(string))
                {
                    return rawValue.ToString();
                }
                else
                {
                    return Convert.ChangeType(rawValue, underlyingType, INV_Culture);
                }
            }
            catch (Exception ex)
            {

                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return null;
                if (underlyingType == typeof(string))
                    return rawValue.ToString();
                return Activator.CreateInstance(underlyingType);
            }
        }
    }


    [Authorize]
    [Route("[controller]")]
    public class Rapor32Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor32Controller> _logger;

        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display= "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
        };

        private string GetSelectedDatabase()
 {return default;
}

        public class UpdateRequest
        {
            public int DocEntry { get; set; }
            public int? LineId { get; set; }
            public string TableName { get; set; }

            public string IhracatDosyaNo { get; set; }
            public string FieldName { get; set; }
            public string FieldValue { get; set; }
        }



        private Dictionary<string, string> GetAnaUdfMapping()  {return default;
}

        private Dictionary<string, string> GetTeminatDetayUdfMapping()  {return default;
}

        private Dictionary<string, string> GetKefaletDetayUdfMapping()  {return default;
}



        [HttpGet("GetFormulas")]
        public async Task<IActionResult> GetFormulas()
 {return Json(new global::System.Collections.Generic.Dictionary<string, string>());   // ön yüz örneği: formül yok
}


        public IActionResult Index()
 {ViewBag.CurrentDbDisplay = "";
ViewBag.CustomTitles = WebApplication3.OrnekDoldurucu.Yeni<System.Collections.Generic.Dictionary<string, string>>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.KrediTeminatViewModel>(12));
}

        [HttpPost("CreateTeminatDetay")]
        public async Task<IActionResult> CreateTeminatDetay([FromBody] TeminatDetayViewModel newDetail)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), docEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("docEntry", 0), lineId = global::WebApplication3.OrnekDoldurucu.Deger<int>("lineId", 0) });
}

        [HttpPost("DeleteTeminatDetay")]
        public async Task<IActionResult> DeleteTeminatDetay([FromBody] UpdateRequest request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("CreateKefaletDetay")]
        public async Task<IActionResult> CreateKefaletDetay([FromBody] KefaletDetayViewModel newDetail)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), docEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("docEntry", 0), lineId = global::WebApplication3.OrnekDoldurucu.Deger<int>("lineId", 0) });
}

        [HttpPost("DeleteKefaletDetay")]
        public async Task<IActionResult> DeleteKefaletDetay([FromBody] UpdateRequest request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}



        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] UpdateRequest request)
 {return Json(new { success = true, message = "Veri başarıyla güncellendi. ✅" });
}


        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] KrediTeminatViewModel newRecord)
 {return Json(new { success = true, message = "Yeni kayıt başarıyla eklendi. ✅", docEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("docEntry", 0), docNum = global::WebApplication3.OrnekDoldurucu.Deger<string>("docNum", 0) });
}



        [HttpPost("Delete")]
        public async Task<IActionResult> Delete([FromBody] UpdateRequest request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private void DeleteFile(string dbName, string dosyaYolu)
 {}

        [HttpPost("UploadDosya/{docEntry}")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadDosya(int docEntry, IFormFile file)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), dosyaAdi = global::WebApplication3.OrnekDoldurucu.Deger<string>("dosyaAdi", 0) });
}


        [HttpPost("UpdateSutunRengi")]
        public async Task<IActionResult> UpdateSutunRengi([FromBody] UpdateRequest request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpPost("UpdateGlobalConfig")]
        public async Task<IActionResult> UpdateGlobalConfig([FromBody] GlobalConfigRequest request)
 {return Json(new { success = true, message = "Formül/Başlık zaten boştu, işlem yapılmadı." });
}
    }
}