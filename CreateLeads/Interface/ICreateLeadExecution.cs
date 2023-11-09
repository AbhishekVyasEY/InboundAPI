namespace CreateLeads
{
    public interface ICreateLeadExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string Bank_Code { set; get; }

        public Task<LeadReturnParam> ValidateLeade(dynamic LeadData);
        public Task<LeadReturnParam> CreateLead(dynamic LeadData);
        public Task<string> EncriptRespons(string ResponsData);
    }
}
