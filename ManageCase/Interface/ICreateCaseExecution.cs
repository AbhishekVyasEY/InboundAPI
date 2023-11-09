namespace ManageCase
{
    public interface ICreateCaseExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { get; set; }

        public Task<CaseReturnParam> CreateCase(dynamic CaseData);
        public Task<CaseReturnParam> ValidateCreateCase(dynamic CaseData);
        public Task<CaseListParam> getCaseList(dynamic CaseData);
        public Task<CaseStatusRtParam> ValidategetCaseStatus(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData);
       
    }
}
