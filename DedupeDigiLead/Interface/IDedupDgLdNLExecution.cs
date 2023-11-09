namespace DedupeDigiLead
{
    public interface IDedupDgLdNLExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { get; set; }

        public Task<dynamic> ValidateDedupDgLdNL(dynamic CaseData, string type);
        public Task<dynamic> getDedupDgLdNLStatus(dynamic RequestData, string type);
        public Task<string> EncriptRespons(string ResponsData);
    }
}
