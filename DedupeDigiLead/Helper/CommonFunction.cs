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
        public CommonFunction(IQueryParser queryParser, ILoggers loggers)
        {
            this._queryParser = queryParser;
            this._loggers = loggers;
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
            string query_url = $"{tablename}()?$select={idfield}&$filter={filterkey} eq '{filtervalue}'";
            var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
            string TableId = await this.getIDFromGetResponce(idfield, responsdtails);
            return TableId;
        }

        public async Task<string> getclassificationId(string classification)
        {            
            return await this.getIDfromMSDTable("ccs_classifications", "ccs_classificationid", "ccs_name", classification);
        }

        public async Task<JArray> getLeadData(string ApplicantId)
        {
            try
            {
                string query_url = $"eqs_accountapplicants()?$select=eqs_internalpan,eqs_aadhar,eqs_passportnumber,eqs_name,eqs_dob,eqs_cinnumber,eqs_dateofregistration,_eqs_entitytypeid_value&$filter=eqs_applicantid eq '{ApplicantId}'";
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

        public async Task<JArray> getNLTRData(string Pan, string aadhar, string passport, string cin)
        {
            try
            {
                int filter = 0;
                string query_url = $"eqs_trnls()?$select=eqs_passports,eqs_pan,eqs_aadhaar,eqs_cin,eqs_dob&$filter=";
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
                        query_url += $"or eqs_passports eq '{passport}' ";
                    }
                    else
                    {
                        query_url += $"eqs_passports eq '{passport}' ";
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
                this._loggers.LogError("getLeadData", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getNLData(string Pan, string aadhar, string passport, string cin)
        {
            try
            {
                int filter = 0;
                string query_url = $"eqs_nls()?$select=eqs_passport,eqs_pan,eqs_aadhaar,eqs_cin,eqs_doiordob&$filter=";
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
                        query_url += $"or eqs_passport eq '{passport}' ";
                    }
                    else
                    {
                        query_url += $"eqs_passport eq '{passport}' ";
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
                this._loggers.LogError("getLeadData", ex.Message);
                throw ex;
            }
        }


        public async Task<string> MeargeJsonString(string json1, string json2)
        {
            string first = json1.Remove(json1.Length - 1, 1);
            string second = json2.Substring(1);
            return first + ", " + second;
        }

    }
}
