using Newtonsoft.Json.Linq;

namespace CRMConnect
{
    public interface IQueryParser
    {
        public Task<List<JObject>> HttpApiCall(string odataQuery, HttpMethod httpMethod, string parameterToPost = "");
        public Task<string> HttpCBSApiCall(string Token, HttpMethod httpMethod, string APIName, string parameterToPost = "");
        public Task<string> PayloadEncryption(string V_requestData, string V_requestedID, string BankCode);
        public Task<string> getOptionSetTextToValue(string tableName, string fieldName, string OptionText);
        public Task<string> getOptionSetValuToText(string tableName, string fieldName, string OptionValue);
        public Task<bool> DeleteFromTable(string tablename, string tableid = "", string filter = "", string filtervalu = "", string tableselecter = "");
        public Task<string> PayloadDecryption(string V_requestData, string BankCode);
        public Task<JArray> getDataFromResponce(List<JObject> RsponsData);
        public Task<string> getAccessToken();
    }
}
