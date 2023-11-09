using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CreateLeads
{
    
    public class LeadMsdProperty
    {
       // public string leadid { get; set; }
        public string eqs_leadchannel { get; set; }     
        public string firstname { get; set; }
        public string middlename { get; set; }
        public string lastname { get; set; }
        public string eqs_companynamepart1 { get; set; }
        public string eqs_companynamepart2 { get; set; }
        public string eqs_companynamepart3 { get; set; }
        public string eqs_dob { get; set; }
        public string eqs_dateofincorporation { get; set; }
        public string eqs_gendercode { get; set; }
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
        public string eqs_ucic { get; set;}
        

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
        public string LeadID { get; set; }
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class UpdateLidStatusReturnParam
    {       
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

}
