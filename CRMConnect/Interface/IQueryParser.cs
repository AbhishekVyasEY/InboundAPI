using Newtonsoft.Json.Linq;

namespace CRMConnect
{
    public interface IQueryParser
    {
        public Task<List<JObject>> HttpApiCall(string odataQuery, HttpMethod httpMethod, string parameterToPost = "");
        public Task<string> PayloadEncryption(string V_requestData, string V_requestedID);
        public Task<string> PayloadDecryption(string V_requestData);
    }
}
