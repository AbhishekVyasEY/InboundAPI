﻿namespace FetchAccountLead
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

        public async Task<string> getBranchCode(string BranchID)
        {
            return await this.getIDfromMSDTable("eqs_branchs", "eqs_branchidvalue", "eqs_branchid", BranchID);
        }

        public async Task<string> getProductCode(string Productid)
        {
            return await this.getIDfromMSDTable("eqs_products", "eqs_productcode", "eqs_productid", Productid);
        }

        public async Task<string> getProductCategoryCode(string Productcatid)
        {
            return await this.getIDfromMSDTable("eqs_productcategories", "eqs_productcategorycode", "eqs_productcategoryid", Productcatid);
        }

        public async Task<string> getStateCode(string state_id)
        {
            return await this.getIDfromMSDTable("eqs_states", "eqs_statecode", "eqs_stateid", state_id);
        }
        public async Task<string> getRelationshipCode(string relationship_id)
        {
            return await this.getIDfromMSDTable("eqs_relationships", "eqs_relationship", "eqs_relationshipid", relationship_id);
        }
        public async Task<string> getRelationshipName(string relationship_id)
        {
            string name = "";
            string query_url = $"eqs_relationships()?$select=eqs_name&$filter=eqs_relationshipid eq '{relationship_id}'";
            var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
            var details = await this.getDataFromResponce(responsdtails);

            if(details != null)
            {
                name = details[0]["eqs_name"].ToString();
            }
            return name;
        }

        public async Task<string> getCuntryCode(string Country_id)
        {
            return await this.getIDfromMSDTable("eqs_countries", "eqs_name", "eqs_countryid", Country_id);
        }

        public async Task<string> getCityCode(string city_id)
        {
            return await this.getIDfromMSDTable("eqs_cities", "eqs_citycode", "eqs_cityid", city_id);
        }


        public async Task<string> getUCIC(string accountapplicant_id)
        {
            return await this.getIDfromMSDTable("eqs_accountapplicants", "eqs_customer", "eqs_accountapplicantid", accountapplicant_id);
        }
        public async Task<string> getDebitCard(string DebitCardId)
        {
            return await this.getIDfromMSDTable("eqs_debitcards", "eqs_cardid", "eqs_debitcardid", DebitCardId);
        }

        public async Task<string> getTitleCode(string title_id)
        {
            return await this.getIDfromMSDTable("eqs_titles", "eqs_name", "eqs_titleid", title_id);
        }

        public async Task<string> getPurposeOfCreation(string id)
        {
            return await this.getIDfromMSDTable("eqs_purposeofcreations", "eqs_name", "eqs_purposeofcreationid", id);
        }

        public async Task<JArray> getNomineDetails(string DDEId)
        {
            string query_url = $"eqs_ddeaccountnominees()?$filter=_eqs_leadaccountddeid_value eq '{DDEId}'";
            var LeadAccountDtl = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
            var Nomini_details = await this.getDataFromResponce(LeadAccountDtl);
            return Nomini_details;
        }
        public async Task<JArray> getLeadAccountDetails(string LdApplicantId, string stage = "")
        {
            if (stage == "D0") 
            {
                string query_url = $"eqs_leadaccounts()?$filter=eqs_crmleadaccountid eq '{LdApplicantId}' &$expand=eqs_Lead($select=)";
                var LeadAccountDtl = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "",true);
                var LeadAccount_dtails = await this.getDataFromResponce(LeadAccountDtl);
                return LeadAccount_dtails;
            }
            else
            {
                string LeadAccountID = await this.getIDfromMSDTable("eqs_leadaccounts", "eqs_leadaccountid", "eqs_crmleadaccountid", LdApplicantId);
                string query_url = $"eqs_ddeaccounts()?$filter=_eqs_leadaccountid_value eq '{LeadAccountID}'";
                var LeadAccountDtl = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var LeadAccount_dtails = await this.getDataFromResponce(LeadAccountDtl);
                return LeadAccount_dtails;
            }
           
        }

        public async Task<JArray> getApplicentsSetails(string LeadAccId)
        {
            try
            {
                string query_url = $"eqs_accountapplicants()?$filter=_eqs_leadaccountid_value eq '{LeadAccId}'";
                var Applicantdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "", true);
                var Applicant_dtails = await this.getDataFromResponce(Applicantdtails);
                return Applicant_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getApplicentsSetails", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getPreferences(string DDeid)
        {
            try
            {
                string query_url = $"eqs_customerpreferences()?$filter=_eqs_leadaccountdde_value eq '{DDeid}'";
                var Customerdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Customer_dtails = await this.getDataFromResponce(Customerdtails);
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
                var Lead_dtails = await this.getDataFromResponce(LeadDetails);
                return Lead_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getLeadDetails", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getCustomerDetails(string filterkey, string filtervalue)
        {
            try
            {
                string query_url = $"contacts()?$expand=eqs_rmemployeeid($select=eqs_name,eqs_emprolelabel,eqs_emprole,eqs_rmempidslot,eqs_area,eqs_branch,eqs_branchcodeslot,eqs_cluster,eqs_department,eqs_division,eqs_emailid,eqs_empcategory,eqs_empphonenumber,eqs_emprolelabel,eqs_empstatus,eqs_region,eqs_resignedflagtwo,eqs_state,eqs_supervisoremailid,eqs_supervisorempidslot,eqs_supervisorname,eqs_zone)&$filter={filterkey} eq '{filtervalue}'";
                var CustomerDetails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "", true);
                var Customer_dtails = await this.getDataFromResponce(CustomerDetails);
                return Customer_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getCustomerDetails", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getAccountRelationshipDetails(string CustomerID)
        {
            try
            {
                string query_url = $"eqs_accountrelationships()?$filter=eqs_customerid eq '{CustomerID}'";
                var CustomerDetails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Customer_dtails = await this.getDataFromResponce(CustomerDetails);
                return Customer_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getAccountDetails", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getServiceDetails(string customerid, string accountid)
        {
            try
            {
                string query_url = $"eqs_doorstepregistries()?$expand=eqs_CashDeliveryLimit($select=eqs_name),eqs_CashPickupLimit($select=eqs_name),eqs_VendorLocationCashDelivery($select=eqs_name),eqs_VendorLocationCashPickup($select=eqs_name),eqs_VendorLocationChequePickup($select=eqs_name),eqs_VendorCashDelivery($select=eqs_vendorid,eqs_name),eqs_VendorCashPickup($select=eqs_vendorid,eqs_name),eqs_VendorChequePickup($select=eqs_vendorid,eqs_name)&$filter=_eqs_customer_value eq '{customerid}' and _eqs_account_value eq '{accountid}'";
                var CustomerDetails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "", true);
                var Customer_dtails = await this.getDataFromResponce(CustomerDetails);
                return Customer_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getAccountDetails", ex.Message);
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
