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

        public async Task<List<JObject>> HttpApiCall(string odataQuery, HttpMethod httpMethod, string parameterToPost = "")
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(this._keyVaultService.ReadSecret("APIUrl"));
            List<HttpContent> httpcontent = new List<HttpContent>();
            List<HttpMessageContent> odataRequestforChild = new List<HttpMessageContent>();

            odataQuery = httpClient.BaseAddress + odataQuery;

            if (!string.IsNullOrEmpty(parameterToPost))
            {
                odataRequestforChild.Add(this.CreateHttpMessageContent(httpMethod, odataQuery, this._log, 1, parameterToPost));
            }
            else
            {
                odataRequestforChild.Add(this.CreateHttpMessageContent(httpMethod, odataQuery, this._log));
            }

            odataRequestforChild.ForEach(x =>
                        httpcontent.Add(x)
                    );
            List<JObject> results = await this.SendBatchRequestAsync(httpClient, httpcontent, this._log).ConfigureAwait(true);
            return results;
        }


        public HttpMessageContent CreateHttpMessageContent(HttpMethod httpMethod, string requestUri, ILogger log, int contentId = 0, string content = null, bool isUpsert = false)
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



        public async Task<string> PayloadEncryption(string V_requestData, string V_requestedID)
        {
            string V_AESSymmetricKey = this._keyVaultService.ReadSecret("VAESSymmetricKey");
            string V_ChecksumKey = this._keyVaultService.ReadSecret("VChecksumKey");
            string V_bankcode = this._keyVaultService.ReadSecret("Vbankcode");

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

        public async Task<string> PayloadDecryption(string V_requestData)
        {
            try
            {
                string decryptedText = "";
                // Decrypting code
                AesManaged des = new AesManaged();
                string V_AESSymmetricKey = this._keyVaultService.ReadSecret("VAESSymmetricKey");
                //des.Key = Encoding.UTF8.GetBytes(V_AESSymmetricKey);
                des.Key = Convert.FromBase64String(V_AESSymmetricKey);
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.PKCS7; // The default is AES/ECB/PKCS5Padding
                ICryptoTransform decrypt = des.CreateDecryptor();
                byte[] decryptFinaldata = Convert.FromBase64String(V_requestData);
                byte[] cipherdecrypt = decrypt.TransformFinalBlock(decryptFinaldata, 0, decryptFinaldata.Length);
                decryptedText = Encoding.UTF8.GetString(cipherdecrypt);
                decryptedText = decryptedText.Replace("\\", "");
                //var json = JsonConvert.DeserializeObject(decryptedText);
                //decryptedText = JsonConvert.SerializeObject(json);
                return decryptedText;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }

    }
}
