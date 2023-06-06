using Newtonsoft.Json.Linq;

namespace DigiCustLead
{
    public interface ICommonFunction
    {
        public Task<JArray> getAccountData(string AccountNumber);
        public Task<string> getBranchId(string BranchCode);
        public Task<JArray> getContactData(string contact_id);
        public Task<string> getEntityID(string Entity);
        public Task<string> getPurposeID(string Purpose);
        public Task<Dictionary<string, string>> getProductId(string ProductCode);
        public Task<string> getTitleId(string Title);
        public Task<string> MeargeJsonString(string json1, string json2);



    }
}
