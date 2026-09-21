using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{
    public class AdatHesaplamaModel
    {
        public List<CariViewModel> CariList { get; set; } = new List<CariViewModel>();
        public string SelectedBpCode { get; set; }
        public string BpName { get; set; }
        public List<AcikFaturaViewModel> OpenInvoices { get; set; } = new List<AcikFaturaViewModel>();


        public List<string> AktarimTipiList { get; set; } = new List<string>();

        public List<string> SelectedAktarimTipi { get; set; } = new List<string>();


        public List<CheckApprovalModel> ExistingChecks { get; set; }
        public decimal? AverageDueDays { get; set; }
        public DateTime? AdatDate { get; set; }
        public string AdatMessage { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
    }



















  
}