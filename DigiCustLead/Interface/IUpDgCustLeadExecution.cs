namespace DigiCustLead
{
    public interface IUpDgCustLeadExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { get; set; }
        public Task<UpdateCustLeadReturn> createDigiCustLeadIndv(dynamic CustLeadData, dynamic Applicant_Data);
        public Task<UpdateCustLeadReturn> createDigiCustLeadCorp(dynamic CustLeadData, dynamic Applicant_Data);
        public Task<UpdateCustLeadReturn> ValidateCustLeadDetls(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData);
      


    }
}
