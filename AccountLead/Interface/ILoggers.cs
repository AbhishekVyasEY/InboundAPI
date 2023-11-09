namespace DigiLead
{
    public interface ILoggers
    {
        public string API_Name { get; set; }
        public string Input_payload { get; set; }
        public void LogInformation(string FunctionName, string InfoMessage);
        public void LogError(string FunctionName, string ErrorMessage);
    }
}
