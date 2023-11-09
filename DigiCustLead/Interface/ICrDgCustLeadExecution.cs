namespace DigiCustLead
{
    public interface ICrDgCustLeadExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { get; set; }
        public Task<CreateCustLeadReturn> createDigiCustLeadIndv(dynamic CustLeadData);
        public Task<CreateCustLeadReturn> createDigiCustLeadCorp(dynamic CustLeadData);
        public Task<CreateCustLeadReturn> ValidateCustLeadDetls(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData);
      


    }
}
