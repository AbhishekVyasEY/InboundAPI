using Newtonsoft.Json.Linq;

namespace ManageCase
{
    public interface ICommonFunction
    {
       
        public Task<JArray> getCaseStatus(string CaseID);
        public Task<JArray> getSRwizard(string CaseID);
        public Task<JArray> getCaseAdditionalDetails(string CaseID, string idfield);
        public Task<string> getAccountId(string BranchCode);
        public Task getAccount_Id(string AccountNumber);
        public Task getAccountNumber(string AccountId);
        public Task getChannelId(string channelName);
        public Task getChannelCode(string channelId);
        public Task<string> getCustomerId(string CustomerCode);
        public Task getCustomer_Id(string uciccode);
        public Task<string> getCustomerCode(string CustomerId);
        public Task getSourceId(string SourceCode);
        public Task getSourceCode(string SourceId);
        public Task getCategoryName(string CategoryId);
        public Task getCategoryId(string CustomerCode);
        public Task getclassificationId(string classification);
        public Task getClassificationName(string classificationId);
        public Task<JArray> getSubCategoryId(string CustomerCode, string CategoryId);
        public Task getSubCategoryName(string SubCategoryId);
        public Task<string> getBranchId(string branchid);
        public Task<string> getProductId(string productcode);
        public Task<string> getNationalityId(string countrycode);
        public Task<string> getPurposeOfCreationId(string purposeofcreation);
        public Task<string> getCustomerAddressId(string customerid, string addtesstypecode);
        public Task<JArray> getCityDetails(string CityID);
        public Task<bool> checkDuplicate(string UCIC, string Account, string Category, string SubCategory);
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
