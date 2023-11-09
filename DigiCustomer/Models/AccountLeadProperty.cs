using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CustomerLead
{
       
   
    
    public class CustomerLeadReturn
    {   
        public Applicent AccountApplicants { get; set; }
      
        public string ReturnCode { get; set; } 
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class Applicent
    {

    }

    public class AccountApplicantCorp : Applicent
    {
        public string title { get; set; }
        public string Companynamepart1 { get; set; }
        public string Companynamepart2 { get; set; }
        public string Companynamepart3 { get; set; }
        public string pan { get; set; }

    }

    public class AccountApplicantIndv : Applicent
    {
      
        public string title { get; set; }
        public string firstname { get; set; }
        public string middlename { get; set; }
        public string lastname { get; set; }      
        public string pan { get; set; }
       

    }


    public class DedupeDigiCustomerReturn
    {
        public dynamic decideNL { get; set; }
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }
    }

    public class Individual
    {
        public string NAME { get; set; }
        public string FATHER_NAME { get; set; }
        public string FATHER_SPOUSE_NAME { get; set; }
        public string MOTHER_NAME { get; set; }
        public string SPOUSE_NAME { get; set; }
        public string DATE_OF_BIRTH { get; set; }
        public string DATE_OF_INC { get; set; }
        public string AADHAR_REFERENCE_NO { get; set; }
        public string PAN { get; set; }
        public string PASSPORT_NO { get; set; }
        public string DRIVING_LICENSE_NO { get; set; }
        public string TIN { get; set; }
        public string TAN { get; set; }
        public string CIN { get; set; }
        public string DIN { get; set; }
        public string NREGA { get; set; }
        public string CKYC { get; set; }
        public string VOTER_CARD_NO { get; set; }
        public string GST_IN { get; set; }
        public string RATION_CARD { get; set; }
        public string MOBILE_TYPE_0 { get; set; }
        public string MOBILE_0 { get; set; }
        public string MOBILE_TYPE_1 { get; set; }
        public string MOBILE_1 { get; set; }
        public string MOBILE_TYPE_2 { get; set; }
        public string MOBILE_2 { get; set; }
        public string MOBILE_TYPE_3 { get; set; }
        public string MOBILE_3 { get; set; }
        public string MOBILE_TYPE_4 { get; set; }
        public string MOBILE_4 { get; set; }
        public string RECORD_TYPE { get; set; }
        public string MATCH_CRITERIA { get; set; }
        public string IS_EXACT_MATCH { get; set; }
        public string IS_PROBABLE_MATCH { get; set; }
        public string MATCHED_ID { get; set; }
        public string UCIC { get; set; }
        public string LEAD_ID { get; set; }
        public string IND_NON_DETAILS { get; set; }
    }

    public class IndividualsData
    {
        public List<Individual> INDIVIDUAL_MATCHES { get; set; }
    }
    public class OrganisationsData
    {
        public List<Individual> ORGANISATION_MATCHES { get; set; }
    }



}
