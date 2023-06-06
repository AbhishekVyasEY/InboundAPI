namespace CreateLeads
{
    public interface ICommonFunction
    {
        public Task<Dictionary<string, string>> getProductId(string ProductCode);
        public Task<string> getCityId(string CityCode);
        public Task<string> getBranchId(string BranchCode);
        public Task<string> getCustomerId(string CustomerCode);
        public Task<string> MeargeJsonString(string json1, string json2);

    }
}
