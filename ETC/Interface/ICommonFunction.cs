using Newtonsoft.Json.Linq;

namespace ETC
{
    public interface ICommonFunction
    {      
       
        public Task<string> MeargeJsonString(string json1, string json2);
        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);
        public Task<string> getetcCustomerId(string CustomerCode);



    }
}
