namespace DigiDocument
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
    using System.Reflection.Metadata;


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

        public async Task<string> getDocCategoryId(string doccategory)
        {            
            return await this.getIDfromMSDTable("eqs_doccategories", "eqs_doccategoryid", "eqs_doccategorycode", doccategory);
        }
        public async Task<string> getDocSubentityId(string docsubcategory, string Cat_code)
        {
            return await this.getIDfromMSDTable("eqs_docsubcategories", "eqs_docsubcategoryid", $"eqs_doccategorycode eq '{Cat_code}' and eqs_docsubcategorycode", docsubcategory);
        }
        public async Task<string> getDocTypeId(string docsubcategory)
        {
            return await this.getIDfromMSDTable("eqs_doctypes", "eqs_doctypeid", "eqs_name", docsubcategory);
        }
        public async Task<string> getSystemuserId(string system_user)
        {
            return await this.getIDfromMSDTable("systemusers", "systemuserid", "fullname", system_user);
        }
        public async Task<string> getLeadId(string leadid)
        {
            return await this.getIDfromMSDTable("leads", "leadid", "eqs_crmleadid", leadid);
        }
        public async Task<string> getLeadAccountId(string leadaccid)
        {
            return await this.getIDfromMSDTable("eqs_leadaccounts", "eqs_leadaccountid", "eqs_crmleadaccountid", leadaccid);
        }
        public async Task<string> getCustomerId(string customer)
        {
            return await this.getIDfromMSDTable("contacts", "contactid", "eqs_customerid", customer);
        }
        public async Task<string> getAccountId(string account)
        {
            return await this.getIDfromMSDTable("eqs_accounts", "eqs_accountid", "eqs_accountno", account);
        }
        public async Task<string> getCaseId(string caseid)
        {
            return await this.getIDfromMSDTable("incidents", "incidentid", "ticketnumber", caseid);
        }
        /*---------------------------------------------------*/
        public async Task<string> getDocCategoryText(string doccategory)
        {
            return await this.getIDfromMSDTable("eqs_doccategories", "eqs_doccategorycode", "eqs_doccategoryid", doccategory);
        }
        public async Task<string> getDocSubentityText(string docsubcategory)
        {
            return await this.getIDfromMSDTable("eqs_docsubcategories", "eqs_docsubcategorycode", "eqs_docsubcategoryid", docsubcategory);
        }
        public async Task<string> getDocTypeText(string docsubcategory)
        {
            return await this.getIDfromMSDTable("eqs_doctypes", "eqs_name", "eqs_doctypeid", docsubcategory);
        }
        public async Task<string> getSystemuserText(string system_user)
        {
            return await this.getIDfromMSDTable("systemusers", "fullname", "systemuserid", system_user);
        }
        public async Task<string> getLeadText(string leadid)
        {
            return await this.getIDfromMSDTable("leads", "eqs_crmleadid", "leadid", leadid);
        }
        public async Task<string> getLeadAccountText(string leadaccid)
        {
            return await this.getIDfromMSDTable("eqs_leadaccounts", "eqs_crmleadaccountid", "eqs_leadaccountid", leadaccid);
        }
        public async Task<string> getCustomerText(string customer)
        {
            return await this.getIDfromMSDTable("contacts", "eqs_customerid", "contactid", customer);
        }
        public async Task<string> getAccountText(string account)
        {
            return await this.getIDfromMSDTable("eqs_accounts", "eqs_accountno", "eqs_accountid", account);
        }
        public async Task<string> getCaseText(string caseid)
        {
            return await this.getIDfromMSDTable("incidents", "ticketnumber", "incidentid", caseid);
        }

        public async Task<string> getDocumentID(string Documentid)
        {
            return await this.getIDfromMSDTable("eqs_leaddocuments", "eqs_leaddocumentid", "eqs_documentid", Documentid);
        }

        

        public async Task<List<Document>> getDocumentList(string query_url)
        {
            List<Document> documentDtls = new List<Document>();
            try
            {                
                var documentdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var document_dtails = await this.getDataFromResponce(documentdtails);
                foreach (var item in document_dtails)
                {
                    Document document = new Document();
                    document.CRMDocumentID = item["eqs_documentid"].ToString();
                    document.CategoryCode = await getDocCategoryText(item["_eqs_doccategory_value"].ToString());
                    document.SubcategoryCode = await getDocSubentityText(item["_eqs_docsubcategory_value"].ToString());
                    document.DocumentType = await getDocTypeText(item["_eqs_doctype_value"].ToString());
                    
                    document.IssuedAt = item["eqs_issuedat"].ToString();
                    document.IssueDate = item["eqs_issuedate"].ToString();
                    document.ExpiryDate = item["eqs_expirydate"].ToString();
                    document.DmsDocumentID = item["eqs_dmsrequestid"].ToString();
                    document.VerificationStatus = item["eqs_verificationstatus"].ToString();                   
                    document.VerifiedOn = item["eqs_verifiedon"].ToString();

                    document.VerifiedBy = await getSystemuserText(item["_eqs_verifiedbyid_value"].ToString());
                    document.MappedCustomerLead = await getLeadText(item["_eqs_leadid_value"].ToString());
                    document.MappedAccountLead = await getLeadAccountText(item["_eqs_leadaccountid_value"].ToString());
                    document.MappedUCIC = await getCustomerText(item["_eqs_ucicid_value"].ToString());
                    document.MappedAccount = await getAccountText(item["_eqs_accountnumberid_value"].ToString());
                    document.MappedServiceRequest = await getCaseText(item["_eqs_caseid_value"].ToString());

                    documentDtls.Add(document);
                }
                return documentDtls;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDocumentList", ex.Message);
                throw ex;
            }
        }

        public async Task<List<MasterConfiguration>> getDocumentDateConfig()
        {
            try
            {
                List<MasterConfiguration> masterConfiguration = new List<MasterConfiguration>();
                string query_url = $"eqs_masterconfigurations()?$select=eqs_key,eqs_value&$filter=eqs_key eq 'docid_issue_expirydate' or eqs_key eq 'docid_issuedat'";
                var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var entities = await this._queryParser.getDataFromResponce(responsdtails);

                foreach (var entity in entities)
                {
                    MasterConfiguration configuration = new MasterConfiguration();

                    configuration.Key = entity["eqs_key"].ToString();
                    configuration.Value = entity["eqs_value"].ToString();
                    masterConfiguration.Add(configuration);
                }

                return masterConfiguration;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDocumentDateConfig", ex.Message);
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
