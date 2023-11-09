namespace DedupeDigiLead
{
    public interface IDedupDgAccountNLExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }

        public Task<dynamic> ValidateDedupDgAccNL(dynamic CaseData, string type);
        public Task<dynamic> getDedupDgAccNLStatus(string ApplicantId, string type);
        public Task<string> EncriptRespons(string ResponsData);
    }
}
