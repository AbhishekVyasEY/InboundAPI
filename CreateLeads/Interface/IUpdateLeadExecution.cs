namespace CreateLeads
{
    public interface IUpdateLeadExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string Bank_Code { set; get; }

        public Task<UpdateLidStatusReturnParam> UpdateLeade(dynamic RequestData);
        public Task<string> EncriptRespons(string ResponsData);

    }
}
