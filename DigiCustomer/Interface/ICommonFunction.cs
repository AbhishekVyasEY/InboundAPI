using Newtonsoft.Json.Linq;

namespace CustomerLead
{
    public interface ICommonFunction
    {       
        public Task<string> getCustomerType(string City_code);
        public Task<JArray> getApplicentDetail(string ApplicantId);
        public Task<string> getTitle(string TitleId);  
        public Task<JArray> getCustomerDetails(string CustId);
        public Task<string> MeargeJsonString(string json1, string json2);
        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);

    }
}
