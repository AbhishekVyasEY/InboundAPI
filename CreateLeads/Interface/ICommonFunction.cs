using Newtonsoft.Json.Linq;

namespace CreateLeads
{
    public interface ICommonFunction
    {
        public Task<Dictionary<string, string>> getProductId(string ProductCode);
        public Task<string> getCityId(string CityCode);
        public Task<string> getBranchId(string BranchCode);
        public Task<JArray> getCustomerDetail(string CustomerCode);
        public Task<string> getEntityTypeId(string EntityTypeCode);
        public Task<string> getLeadIdByApplicent(string Applicent);
        public Task<string> getLeadId(string Lead);
        public Task<string> getSubEntityTypeId(string subEntityTypeCode);
        public Task<string> MeargeJsonString(string json1, string json2);

        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);
    }
}
