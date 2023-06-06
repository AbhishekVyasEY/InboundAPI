using Newtonsoft.Json.Linq;

namespace ManageCase
{
    public interface IQueryParser
    {
        public Task<List<JObject>> HttpApiCall(string odataQuery, HttpMethod httpMethod, string parameterToPost = "");
    }
}
