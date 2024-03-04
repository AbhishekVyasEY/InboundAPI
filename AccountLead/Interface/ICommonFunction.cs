using Newtonsoft.Json.Linq;

namespace AccountLead
{
    public interface ICommonFunction
    {      
        public Task<string> getBranchId(string branch_Id);
        public Task<string> getBranchCode(string BranchId);
        public Task<string> getAccRelationshipId(string AccRelationship_code);
      

        public Task<Dictionary<string, string>> getProductId(string ProductCode);
        public Task<JArray> getAccountNominee(string leadaccountid);
        public Task<JArray> getAccountApplicd(string leadaccountid);
        public Task<string> getProductCode(string ProductId);
        public Task<string> getProductCategory(string CategoryId);
        public Task<string> getRelationshipId(string Relationship_code);
        public Task<string> getLeadSourceId(string LeadSource_code);

        public Task<JArray> getAccountLeadData(string contact_id);
        public Task<JArray> getDataFromResponce(List<JObject> RsponsData);
        public Task<string> MeargeJsonString(string json1, string json2);
        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);

        public Task<JArray> getAddressData(string individuaID, string type="");


        public Task<string> getCityName(string CityId);
        public Task<string> getStateName(string StateId);
        public Task<string> getCountryName(string CountryId);
        public Task<string> getAccountRelation(string accRelationId);
        public Task<JArray> getApplicentData(string ApplicantID);
        public Task<JArray> getApplicantIndivDDE(string ApplicantID);
        public Task<string> getCRMCodeTransformation(string apiName, string attribute);
        public Task<JArray> getApplicentFinalDDEbyAccountLead(string AccountDDEId);
        public Task<JArray> getApplicantCorpDDE(string ApplicantID);
        public Task<Dictionary<string, string>> getInstrakitStatus(string leadaccountdde_ID);

    }
}
