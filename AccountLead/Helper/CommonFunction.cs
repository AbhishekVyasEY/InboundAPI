namespace AccountLead
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
    using System.Text;
    using System.Net.Http;
    using Newtonsoft.Json;
    using Azure.Core;

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
                this._logger.LogError("getIDfromMSDTable", ex.Message,$"Table {tablename} filterkey {filterkey} filtervalue {filtervalue}");
                throw;
            }
        }

        public async Task<string> getBranchId(string BranchCode)
        {
            return await this.getIDfromMSDTable("eqs_branchs", "eqs_branchid", "eqs_branchidvalue", BranchCode);
        }

        public async Task<string> getBranchCode(string BranchId)
        {
            return await this.getIDfromMSDTable("eqs_branchs", "eqs_branchidvalue", "eqs_branchid", BranchId);
        }

        public async Task<string> getAccRelationshipId(string AccRelationship_code)
        {
            return await this.getIDfromMSDTable("eqs_accountrelationshipses", "eqs_accountrelationshipsid", "eqs_key", AccRelationship_code);
        }


       
        public async Task<string> getLeadSourceId(string LeadSource_code)
        {
            return await this.getIDfromMSDTable("eqs_leadsources", "eqs_leadsourceid", "eqs_name", LeadSource_code);
        }

        public async Task<string> getRelationshipId(string Relationship_code)
        {
            return await this.getIDfromMSDTable("eqs_relationships", "eqs_relationshipid", "eqs_relationship", Relationship_code);
        }

      


        public async Task<Dictionary<string, string>> getProductId(string ProductCode)
        {
            try
            {
                string query_url = $"eqs_products()?$select=eqs_productid,_eqs_businesscategoryid_value,_eqs_productcategory_value,eqs_crmproductcategorycode&$filter=eqs_productcode eq '{ProductCode}' and statecode eq 0";
                var productdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                string ProductId = await this.getIDFromGetResponce("eqs_productid", productdtails);
                string businesscategoryid = await this.getIDFromGetResponce("_eqs_businesscategoryid_value", productdtails);
                string productcategory = await this.getIDFromGetResponce("_eqs_productcategory_value", productdtails);
                string crmproductcategorycode = await this.getIDFromGetResponce("eqs_crmproductcategorycode", productdtails);
                Dictionary<string, string> ProductData = new Dictionary<string, string>() {
                    { "ProductId", ProductId },
                    { "businesscategoryid", businesscategoryid },
                    { "productcategory", productcategory },
                    { "crmproductcategorycode", crmproductcategorycode }
                };
                return ProductData;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getProductId", ex.Message);
                throw ex;
            }
        }

        /*--------------------------------*/

       

        public async Task<JArray> getAccountNominee(string ddeaccountid)
        {
            try
            {
                string query_url = $"eqs_ddeaccountnominees()?$select=eqs_nomineename,eqs_emailid,eqs_mobile,eqs_nomineedob,eqs_addressline1,eqs_addressline2,eqs_addressline3,eqs_pincode,_eqs_city_value,_eqs_state_value,_eqs_country_value,eqs_guardianname,eqs_guardianmobile,eqs_guardianaddressline1,eqs_guardianaddressline2,eqs_guardianaddressline3,_eqs_guardiancity_value,_eqs_guardianstate_value,_eqs_guardiancountry_value,eqs_guardianpincode&$filter=_eqs_leadaccountddeid_value eq '{ddeaccountid}'";
                var Nomineedtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "", true);
                var Nominee_dtails = await this.getDataFromResponce(Nomineedtails);
                return Nominee_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getAccountNominee", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getAccountApplicd(string leadaccountid)
        {
            try
            {
                string query_url = $"eqs_accountapplicants()?$select=eqs_customer,eqs_name,eqs_isprimaryholder,_eqs_accountrelationship_value,eqs_dob&$orderby=eqs_isprimaryholder desc&$filter=_eqs_leadaccountid_value eq '{leadaccountid}'";
                var Applicentdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Applicent_dtails = await this.getDataFromResponce(Applicentdtails);
                return Applicent_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getAccountApplicd", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getAccountLeadData(string AccountID)
        {
            try
            {
                string leadaccount_id = await this.getIDfromMSDTable("eqs_leadaccounts", "eqs_leadaccountid", "eqs_crmleadaccountid", AccountID);
                string Stage = await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_dataentrystage", "Final");
                string query_url = $"eqs_ddeaccounts()?$filter=_eqs_leadaccountid_value eq '{leadaccount_id}' and eqs_dataentrystage eq {Stage}&$expand=eqs_productid($select=eqs_compoundingfrequencytype,eqs_payoutfrequencytype,eqs_productcode),eqs_leadaccountid($select=eqs_crmleadaccountid)";
                var Accountdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Account_dtails = await this.getDataFromResponce(Accountdtails);
                return Account_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getAccountLeadData", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getApplicentData(string ApplicantID)
        {
            try
            {
                string query_url = $"eqs_accountapplicants()?$select=_eqs_entitytypeid_value&$filter=eqs_applicantid eq '{ApplicantID}'";
                var Accountdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "", true);
                var Account_dtails = await this.getDataFromResponce(Accountdtails);
                return Account_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getApplicentData", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getApplicentFinalDDEbyAccountLead(string AccountDDEId)
        {
            try
            {
                string DataEntryStage = await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_dataentrystage", "Final");
                string query_url = $"eqs_ddeindividualcustomers()?$filter=_eqs_leadaccountdde_value eq '{AccountDDEId}' and eqs_dataentrystage eq {DataEntryStage}";
                var Accountdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "", true);
                var Account_dtails = await this.getDataFromResponce(Accountdtails);
                return Account_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getApplicentIndivDDE", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getApplicantIndivDDE(string ApplicantID)
        {
            try
            {
                string accountapplicantid = await this.getIDfromMSDTable("eqs_accountapplicants", "eqs_accountapplicantid", "eqs_applicantid", ApplicantID);
                //string DataEntryStage = await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_dataentrystage", "Final");

                string query_url = $"eqs_ddeindividualcustomers()?$select=eqs_ddeindividualcustomerid,eqs_readyforonboarding,eqs_onboardingvalidationmessage,_eqs_accountapplicantid_value,eqs_firstname,eqs_middlename,eqs_lastname,eqs_shortname,eqs_mobilenumber,eqs_emailid,eqs_dob,eqs_mothermaidenname,_eqs_sourcebranchid_value,_eqs_subentitytypeid_value,_eqs_corporatecompanyid_value,eqs_gendercode,_eqs_leadaccountdde_value,_eqs_custpreferredbranchid_value,eqs_customeridcreated,eqs_aadharreference,eqs_pannumber,_eqs_leadid_value&$filter=_eqs_accountapplicantid_value eq '{accountapplicantid}' and eqs_dataentrystage eq 615290002 &$expand=eqs_corporatecompanyid($select=eqs_corporatecode),eqs_custpreferredbranchId($select=eqs_branchidvalue),eqs_subentitytypeId($select=eqs_name)";
                var Accountdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Account_dtails = await this.getDataFromResponce(Accountdtails);
                return Account_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getApplicentIndivDDE", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getApplicantCorpDDE(string ApplicantID)
        {
            try
            {
                string accountapplicantid = await this.getIDfromMSDTable("eqs_accountapplicants", "eqs_accountapplicantid", "eqs_applicantid", ApplicantID);
                string DataEntryStage = await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_dataentrystage", "Final");

                //string query_url = $"eqs_ddecorporatecustomers()?$filter=_eqs_accountapplicantid_value eq '{accountapplicantid}' and eqs_dataentrystage eq 615290002";
                string query_url = $"eqs_ddecorporatecustomers()?$filter=_eqs_accountapplicantid_value eq '{accountapplicantid}' and eqs_dataentrystage eq 615290002 &$expand=eqs_preferredhomebranchId($select=eqs_branchidvalue)";
                var Accountdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Account_dtails = await this.getDataFromResponce(Accountdtails);
                return Account_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getApplicentIndivDDE", ex.Message);
                throw ex;
            }
        }

        public async Task<Dictionary<string, string>> getInstrakitStatus(string leadaccountdde_ID)
        {
            Dictionary<string, string> respons = new Dictionary<string, string>();
            try
            {
                string query_url = $"eqs_ddeaccounts()?$select=eqs_instakitcode,eqs_accountnocreated,eqs_primaryapplicantcustid&$filter=eqs_ddeaccountid eq '{leadaccountdde_ID}'";
                var ddeaccountdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var ddeaccount_dtails = await this.getDataFromResponce(ddeaccountdtails);

                string Instrakit_Text = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_instakitcode", ddeaccount_dtails[0]["eqs_instakitcode"].ToString());
                respons.Add("eqs_instakitcode", Instrakit_Text);
                respons.Add("accountnumner", ddeaccount_dtails[0]["eqs_accountnocreated"].ToString());
                respons.Add("custid", ddeaccount_dtails[0]["eqs_primaryapplicantcustid"].ToString());

                return respons;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getInstrakitStatus", ex.Message);
                throw ex;
            }

        }

        public async Task<JArray> getAddressData(string individuaID, string type = "")
        {
            try
            {
                string query_url = $"eqs_leadaddresses()?$select=eqs_addressline1,eqs_addressline2,eqs_addressline3,eqs_addressline4,eqs_pincode,_eqs_cityid_value,_eqs_stateid_value,_eqs_countryid_value&$filter=_eqs_individualdde_value eq '{individuaID}'";
                if (type=="corp")
                {
                    query_url = $"eqs_leadaddresses()?$select=eqs_addressline1,eqs_addressline2,eqs_addressline3,eqs_addressline4,eqs_pincode,_eqs_cityid_value,_eqs_stateid_value,_eqs_countryid_value&$filter=_eqs_corporatedde_value eq '{individuaID}'";
                }
                var Accountdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Account_dtails = await this.getDataFromResponce(Accountdtails);
                return Account_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getAddressData", ex.Message);
                throw ex;
            }
        }


        public async Task<string> getCityName(string CityId)
        {
            return await this.getIDfromMSDTable("eqs_cities", "eqs_name", "eqs_cityid", CityId);
        }
        public async Task<string> getStateName(string StateId)
        {
            return await this.getIDfromMSDTable("eqs_states", "eqs_name", "eqs_stateid", StateId);
        }
        public async Task<string> getCountryName(string CountryId)
        {
            return await this.getIDfromMSDTable("eqs_countries", "eqs_name", "eqs_countryid", CountryId);
        } 
        
        public async Task<string> getAccountRelation(string accRelationId)
        {
            return await this.getIDfromMSDTable("eqs_accountrelationshipses", "eqs_key", "eqs_accountrelationshipsid", accRelationId);
        }
        public async Task<string> getProductCode(string ProductId)
        {
            return await this.getIDfromMSDTable("eqs_products", "eqs_productcode", "eqs_productid", ProductId);
        }
        public async Task<string> getProductCategory(string CategoryId)
        {
            return await this.getIDfromMSDTable("eqs_productcategories", "eqs_productcategorycode", "eqs_productcategoryid", CategoryId);
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
