namespace DMSCallBack
{
    public interface IDCMCallBackExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { set; get; }
 
        public Task<DMSCallBackReturn> UpdateDCMSuccess(string RequestId, dynamic succData);
        public Task<DMSCallBackReturn> UpdateDCMError(string RequestId, dynamic errorData);
        public Task<DMSCallBackReturn> ValidateDMSInput(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData);
      


    }
}
