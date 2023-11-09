namespace DigiDocument
{
    public interface IDgDocDtlExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { set; get; }
      
        public Task<UpdateDgDocDtlReturn> ValidateDocumentInput(dynamic CaseData);
        public Task<GetDgDocDtlReturn> GetDocumentList(dynamic RequestData);
        public Task<string> EncriptRespons(string ResponsData);
        
       

    }
}
