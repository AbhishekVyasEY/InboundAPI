namespace GFSProduct
{
    public interface IFetchGFSProductExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { set; get; }
        public Task<GFSProducrListReturn> getUcicToProduct(string customerID, string CategoryCode);
        public Task<GFSProducrListReturn> getApplicentToProduct(string leadId, string CategoryCode);
        public Task<GFSProducrListReturn> ValidateProductInput(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData);
        


    }
}
