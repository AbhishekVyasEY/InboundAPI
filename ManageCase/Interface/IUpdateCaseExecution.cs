namespace ManageCase
{
    public interface IUpdateCaseExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { get; set; }
        public string Bank_Code { set; get; }

        
        public Task<UpdateCaseReturnParam> ValidateUpdateCase(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData, string Bankcode);
    }
}
