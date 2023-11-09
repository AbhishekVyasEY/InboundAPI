namespace CRMConnect
{
    public interface ILoggers
    {
        public string API_Name { get; set; }
        public string Input_payload { get; set; }
        public string Channel_ID { get; set; }
        public string Transaction_ID { get; set; }
        public void LogInformation(string FunctionName, string InfoMessage, string Additionalinfo = "");
        public void LogError(string FunctionName, string ErrorMessage, string Additionalinfo = "");
    }
}
