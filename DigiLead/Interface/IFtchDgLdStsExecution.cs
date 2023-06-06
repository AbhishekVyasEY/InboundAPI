namespace DigiLead
{
    public interface IFtchDgLdStsExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public Task<FtchDgLdStsReturn> getDigiLeadStatus(dynamic CaseData);
        public Task<FtchDgLdStsReturn> ValidateFtchDgLdSts(dynamic CaseData, string appkey);
        public Task<string> EncriptRespons(string ResponsData);
        public Task CRMLog(string InputRequest, string OutputRespons, string CallStatus);


    }
}
