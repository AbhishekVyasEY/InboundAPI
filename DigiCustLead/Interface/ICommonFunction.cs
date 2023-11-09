using Newtonsoft.Json.Linq;

namespace DigiCustLead
{
    public interface ICommonFunction
    {
        public Task<string> getDDEFinalAccountIndvData(string AccountNumber);
        public Task<string> getDDEFinalAccountCorpData(string AccountNumber);
        public Task<string> getBranchId(string BranchCode);
        public Task<JArray> getContactData(string contact_id);
        public Task<string> getEntityID(string Entity);
        public Task<string> getSubentitytypeID(string Subentitytype);
        public Task<string> getSubentitytypeText(string SubentitytypeID);
        public Task<string> getPurposeID(string Purpose);
        public Task<Dictionary<string, string>> getProductId(string ProductCode);
        public Task<string> getTitleId(string Title);
        public Task<string> getTitleText(string TitleID);
        public Task<string> MeargeJsonString(string json1, string json2);
        public Task<JArray> getApplicentData(string Applicent_id);
        public Task<string> getEntityName(string EntityId);
        public Task<string> getRelationshipID(string relationshipCode);
        public Task<string> getAccRelationshipID(string accrelationshipCode);
        public Task<string> getCountryID(string CountryCode);
        public Task<string> getPincodeID(string PincodeCode);
        public Task<string> getCityID(string CityCode);
        public Task<string> getStateID(string StateCode);
        public Task<string> getAddressID(string DDEID, string types);
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
        public Task<string> getBranchText(string BranchwId);
        public Task<string> getRelationshipText(string relationshipId);
        public Task<string> getAccRelationshipText(string accrelationshipId);
        public Task<string> getFatcaAddressID(string FatcaID);
        public Task<string> getPurposeText(string Purpose);
        public Task<string> getCountryText(string CountryId);
        public Task<string> getcorporatemasterID(string CorporateCode);
        public Task<string> getcorporatemasterText(string CorporateId);
        public Task<string> getdesignationmasterID(string DesignatioCode);
        public Task<string> getdesignationmasterText(string DesignatioId);
        public Task<string> getBusinessTypeText(string businessTypeId);
        public Task<string> getIndustryText(string industryId);
        public Task<string> getStateText(string StateID);
        public Task<string> getCityText(string CityId);
        public Task<string> getNomineeText(string FatcaID);
        public Task<string> getCorporateDDEText(string DDEID);
        public Task<string> getFatcaText(string FatcaID);
        public Task<string> getIndividualDDEText(string DDEID);
        public Task<JArray> getDDEFinalFatcaDetail(string DDEId, string type);
        public Task<string> getKYCVerificationID(string DDEId, string type);
        public Task<string> getCustomerText(string customerId);
        public Task<string> getAccountapplicantName(string AccountapplicantId);
        public Task<string> getLeadsourceName(string leadsourceid);
        public Task<string> getSystemuserName(string systemuserid);
        public Task<string> getBankName(string bankid);
        public Task<JArray> getDDEFinalDocumentDetail(string DDEId, string type);
        public Task<JArray> getkycverificationDetail(string kycverificationId);
        public Task<string> getDocCategoryText(string doccatId);
        public Task<string> getDocSubCategoryText(string docsubcatId);
        public Task<string> getDocTypeText(string docTypeId);
        public Task<JArray> getDDEFinalCPDetail(string DDEId);
        public Task<JArray> getDDEFinalBODetail(string DDEId);
        public Task<JArray> getFATCAAddress(string FatcaID);


    }
}
