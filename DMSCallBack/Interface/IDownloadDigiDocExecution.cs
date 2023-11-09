namespace DMSCallBack
{
    public interface IDownloadDigiDocExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { set; get; }
 
        public Task<DMSCallBackReturn> ExecDownloadDigiDoc(dynamic Documentdetails, dynamic succData);
     
        public Task<DMSCallBackReturn> ValidateDownloadDocInput(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData);
       


    }
}
