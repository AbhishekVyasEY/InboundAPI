namespace DMSCallBack
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Xml.Linq;
    using CRMConnect;
    using System.Xml;
    using System.Threading.Channels;
    using System.ComponentModel;
    using System;
    using System.Globalization;

    public class DCMCallBackExecution : IDCMCallBackExecution
    {

        private ILoggers _logger;
        private IQueryParser _queryParser;
        public string Bank_Code { set; get; }

        public string Channel_ID
        {
            set
            {
                _logger.Channel_ID = value;
            }
            get
            {
                return _logger.Channel_ID;
            }
        }
        public string Transaction_ID
        {
            set
            {
                _logger.Transaction_ID = value;
            }
            get
            {
                return _logger.Transaction_ID;
            }
        }

        public string appkey { set; get; }

        public string API_Name { set
            {
                _logger.API_Name = value;
            }
        }
        public string Input_payload { set {
                _logger.Input_payload = value;
            } 
        }

        private readonly IKeyVaultService _keyVaultService;

                
        private ICommonFunction _commonFunc;

        public DCMCallBackExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
           
           
        }


        public async Task<DMSCallBackReturn> ValidateDMSInput(dynamic RequestData)
        {
            DMSCallBackReturn ldRtPrm = new DMSCallBackReturn();
            RequestData = await this.getRequestData(RequestData, "dmsAddDocumentResponse");
          
            try
            {
                string requestId = await this._commonFunc.getDocumentID(RequestData.requestId.ToString()); 
                string Result_type = RequestData.result;
               

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "DMSCallBackappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        if ((!string.IsNullOrEmpty(requestId) && requestId != "") && (!string.IsNullOrEmpty(Result_type) && Result_type != ""))
                        {
                            if (Result_type == "OK")
                            {
                                ldRtPrm = await this.UpdateDCMSuccess(requestId, RequestData.addDocResp);
                            }
                            else if (Result_type == "ERROR")
                            {
                                ldRtPrm = await this.UpdateDCMError(requestId, RequestData.error);
                            }
                            else
                            {
                                this._logger.LogInformation("ValidateDMSInput", "Action not authorized");
                                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                                ldRtPrm.Message = "Action not authorized";
                            }
                            

                        }
                        else
                        {
                            this._logger.LogInformation("ValidateDMSInput", "RequestId or Result_type is incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "RequestId or Result_type is incorrect";
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateDMSInput", "Transaction_ID or Channel_ID is incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or Channel_ID is incorrect";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateDMSInput", "Appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Appkey is incorrect";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateDMSInput", ex.Message);
                throw ex;
            }
            
        }


        public bool checkappkey(string appkey, string APIKey)
        {
            if (this._keyVaultService.ReadSecret(APIKey) == appkey)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        

       

        public async Task<DMSCallBackReturn> UpdateDCMSuccess(string RequestId, dynamic succData)
        {
            DMSCallBackReturn csRtPrm = new DMSCallBackReturn();
            try
            {
                foreach (var item in succData)
                {
                    Dictionary<string, string> DataMapping = new Dictionary<string, string>();

                    DataMapping.Add("eqs_dmsdocumentname", item.docNm.ToString());
                    DataMapping.Add("eqs_dmsdocumentid", item.docIndx.ToString());
                    string update_date = DateTime.Now.Date.ToString("yyyy-MM-dd") + " " + DateTime.Now.ToString("HH") + ":" + DateTime.Now.ToString("mm") + ":" + DateTime.Now.ToString("ss");

                    DataMapping.Add("eqs_uploadeddatetimefordms", update_date);
                    DataMapping.Add("eqs_dmsdocumentuploadstatus", "true");

                    string postDataParametr = JsonConvert.SerializeObject(DataMapping);

                    var document_details = await this._queryParser.HttpApiCall($"eqs_leaddocuments({RequestId})?$select=eqs_documentid", HttpMethod.Patch, postDataParametr);

                    if (document_details.Count > 0)
                    {
                        dynamic respons_code = document_details[0];
                        if (respons_code.responsecode == 200)
                        {
                            csRtPrm.Documentid = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "eqs_documentid");
                            csRtPrm.ReturnCode = "CRM - SUCCESS";
                            csRtPrm.Message = OutputMSG.Case_Success;
                        }
                        else if (respons_code.responsecode == null || respons_code.responsecode == 400)
                        {
                            this._logger.LogInformation("UpdateDCMSuccess", JsonConvert.SerializeObject(document_details));
                            csRtPrm.ReturnCode = "CRM-ERROR-102";
                            csRtPrm.Message = "DMS Call back fail.";
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("UpdateDCMSuccess", JsonConvert.SerializeObject(document_details));
                        csRtPrm.ReturnCode = "CRM-ERROR-102";
                        csRtPrm.Message = "DMS Call back fail.";
                    }
                }
                

                

            }
            catch (Exception ex)
            {
                this._logger.LogError("UpdateDCMSuccess", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = "DMS Call back fail.";
            }

            return csRtPrm;
        }

        public async Task<DMSCallBackReturn> UpdateDCMError(string RequestId, dynamic ErrorData)
        {
            DMSCallBackReturn csRtPrm = new DMSCallBackReturn();
            try
            {
                Dictionary<string, string> DataMapping = new Dictionary<string, string>();

                DataMapping.Add("eqs_dmsdocumentuploadstatus", "true");
                DataMapping.Add("eqs_integrationerrorcode", ErrorData.code.ToString());
                DataMapping.Add("eqs_integrationerrormessage", ErrorData.reason.ToString());
                string update_date = DateTime.Now.Date.ToString("yyyy-MM-dd") + " " + DateTime.Now.ToString("HH") + ":" + DateTime.Now.ToString("mm") + ":" + DateTime.Now.ToString("ss");

                DataMapping.Add("eqs_uploadeddatetimefordms", update_date);

                string postDataParametr = JsonConvert.SerializeObject(DataMapping);

                var document_details = await this._queryParser.HttpApiCall($"eqs_leaddocuments({RequestId})?$select=eqs_documentid", HttpMethod.Patch, postDataParametr);

                if (document_details.Count > 0)
                {
                    dynamic respons_code = document_details[0];
                    if (respons_code.responsecode == 200)
                    {
                        csRtPrm.Documentid = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "eqs_documentid");
                        csRtPrm.ReturnCode = "CRM - SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }
                    else if (respons_code.responsecode == null || respons_code.responsecode == 400)
                    {
                        this._logger.LogInformation("UpdateDCMError", JsonConvert.SerializeObject(document_details));
                        csRtPrm.ReturnCode = "CRM-ERROR-102";
                        csRtPrm.Message = "DMS Call back fail.";
                    }
                }
                else
                {
                    this._logger.LogInformation("UpdateDCMSuccess", JsonConvert.SerializeObject(document_details));
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = "DMS Call back fail.";
                }

            }
            catch (Exception ex)
            {
                this._logger.LogError("UpdateDCMError", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = "DMS Call back fail.";
            }

            return csRtPrm;
        }



       

        public async Task<string> EncriptRespons(string ResponsData)
        {
            return await _queryParser.PayloadEncryption(ResponsData, Transaction_ID, this.Bank_Code);
        }

        private async Task<dynamic> getRequestData(dynamic inputData, string APIname)
        {

            dynamic rejusetJson;
            try
            {
                var EncryptedData = inputData.req_root.body.payload;
                string BankCode = inputData.req_root.header.cde.ToString();
                this.Bank_Code = BankCode;
                string xmlData = await this._queryParser.PayloadDecryption(EncryptedData.ToString(), BankCode);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlData);
                string xpath = "PIDBlock/payload";
                var nodes = xmlDoc.SelectSingleNode(xpath);
                foreach (XmlNode childrenNode in nodes)
                {
                    JObject rejusetJson1 = (JObject)JsonConvert.DeserializeObject(childrenNode.Value);

                    dynamic payload = rejusetJson1[APIname];

                    this.appkey = payload.msgHdr.authInfo.token.ToString();
                    this.Transaction_ID = payload.msgHdr.conversationID.ToString();
                    this.Channel_ID = payload.msgHdr.channelID.ToString();

                    rejusetJson = payload.msgBdy;

                    return rejusetJson;

                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("getRequestData", ex.Message);
            }

            return "";

        }

    }
}
