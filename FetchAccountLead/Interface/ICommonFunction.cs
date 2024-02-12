using Newtonsoft.Json.Linq;

namespace FetchAccountLead
{
    public interface ICommonFunction
    {      
        public Task getBranchCode(string branch_Id);
        public Task getStateCode(string state_id);
        public Task<JArray> getApplicentsSetails(string LeadAccId);
        public Task<JArray> getPreferences(string applicantid);
        public Task getProductCode(string Productid);
        public Task getProductCategoryCode(string Productcatid);
        public Task getCuntryCode(string Country_id);
        public Task getCityCode(string city_id);
        public Task<string> getTitleCode(string title_id);
        public Task<string> getRelationshipCode(string relationship_id);
        public Task getRelationshipName(string relationship_id);


        public Task<string> getUCIC(string accountapplicant_id);
        public Task<string> getDebitCard(string DebitCardId);

        public Task<JArray> getNomineDetails(string DDEId);
        public Task<JArray> getLeadDetails(string contact_id);
        public Task<JArray> getDataFromResponce(List<JObject> RsponsData);
        public Task<JArray> getLeadAccountDetails(string LdApplicantId, string stage = "");
        public Task<JArray> getCustomerDetails(string filterkey, string filtervalue);
        public Task<JArray> getAccountRelationshipDetails(string CustomerID);
        public Task<JArray> getServiceDetails(string customerid, string accountid);
        public Task<string> getPurposeOfCreation(string id);
        public Task<string> MeargeJsonString(string json1, string json2);
        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);
    }
}
