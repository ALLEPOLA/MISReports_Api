using System;

namespace MISReports_Api.Models
{
    public class Employee
    {
        public DateTime auth_date { get; set; }
        public int order_id { get; set; }
        public string acct_number { get; set; }
        public string bank_code { get; set; }
        public decimal bill_amt { get; set; }
        public decimal tax_amt { get; set; }
        public decimal tot_amt { get; set; }
        public string auth_code { get; set; }
        public string card_no { get; set; }
    }

    public class CrdTemp
    {
        public int order_id { get; set; }
        public string acct_number { get; set; }
        public string custname { get; set; }
        public string username { get; set; }
        public decimal bill_amt { get; set; }
        public decimal tax_amt { get; set; }
        public decimal tot_amt { get; set; }
        public string trstatus { get; set; }
        public string authcode { get; set; }
        public DateTime pmnt_date { get; set; }
        public DateTime auth_date { get; set; }
        public string cebres { get; set; }
        public int serl_no { get; set; }
        public string bank_code { get; set; }
        public string bran_code { get; set; }
        public string inst_status { get; set; }
        public string updt_status { get; set; }
        public string updt_flag { get; set; }
        public string post_flag { get; set; }
        public string err_flag { get; set; }
        public DateTime post_date { get; set; }
        public string card_no { get; set; }
        public string payment_type { get; set; }
        public string ref_number { get; set; }
        public string reference_type { get; set; }
        public string sms_st { get; set; }
    }

    public class OUMResponse
    {
        public object data { get; set; }
        public string errorMessage { get; set; }
        public string errorDetails { get; set; }
    }
}