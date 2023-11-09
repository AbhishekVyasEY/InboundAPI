using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace AccountLead
{

    public class LeadParam
    {
        public string leadid { get; set; }
        public string Lead_id { get; set; }
        public string eqs_ucic { get; set; }
        public string productCategory { get; set; }
        public string productid { get; set; }
        public string branchid { get; set; }

        public string LeadAccountid { get; set; }
        public string LeadAccount_id { get; set; }

    }

    public class AccountLeadReturn
    {
        public string AccountLeadId { get; set; }
        public List<string> Applicants { get; set; }
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class AccountByLeadReturn
    {
        public string AccountNo { get; set; }
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class CustomerByLeadReturn
    {
        public string customerId { get; set; }
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class ApplicentRelation
    {
        public string customerId { get; set; }
        public string customerName { get; set; }
        public string relation { get; set; }
    }

    public class AccountApplicant
    {
        public string contactid { get; set; }
        public string title { get; set; }
        public string UCIC { get; set; }
        public string customerName { get; set; }       
        public string entityType { get; set; }
        public string subentityType { get; set; }
        public string customerAccountRelation { get; set; }
        public string customerAccountRelationTitle { get; set; }
        public bool? isPrimaryHolder { get; set; }
        public bool? isStaff { get; set; }
        public string relationToPrimaryHolder { get; set; }
        public string age { get; set; }

        public string customerPhoneNumber { get; set; }
        public string customerEmailID { get; set; }
        public string gender { get; set; }
        public string pan { get; set; }
        public string dob { get; set; }

        public string firstname { get; set; }
        public string lastname { get; set; }
        public string eqs_companynamepart1 { get; set; }
        public string eqs_companynamepart2 { get; set; }
        public string eqs_companynamepart3 { get; set; }
        public string eqs_dateofincorporation { get; set; }
        
        public Preferences preferences { get; set; }

    }

    public class Preferences
    {
        public bool? sms { get; set; }
        public bool? allSMSAlerts { get; set; }
        public bool? onlyTransactionAlerts { get; set; }
        public bool? passbook { get; set; }
        public bool? physicalStatement { get; set; }
        public bool? emailStatement { get; set; }
        public bool? netBanking { get; set; }
        public bool? bankGuarantee { get; set; }
        public bool? letterofCredit { get; set; }
        public bool? businessLoan { get; set; }
        public bool? doorStepBanking { get; set; }
        public bool? doorStepBankingOnCall { get; set; }
        public bool? doorStepBankingBeat { get; set; }
        public bool? tradeForex { get; set; }
        public bool? loanAgainstProperty { get; set; }
        public bool? overdraftsagainstFD { get; set; }
        public bool? preferencesCopied { get; set; }
        public bool? bankLevelAlerts { get; set; }
        public string netBankingRights { get; set; }
        public string mobileBankingNumber { get; set; }
        public string InternationalLimitActivation { get; set; }
        
    }

    public class AccountLead
    {
        public string accountType { get; set; }
        public string productCategory { get; set; }
        public string productCode { get; set; }
        public string accountOpeningFlow { get; set; }
        public string leadsource { get; set; }
        public string sourceBranch { get; set; }
        public string initialDepositType { get; set; }
        public string fieldEmployeeCode { get; set; }
        public string applicationDate { get; set; }
        public string tenureInMonths { get; set; }
        public string tenureInDays { get; set; }
        public string rateOfInterest { get; set; }
        public string fundsTobeDebitedFrom { get; set; }
        public string initialDeposit { get; set; }
        public string depositAmount { get; set; }
        public string mopRemarks { get; set; }
        public string fdAccOpeningDate { get; set; }
        public bool? sweepFacility { get; set; }


    }





}
