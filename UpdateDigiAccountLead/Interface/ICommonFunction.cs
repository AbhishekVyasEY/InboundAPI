using Newtonsoft.Json.Linq;

namespace UpdateAccountLead
{
    public interface ICommonFunction
    {      
        public Task<string> getDocCategoryId(string Category_code);
        public Task<string> getDocSubcatId(string Subcat_code);
        public Task<JArray> getApplicentsSetails(string LeadAccId);
        public Task<JArray> getPreferences(string applicantid);
        public Task<string> getApplicentID(string ucic);
        public Task<string> getDebitCardID(string DebitCardcode);
        public Task<string> getNomineeID(string ddeID);
        public Task<string> getDocTypeId(string Type_code);
        public Task<string> getRelationShipId(string RelationShipCode);
        public Task<string> getAccountId(string accountNo);
        public Task<string> getCityId(string City_code);
        public Task<string> getCuntryId(string cuntry_code);
        public Task<string> getStateId(string state_code);
        public Task<string> getProductId(string ProductCode);
        public Task<string> getProductCategoryId(string CategoryCode);

        public Task<JArray> getLeadDetails(string contact_id);
        public Task<JArray> getAccountLeadData(string DDEId);
        public Task<JArray> getLeadAccountDetails(string LdApplicantId);
        public Task<string> MeargeJsonString(string json1, string json2);
        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);

        public Task<string> getPreferenceID(string PrederenceId, string DDEID);

    }
}
