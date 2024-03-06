using Newtonsoft.Json.Linq;

namespace DigiCustLead
{
    public interface ICommonFunction
    {
        public Task<string> getDDEFinalAccountIndvData(string AccountNumber);
        public Task<string> getDDEFinalAccountCorpData(string AccountNumber);
        public Task<string> getBranchId(string BranchCode);
        public Task<string> getRMId(string Code);
        public Task<JArray> getContactData(string contact_id);
        public Task<string> getEntityID(string Entity);
        public Task<string> getSubentitytypeID(string SubEntityType, string SubEntityKey);
        public Task getSubentitytypeText(string SubentitytypeID);
        public Task<string> getPurposeID(string Purpose);
        public Task<Dictionary<string, string>> getProductId(string ProductCode);
        public Task<JArray> getDDEFinalIndvCustomerId(string ddeId);
        public Task<JArray> getDDEFinalCorpCustomerId(string ddeId);
        public Task<string> getTitleId(string Title);
        public Task getTitleText(string TitleID);
        public Task<string> MeargeJsonString(string json1, string json2);
        public Task<JArray> getApplicentData(string Applicent_id);
        public Task<string> getEntityName(string EntityId);
        public Task<string> getRelationshipID(string relationshipCode);
        public Task<string> getAccRelationshipID(string accrelationshipCode);
        public Task<string> getCountryID(string CountryCode);
        public Task<string> getPincodeID(string PincodeCode);
        public Task<string> getCityID(string CityCode);
        public Task<string> getStateID(string StateCode);
        public Task<string> getAddressID(string DDEID, string AddressID, string types);
        public  Task<string> getFatcaID(string DDEID, string types);
        public Task<string> getDocumentId(string ddeId);
        public Task<string> getDocCategoryId(string doccatCode);
        public Task<string> getDocSubCategoryId(string docsubcatCode);
        public Task<string> getDocTypeId(string docTypeCode);
        public Task<string> getBusinessTypeId(string businessTypeCode);
        public Task<string> getIndustryId(string industryName);
        public Task<string> getBOId(string DDEID);
        public Task<string> getCPId(string DDEID);
        public Task<JArray> getDDEFinalIndvDetail(string AccountNumber);
        public Task<JArray> getDDEFinalCorpDetail(string AccountNumber);
        public Task<JArray> getDDEFinalAddressDetail(string DDEId, string type);
        public Task getBranchText(string BranchwId);
        public Task<string> getRelationshipText(string relationshipId);
        public Task getAccRelationshipText(string accrelationshipId);
        public Task<string> getFatcaAddressID(string FatcaID, string AddressID);
        public Task getPurposeText(string Purpose);
        public Task<string> getCountry_Text(string CountryId);
        public Task getCountryText(string CountryId);
        public Task<string> getcorporatemasterID(string CorporateCode);
        public Task getcorporatemasterText(string CorporateId);
        public Task<string> getdesignationmasterID(string DesignatioCode);
        public Task getdesignationmasterText(string DesignatioId);
        public Task getBusinessTypeText(string businessTypeId);
        public Task getIndustryText(string industryId);
        public Task getStateText(string StateID);
        public Task getCityText(string CityId);
        public Task<string> getNomineeText(string FatcaID);
        public Task<string> getCorporateDDEText(string DDEID);
        public Task<string> getFatcaText(string FatcaID);
        public Task<string> getIndividualDDEText(string DDEID);
        public Task<JArray> getDDEFinalFatcaDetail(string DDEId, string type);
        public Task<string> getKYCVerificationID(string DDEId, string type);
        public Task<string> getDDEEntry(string AccountapplicantID, string type);
        public Task<JArray> getAccountApplicantDetail(string AccountApplicantID);
        public Task<string> getCustomerText(string customerId);
        public Task getAccountapplicantName(string AccountapplicantId);
        public Task getLeadsourceName(string leadsourceid);
        public Task<string> getLeadsourceId(string leadsourcename);
        public Task getSystemuserName(string systemuserid);
        public Task getBankName(string bankid);
        public Task<JArray> getDDEFinalDocumentDetail(string DDEId, string type);
        public Task<JArray> getkycverificationDetail(string kycverificationId);
        public Task<string> getDocCategoryText(string doccatId);
        public Task<string> getDocSubCategoryText(string docsubcatId);
        public Task<string> getDocTypeText(string docTypeId);
        public Task<JArray> getDDEFinalCPDetail(string DDEId);
        public Task<JArray> getDDEFinalBODetail(string DDEId);
        public Task<JArray> getFATCAAddress(string FatcaID);
        public Task<string> getEducationId(string Education);
        public Task<string> getProfessionId(string Qualification);

    }
}
