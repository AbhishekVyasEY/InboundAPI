namespace UpdateAccountLead
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
    using Microsoft.Rest.Azure.OData;
    using System.Net.Http;


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
                    string query_url = $"{tablename}()?$select={idfield}&$filter={filterkey} eq '{filtervalue}'";
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

        public async Task<string> getDocCategoryId(string Category_code)
        {
            return await this.getIDfromMSDTable("eqs_doccategories", "eqs_doccategoryid", "eqs_doccategorycode", Category_code);
        }

        public async Task<string> getDocSubcatId(string Subcat_code)
        {
            return await this.getIDfromMSDTable("eqs_docsubcategories", "eqs_docsubcategoryid", "eqs_docsubcategorycode", Subcat_code);
        }

        public async Task<string> getApplicentID(string ucic)
        {
            return await this.getIDfromMSDTable("eqs_accountapplicants", "eqs_accountapplicantid", "eqs_customer", ucic);
        }
        
        public async Task<string> getDebitCardID(string DebitCardcode)
        {
            return await this.getIDfromMSDTable("eqs_debitcards", "eqs_debitcardid", "eqs_cardid", DebitCardcode);
        }

        public async Task<string> getNomineeID(string ddeID)
        {
            return await this.getIDfromMSDTable("eqs_ddeaccountnominees", "eqs_ddeaccountnomineeid", "_eqs_leadaccountddeid_value", ddeID);
        }

        public async Task<string> getDocTypeId(string Type_code)
        {
            return await this.getIDfromMSDTable("eqs_doctypemasters", "eqs_doctypemasterid", "eqs_doctypemasteridvalue", Type_code);
        }

        public async Task<string> getRelationShipId(string RelationShipCode)
        {
            return await this.getIDfromMSDTable("eqs_relationships", "eqs_relationshipid", "eqs_relationship", RelationShipCode);
        }

        public async Task<string> getStateId(string state_code)
        {
            return await this.getIDfromMSDTable("eqs_states", "eqs_stateid", "eqs_statecode", state_code);
        }

        public async Task<string> getCityId(string City_code)
        {
            return await this.getIDfromMSDTable("eqs_cities", "eqs_cityid", "eqs_citycode", City_code);
        } 
        
        public async Task<string> getCuntryId(string cuntry_code)
        {
            return await this.getIDfromMSDTable("eqs_countries", "eqs_countryid", "eqs_countrycode", cuntry_code);
        }
        
        public async Task<string> getPreferenceID(string PrederenceId, string DDEID)
        {            
            return await this.getIDfromMSDTable("eqs_customerpreferences", "eqs_customerpreferenceid", $"_eqs_leadaccountdde_value eq '{DDEID}' and eqs_preferenceid", PrederenceId);
        }
        public async Task<string> getAccountId(string accountNo)
        {
            return await this.getIDfromMSDTable("eqs_accounts", "eqs_accountid", "eqs_accountno", accountNo);
        }

        public async Task<JArray> getLeadAccountDetails(string LdApplicantId)
        {
            try
            {
                string query_url = $"eqs_leadaccounts()?$select=eqs_leadaccountid,eqs_crmleadaccountid,eqs_ddefinalid,_eqs_typeofaccountid_value,_eqs_productid_value,_eqs_lead_value&$filter=eqs_crmleadaccountid eq '{LdApplicantId}'";
                var LeadAccountDtl = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var LeadAccount_dtails = await this._queryParser.getDataFromResponce(LeadAccountDtl);
                return LeadAccount_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getLeadAccountDetails", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getApplicentsSetails(string LeadAccId)
        {
            try
            {
                string query_url = $"eqs_accountapplicants()?$filter=_eqs_leadaccountid_value eq '{LeadAccId}'";
                var Applicantdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Applicant_dtails = await this._queryParser.getDataFromResponce(Applicantdtails);
                return Applicant_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getApplicentsSetails", ex.Message);
                throw ex;
            }
        }
        
        public async Task<JArray> getPreferences(string applicantid)
        {
            try
            {
                string query_url = $"eqs_customerpreferences()?$filter=_eqs_applicantid_value eq '{applicantid}'";
                var Customerdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Customer_dtails = await this._queryParser.getDataFromResponce(Customerdtails);
                return Customer_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getPreferences", ex.Message);
                throw ex;
            }
        }

        

        public async Task<JArray> getLeadDetails(string Lead_id)
        {
            try
            {
                string query_url = $"leads({Lead_id})?";
                var LeadDetails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Lead_dtails = await this._queryParser.getDataFromResponce(LeadDetails);
                return Lead_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getLeadDetails", ex.Message);
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
