namespace DigiLead
{
    public interface IFtchDgLdStsExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { get; set; }
        public Task<FtchDgLdStsReturn> getDigiLeadStatus(dynamic CaseData);
        public Task<FtchDgLdStsReturn> ValidateFtchDgLdSts(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData);
       


    }
}
