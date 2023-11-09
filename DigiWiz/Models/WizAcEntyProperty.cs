using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DigiWiz
{
    
   

   
    public class WizAcEntyReturn
    {
        public string accountNumber { get; set; }
        public string accountCreatedOn { get; set; }
        public string accountTitle { get; set; }
        public string productCategory { get; set; }
        public string productVariant { get; set; }

        public List<CustomerInfo>? customerInfo { get; set; }
       
        public string ReturnCode { get; set; } 
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class CustomerInfo
    {
        public string UCICCreatedOn { get; set; }
        public string entityFlag { get; set; }
        public string subentityFlag { get; set; }
       
        public string phoneNumber { get; set; }
        public string ucic { get; set; }
     

    }

    




}
