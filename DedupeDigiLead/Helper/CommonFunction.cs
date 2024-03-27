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

namespace DedupeDigiLead
{
    public class CommonFunction : ICommonFunction
    {
        public IQueryParser _queryParser;
        public ILoggers _loggers;
        public IMemoryCache _cache;
        public CommonFunction(IMemoryCache cache, IQueryParser queryParser, ILoggers loggers)
        {
            this._queryParser = queryParser;
            this._loggers = loggers;
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
                this._loggers.LogError("getIDfromMSDTable", ex.Message, $"Table {tablename} filterkey {filterkey} filtervalue {filtervalue}");
                throw;
            }
        }

        public async Task<string> getclassificationId(string classification)
        {            
            return await this.getIDfromMSDTable("ccs_classifications", "ccs_classificationid", "ccs_name", classification);
        }

        public async Task<List<string>> getLeadAccData(string LeadAccId)
        {
            try
            {
                List<string> Accounts = new List<string>();
                List<string> Accounts1;
                if (!this.GetMvalue<List<string>>("LeadAccId" + LeadAccId, out Accounts1))
                {
                    string lead_accountid = await this.getIDfromMSDTable("eqs_leadaccounts", "eqs_leadaccountid", "eqs_crmleadaccountid", LeadAccId);
                    string query_url = $"eqs_accountapplicants()?$select=eqs_applicantid&$filter=_eqs_leadaccountid_value eq '{lead_accountid}'";
                    var AccountDetails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                    var Account_Details = await this.getDataFromResponce(AccountDetails);
                    foreach (var account in Account_Details)
                    {
                        Accounts.Add(account["eqs_applicantid"].ToString());
                    }
                    this.SetMvalue<List<string>>("LeadAccId" + LeadAccId, 5, Accounts);
                }
                else
                {
                    Accounts = Accounts1;
                }
                return Accounts;
            }
            catch (Exception ex)
            {
                this._loggers.LogError("getLeadAccData", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getLeadData(string ApplicantId)
        {
            try
            {
                string query_url = $"eqs_accountapplicants()?$select=eqs_internalpan,eqs_aadhar,eqs_passportnumber,eqs_name,eqs_dob,eqs_cinnumber,eqs_dateofincorporation,eqs_firstname,eqs_middlename,eqs_lastname,eqs_companynamepart1,eqs_companynamepart2,eqs_companynamepart3&$filter=eqs_applicantid eq '{ApplicantId}' &$expand=eqs_entitytypeid($select=eqs_name)";
                var Leaddtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Lead_dtails = await this.getDataFromResponce(Leaddtails);
                return Lead_dtails;
            }
            catch (Exception ex)
            {
                this._loggers.LogError("getLeadData", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getNLTRData(string Custype, string Pan, string aadhar, string passport, string cin, string firstname, string middlename, string lastname, string dob)
        {
            try
            {
                int filter = 0;
                string query_url = $"eqs_trnls()?$select=eqs_trnlid,eqs_uid&$filter=";
                if (!string.IsNullOrEmpty(Pan))
                {
                    query_url += $"eqs_pan eq '{Pan}' ";
                    filter++;
                }
                if (Custype == "Individual")
                {
                    if (!string.IsNullOrEmpty(aadhar))
                    {
                        if (filter > 0)
                        {
                            query_url += $"or eqs_aadhaar eq '{aadhar}' ";
                        }
                        else
                        {
                            query_url += $"eqs_aadhaar eq '{aadhar}' ";
                        }
                        filter++;
                    }
                    if (!string.IsNullOrEmpty(passport))
                    {
                        if (filter > 0)
                        {
                            query_url += $"or eqs_passports eq '{passport}' ";
                        }
                        else
                        {
                            query_url += $"eqs_passports eq '{passport}' ";
                        }
                        filter++;
                    }
                    if (!string.IsNullOrEmpty(firstname))
                    {
                        if (filter > 0)
                        {
                            query_url += $"or eqs_fullname eq '{firstname + middlename + lastname}' or eqs_aliases eq '{firstname + " " + middlename + " " + lastname}' ";
                        }
                        else
                        {
                            query_url += $"eqs_fullname eq '{firstname + middlename + lastname}' or eqs_aliases eq '{firstname + " " + middlename + " " + lastname}' ";
                        }
                        filter++;
                    }
                    if (!string.IsNullOrEmpty(firstname) && !string.IsNullOrEmpty(lastname) && !string.IsNullOrEmpty(dob))
                    {
                        DateTime DOBdateTime = Convert.ToDateTime(dob);
                        string dobS = DOBdateTime.ToString("yyyyMMdd");
                        if (filter > 0)
                        {
                            query_url += $"or contains(eqs_firstname, '{firstname}') or contains(eqs_lastname, '{firstname}') or contains(eqs_aliases, '{firstname}')  or contains(eqs_alternativespelling, '{firstname}') or contains(eqs_firstname, '{lastname}') or contains(eqs_lastname, '{lastname}') or contains(eqs_aliases, '{lastname}')  or contains(eqs_alternativespelling, '{lastname}') and contains(eqs_dobs, '{dobS}') "; 
                        }
                        else
                        {
                            query_url += $"contains(eqs_firstname, '{firstname}') or contains(eqs_lastname, '{firstname}') or contains(eqs_aliases, '{firstname}')  or contains(eqs_alternativespelling, '{firstname}') or contains(eqs_firstname, '{lastname}') or contains(eqs_lastname, '{lastname}') or contains(eqs_aliases, '{lastname}')  or contains(eqs_alternativespelling, '{lastname}') and contains(eqs_dobs, '{dobS}') ";
                        }
                        filter++;
                    }
                }
                else if (Custype == "Corporate")
                {
                    if (!string.IsNullOrEmpty(cin))
                    {
                        if (filter > 0)
                        {
                            query_url += $"or eqs_cin eq '{cin}' ";
                        }
                        else
                        {
                            query_url += $"eqs_cin eq '{cin}' ";
                        }
                        filter++;
                    }
                    if (!string.IsNullOrEmpty(firstname))
                    {
                        if (filter > 0)
                        {
                            query_url += $"or eqs_fullname eq '{firstname + middlename + lastname}' ";
                        }
                        else
                        {
                            query_url += $"eqs_fullname eq '{firstname + middlename + lastname}' ";
                        }
                        filter++;
                    }
                    if (!string.IsNullOrEmpty(firstname) && !string.IsNullOrEmpty(dob))
                    {
                        DateTime DOBdateTime = Convert.ToDateTime(dob);
                        string dobS = DOBdateTime.ToString("yyyyMMdd");
                        if (filter > 0)
                        {
                            query_url += $"or contains(eqs_fullname, '{firstname}') or contains(eqs_companiesad, '{firstname}') or contains(eqs_aliasesad, '{firstname}')  or contains(eqs_alternativespelling, '{firstname}') and contains(eqs_dobs, '{dobS}') ";
                        }
                        else
                        {
                            query_url += $"contains(eqs_fullname, '{firstname}') or contains(eqs_companiesad, '{firstname}') or contains(eqs_aliasesad, '{firstname}')  or contains(eqs_alternativespelling, '{firstname}') and contains(eqs_dobs, '{dobS}') ";
                        }
                        filter++;
                    }
                }
                
                if (filter > 0)
                {
                    var NLTRdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                    var NLTR_dtails = await this.getDataFromResponce(NLTRdtails);
                    return NLTR_dtails;
                }
                else
                {
                    return new JArray();
                }
                
            }
            catch (Exception ex)
            {
                this._loggers.LogError("getNLTRData", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getNLData(string Pan, string aadhar, string passport, string cin)
        {
            try
            {
                int filter = 0;
                string query_url = $"eqs_nls()?$select=eqs_nlid,eqs_recordid&$filter=";
                if (!string.IsNullOrEmpty(Pan))
                {
                    query_url += $"eqs_pan eq '{Pan}' ";
                    filter++;
                }
                if (!string.IsNullOrEmpty(aadhar))
                {
                    if (filter > 0)
                    {
                        query_url += $"or eqs_aadhaar eq '{aadhar}' ";
                    }
                    else
                    {
                        query_url += $"eqs_aadhaar eq '{aadhar}' ";
                    }
                    filter++;
                }
                if (!string.IsNullOrEmpty(passport))
                {
                    if (filter > 0)
                    {
                        query_url += $"or contains(eqs_passport, '{passport}') ";
                    }
                    else
                    {
                        query_url += $"contains(eqs_passport, '{passport}') ";
                    }
                    filter++;
                }
                if (!string.IsNullOrEmpty(cin))
                {
                    if (filter > 0)
                    {
                        query_url += $"or eqs_cin eq '{cin}' ";
                    }
                    else
                    {
                        query_url += $"eqs_cin eq '{cin}' ";
                    }
                    filter++;
                }
                
                if (filter > 0)
                {
                    var NLTRdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                    var NLTR_dtails = await this.getDataFromResponce(NLTRdtails);
                    return NLTR_dtails;
                }
                    else
                {
                    return new JArray();
                }
        }
            catch (Exception ex)
            {
                this._loggers.LogError("getNLData", ex.Message);
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
