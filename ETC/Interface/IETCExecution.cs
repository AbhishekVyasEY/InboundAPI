namespace ETC
{
    public interface IETCExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { set; get; }
 
        public Task<ETCReturn> ValidateETCCustomertInput(dynamic CaseData);
        public Task<ETCReturn> ValidateUpETCCustomertInput(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData);
       


    }
}
