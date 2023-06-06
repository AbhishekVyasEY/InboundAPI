using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DedupeDigiLead
{

    public class DedupDgLdNLReturn
    {
        public bool decideNL { get; set; }   
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }
    }

    public class DedupDgLdNLTRReturn
    {
        public bool decideNLTR { get; set; }
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }
    }


    public class DedupeDigiLeadProperty
    {
        public string ccs_classification { get; set; }
        public string eqs_customerid { get; set; }
        public string Accountid { get; set; }
        public string customerid { get; set; }
        public string CategoryId { get; set; }
        public string SubCategoryId { get; set; }


    }

   

}
