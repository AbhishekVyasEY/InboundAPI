using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DigiCustLead
{

    public class CustLeadElement
    {
        public string eqs_crmproductcategorycode { get; set; }
        public int leadsourcecode { get; set; }
        public string firstname { get; set; }
        public string middlename { get; set; }
        public string lastname { get; set; }
        public string mobilephone { get; set; }
        public string yomifullname
        {
            get
            {
                return firstname + " " + middlename + " " + lastname;
            }

        }

        public string eqs_pincode { get; set; }
        public string  eqs_dob { get; set; }
        public string eqs_internalpan { get; set; }
        public string eqs_voterid { get; set; }
        public string eqs_dlnumber { get; set; }
        public string eqs_passportnumber { get; set; }
        public string eqs_ckycnumber { get; set; }

    }

    public class CustLeadElementCorp
    {
        public string eqs_crmproductcategorycode { get; set; }
        public int leadsourcecode { get; set; }
        public string eqs_companynamepart1 { get; set; }
        public string eqs_companynamepart2 { get; set; }
        public string eqs_companynamepart3 { get; set; }
        public string eqs_contactmobile { get; set; }
        public string eqs_contactperson { get; set; }
        public string eqs_cinnumber { get; set; }
        public string eqs_tannumber { get; set; }
        public string eqs_gstnumber { get; set; }
        public string eqs_cstvatnumber { get; set; }
 
        public string eqs_internalpan { get; set; }
    }

    public class CreateCustLeadReturn
    {
        public string AccountapplicantID { get; set; }
        public string LeadID { get; set; }

        public string ReturnCode { get; set; } 
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class UpdateCustLeadReturn
    {
      
        public string FATCAID { get; set; }
        public List<AddressResponse> Address { get; set; }
        public List<BOResponse> BOID { get; set; }
        public List<CPResponse> CPID { get; set; }
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class AddressResponse
    {
        public AddressResponse(string addressType, string addressId)
        {
            AddressType = addressType;
            AddressId = addressId;
        }
        public string AddressType { get; set; }
        public string AddressId { get; set; }
    }

    public class BOResponse
    {
        public BOResponse(string boId, string boListingType, string boName)
        {
            BOID = boId;
            BOListingType = boListingType;
            BOName = boName;
        }
        public string BOID { get; set; }
        public string BOListingType { get; set; }
        public string BOName { get; set; }
    }

    public class CPResponse
    {
        public CPResponse(string cpId, string nameofCP)
        {
            CPID = cpId;
            NameofCP = nameofCP;
        }
        public string CPID { get; set; }
        public string NameofCP { get; set; }
    }

    public class FetchCust_LeadReturn
    {
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }
    }

    public class FetchCustD0Return : FetchCust_LeadReturn
    {
        public string EntityType { get; set; }        
        public string EntityFlagType { get; set; }
        public string ProductCode { get; set; }
        public IndividualEntry IndividualEntry { get; set; }
        public CorporateEntry CorporateEntry { get; set; }
        public string UCIC { get; set; }
        public string BranchCode { get; set; }
    }

    public class IndividualEntry
    {
        public string ApplicantId { get; set; }
        public string Drivinglicense { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string ShortName { get; set; }
        public string Title { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string PurposeOfCreation { get; set; }
        public string MobilePhone { get; set; }
        public string Passport { get; set; }
        public string AadharReference { get; set; }
        public string Dob { get; set; }
        public string CKYCNumber { get; set; }
        public string Voterid { get; set; }
        public string PANForm60 { get; set; }
        public string PAN { get; set; }
        public string Pincode { get; set; }
        public string MotherMaidenName { get; set; }
        public string ReasonNotApplicable { get; set; }
    }

    public class CorporateEntry
    {
        public string CompanyName { get; set; }
        public string CompanyName2 { get; set; }
        public string CompanyName3 { get; set; }
        public string PocNumber { get; set; }
        public string PocName { get; set; }
        public string CinNumber { get; set; }
        public string TanNumber { get; set; }
        public string GstNumber { get; set; }
        public string CstNumber { get; set; }
        public string PAN { get; set; }
        public string DateOfIncorporation { get; set; }
        public string PurposeOfCreation { get; set; }
    }


    public class FetchCustLeadReturn : FetchCust_LeadReturn
    {
        public Individual individual { get; set; }
        public Corporate corporate { get; set; }       
    }

    public class Individual
    {
        public Generalind general { get; set; }
        public ProspectDetails prospectDetails { get; set; }
        public IdentificationDetails identificationDetails { get; set; }
        public KYCVerification kycverification { get; set; }
        public List<Address> address { get; set; }
        public RMDetails RMDetails { get; set; }
        public FATCA fatca { get; set; }
        public NRIDetails nridetails { get; set; }
  
    }

    public class Generalind
    {
        public string DDEId { get; set; }
        public string AccountApplicant { get; set; }
        public string EntityType { get; set; }
        public string SubEntityType { get; set; }
        public string IsPrimaryHolder { get; set; }
        public string ResidencyType { get; set; }
        public string SourceBranch { get; set; }
        public string CustomerspreferredBranch { get; set; }
        public string LGCode { get; set; }
        public string LCCode { get; set; }
        public string RelationshiptoPrimaryHolder { get; set; }
        public string AccountRelationship { get; set; }
        public string PhysicalAOFnumber { get; set; }
        public string IsMFICustomer { get; set; }
        public string IsDeferral { get; set; }
        public string PurposeofCreation { get; set; }
        public string InstaKitCustomerId { get; set; }
        public string Deferral { get; set; }
        public string CustomerIdCreated { get; set; }
        public string DataValidated { get; set; }
        public string LeadNumber { get; set; }
        public string LeadChannel { get; set; }
        public string SourceofLead { get; set; }
        public string Leadcreatedon { get; set; }
        public string Leadcreatedby { get; set; }
    }


    public class Corporate
    {
        public Generalcorp general { get; set; }
        public ProspectDetailscorp prospectDetails { get; set; }
        public IdentificationDetailscorp identificationDetails { get; set; }
        public KYCVerification kycverification { get; set; }
        public List<Address> address { get; set; }
        public RMDetails RMDetails { get; set; }
        public CorpFATCA fatca { get; set; }   
        public PointOfContact pointOfContact { get; set; }
        public List<BODetails> boDetails { get; set; }
        public List<CPDetails> cpDetails { get; set; }
    }

    public class Generalcorp
    {
        public string Name { get; set; }
        public string EntityType { get; set; }
        public string SubEntityType { get; set; }
        public string BankName { get; set; }
        public string ResidencyType { get; set; }
        public string SourceBranchTerritory { get; set; }
        public string CustomerspreferredBranch { get; set; }
        public string LGCode { get; set; }
        public string LCCode { get; set; }
        public string DataEntryStage { get; set; }
        public string AccountApplicant { get; set; }
        public string IsPrimaryHolder { get; set; }
        public string AccountRelationship { get; set; }
        public string InstaKitCustomerID { get; set; }
        public string PhysicalAOFnumber { get; set; }
        public string IsaMFICustomer { get; set; }
        public string IsDeferral { get; set; }
        public string PurposeofCreation { get; set; }
        public string CustomerIdCreated { get; set; }
        public string IsCommAddrRgstOfficeAddrSame { get; set; }
}
    public class ProspectDetailscorp
    {
        public Aboutprospect aboutprospect { get; set; }
        public Aboutbusiness aboutbusiness { get; set; }
    }

    public class Aboutprospect
    {
        public string Title { get; set; }
        public string CompanyName1 { get; set; }
        public string CompanyNamePart2 { get; set; }
        public string CompanyNamePart3 { get; set; }
        public string ShortName { get; set; }
        public string DateofIncorporation { get; set; }
        public string PreferredLanguage { get; set; }
        public string IsCompanyListed { get; set; }
        public string EmailId { get; set; }
        public string FaxNumber { get; set; }
        public string Program { get; set; }
        public string NPOPO { get; set; }
        public string IsAMFIcustomer { get; set; }
        public string UCICCreatedOn { get; set; }
        public string LOBCode { get; set; }
        public string AOBusinessOperation { get; set; }

    }
    public class Aboutbusiness
    {
        public string BusinessType { get; set; }
        public string Industry { get; set; }
        public string IndustryOthers { get; set; }
        public string CompanyTurnover { get; set; }
        public string CompanyTurnoverValue { get; set; }
        public string NoofBranchesRegionalOffices { get; set; }
        public string CurrentEmployeeStrength { get; set; }
        public string AverageSalarytoEmployee { get; set; }
        public string MinimumSalarytoEmployee { get; set; }

    }

    public class ProspectDetails
    {
        public string Title { get; set; }
        public string Firstname { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string DateofBirth { get; set; }
        public string Education { get; set; }
        public string MaritalStatus { get; set; }
        public string Profession { get; set; }
        public string AnnualIncomeBand { get; set; }
        public string EmployerType { get; set; }
        public string Age { get; set; }
        public string Gender { get; set; }
        public string ShortName { get; set; }
        public string MobileNumber { get; set; }
        public string EmailID { get; set; }
        public string Nationality { get; set; }
        public string EmployerName { get; set; }
        public string IsPhysicallyChallenged { get; set; }
        public string OfficePhone { get; set; }
        public string WorkPlaceAddress { get; set; }
        public string Countryofbirth { get; set; }
        public string CityofBirth { get; set; }
        public string FathersName { get; set; }
        public string MothersMaidenName { get; set; }
        public string SpouseName { get; set; }
        public string Country { get; set; }
        public string Program { get; set; }
        public string Designation { get; set; }
        public string EstimatedAgriculturalIncome { get; set; }
        public string EstimatedNonAgriculturalIncome { get; set; }
        public string IsStaff { get; set; }
        public string EquitasStaffCode { get; set; }
        public string Language { get; set; }
        public string PolitcallyExposedPerson { get; set; }
        public string LOBCode { get; set; }
        public string AOBusinessOperation { get; set; }
        public string LOBType { get; set; }

    }

    public class IdentificationDetailscorp
    {
        public string POCPanForm60 { get; set; }
        public string POCPANNumber { get; set; }
        public string TANNumber { get; set; }
        public string CSTVATnumber { get; set; }
        public string GSTNumber { get; set; }
        public string CKYCRefenceNumber { get; set; }
        public string CINRegisteredNumber { get; set; }
        public string CKYCUpdatedDate { get; set; }
        public string KYCVerificationMode { get; set; }
        public string KYCVerificationDate { get; set; }
    }
    public class IdentificationDetails
    {
        public string PanForm60 { get; set; }
        public string PanNumber { get; set; }
        public string AadharReference { get; set; }
        public string CKYCreferenceNumber { get; set; }
        public string KYCVerificationMode { get; set; }
        public string InternalPAN { get; set; }
        public string PanAcknowledgementNumber { get; set; }
        public string PassportNumber { get; set; }
        public string VoterID { get; set; }
        public string DrivinglicenseNumber { get; set; }
        public string VerificationDate { get; set; }
    }
    public class KYCVerification
    {
        public string KYCVerificationID { get; set; }
        public string EmpName { get; set; }
        public string EmpID { get; set; }
        public string EmpDesignation { get; set; }
        public string EmpBranch { get; set; }
        public string InstitutionName { get; set; }
        public string InstitutionCode { get; set; }
    }
    public class Address
    {
        public string AddressId { get; set; }
        public string ApplicantAddress { get; set; }
        public string AddressType { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressLine3 { get; set; }
        public string AddressLine4 { get; set; }
        public string MobileNumber { get; set; }
        public string FaxNumber { get; set; }
        public string OverseasMobileNumber { get; set; }
        public string PinCodeMaster { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PinCode { get; set; }
        public string POBox { get; set; }
        public string Landmark { get; set; }
        public string LandlineNumber { get; set; }
        public string AlternateMobileNumber { get; set; }
        public string LocalOverseas { get; set; }
    }
    public class RMDetails
    {
        public string ServiceRMCode { get; set; }
        public string ServiceRMName { get; set; }
        public string ServiceRMRole { get; set; }
        public string BusinessRMCode { get; set; }
        public string BusinessRMName { get; set; }
        public string BusinessRMRole { get; set; }
    }
    public class FATCA
    {
        public string TaxResident { get; set; }
        public string CityofBirth { get; set; }

        public FATCADetails FATCADetails { get; set; }
    }

    public class CorpFATCA
    {
        public string TaxResidentType { get; set; }
        public string FinancialType { get; set; }
        public string CityOfIncorporation { get; set; }
        public string CountryOfIncorporation { get; set; }

        public FATCADetails FATCADetails { get; set; }
    }
    public class FATCADetails
    {
        public string FATCAID { get; set; }
        public string TaxResident { get; set; }
        public string CityofBirth { get; set; }
        public string Country { get; set; }
        public string OtherIdentificationNumber { get; set; }
        public string TaxIdentificationNumber { get; set; }
        public string FATCADeclaration { get; set; }
        public string CountryofTaxResidencyTaxType { get; set; }
        public string AddressType { get; set; }
        public FATCAAddress Address { get; set; }
    }
    public class FATCAAddress
    {
        public string FATCAAddressID { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressLine3 { get; set; }
        public string AddressLine4 { get; set; }
        public string MobileNumber { get; set; }
        public string FaxNumber { get; set; }
        public string OverseasMobileNumber { get; set; }
        public string PinCodeMaster { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PinCode { get; set; }
        public string POBox { get; set; }
        public string Landmark { get; set; }
        public string LandlineNumber { get; set; }
        public string AlternateMobileNumber { get; set; }
        public string LocalOverseas { get; set; }
    }

    public class NRIDetails
    {
        public string VisaType { get; set; }
        public string VisaIssuedDate { get; set; }
        public string KYCMode { get; set; }
        public string VisaExpiryDate { get; set; }
        public string Seafarer { get; set; }
        public string VISAOCICDCNumber { get; set; }
        public string TaxIdentificationNumber { get; set; }
        public string ResidenceStatus { get; set; }
        public string OtherIdentificationNumber { get; set; }
        public string SMSOTPMobilepreference { get; set; }
        public string PassportIssuedAt { get; set; }
        public string PassportIssuedDate { get; set; }
        public string TaxType { get; set; }
        public string PassportExpiryDate { get; set; }
    }
    public class PointOfContact
    {
        public string ContactPersonName { get; set; }
        public string POClookUp { get; set; }
        public string POCEmailId { get; set; }
        public string ContactMobilePhone { get; set; }
        public string POCUCIC { get; set; }
        public string POCPhoneNumber { get; set; }
    }
    public class BODetails
    {
        public string BOID { get; set; }
        public string BOType { get; set; }
        public string BOListingType { get; set; }
        public string BOUCIC { get; set; }
        public string BOName { get; set; }
        public string BOListingDetails { get; set; }
        public string Holding { get; set; }
    }
    public class CPDetails
    {
        public string CPID { get; set; }
        public string NameofCP { get; set; }
        public string CPUCIC { get; set; }
        public string Holding { get; set; }
    }


}
