using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace GFSProduct
{

    public class productFilter
    {
        public string gender { get; set; }
        public string age { get; set; }
        public string subentity { get; set; }
        public string customerSegment { get; set; }
        public string IsStaff { get; set; }
        public string productCategory { get; set; }
    }

    
    public class GFSProducrListReturn
    {
        public List<RDProductsApplicable>? rdproductsApplicable { get; set; }
        public List<FDProductsApplicable>? fdproductsApplicable { get; set; }
        public List<ApplicableCASAProducts>? applicableCASAProducts { get; set; }
       
        public string ReturnCode { get; set; } 
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class ApplicableCASAProducts
    {
        public string productCode { get; set; }
        public string productName { get; set; }
    
        public bool? chequeBook { get; set; }
        public bool? debitCard { get; set; }
        public string applicableDebitCard { get; set; }
        public string defaultDebitCard { get; set; }
        public bool? instaKit { get; set; }
        public bool? doorStep { get; set; }
       
        public bool? PMAY { get; set; }
        public bool? isElite { get; set; }
        public string srnoofchequeleaves { get; set; }
        public string noofchequeleaves { get; set; }
        public string srdefaultchequeleaves { get; set; }
        public string defaultchequeleaves { get; set; }


    }

    public class FDProductsApplicable
    {
        public int? productCode { get; set; }
        public string productName { get; set; }
       
        public string minTenureDays { get; set; }
        public string maxTenureDays { get; set; }
        public string minTenureMonths { get; set; }
        public string maxTenureMonths { get; set; }
        public string minAmount { get; set; }
        public string maxAmount { get; set; }
        public int? depositVariance { get; set; }
        public string payoutFrequency { get; set; }
        public string compoundingFrequency { get; set; }
        public string renewalOptions { get; set; }
        public string payoutFrequencyType { get; set; }
        public string compoundingFrequencyType { get; set; }
        public bool? isElite { get; set; }
        public bool? isDisplayForBranch { get; set; }
    }

    public class RDProductsApplicable
    {
        public int? productCode { get; set; }
        public string productName { get; set; }
        
        public string minTenureDays { get; set; }
        public string maxTenureDays { get; set; }
        public string minTenureMonths { get; set; }
        public string maxTenureMonths { get; set; }
        public string minAmount { get; set; }
        public string maxAmount { get; set; }
        public int? depositVariance { get; set; }
        public string payoutFrequency { get; set; }
        public string compoundingFrequency { get; set; }
        public string renewalOptions { get; set; }
        public string payoutFrequencyType { get; set; }
        public string compoundingFrequencyType { get; set; }
        public bool? isElite { get; set; }
        public bool? isDisplayForBranch { get; set; }
    }





}
