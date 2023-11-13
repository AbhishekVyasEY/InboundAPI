using Newtonsoft.Json.Linq;

namespace FetchAccountLead
{
    public interface ICommonFunction
    {      
        public Task<string> getBranchCode(string branch_Id);
        public Task<string> getStateCode(string state_id);
        public Task<JArray> getApplicentsSetails(string LeadAccId);
        public Task<JArray> getPreferences(string applicantid);
        public Task<string> getProductCode(string Productid);
        public Task<string> getProductCategoryCode(string Productcatid);
        public Task<string> getCuntryCode(string Country_id);
        public Task<string> getCityCode(string city_id);
        public Task<string> getTitleCode(string title_id);
        public Task<string> getRelationshipCode(string relationship_id);


        public Task<string> getUCIC(string accountapplicant_id);
        public Task<string> getDebitCard(string DebitCardId);

        public Task<JArray> getNomineDetails(string DDEId);
        public Task<JArray> getLeadDetails(string contact_id);
        public Task<JArray> getDataFromResponce(List<JObject> RsponsData);
        public Task<JArray> getLeadAccountDetails(string LdApplicantId);
        public Task<string> MeargeJsonString(string json1, string json2);
        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);

    }
}
