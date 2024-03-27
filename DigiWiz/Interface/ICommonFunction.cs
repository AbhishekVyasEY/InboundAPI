using Newtonsoft.Json.Linq;

namespace DigiWiz
{
    public interface ICommonFunction
    {
        public Task<JArray> getAccountData(string AccountNumber);
        public Task<string> getProductCatName(string product_Cat_Id);
        public Task getContactData(string contact_id);
        public Task<string> getEntityType(string EntityId);

        public Task<string> getSubEntityType(string subEntityId);
        public Task<JArray> getAllCustomers(string accountid);
        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);

    }
}
