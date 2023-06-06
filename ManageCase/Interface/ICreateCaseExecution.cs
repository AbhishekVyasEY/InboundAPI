namespace ManageCase
{
    public interface ICreateCaseExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        
        public Task<CaseReturnParam> CreateCase(dynamic CaseData);
        public Task<CaseReturnParam> ValidateCreateCase(dynamic CaseData, string appkey);
        public Task<CaseListParam> getCaseList(dynamic CaseData, string appkey);
        public Task<CaseStatusRtParam> ValidategetCaseStatus(dynamic CaseData, string appkey);
        public Task<string> EncriptRespons(string ResponsData);
        public Task CRMLog(string InputRequest, string OutputRespons, string CallStatus);
    }
}
