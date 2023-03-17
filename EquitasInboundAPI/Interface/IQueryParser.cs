using Newtonsoft.Json.Linq;

namespace EquitasInboundAPI
{
    public interface IQueryParser
    {
        public Task<List<JObject>> HttpApiCall(string odataQuery, HttpMethod httpMethod, string parameterToPost = "");
    }
}
