using Newtonsoft.Json.Linq;

namespace DigiLead
{
    public interface ICommonFunction
    {
        public Task<string> getTitle(string TitleId);
        public Task<string> getPurposeOfCreation(string PurposeOfCreatioId);
        public Task<JArray> getDataFromResponce(List<JObject> RsponsData);

    }
}
