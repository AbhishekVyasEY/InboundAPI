namespace FetchAccountLead
{
    public interface IFtCustDtlExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { set; get; }
 
        public Task<FetchCustomerDtlReturn> ValidateCustInput(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData);
       


    }
}
