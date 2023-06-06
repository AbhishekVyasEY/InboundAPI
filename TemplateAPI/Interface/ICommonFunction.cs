using Newtonsoft.Json.Linq;

namespace DigiWiz
{
    public interface ICommonFunction
    {
        public Task<JArray> getAccountData(string AccountNumber);
        public Task<string> getProductCatName(string product_Cat_Id);
        public Task<JArray> getContactData(string contact_id);


       

    }
}
