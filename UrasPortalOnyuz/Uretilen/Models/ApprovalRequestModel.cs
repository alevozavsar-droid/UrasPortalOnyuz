using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class ApprovalRequestModel
    {
        public List<string> SelectedInvoiceDocEntries { get; set; }
        public string ApproverEmail { get; set; }
        public string BpCode { get; set; }
    }

    public class ApprovalPerformModel
    {
        public List<string> DocEntries { get; set; }
    }

    public class FullApprovalRequestModel
    {
        public List<string> SelectedInvoiceDocEntries { get; set; }
        public string BpCode { get; set; }
        public string BpName { get; set; }
        public string ApproverCode { get; set; }
        public string ApproverEmail { get; set; }
        public string InvoiceAdatDate { get; set; }
        public string ChecksAdatDate { get; set; }
        public string CombinedAdatDate { get; set; }
        public string TotalBalance { get; set; }
    }

    public class CheckApprovalModel
    {
        public int DocEntry { get; set; }
        public string U_BE1_CARDCODE { get; set; }
        public string U_BE1_CARDNAME { get; set; }
        public DateTime? U_BE1_DUEDATE { get; set; }
        public string U_BE1_CHECKNUMBER { get; set; }
        public decimal U_BE1_AMOUNT { get; set; }
        public string U_BE1_APPROVALSTATUS { get; set; }
        public string U_BE1_APPROVERCODE { get; set; }
        public DateTime? CreateDate { get; set; }
        public string Creator { get; set; }
        public DateTime? UpdateDate { get; set; }
    }

    public class CheckRowModel
    {
        public string DueDate { get; set; }
        public string DocEntry { get; set; }
        public string CheckNumber { get; set; }
        public string Amount { get; set; }
        public string FindeksNote { get; set; }
    }

    public class CheckApprovalRequestModel
    {
        public string BpCode { get; set; }
        public string BpName { get; set; }
        public string ApproverCode { get; set; }
        public string ApproverEmail { get; set; }
        public string InvoiceAdatDate { get; set; }
        public string ChecksAdatDate { get; set; }
        public string CombinedAdatDate { get; set; }
        public string TotalBalance { get; set; }
        public string InvoiceTotalBalance { get; set; } // Yeni eklendi
        public string ChecksTotalBalance { get; set; } // Yeni eklendi
        public string AdatDifference { get; set; } // Yeni eklendi
        public List<CheckRowModel> CheckRows { get; set; }
    }

    public class ApproverViewModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}