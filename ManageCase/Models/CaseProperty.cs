using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;

namespace ManageCase
{
    
    public class LeadMsdProperty
    {     
       
        public string eqs_casetype { get; set; }
        public string title { get; set; }
        public int prioritycode { get; set; }
        public string description { get; set; }


    }


    public class CaseProperty
    {
        public string channelId { get; set; }
        public string ccs_classification { get; set; }
        public string eqs_customerid { get; set; }
        public string Accountid { get; set; }
        public string customerid { get; set; }
        public string CategoryId { get; set; }
        public string SubCategoryId { get; set; }
        public string SourceId { get; set; }

    }

    public class CaseReturnParam
    {
        public string ReturnCode { get; set; }      
        public string CaseID { get; set; }
        public string Message { get; set; }
        public string ExecutionTime { get; set; }
        public string TransactionID { get; set; }

    }

    public class CaseStatusRtParam
    {
        public string CaseID { get; set; }
        public string Accountid { get; set; }
        public string customerid { get; set; }
        public string Description { get; set; }
        public string CaseStatus { get; set; }
        public string Casetype { get; set; }
        public string Subject { get; set; }
        public string Classification { get; set; }
        public string category { get; set; }
        public string subcategory { get; set; }
        public string openDate { get; set; }
        public string modifiedDate { get; set; }
        public string closeDate { get; set; }
        public string Channel { get; set; }
        public string Source { get; set; }
        public string Priority { get; set; }
        public JObject AdditionalField { get; set; }
        public string ReturnCode { get; set; }               
        public string Message { get; set; }
        public string ExecutionTime { get; set; }
        public string TransactionID { get; set; }

    }

    public class CaseDetails
    {
        public string CaseID { get; set; }
        public string Accountid { get; set; }
        public string customerid { get; set; }
        public string Description { get; set; }
        public string CaseStatus { get; set; }
        public string Casetype { get; set; }
        public string Subject { get; set; }
        public string Classification { get; set; }
        public string category { get; set; }
        public string subcategory { get; set; }
        public string openDate { get; set; }
        public string modifiedDate { get; set; }
        public string closeDate { get; set; }
        public string Channel { get; set; }
        public string Source { get; set; }
        public string Priority { get; set; }
        public JObject AdditionalField { get; set; }

    }

    public class CaseListParam
    {
        public string ReturnCode { get; set; }
        public List<CaseDetails> AllCases { get; set; }
        public string Message { get; set; }
        public string ExecutionTime { get; set; }
        public string TransactionID { get; set; }

    }

    public class MandatoryField
    {
        public string InputField { get; set; }
        public string CRMField { get; set; }
        public string CRMValue { get; set; }
        public string CRMType { get; set; }
        public string IDFieldName { get; set; }
        public string CRMTable { get; set; }
        public string FilterField { get; set; }
    }

}
