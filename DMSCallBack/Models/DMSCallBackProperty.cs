using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DMSCallBack
{

     
    public class DMSCallBackReturn
    {        
       public string Documentid { get; set; } 
        public string ReturnCode { get; set; } 
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

   


}
