using Newtonsoft.Json.Linq;

namespace DigiLead
{
    public interface ICommonFunction
    {
        public Task<string> getTitle(string TitleId);
        public Task<string> getPurposeOfCreation(string PurposeOfCreatioId);
        public Task<string> getLeadType(string EntityTypeId);
        public Task<JArray> getDataFromResponce(List<JObject> RsponsData);
        public Task<string> getEntityType(string EntityTypeId);
        public Task<string> getSubEntityType(string SubEntityTypeId);

    }
}
