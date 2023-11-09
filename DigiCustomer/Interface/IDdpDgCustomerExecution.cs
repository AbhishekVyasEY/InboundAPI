namespace CustomerLead
{
    public interface IDdpDgCustomerExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { set; get; }
        public string Bank_Code { set; get; }

        public Task<DedupeDigiCustomerReturn> ValidateInput(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData, string Bankcode);
      


    }
}
