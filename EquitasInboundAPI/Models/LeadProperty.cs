using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace EquitasInboundAPI
{
    
    public class LeadMsdProperty
    {
       // public string leadid { get; set; }
        public int leadsourcecode { get; set; }
        public string firstname { get; set; }
        public string middlename { get; set; }
        public string lastname { get; set; }
        public string yomifullname
        {
            get
            {
                return firstname + " " + lastname;
            }

        }

        public string description { get; set; }
        public string mobilephone { get; set; }
        public string eqs_pincode { get; set; }
        public string emailaddress1 { get; set; }
        public string eqs_crmproductcategorycode { get; set;}
      //  public string eqs_SelfieJourneyStatus { get; set;}
        

    }


    public class LeadProperty
    {
        public string LeadID { get; set; }
        

        [JsonPropertyName("FirstName")]
        public string firstname { get; set; }

        [JsonPropertyName("LastName")]
        public string lastname { get; set; }

        
        public string yomifullname {
            get
            {
                return firstname + " " + lastname;
            }
             
        }

        [JsonPropertyName("MobileNumber")]
        public string mobilephone { get; set; }

        [JsonPropertyName("Pincode")]
        public string eqs_pincode { get; set; }

        public string emailaddress1 { get; set; }


        public string CityName { get; set; }
        public string CityId { get; set; }

        public string BranchCode { get; set; }
        public string BranchId { get; set; }        

        public string CustomerID { get; set; }
        public string ETBCustomerID { get; set; }
        
        public string ProductCode { get; set; }
        public string ProductId { get; set; }
        public string Businesscategoryid { get; set; }
        public string Productcategoryid { get; set;}



    }

    public class LeadReturnParam
    {
        public int IsError { get; set; }
        public string LeadID { get; set; }
        public string ErrorMessage { get; set; }
        public string InfoMessage { get; set; }

    }

}
