using Newtonsoft.Json.Linq;

namespace DigiDocument
{
    public interface ICommonFunction
    {      
        public Task<string> getDocCategoryId(string product_Cat_Id);
        public Task<string> getDocSubentityId(string subentity_Id);
        public Task<string> getDocTypeId(string docsubcategory);
        public Task<string> getSystemuserId(string system_user);
        public Task<string> getLeadId(string leadid);
        public Task<string> getLeadAccountId(string leadaccid);
        public Task<string> getCustomerId(string customer);
        public Task<string> getAccountId(string account);
        public Task<string> getCaseId(string caseid);
        public Task<string> getDocumentID(string Documentid);

        public Task<List<Document>> getDocumentList(string query_url);

        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);

    }
}
