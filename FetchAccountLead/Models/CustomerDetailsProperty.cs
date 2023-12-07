using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace FetchAccountLead
{
    public class FetchCustomerDtlReturn
    {
        public GeneralDetails General { get; set; }
        public CustomerPreferences CustomerPreferences { get; set; }
        public List<ServiceDetails> DSBServiceDetails { get; set; }
        public RMDetails RMDetails { get; set; }
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class RMDetails
    {
        public string RMCode { get; set; }
        public string RMName { get; set; }
        public string RMRole { get; set; }
        public string RMType { get; set; }

        public string Area { get; set; }
        public string Branch { get; set; }
        public string BranchCode { get; set; }
        public string Cluster { get; set; }
        public string Department { get; set; }
        public string Divison { get; set; }
        public string EmailID { get; set; }
        public string EmpCategory { get; set; }
        public string EmpPhoneNumber { get; set; }
        public string EmpRoleLabel { get; set; }
        public string EmpStatus { get; set; }
        public string Region { get; set; }
        public string ResignedFlag { get; set; }
        public string State { get; set; }
        public string SupervisorEmailID { get; set; }
        public string SupervisorEmpID { get; set; }
        public string SupervisorName { get; set; }
        public string Zone { get; set; }

    }

    public class CustomerPreferences
    {
        public string NetBanking { get; set; }
        public string MobileBanking { get; set; }
        public string SMS { get; set; }
        public string AllSMSAlerts { get; set; }
        public string PhysicalStatement { get; set; }
        public string EmailStatement { get; set; }

    }

    public class ServiceDetails
    {
        public string ServiceName { get; set; }
        public string IsRegistered { get; set; }
        public string ServiceType { get; set; }
        public string Limit { get; set; }
        public string VendorID { get; set; }
        public string VendorName { get; set; }
        public string Location { get; set; }

    }

    public class GeneralDetails
    {
        public string IsMinor { get; set; }
        public string IsMFICustomer { get; set; }
        public string NPAClassification { get; set; }
        public string PANVerifiedStatus { get; set; }
        public string IsDeferral { get; set; }
        public string Form60 { get; set; }
        public string PoliticallyExposedPerson { get; set; }
        public string PurposeOfCreation { get; set; }
        public string NRIKYCMode { get; set; }
        public string NRIMobileNoPref { get; set; }
        public string NRIVisaType { get; set; }
        public string AlternateMandatoryCheck { get; set; }
        public string NPO { get; set; }
        public string EntityFlag { get; set; }
        public string EntityKey { get; set; }

    }
}
