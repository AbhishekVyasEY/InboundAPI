namespace DigiWiz
{
    public interface IGetDigiWizAcEntyDetlsExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { get; set; }
        public Task<WizAcEntyReturn> getWizAcEntyDetls(string AccountNumber);
        public Task<WizAcEntyReturn> ValidateWizAcEntyDetls(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData);
       


    }
}
