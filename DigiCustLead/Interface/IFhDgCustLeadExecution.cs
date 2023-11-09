namespace DigiCustLead
{
    public interface IFhDgCustLeadExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { get; set; }
        public Task<Individual> getDigiCustLeadIndv(string applicentID);
        public Task<Corporate> getDigiCustLeadCorp(string applicentID);
        public Task<FetchCustLeadReturn> ValidateFetchLeadDetls(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData);
        


    }
}
