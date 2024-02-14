﻿using Microsoft.Extensions.Caching.Memory;
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
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Immutable;
using System.Timers;
using System.Threading.Channels;

namespace ManageCase
{
    public class CommonFunction: ICommonFunction
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

        public async Task<string> getIDFromGetResponce(string primaryField ,List<JObject> RsponsData)
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

       
        

        public async Task getChannelId(string channelCode)
        {
            string query_url = $"eqs_casechannels()?$select=eqs_casechannelid&$filter=eqs_channelid eq '{channelCode}' and statecode eq 0";
            await this._queryParser.SetBatchCall(query_url, HttpMethod.Get, "");            
        }
        public async Task getSourceId(string SourceCode)
        {
            string query_url = $"eqs_casesources()?$select=eqs_casesourceid&$filter=eqs_sourceid eq '{SourceCode}' and statecode eq 0";
            await this._queryParser.SetBatchCall(query_url, HttpMethod.Get, "");           
        }
        public async Task getCustomer_Id(string uciccode)
        {
            string query_url = $"contacts()?$select=contactid&$filter=eqs_customerid eq '{uciccode}' and statecode eq 0";
            await this._queryParser.SetBatchCall(query_url, HttpMethod.Get, "");           
        }
        public async Task getCategoryId(string CategoryCode)
        {
            string query_url = $"ccs_categories()?$select=ccs_categoryid&$filter=ccs_code eq '{CategoryCode}' and statecode eq 0";
            await this._queryParser.SetBatchCall(query_url, HttpMethod.Get, "");           
        }
        public async Task getAccount_Id(string AccountNumber)
        {
            string query_url = $"eqs_accounts()?$select=eqs_accountid&$filter=eqs_accountno eq '{AccountNumber}' and statecode eq 0";
            await this._queryParser.SetBatchCall(query_url, HttpMethod.Get, "");            
        }
        public async Task getclassificationId(string classification)
        {
            string query_url = $"ccs_classifications()?$select=ccs_classificationid&$filter=ccs_code eq '{classification}' and statecode eq 0";
            await this._queryParser.SetBatchCall(query_url, HttpMethod.Get, "");            
        }
        public async Task getClassificationName(string classificationId)
        {
            string query_url = $"ccs_classifications()?$select=ccs_name&$filter=ccs_classificationid eq '{classificationId}' and statecode eq 0";
            await this._queryParser.SetBatchCall(query_url, HttpMethod.Get, "");           
        }
        public async Task getCategoryName(string CategoryId)
        {
            string query_url = $"ccs_categories()?$select=ccs_name&$filter=ccs_categoryid eq '{CategoryId}' and statecode eq 0";
            await this._queryParser.SetBatchCall(query_url, HttpMethod.Get, "");            
        }
        public async Task getSubCategoryName(string SubCategoryId)
        {
            string query_url = $"ccs_subcategories()?$select=ccs_name&$filter=ccs_subcategoryid eq '{SubCategoryId}' and statecode eq 0";
            await this._queryParser.SetBatchCall(query_url, HttpMethod.Get, "");           
        }
        public async Task getChannelCode(string channelId)
        {
            string query_url = $"eqs_casechannels()?$select=eqs_channelid&$filter=eqs_casechannelid eq '{channelId}' and statecode eq 0";
            await this._queryParser.SetBatchCall(query_url, HttpMethod.Get, "");
        }
        public async Task getSourceCode(string SourceId)
        {
            string query_url = $"eqs_casesources()?$select=eqs_sourceid&$filter=eqs_casesourceid eq '{SourceId}' and statecode eq 0";
            await this._queryParser.SetBatchCall(query_url, HttpMethod.Get, "");           
        }
        public async Task getAccountNumber(string AccountId)
        {
            string query_url = $"eqs_accounts()?$select=eqs_accountno&$filter=eqs_accountid eq '{AccountId}' and statecode eq 0";
            await this._queryParser.SetBatchCall(query_url, HttpMethod.Get, "");            
        }
        public async Task getCustomerCode(string CustomerId)
        {
            string query_url = $"contacts()?$select=eqs_customerid&$filter=contactid eq '{CustomerId}' and statecode eq 0";
            await this._queryParser.SetBatchCall(query_url, HttpMethod.Get, "");            
        }

