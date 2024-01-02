namespace CRMConnect
{
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Identity.Client;
    using Newtonsoft.Json;
    using System.Text;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Newtonsoft.Json.Linq;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Rest.Azure.OData;
    using System;
    using System.Xml;
    using Azure.Core;
    using System.Reflection.PortableExecutable;
    using Azure;
    using System.Xml.Linq;


    public class QueryParser : IQueryParser
    {
        readonly ILogger<QueryParser> _log;
        /// <summary>
        /// initiates KeyVault service to get configurations
        /// </summary>
        private readonly IKeyVaultService _keyVaultService;

        private const int CacheExpiryTimeAdjustmentMinutes = -5;
        private const string BearerHeaderValueCacheKey = nameof(HttpInterceptor) + "-BearerValue";
        private static readonly object SyncObject = new object();
        private readonly IMemoryCache _cache;
        private readonly ILoggers _errorLogger;


        public QueryParser(ILogger<QueryParser> logger, IKeyVaultService keyVaultService, IMemoryCache cache, ILoggers loggers)
        {  
              
            this._keyVaultService = keyVaultService;
            this._log = logger;
            this._cache = cache;
            this._errorLogger = loggers;
        }

        public async Task<List<JObject>> HttpApiCall(string odataQuery, HttpMethod httpMethod, string parameterToPost = "", bool isFormattedValueRequired = false)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(this._keyVaultService.ReadSecret("APIUrl"));                
                List<HttpContent> httpcontent = new List<HttpContent>();
                List<HttpMessageContent> odataRequestforChild = new List<HttpMessageContent>();

                odataQuery = httpClient.BaseAddress + odataQuery;
               
                if (!string.IsNullOrEmpty(parameterToPost))
                {
                    odataRequestforChild.Add(this.CreateHttpMessageContent(httpMethod, odataQuery, this._log, 1, parameterToPost, isFormattedValueRequired));
                }
                else
                {
                    odataRequestforChild.Add(this.CreateHttpMessageContent(httpMethod, odataQuery, this._log, isFormattedValueRequired: isFormattedValueRequired));
                }

                odataRequestforChild.ForEach(x =>
                            httpcontent.Add(x)
                        );
                List<JObject> results = await this.SendBatchRequestAsync(httpClient, httpcontent, this._log).ConfigureAwait(true);
                _errorLogger.LogInformation("HttpApiCall",$"URL :- {odataQuery} \n Request Param:- {parameterToPost}", JsonConvert.SerializeObject(results));
                return results;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("HttpApiCall", $"Error from CBS connect: {ex.Message} \n {ex.InnerException!} \n {ex.StackTrace!}", $"Query {odataQuery}  \n  post param {parameterToPost}");
                throw;
            }
        }

        public async Task<string> HttpCBSApiCall(string Token, HttpMethod httpMethod, string APIName, string parameterToPost = "")
        {
            string requestUri = this._keyVaultService.ReadSecret(APIName);
            string responJsonText = "";
            try
            {
                HttpClient httpClient = new HttpClient();               

                HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, requestUri);
                StringContent stringContent = new StringContent(parameterToPost);
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json;type=entry");
                requestMessage.Content = stringContent;
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                
                _errorLogger.LogInformation("HttpCBSApiCall Request", parameterToPost, "JWD Token \n" + Token);
                var response = await httpClient.SendAsync(requestMessage).ConfigureAwait(false);
                responJsonText = await response.Content.ReadAsStringAsync();
                dynamic responsej = JsonConvert.DeserializeObject(responJsonText);
                string ret_responJsonText = "";
                if (responsej.req_root!=null)
                {
                    string xmlData = await PayloadDecryption(responsej.req_root.body.payload.ToString(), "FI0060");
                    _errorLogger.LogInformation("HttpCBSApiCall Response", xmlData, parameterToPost);
                    string xpath = "PIDBlock/payload";
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xmlData);
                    var nodes = xmlDoc.SelectSingleNode(xpath);
                    
                    foreach (XmlNode childrenNode in nodes)
                    {
                        ret_responJsonText = childrenNode.Value.ToString();
                    }
                }
                else
                {
                    _errorLogger.LogInformation("HttpCBSApiCall Response", responJsonText, parameterToPost + "\n JWT Token \n " + Token);
                    ret_responJsonText = responJsonText;
                }
                
                //HttpResponseMessage respon = await httpClient.PostAsync(requestUri, new StringContent(parameterToPost, System.Text.Encoding.UTF8, "application/json"));
                //string responJsonText = await respon.Content.ReadAsStringAsync();
                return ret_responJsonText;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("HttpCBSApiCall", $"Error from CBS connect: {ex.Message} {ex.InnerException!} {ex.StackTrace!}", $"API {requestUri} \n  parameterToPost:-  {parameterToPost} \n responJsonText:- {responJsonText} ");
                throw;
            }
            
        }


        public HttpMessageContent CreateHttpMessageContent(HttpMethod httpMethod, string requestUri, ILogger log, int contentId = 0, string content = null, bool isFormattedValueRequired = false)
        {
            try
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, new Uri(requestUri));
                HttpMessageContent messageContent = new HttpMessageContent(requestMessage);
                messageContent.Headers.Remove("Content-Type");
                messageContent.Headers.Add("Content-Type", "application/http");
                messageContent.Headers.Add("Content-Transfer-Encoding", "binary");

                //// only GET request requires Accept header
                if (httpMethod == HttpMethod.Get)
                {
                    requestMessage.Headers.Add("Accept", "application/json");
                    if (isFormattedValueRequired)
                        requestMessage.Headers.Add("Prefer", "odata.include-annotations=*");
                }
                else if (httpMethod == HttpMethod.Delete)
                {
                    messageContent.Headers.Add("Content-ID", contentId.ToString(CultureInfo.CurrentCulture));
                }
                else
                {
                    //// request other than GET may have content, which is normally JSON
                    if (!string.IsNullOrEmpty(content))
                    {
                        StringContent stringContent = new StringContent(content);
                        stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json;type=entry");
                        stringContent.Headers.Add("Prefer", "return=representation");
                        stringContent.Headers.Add("OData-MaxVersion", "4.0");
                        stringContent.Headers.Add("OData-Version", "4.0");

                        requestMessage.Content = stringContent;
                    }

                    messageContent.Headers.Add("Content-ID", contentId.ToString(CultureInfo.CurrentCulture));
                }

                return messageContent;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("CreateHttpMessageContent", $"Error from CreateHttpMessageContent: {ex.Message} {ex.InnerException!} {ex.StackTrace!}");
                throw;
            }
        }


        public async Task<List<JObject>> SendBatchRequestAsync(HttpClient httpClient, List<HttpContent> listOfRequests, ILogger log, int count = 0)
        {
            try
            {
                using (MultipartContent batchContent = new MultipartContent("mixed", new StringBuilder("batch_").Append(Guid.NewGuid()).ToString()))
                {

                    if (listOfRequests != null)
                    {
                        foreach (var request in listOfRequests)
                        {
                            batchContent.Add(request);
                        }
                    }
                    string baseUrl = Convert.ToString(httpClient?.BaseAddress, CultureInfo.CurrentCulture);
                    string preferred_header = "odata.include-annotations=\"OData.Community.Display.V1.FormattedValue\"";
                    using (HttpRequestMessage batchRequest = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(baseUrl + "$batch")
                    })
                    {
                        CancellationTokenSource cancellationToken = new CancellationTokenSource();
                        if (!this._cache.TryGetValue<string>(BearerHeaderValueCacheKey, out var bearerHeaderValue))
                        {
                            // lock all threads with sync
                            lock (SyncObject)
                            {
                                if (!this._cache.TryGetValue<string>(BearerHeaderValueCacheKey, out bearerHeaderValue))
                                {
                                    // Get new token
                                    bearerHeaderValue = this.AcquireNewTokenAsync(cancellationToken.Token).GetAwaiter().GetResult();
                                }
                            }
                        }
                        cancellationToken.Cancel();

                        batchRequest.Content = batchContent;
                        batchRequest.Headers.Add("Prefer", preferred_header);
                        batchRequest.Headers.Add("OData-MaxVersion", "4.0");
                        batchRequest.Headers.Add("OData-Version", "4.0");
                        batchRequest.Headers.Add("Accept", "application/json");
                        batchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerHeaderValue);


                        HttpResponseMessage response = await httpClient.SendAsync(batchRequest).ConfigureAwait(true);
                       // _errorLogger.requestPerser(batchRequest, response);

                       // log.LogInformationWithTraceID($"Send batch request to CE Started");
                        MultipartMemoryStreamProvider body = await response.Content.ReadAsMultipartAsync().ConfigureAwait(true);
                        List<HttpResponseMessage> contents = await ReadHttpContents(body, log).ConfigureAwait(true);
                       // log.LogInformationWithTraceID($"Send batch request to CE completed");
                        var results = await HandleResponses(contents, log).ConfigureAwait(true);
                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("SendBatchRequestAsync", $"Error from QueryParser > SendBatchRequestAsync: {ex.Message} {ex.InnerException!} {ex.StackTrace!}");
                throw;
            }
        }


        private async Task<string> AcquireNewTokenAsync(CancellationToken cancellationToken)
        {
            var authority = $"https://login.microsoftonline.com/{this._keyVaultService.ReadSecret("TenantId")}";
            var app =
                ConfidentialClientApplicationBuilder.Create(
                    this._keyVaultService.ReadSecret("DynamicsClientId"))
                                                    .WithClientSecret(this._keyVaultService.ReadSecret("DynamicsSecretId"))
                                                    .WithAuthority(authority)
                                                    .Build();

            var authResult = await app.AcquireTokenForClient(new[] { this._keyVaultService.ReadSecret("DynamicsScope") })
                .ExecuteAsync(cancellationToken).ConfigureAwait(true);

            string bearerHeaderValue = authResult.AccessToken;

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(authResult.ExpiresOn.AddMinutes(CacheExpiryTimeAdjustmentMinutes));

            this._cache.Set(BearerHeaderValueCacheKey, bearerHeaderValue, cacheEntryOptions);

            return bearerHeaderValue;
        }

        public async Task<List<HttpResponseMessage>> ReadHttpContents(MultipartMemoryStreamProvider body, ILogger log)
        {
            try
            {
                List<HttpResponseMessage> results = new List<HttpResponseMessage>();
                if (body?.Contents != null)
                {
                    foreach (HttpContent c in body.Contents)
                    {
                        if (c.IsMimeMultipartContent())
                        {
                            results.AddRange(await ReadHttpContents(await c.ReadAsMultipartAsync().ConfigureAwait(true), log).ConfigureAwait(true));
                        }
                        else if (c.IsHttpResponseMessageContent())
                        {
                            HttpResponseMessage responseMessage = await c.ReadAsHttpResponseMessageAsync().ConfigureAwait(true);
                            if (responseMessage != null)
                            {
                                results.Add(responseMessage);
                            }
                        }
                        else
                        {
                            HttpResponseMessage responseMessage = DeserializeToResponse(await c.ReadAsStreamAsync().ConfigureAwait(true), log);
                            if (responseMessage != null)
                            {
                                results.Add(responseMessage);
                            }
                        }
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("AcquireNewTokenAsync", $"Error from ReadHttpContents: {ex.Message}");
                throw;
            }
        }


        public async Task<List<JObject>> HandleResponses(List<HttpResponseMessage> responses, ILogger log)
        {
            try
            {
                List<JObject> result = new List<JObject>();
                foreach (HttpResponseMessage response in responses)
                {
                    //// For Post or Patch request(204)
                    if (response.StatusCode == HttpStatusCode.NoContent)
                    {
                        if (response.Headers.Contains("OData-EntityId"))
                        {
                            string entityUri = response.Headers.GetValues("OData-EntityId").FirstOrDefault();
                            JObject uriValues = new JObject
                        {
                            new JProperty("responsecode", HttpStatusCode.NoContent),
                            new JProperty("responsebody", entityUri)
                        };
                            result.Add(uriValues);
                            
                        }
                    }
                    else if (response.StatusCode == HttpStatusCode.OK)
                    {
                        //// for get request(200)
                        string results = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                        JObject mainContent = new JObject
                        {
                            new JProperty("responsecode", HttpStatusCode.OK),
                            new JProperty("responsebody", JObject.Parse(results))
                        };
                        result.Add(mainContent);
                        
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        //// for Not found request(404)
                        string reqId = response.Headers.GetValues("REQ_ID").FirstOrDefault();
                        string results = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                        JObject mainContent = new JObject
                        {
                            new JProperty("responsecode", HttpStatusCode.NotFound),
                            new JProperty("responsebody", JObject.Parse(results))
                        };
                        result.Add(mainContent);
                        _errorLogger.LogError("HandleResponses", $"Requested Entity:" + reqId + " Not found");
                    }
                    else
                    {
                        string results = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                        JObject error = new JObject
                        {
                            new JProperty("responsecode", response.StatusCode),
                            new JProperty("responsebody", JObject.Parse(results))
                        };
                        result.Add(error);
                        _errorLogger.LogError("HandleResponses", $"MSD Dataverse API Request failed. Reason: {JsonConvert.SerializeObject(error)}");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("HandleResponses", $"Error from HandleResponses: {ex.Message}");
                throw;
            }
        }


        public async Task<string> getAccessToken()
        {
            string Token_Id;
            try
            {
                if (!this.GetMvalue<string>("wso2token", out Token_Id))
                {
                    Token_Id = this._keyVaultService.ReadSecret("CBSToken");

                    this.SetMvalue<string>("wso2token", 1440, Token_Id);
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("getAccessToken", $"Error while fetching WSO2 Access Token : {ex.Message}");
                throw;
            }

            return Token_Id;
        }


        private HttpResponseMessage DeserializeToResponse(Stream stream, ILogger log)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    response.Content = new ByteArrayContent(memoryStream.ToArray());
                    response.Content.Headers.Add("Content-Type", "application/http;msgtype=response");
                    return response.Content.ReadAsHttpResponseMessageAsync().Result;
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogError("DeserializeToResponse", $"Error from DeserializeToResponse: {ex.Message}");
                throw;
            }
        }



        public async Task<string> PayloadEncryption(string V_requestData, string V_requestedID, string BankCode)
        {
            string V_bankcode = BankCode;   //this._keyVaultService.ReadSecret("Vbankcode");
            string V_AESSymmetricKey = this._keyVaultService.ReadSecret("VAESSymmetricKey" + "-" + BankCode);
            string V_ChecksumKey = this._keyVaultService.ReadSecret("VChecksumKey" + "-" + BankCode);
            

            string finalrequestdata = "";
            V_requestData = V_requestData.Replace("\\", "");
            string PIDBlock1 = "<?xml version=" + '"' + "1.0" + '"' + " encoding=" + '"' + "UTF-8" + '"' + "?>\n<PIDBlock>\n<payload>";
            string CheckSumValue = await SHA512(V_requestData + V_ChecksumKey);
            //CheckSumValue = HexStringToBinary(CheckSumValue);

            string PIDBlock2 = "</payload>\n<checksum>";
            string Payloadvalue = V_requestData;
            string PIDBlock3 = "</checksum>\n</PIDBlock>";

            string FinalEncryptionData = PIDBlock1 + Payloadvalue + PIDBlock2 + CheckSumValue + PIDBlock3;
            //string FinalEncryptionData =   Payloadvalue;

            string encryptedText = await EQEncryption(V_AESSymmetricKey, FinalEncryptionData);
            DateTime CurrentDate = Convert.ToDateTime(Convert.ToString(DateTime.Now));
            string Cdate = CurrentDate.ToString("yyyy-MM-ddTHH:MM:ss.214Z", CultureInfo.InvariantCulture);
            string currentdatetime = Cdate;

            string Frame1 = "{ " + '"' + "req_root" + '"' + ":{" + '"' + "header" + '"' + ":{" + '"' + "dateTime" + '"' + ":" + '"' + currentdatetime + '"' + "," + '"' + "cde" + '"' + ":" + '"' + V_bankcode + '"' + "," + '"' + "requestId" + '"' + ":" + '"' + V_requestedID + '"' + "," + '"' + "version" + '"' + ":" + '"' + "1.0" + '"' + "}," + '"' + "body" + '"' + ":{" + '"' + "payload" + '"' + ":" + '"';
            string Frame2 = '"' + "}}}";

            finalrequestdata = Frame1 + encryptedText + Frame2;
            return finalrequestdata;
        }

        public async Task<string> EQEncryption(string V_AESSymmetricKey, string V_requestData)
        {
            string encryptedText = "";

            // Encrypting code
            AesManaged tdes = new AesManaged();
            //tdes.Key = Encoding.UTF8.GetBytes(V_AESSymmetricKey);
            tdes.Key = Convert.FromBase64String(V_AESSymmetricKey);
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7; // The default is AES/ECB/PKCS5Padding
            ICryptoTransform encrypt = tdes.CreateEncryptor();
            byte[] encryptFinaldata = Encoding.UTF8.GetBytes(V_requestData);
            byte[] cipherencrypt = encrypt.TransformFinalBlock(encryptFinaldata, 0, encryptFinaldata.Length);
            encryptedText = Convert.ToBase64String(cipherencrypt);
            return encryptedText;
        }

        public async Task<string> SHA512(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            using (var hash = System.Security.Cryptography.SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);

                // Convert to text
                // StringBuilder Capacity is 128, because 512 bits / 8 bits in byte * 2 symbols for byte 
                var hashedInputStringBuilder = new System.Text.StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));
                return hashedInputStringBuilder.ToString();
            }
        }

        public async Task<string> PayloadDecryption(string V_requestData, string BankCode, string APIName= "")
        {
            try
            {
                string decryptedText = "";
                // Decrypting code
                AesManaged des = new AesManaged();
                string V_AESSymmetricKey = this._keyVaultService.ReadSecret("VAESSymmetricKey" + "-" + BankCode);
                //des.Key = Encoding.UTF8.GetBytes(V_AESSymmetricKey);
                des.Key = Convert.FromBase64String(V_AESSymmetricKey);
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.PKCS7; // The default is AES/ECB/PKCS5Padding
                ICryptoTransform decrypt = des.CreateDecryptor();
                byte[] decryptFinaldata = Convert.FromBase64String(V_requestData);
                byte[] cipherdecrypt = decrypt.TransformFinalBlock(decryptFinaldata, 0, decryptFinaldata.Length);
                decryptedText = Encoding.UTF8.GetString(cipherdecrypt);
                decryptedText = decryptedText.Replace("\\", "");
                if (!string.IsNullOrEmpty(APIName))
                {
                    string APIPermissions, APIPermissions1;
                    if (!this.GetMvalue<string>("APIPermissions", out APIPermissions1))
                    {
                        APIPermissions = this._keyVaultService.ReadSecret("APIPermissions");
                        this.SetMvalue<string>("APIPermissions", 10080, APIPermissions);
                    }
                    else
                    {
                        APIPermissions = APIPermissions1;
                    }
                    dynamic ApiPerData = JsonConvert.DeserializeObject(APIPermissions);
                    int match = 0;
                    foreach (var Item in ApiPerData[BankCode])
                    {
                        if (Item.ToString() == APIName)
                        {
                            match = 1; 
                            break;
                        }
                    }
                    if (match == 0)
                    {
                        decryptedText = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<PIDBlock>\r\n<payload>{\"" + APIName + "\":{\"msgHdr\":{\"channelID\":\"12\",\"transactionType\":\"\",\"transactionSubType\":\"\",\"conversationID\":\"CreateLead1684402315\",\"externalReferenceId\":\"\",\"isAsync\":false,\"authInfo\":{\"branchID\":\"\",\"userID\":\"\",\"token\":\"Y62RxQDpPW\"}},\"msgBdy\":{\"ErrorNo\":\"Error99\"}}}</payload>\r\n<checksum></checksum></PIDBlock>";
                    }
                }
                //var json = JsonConvert.DeserializeObject(decryptedText);
                //decryptedText = JsonConvert.SerializeObject(json);
                return decryptedText;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }


        public async Task<string> getOptionSetTextToValue(string tableName, string fieldName, string OptionText)
        {
            try
            {
                List<Optionsetvlu> optionSet, optionSet1;

                if (!this.GetMvalue<List<Optionsetvlu>>(tableName + "optionsetvalue", out optionSet1))
                {
                    string query_url = $"stringmaps()?$select=value,attributevalue,objecttypecode,attributename&$filter=objecttypecode eq '{tableName}'";
                    var responsdtails = await this.HttpApiCall(query_url, HttpMethod.Get, "");
                    var Optiondata = await this.getDataFromResponce(responsdtails);
                    optionSet = new List<Optionsetvlu>();
                    foreach (var item in Optiondata)
                    {
                        Optionsetvlu optionsetvlu = new Optionsetvlu();
                        optionsetvlu.optionKey = item["value"].ToString();
                        optionsetvlu.optionValu = item["attributevalue"].ToString();
                        optionsetvlu.optionTable = item["objecttypecode"].ToString();
                        optionsetvlu.optionField = item["attributename"].ToString();
                        optionSet.Add(optionsetvlu);
                    }

                    this.SetMvalue<List<Optionsetvlu>>(tableName + "optionsetvalue", 10080, optionSet);
                }
                else
                {
                    optionSet = optionSet1;
                }
               
                string opztionValueId = optionSet.Where(x=>x.optionTable == tableName && x.optionField == fieldName && x.optionKey == OptionText).FirstOrDefault().optionValu;

                return opztionValueId;
                              
            }
            catch(Exception ex)
            {
                _errorLogger.LogError("getOptionSetTextToValue", ex.Message,$"Table:- {tableName} Eield name:- {fieldName} option value :- {OptionText}");
                return null;
            }
           
            

            
        }

        public async Task<string> getOptionSetValuToText(string tableName, string fieldName, string OptionValue)
        {
            try
            {
                List<Optionsetvlu> optionSet, optionSet1;

                if (!this.GetMvalue<List<Optionsetvlu>>(tableName + "optionsetvalue", out optionSet1))
                {
                    string query_url = $"stringmaps()?$select=value,attributevalue,objecttypecode,attributename&$filter=objecttypecode eq '{tableName}'";
                    var responsdtails = await this.HttpApiCall(query_url, HttpMethod.Get, "");
                    var Optiondata = await this.getDataFromResponce(responsdtails);
                    optionSet = new List<Optionsetvlu>();
                    foreach (var item in Optiondata)
                    {
                        Optionsetvlu optionsetvlu = new Optionsetvlu();
                        optionsetvlu.optionKey = item["value"].ToString();
                        optionsetvlu.optionValu = item["attributevalue"].ToString();
                        optionsetvlu.optionTable = item["objecttypecode"].ToString();
                        optionsetvlu.optionField = item["attributename"].ToString();
                        optionSet.Add(optionsetvlu);
                    }

                    this.SetMvalue<List<Optionsetvlu>>(tableName + "optionsetvalue", 10080, optionSet);
                }
                else
                {
                    optionSet = optionSet1;
                }

                string opztionValueId = optionSet.Where(x => x.optionTable == tableName && x.optionField == fieldName && x.optionValu == OptionValue).FirstOrDefault().optionKey;

                return opztionValueId;

            }
            catch (Exception ex)
            {
                _errorLogger.LogError("getOptionSetValuToText", ex.Message, $"Table:- {tableName} Eield name:- {fieldName} option value :- {OptionValue}");
                return null;
            }

        }

        public async Task<bool> DeleteFromTable(string tablename, string tableid = "", string filter = "", string filtervalu = "", string tableselecter = "")
        {
            if (!string.IsNullOrEmpty(tableid))
            {
                await this.HttpApiCall($"{tablename}({tableid})?", HttpMethod.Delete, "");
                return true;
            }
            else if (!string.IsNullOrEmpty(filter) && !string.IsNullOrEmpty(filtervalu))
            {
                HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(this._keyVaultService.ReadSecret("APIUrl"));
                List<HttpMessageContent> odataRequestforChild = new List<HttpMessageContent>();
                List<HttpContent> httpcontent = new List<HttpContent>();

                string query_url = $"{tablename}()?$select={tableselecter}&$filter={filter} eq '{filtervalu}'";
                var deleteItems = await this.HttpApiCall(query_url, HttpMethod.Get, "");
                var delete_Items = await this.getDataFromResponce(deleteItems);
                foreach (var item in delete_Items)
                {
                    query_url = httpClient.BaseAddress + $"{tablename}({item[tableselecter]})" + "?";
                    odataRequestforChild.Add(this.CreateHttpMessageContent(HttpMethod.Delete, query_url, this._log));
                }

                if (odataRequestforChild.Any())
                {
                    odataRequestforChild.ForEach(x =>
                            httpcontent.Add(x)
                        );
                    await this.SendBatchRequestAsync(httpClient, httpcontent, this._log).ConfigureAwait(true);
                }

                return true;
            }
            else
            {
                return false;
            }

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

    public class Optionsetvlu
    {
        public string optionKey { get; set; }
        public string optionValu { get; set; }
        public string optionTable { get; set; }
        public string optionField { get; set; }
        
    }
}
