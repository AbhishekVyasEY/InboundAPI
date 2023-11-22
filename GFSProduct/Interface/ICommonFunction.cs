using Newtonsoft.Json.Linq;

namespace GFSProduct
{
    public interface ICommonFunction
    {      
        public Task<string> getCategoryId(string product_Cat_Id);
        public Task<string> getSubentity(string subentity_Id);
        public Task<JArray> getApplicantDetails(string ApplicantId);
        public Task<JArray> getCustomerDetails(string CustomerId);
        public Task<JArray> getProductData(productFilter product_Filter);
        public Task<JArray> getContactData(string contact_id);
        public Task<string> getEntityType(string EntityTypeId);
        public Task<JArray> getIndividualDdeDetails(string ApplicantId);
        public Task<JArray> getProductSubTypeLink(string SubTypeId);
        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);
    }
}
