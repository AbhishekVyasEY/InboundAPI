using Newtonsoft.Json.Linq;

namespace FetchAccountLead
{
    public interface ICommonFunction
    {      
        public Task<string> getBranchCode(string branch_Id);
        public Task<string> getAccRelationshipCode(string AccRelationship_id);
        public Task<JArray> getApplicentsSetails(string LeadAccId);
        public Task<JArray> getPreferences(string applicantid);
        public Task<string> getProductCode(string Productid);
        public Task<string> getProductCategoryCode(string Productcatid);
        public Task<string> getEntityCode(string Entity_id);
        public Task<string> getSubEntityCode(string SubEntity_id);
        public Task<string> getTitleCode(string title_id);
        public Task<string> getRelationshipCode(string Relationship_id);

        public Task<JArray> getLeadDetails(string contact_id);
        public Task<JArray> getDataFromResponce(List<JObject> RsponsData);
        public Task<JArray> getLeadAccountDetails(string LdApplicantId);
        public Task<string> MeargeJsonString(string json1, string json2);
        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);

    }
}
