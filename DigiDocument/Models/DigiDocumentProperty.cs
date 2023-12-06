using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DigiDocument
{

    public class GetDgDocDtlReturn
    {
        public List<Document> DocumentIDs { get; set; }
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class UpdateDgDocDtlReturn
    {
        public List<DocUpdateStatus> DocUpdateStatus { get; set; }
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class Document
    {
        public string CRMDocumentID { get; set; }
        public string DocumentType { get; set; }
        public string CategoryCode { get; set; }
        public string SubcategoryCode { get; set; }
        public string IssuedAt { get; set; }
        public string IssueDate { get; set; }
        public string ExpiryDate { get; set; }
        public string DmsDocumentID { get; set; }
        public string VerificationStatus { get; set; }
        public string VerifiedBy { get; set; }
        public string VerifiedOn { get; set; }
        public string MappedCustomerLead { get; set; }
        public string MappedAccountLead { get; set; }
        public string MappedUCIC { get; set; }
        public string MappedAccount { get; set; }
        public string MappedServiceRequest { get; set; }
    }

    public class DocUpdateStatus
    {
        public string SubCategoryCode { get; set; }
        public string DocId { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class MasterConfiguration
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}