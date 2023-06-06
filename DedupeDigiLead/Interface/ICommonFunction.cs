using Newtonsoft.Json.Linq;

namespace DedupeDigiLead
{
    public interface ICommonFunction
    {
        public Task<JArray> getLeadData(string LeadID);
        public Task<JArray> getNLTRData(string Pan,string aadhar, string passport, string cin);
        public Task<JArray> getNLData(string Pan, string aadhar, string passport, string cin);

    }
}