        public async Task<string> getCustomerId(string uciccode)
        {           
            return await this.getIDfromMSDTable("contacts", "contactid", "eqs_customerid", uciccode);
        }       

        public async Task<string> getAccountId(string AccountNumber)
        {
            return await this.getIDfromMSDTable("eqs_accounts", "eqs_accountid", "eqs_accountno", AccountNumber);
        }            
        
        public async Task<string> getBranchId(string branchid)
        {
            return await this.getIDfromMSDTable("eqs_branchs", "eqs_branchid", "eqs_branchidvalue", branchid);
        }

        public async Task<string> getProductId(string productcode)
        {
            return await this.getIDfromMSDTable("eqs_products", "eqs_productid", "eqs_productcode", productcode);
        }

        public async Task<string> getNationalityId(string countrycode)
        {
            return await this.getIDfromMSDTable("eqs_countries", "eqs_countryid", "eqs_countryalphacpde", countrycode);
        }

        public async Task<string> getPurposeOfCreationId(string purposeofcreation)
        {
            return await this.getIDfromMSDTable("eqs_purposeofcreations", "eqs_purposeofcreationid", "eqs_name", purposeofcreation);
        }

        public async Task<string> getCustomerAddressId(string customerid, string addtesstypecode)
        {
            try
            {
                string query_url = $"eqs_addresses()?$select=eqs_addressid&$filter=(_eqs_customer_value eq {customerid} and eqs_addresstypeid eq {addtesstypecode})";
                var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                string addressId = await this.getIDFromGetResponce("eqs_addressid", responsdtails);
                return addressId;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getCustomerAddressId", ex.Message);
                throw ex;
            }
        }

        public async Task<string> getSubCategoryId(string subCategoryCode, string CategoryID)
        {
            try
            {
                string query_url = $"ccs_subcategories()?$select=ccs_subcategoryid&$filter=ccs_code eq '{subCategoryCode}' and _ccs_category_value eq '{CategoryID}' and statecode eq 0";
                var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                string subCatId = await this.getIDFromGetResponce("ccs_subcategoryid", responsdtails);
                return subCatId;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getSubCategoryId", ex.Message);
                throw ex;
            }

        }

        public async Task<JArray> getCaseAdditionalFields(string subCategoryCode)
        {
            try
            {
                string query_url = $"eqs_fieldvisibilitymetadataconfigurations()?$select=eqs_showfield&$filter=_eqs_subcategory_value eq '{subCategoryCode}'  and statecode eq 0";
                var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var inputFields = await this._queryParser.getDataFromResponce(responsdtails);
                return inputFields;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getCaseAdditionalFields", ex.Message);
                throw ex;
            }

        }

        public async Task<JArray> getCityDetails(string CityID)
        {
            try
            {
                string query_url = $"eqs_cities?$select=eqs_cityid,_eqs_countryid_value,_eqs_stateid_value&$filter=eqs_citycode eq '{CityID}' and statecode eq 0";
                var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var inputFields = await this._queryParser.getDataFromResponce(responsdtails);
                return inputFields;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getCaseStatus", ex.Message);
                throw ex;
            }

        }

        public async Task<JArray> getCaseStatus(string CaseID)
        {
            try
            {
                string query_url = $"incidents()?$select=ticketnumber,statuscode,title,createdon,modifiedon,ccs_resolveddate,eqs_casetype,_ccs_classification_value,_ccs_category_value,_ccs_subcategory_value,eqs_casepayload,description,eqs_casepriority,_eqs_casechannel_value,_eqs_casesource_value,_eqs_account_value,_customerid_value&$filter=ticketnumber eq '{CaseID}'";
                var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var inputFields = await this._queryParser.getDataFromResponce(responsdtails);
                return inputFields;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getCaseStatus", ex.Message);
                throw ex;
            }

        }

        public async Task<JArray> getExistingCase(string CaseID)
        {
            try
            {
                string query_url = $"incidents()?$filter=title eq '{CaseID}'";
                var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var inputFields = await this._queryParser.getDataFromResponce(responsdtails);
                return inputFields;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getCaseStatus", ex.Message);
                throw ex;
            }

        }

