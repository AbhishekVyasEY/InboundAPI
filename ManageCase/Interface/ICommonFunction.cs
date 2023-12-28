using Newtonsoft.Json.Linq;

namespace ManageCase
{
    public interface ICommonFunction
    {
       
        public Task<JArray> getCaseStatus(string CityCode);
        public Task<JArray> getCaseAdditionalDetails(string CaseID, string idfield);
        public Task<string> getAccountId(string BranchCode);
        public Task<string> getAccountNumber(string AccountId);
        public Task<string> getChannelId(string channelName);
        public Task<string> getChannelCode(string channelId);
        public Task<string> getCustomerId(string CustomerCode);
        public Task<string> getCustomerCode(string CustomerId);
        public Task<string> getSourceId(string SourceCode);
        public Task<string> getSourceCode(string SourceId);
        public Task<string> getCategoryName(string CategoryId);
        public Task<string> getCategoryId(string CustomerCode);
        public Task<string> getclassificationId(string classification);
        public Task<string> getClassificationName(string classificationId);
        public Task<string> getSubCategoryId(string CustomerCode, string CategoryId);
        public Task<string> getSubCategoryName(string SubCategoryId);
        public Task<string> getBranchId(string branchid);
        public Task<string> getProductId(string productcode);
        public Task<string> getNationalityId(string countrycode);
        public Task<string> getPurposeOfCreationId(string purposeofcreation);
        public Task<string> getCustomerAddressId(string customerid, string addtesstypecode);
        public Task<bool> checkDuplicate(string UCIC, string Account, string Classification, string Category, string SubCategory);
        public Task<JArray> getCaseAdditionalFields(string subCategoryCode);
        public Task<JArray> getExistingCase(string CaseID);
        public Task<JArray> getCustomerData(string customerid);
        public Task<string> MeargeJsonString(string json1, string json2);
        public Task<List<MandatoryField>> getMandatoryFields(string subCategoryID);
        public Task<string> getIDfromMSDTable(string tablename, string idfield, string filterkey, string filtervalue);
        public bool GetMvalue<T>(string keyname, out T? Outvalue);
        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue);
    }
}
