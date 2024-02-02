namespace GFSProduct
{

    using Microsoft.Extensions.Caching.Memory;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json.Linq;
    using System.Net;
    using System.Diagnostics.Metrics;
    using CRMConnect;


    public class CommonFunction : ICommonFunction
    {
        public IQueryParser _queryParser;
        private ILoggers _logger;
        public IMemoryCache _cache;
        public CommonFunction(IMemoryCache cache, ILoggers logger, IQueryParser queryParser)
        {
            this._queryParser = queryParser;
            this._logger = logger;
            this._cache = cache;
        }
        public async Task<string> AcquireNewTokenAsync()
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                //string tenantID = "62c7002c-da9e-4e54-86e7-4c53c52518ad";
                //string ClientID = "6cded931-9ddc-47d4-a31b-c4c135036c3b";
                //string ClientSecrets = "OQC8Q~gx9aRsgmf.Q-v.2nfsrgwsdhF1VadXCc_H";

                string tenantID = "e22e4eaa-623f-4164-abb8-ac89a5a17e13";
                string ClientID = "fa5dddfc-4e66-4522-93aa-9e46e78d2c00";
                string ClientSecrets = "Aym8Q~LzrJIqAGQ9CtkmMWkYU3~-ACdfoDFdubjX";

                var authority = $"https://login.microsoftonline.com/{tenantID}";
                var app =
                    ConfidentialClientApplicationBuilder.Create(ClientID)
                                                        .WithClientSecret(ClientSecrets)
                                                        .WithAuthority(authority)
                                                        .Build();

                var authResult = await app.AcquireTokenForClient(new[] { "https://orgc39e5cd7.crm8.dynamics.com/.default" })
                    .ExecuteAsync(cancellationTokenSource.Token).ConfigureAwait(true);

                string bearerHeaderValue = authResult.AccessToken;
                cancellationTokenSource.Cancel();
                return bearerHeaderValue;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }

        public static string GetIdFromPostRespons(string PResponseData)
        {

            string Respons_Id = PResponseData.Substring(PResponseData.IndexOf('(') + 1, PResponseData.IndexOf(')') - PResponseData.IndexOf('(') - 1);
            return Respons_Id;
        }

        public static string GetIdFromPostRespons201(dynamic PResponseData, string datakey)
        {
            string Respons_Id = PResponseData[datakey];
            return Respons_Id;
        }

        public async Task<string> getIDFromGetResponce(string primaryField, List<JObject> RsponsData)
        {
            string resourceID = "";
            foreach (JObject item in RsponsData)
            {
                if (Enum.TryParse(item["responsecode"].ToString(), out HttpStatusCode responseStatus) && responseStatus == HttpStatusCode.OK)
                {
                    dynamic responseValue = item["responsebody"];
                    JArray resArray = new JArray();
                    string urlMetaData = string.Empty;
                    if (responseValue?.value != null)
                    {
                        resArray = (JArray)responseValue?.value;
                        urlMetaData = responseValue["@odata.context"];
                    }
                    else if (responseValue is JArray)
                    {
                        resArray = responseValue;

                    }
                    else
                    {
                        resArray.Add(responseValue);
                        urlMetaData = responseValue["@odata.context"];
                    }

                    if (resArray != null && resArray.Any())
                    {
                        //int startIndex = urlMetaData.IndexOf("metadata#", StringComparison.Ordinal) + "metadata#".Length;
                        //int endIndex = urlMetaData.IndexOf("(", StringComparison.Ordinal);
                        //string entityName = urlMetaData.Substring(startIndex, endIndex - startIndex);

                        foreach (var record in resArray)
                        {
                            resourceID = record[primaryField]?.ToString();
                        }

                    }
                }
            }
            return resourceID;
        }

        public async Task<JArray> getDataFromResponce(List<JObject> RsponsData)
        {
            string resourceID = "";
            foreach (JObject item in RsponsData)
            {
                if (Enum.TryParse(item["responsecode"].ToString(), out HttpStatusCode responseStatus) && responseStatus == HttpStatusCode.OK)
                {
                    dynamic responseValue = item["responsebody"];
                    JArray resArray = new JArray();
                    string urlMetaData = string.Empty;
                    if (responseValue?.value != null)
                    {
                        resArray = (JArray)responseValue?.value;
                        urlMetaData = responseValue["@odata.context"];
                    }
                    else if (responseValue is JArray)
                    {
                        resArray = responseValue;

                    }
                    else
                    {
                        resArray.Add(responseValue);
                        urlMetaData = responseValue["@odata.context"];
                    }

                    if (resArray != null && resArray.Any())
                    {

                        return resArray;

                    }
                }
            }
            return new JArray();
        }

        public async Task<string> getIDfromMSDTable(string tablename, string idfield, string filterkey, string filtervalue)
        {
            try
            {
                string Table_Id;
                string TableId;
                if (!this.GetMvalue<string>(tablename + filtervalue, out Table_Id))
                {
                    string query_url = $"{tablename}()?$select={idfield}&$filter={filterkey} eq '{filtervalue}' and statecode eq 0";
                    var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                    TableId = await this.getIDFromGetResponce(idfield, responsdtails);

                    this.SetMvalue<string>(tablename + filtervalue, 1400, TableId);
                }
                else
                {
                    TableId = Table_Id;
                }
                return TableId;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getIDfromMSDTable", ex.Message, $"Table {tablename} filterkey {filterkey} filtervalue {filtervalue}");
                throw;
            }
        }

        public async Task<string> getCategoryId(string product_Cat_Id)
        {
            return await this.getIDfromMSDTable("eqs_productcategories", "eqs_productcategoryid", "eqs_productcategorycode", product_Cat_Id);
        }

        public async Task<string> getSubentity(string subentity_Id)
        {
            return await this.getIDfromMSDTable("eqs_subentitytypes", "eqs_name", "eqs_subentitytypeid", subentity_Id);
        }

        public async Task<string> getEntityType(string entityTypeId)
        {
            return await this.getIDfromMSDTable("eqs_entitytypes", "eqs_name", "eqs_entitytypeid", entityTypeId);
        }

        public async Task<JArray> getIndividualDdeDetails(string ApplicantId)
        {
            try
            {
                string finalValue = await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_dataentrystage", "Final");
                string query_url = $"eqs_ddeindividualcustomers()?$select=eqs_isstaffcode,eqs_programcode&$filter=_eqs_accountapplicantid_value eq '{ApplicantId}' and eqs_dataentrystage eq {finalValue}";
                var Applicantdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Applicant_dtails = await this.getDataFromResponce(Applicantdtails);
                return Applicant_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getIndividualDdeDetails", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getCorporateProducts(string SubTypeId, string ProductCategoryId)
        {
            try
            {
                string fetchxml = $"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>"+
                                    "<entity name='eqs_product'>"+
                                    "<attribute name='eqs_productid'/>" +
                                    "<attribute name='eqs_name'/>" +
                                    "<attribute name='eqs_productcode'/>" +
                                    "<attribute name='eqs_productcategory'/>" +
                                    "<attribute name='eqs_mintenuredays'/>" +
                                    "<attribute name='eqs_maxtenuredays'/>" +
                                    "<attribute name='eqs_mintenuremonths'/>" +
                                    "<attribute name='eqs_maxtenuremonths'/>" +
                                    "<attribute name='eqs_minamount'/>" +
                                    "<attribute name='eqs_maxamount'/>" +
                                    "<attribute name='eqs_depositvariance'/>" +
                                    "<attribute name='eqs_payoutfrequency'/>" +
                                    "<attribute name='eqs_compoundingfrequency'/>" +
                                    "<attribute name='eqs_renewaloptions'/>" +
                                    "<attribute name='eqs_payoutfrequencytype'/>" +
                                    "<attribute name='eqs_compoundingfrequencytype'/>" +
                                    "<attribute name='eqs_iselite'/>" +
                                    "<attribute name='eqs_chequebook'/>" +
                                    "<attribute name='eqs_debitcard'/>" +
                                    "<attribute name='eqs_applicabledebitcard'/>" +
                                    "<attribute name='eqs_defaultdebitcard'/>" +
                                    "<attribute name='eqs_instakit'/>" +
                                    "<attribute name='eqs_doorstep'/>" +
                                    "<attribute name='eqs_pmay'/>" +
                                    "<attribute name='eqs_srnoofchequeleaves'/>" +
                                    "<attribute name='eqs_noofchequeleaves'/>" +
                                    "<attribute name='eqs_srdefaultchequeleaves'/>" +
                                    "<attribute name='eqs_defaultchequeleaves'/>" +
                                    "<order attribute='eqs_name' descending='false'/>" +
                                    "<filter type='and'>" +
                                    "<condition attribute='statecode' operator='eq' value='0'/>" +
                                    $"<condition attribute='eqs_productcategory' operator='eq' uitype='eqs_productcategory' value='{ProductCategoryId}'/>" +
                                    "</filter>" +
                                    "<link-entity name='eqs_product_eqs_subentitytype' from='eqs_productid' to='eqs_productid' visible='false' intersect='true'>" +
                                    "<link-entity name='eqs_subentitytype' from='eqs_subentitytypeid' to='eqs_subentitytypeid' alias='am'>" +
                                    "<filter type='and'>" +
                                    $"<condition attribute='eqs_subentitytypeid' operator='eq' uitype='eqs_subentitytype' value='{SubTypeId}'/>" +
                                    "</filter>" +
                                    "</link-entity>" +
                                    "</link-entity>" +
                                    "</entity>" +
                                    "</fetch>";
               
                var escapedFetchXML = Uri.EscapeDataString(fetchxml);
                string query_url = $"eqs_products?fetchXml=" + escapedFetchXML;
                var Customerdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Customer_dtails = await this.getDataFromResponce(Customerdtails);
                return Customer_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getCustomerDetails", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getApplicantDetails(string ApplicantId)
        {
            try
            {
                string query_url = $"eqs_accountapplicants()?$select=_eqs_entitytypeid_value,eqs_gendercode,eqs_leadage,_eqs_subentity_value,eqs_customersegment,eqs_isstaffcode&$filter=eqs_applicantid eq '{ApplicantId}'";
                var Applicantdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Applicant_dtails = await this.getDataFromResponce(Applicantdtails);
                return Applicant_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getApplicantDetails", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getCustomerDetails(string CustomerId)
        {
            try
            {
                string query_url = $"contacts()?$select=_eqs_entitytypeid_value,eqs_gender,eqs_age,_eqs_subentitytypeid_value,eqs_program,eqs_customersegment,eqs_isstafffcode&$filter=eqs_customerid eq '{CustomerId}'";
                var Customerdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Customer_dtails = await this.getDataFromResponce(Customerdtails);
                return Customer_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getCustomerDetails", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getProductData(productFilter product_Filter)
        {
            try
            {
                string prodSrkey = product_Filter.productCategory;
                string filter = $"_eqs_productcategory_value eq '{product_Filter.productCategory}'  and statecode eq 0 ";

                if (string.IsNullOrEmpty(product_Filter.gender))
                {
                    filter += $"and eqs_woman eq false ";
                    prodSrkey += "GF";
                }

                if (!string.IsNullOrEmpty(product_Filter.age))
                {
                    filter += $"and eqs_maxage gt {product_Filter.age} and eqs_minage lt {product_Filter.age} ";
                    prodSrkey += "ag" + product_Filter.age;
                }

                if (string.IsNullOrEmpty(product_Filter.subentity))
                {
                    filter += $"and eqs_nri eq false ";
                    prodSrkey += "nriF";
                }

                //Commented based on CRMMSD365-4397. If Customer is not elite, display all Products
                //if (string.IsNullOrEmpty(product_Filter.customerSegment))
                //{
                //    filter += $"and eqs_iselite eq false ";
                //    prodSrkey += "elitF";
                //}

                if (string.IsNullOrEmpty(product_Filter.IsStaff))
                {
                    filter += $"and eqs_staff eq false ";
                    prodSrkey += "stfF";
                }

                JArray Product_dtails, Product_dtails1;

                if (!this.GetMvalue<JArray>(prodSrkey, out Product_dtails1))
                {
                    string query_url = $"eqs_products()?$filter={filter}";
                    var Productdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                    Product_dtails = await this.getDataFromResponce(Productdtails);

                    this.SetMvalue<JArray>(prodSrkey, 5, Product_dtails);
                }
                else
                {
                    Product_dtails = Product_dtails1;
                }

                return Product_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getProductData", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getContactData(string contact_id)
        {
            try
            {
                string query_url = $"contacts({contact_id})?$select=createdon,eqs_entityflag,eqs_subentitytypeid,mobilephone,eqs_customerid";
                var Accountdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Account_dtails = await this.getDataFromResponce(Accountdtails);
                return Account_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getContactData", ex.Message);
                throw ex;
            }
        }

        public async Task<string> MeargeJsonString(string json1, string json2)
        {
            string first = json1.Remove(json1.Length - 1, 1);
            string second = json2.Substring(1);
            return first + ", " + second;
        }

        public bool GetMvalue<T>(string keyname, out T? Outvalue)
        {
            if (!this._cache.TryGetValue<T>(keyname, out Outvalue))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(timevalid));

            this._cache.Set<T>(keyname, inputvalue, cacheEntryOptions);
        }

    }
}