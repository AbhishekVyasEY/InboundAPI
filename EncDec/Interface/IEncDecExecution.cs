namespace encdec
{
    public interface IEncDecExecution
    {   
        public string Channel_ID { set; get; }  
        public string appkey { set; get; }
 
        public Task<string> EQEncryptionInput(dynamic CaseData);
        public Task<string> EQDecryptionInput(dynamic CaseData);
      
    }
}
