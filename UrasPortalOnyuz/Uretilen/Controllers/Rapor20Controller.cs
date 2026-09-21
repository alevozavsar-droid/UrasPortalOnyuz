// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebApplication3.Models;
using static WebApplication3.Controllers.MobilSiparisController;
using static WebApplication3.Controllers.Rapor103Controller;
using AccountBalanceViewModel = WebApplication3.Models.AccountBalanceViewModel;
using CariViewModel = WebApplication3.Models.CariViewModel;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{

    public static class ExtensionMethods
    {
        public static DataTable ToDataTable<T>(this IEnumerable<T> data)
        {
            DataTable dataTable = new DataTable();
            if (data == null || !data.Any())
            {
                dataTable.Columns.Add("Value"); return dataTable;
            }
            var properties = typeof(T).GetProperties();
            foreach (var prop in properties) dataTable.Columns.Add(prop.Name);
            foreach (var item in data)
            {
                var row = dataTable.NewRow();
                foreach (var prop in properties) row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                dataTable.Rows.Add(row);
            }
            return dataTable;
        }
    }

    /* ===================================================================================
       YENİ DTO/MODEL SINIFLARI 
    =================================================================================== */

    public class OpenTransactionModel
    {
        public int TransId { get; set; }
        public int LineId { get; set; }
        public string DocDate { get; set; }
        public string DueDate { get; set; }
        public string Memo { get; set; }
        public string TransType { get; set; }
        public decimal OpenDebit { get; set; }
        public decimal OpenCredit { get; set; }
        public string AktarimTarihi { get; set; }
        public string IslemTipi { get; set; }
        public string ParaBirimi { get; set; }
        public string KurFarkiParaBirimi { get; set; } // YENİ EKLENEN ALAN
        public decimal DovizliBorc { get; set; }
        public decimal DovizliAlacak { get; set; }
        public bool IsBOE { get; set; }

        public string CompanyName { get; set; }
        public string DbName { get; set; }
    }

    public class ReconciliationRequestModel
    {
        public string CardCode { get; set; }
        public string TargetDbName { get; set; }
        public List<ReconLine> Lines { get; set; }
        public bool UseBpDueDate { get; set; }
    }

    public class ReconLine
    {
        public int TransId { get; set; }
        public int LineId { get; set; }
        public decimal ReconcileAmount { get; set; }
        public decimal ReconcileAmountFC { get; set; }
    }

    public class SimulationLineModel
    {
        public int TransId { get; set; }
        public int LineId { get; set; }
        public decimal Tutar { get; set; }
        public decimal TutarFC { get; set; }
        public int GrupNo { get; set; }
    }

    public class UyariViewModel
    {
        public string UyariTipi { get; set; }
        public string AnalizSonucu { get; set; }
    }

    public class MutabakatKaydiModel
    {
        public DateTime MutabakatTarihi { get; set; }
        public decimal BakiyeTRY { get; set; }
        public string Kullanici { get; set; }
    }

    public class Rapor20CariEkstreViewModel
    {
        public int IslemNo { get; set; }
        public int SatirNo { get; set; }
        public string CheckNum { get; set; }
        public DateTime? KayitTarihi { get; set; }
        public DateTime? VadeTarihi { get; set; }
        public string MuhatapKodu { get; set; }
        public string MuhatapAdi { get; set; }
        public string Phone1 { get; set; }
        public string Street { get; set; }
        public string County { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Aciklama { get; set; }
        public string AktarimTipi { get; set; }
        public string IslemTipi { get; set; }

        public string BakiyeDurumu { get; set; }

        public decimal? TRYB { get; set; }
        public decimal? TRYA { get; set; }
        public decimal? IslemB { get; set; }
        public decimal? IslemA { get; set; }
        public decimal? TRY_KmlBky { get; set; }
        public decimal? Islem_KmlBky { get; set; }
        public string IslemPB { get; set; }
        public string Sirket { get; set; }
    }
}