        public async Task<JArray> getCustomerData(string customerid)
        {
            try
            {
                string query_url = $"contacts({customerid})?$select=_eqs_entitytypeid_value";
                var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "", true);
                var inputFields = await this._queryParser.getDataFromResponce(responsdtails);
                return inputFields;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getCaseStatus", ex.Message);
                throw ex;
            }

        }
        public async Task<JArray> getCaseAdditionalDetails(string CaseID, string idfield)
        {
            try
            {
                string query_url = $"incidents()?$select={idfield}&$filter=ticketnumber eq '{CaseID}'";
                var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "", true);
                var incidentFields = await this._queryParser.getDataFromResponce(responsdtails);
                return incidentFields;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getCaseAdditionalDetails", ex.Message);
                throw ex;
            }
        }

        public async Task<List<MandatoryField>> getMandatoryFields(string subCategoryID)
        {
            try
            {
                List<MandatoryField> mandatoryFields = new List<MandatoryField>();
                string query_url = $"eqs_keyvaluerepositories()?$select=eqs_key,eqs_value,eqs_datatype,eqs_referencefield,eqs_entityname,eqs_entityid&$filter=_eqs_subcategory_value eq '{subCategoryID}'";
                var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var inputFields = await this._queryParser.getDataFromResponce(responsdtails);

                foreach (var field in inputFields)
                {
                    mandatoryFields.Add(new MandatoryField()
                    {
                        InputField = field["eqs_key"].ToString(),
                        CRMField = field["eqs_value"].ToString(),
                        CRMValue = "",
                        IDFieldName = field["eqs_entityid"].ToString(),
                        CRMType = field["eqs_datatype"].ToString(),
                        CRMTable = field["eqs_entityname"].ToString(),
                        FilterField = field["eqs_referencefield"].ToString()
                    });
                }

                return mandatoryFields;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getMandatoryFields", ex.Message);
                throw ex;
            }

        }

        public async Task<bool> checkDuplicate(string UCIC, string Account, string Classification, string Category, string SubCategory)
        {
            try
            {
                await this.getCustomer_Id(UCIC);
                string Accountid = "";
                if (!string.IsNullOrEmpty(Account))
                {
                    await this.getAccount_Id(Account);
                }
                await this.getclassificationId(Classification);
                await this.getCategoryId(Category);

                var Batch_results1 = await this._queryParser.GetBatchResult();
                string customerid = (Batch_results1[0]["contactid"]!=null) ? Batch_results1[0]["contactid"].ToString() : "";
                string ccs_classification, CategoryId;
                if (!string.IsNullOrEmpty(Account))
                {
                    Accountid = (Batch_results1[1]["eqs_accountid"] != null) ? Batch_results1[1]["eqs_accountid"].ToString() : "";
                    ccs_classification = (Batch_results1[2]["ccs_classificationid"] != null) ? Batch_results1[2]["ccs_classificationid"].ToString() : "";
                    CategoryId = (Batch_results1[3]["ccs_categoryid"] != null) ? Batch_results1[3]["ccs_categoryid"].ToString() : "";
                }
                else
                {
                    ccs_classification = (Batch_results1[1]["ccs_classificationid"] != null) ? Batch_results1[1]["ccs_classificationid"].ToString() : "";
                    CategoryId = (Batch_results1[2]["ccs_categoryid"] != null) ? Batch_results1[2]["ccs_categoryid"].ToString() : "";
                }
                    
             

                string SubCategoryId = await this.getSubCategoryId(SubCategory, CategoryId);

                string query_url = $"incidents()?$select=incidentid,statuscode&$filter=_customerid_value eq '{customerid}' and _ccs_classification_value eq '{ccs_classification}' and _ccs_category_value eq '{CategoryId}' and _ccs_subcategory_value eq '{SubCategoryId}'";

                if (!string.IsNullOrEmpty(Accountid))
                {
                    query_url += $" and _eqs_account_value eq '{Accountid}'";
                }
                
                var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var responsedata = await this._queryParser.getDataFromResponce(responsdtails);
                if (responsedata.Count > 0)
                {
                    if (Convert.ToInt64(responsedata[0]["statuscode"].ToString()) < 2)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                this._logger.LogError("checkDuplicate", ex.Message);
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

        public void SetMvalue<T>(string keyname,double timevalid , T inputvalue)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(timevalid));

            this._cache.Set<T>(keyname, inputvalue, cacheEntryOptions);
        }

    }
}
