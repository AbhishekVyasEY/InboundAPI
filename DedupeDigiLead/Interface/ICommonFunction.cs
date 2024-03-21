using Newtonsoft.Json.Linq;

namespace DedupeDigiLead
{
    public interface ICommonFunction
    {
        public Task<JArray> getLeadData(string ApplicantId);
        public Task<JArray> getNLTRData(string Custype, string Pan,string aadhar, string passport, string cin, string firstname, string middlename, string lastname, string dob);
        public Task<JArray> getNLData(string Pan, string aadhar, string passport, string cin);
        public Task<List<string>> getLeadAccData(string LeadAccId);

        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);

    }
}
