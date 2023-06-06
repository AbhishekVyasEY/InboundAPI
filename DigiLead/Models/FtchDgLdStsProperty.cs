using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DigiLead
{
    
   

   
    public class FtchDgLdStsReturn
    {
        public string LeadID { get; set; }
        public IndividualDetails? individualDetails { get; set; }
        public CorporateDetails? corporateDetails { get; set; }
        public string ReturnCode { get; set; } 
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class IndividualDetails
    {
        public string title { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string middleName { get; set; }
        public string shortName { get; set; }
        public string mobilePhone { get; set; }
        public string dob { get; set; }
        public string aadhar { get; set; }
        public string PAN { get; set; }
        public string motherMaidenName { get; set; }
        public string identityType { get; set; }
        public string NLFound { get; set; }
        public string purposeOfCreation { get; set; }
        public string reasonNotApplicable { get; set; }
        public string reason { get; set; }
        public string voterid { get; set; }
        public string drivinglicense { get; set; }
        public string passport { get; set; }
        public string ckycnumber { get; set; }

    }

    public class CorporateDetails
    {
        public string companyName { get; set; }
        public string companyName2 { get; set; }
        public string companyName3 { get; set;}
        public string companyPhone { get; set;}
        public string aadhar { get; set;}
        public string pocNumber { get; set; }
        public string pocName { get; set; }
        public string cinNumber { get; set;}
        public string dateOfIncorporation { get; set;}
        public string pan { get; set; }
        public string tanNumber { get; set; }
        public string tinNumber { get; set; }
        public string NLFound { get; set; }
        public string reason { get; set; }
        public string identityType { get; set; }
        public string gstNumber { get; set; }
        public string alternateMandatoryCheck { get; set; }
        public string purposeOfCreation { get; set; }
        public string cstNumber { get; set; }

    }




}
