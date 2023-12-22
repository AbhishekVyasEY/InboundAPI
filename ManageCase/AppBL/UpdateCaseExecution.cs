namespace ManageCase
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
    using System.Threading.Channels;
    using System.Xml;
    using System.Threading.Tasks;
    using System;
    using Microsoft.Identity.Client;
    using System.Dynamic;
    using System.Runtime.ConstrainedExecution;

    public class UpdateCaseExecution : IUpdateCaseExecution
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

        public string appkey { get; set; }

        public string API_Name
        {
            set
            {
                _logger.API_Name = value;
            }
        }
        public string Input_payload
        {
            set
            {
                _logger.Input_payload = value;
            }
        }

        private readonly IKeyVaultService _keyVaultService;
        private ICommonFunction _commonFunc;

        public UpdateCaseExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
            this._logger = logger;
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
        }

        public async Task<UpdateCaseReturnParam> ValidateUpdateCase(dynamic RequestData)
        {
            RequestData = await this.getRequestData(RequestData, "UpdateCase");
            UpdateCaseReturnParam caseRtPrm = new UpdateCaseReturnParam();
            try
            {

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "UpdateCaseappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        caseRtPrm = await this.UpdateCase(RequestData);
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateLeadtInput", "Transaction_ID or Channel_ID in incorrect.");
                        caseRtPrm.ReturnCode = "CRM-ERROR-102";
                        caseRtPrm.Message = "Transaction_ID or Channel_ID in incorrect.";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateLeadtInput", "Appkey is incorrect");
                    caseRtPrm.ReturnCode = "CRM-ERROR-102";
                    caseRtPrm.Message = "Appkey is incorrect";
                }
                return caseRtPrm;
            } 
            catch (Exception ex)
            {
                this._logger.LogError("ValidateUpdateCase", ex.Message);
                throw ex;
            }



        }

        private async Task<UpdateCaseReturnParam> UpdateCase(dynamic RequestData)
        {
            UpdateCaseReturnParam caseRtPrm = new UpdateCaseReturnParam();

            return caseRtPrm;
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

        public async Task<string> EncriptRespons(string ResponsData, string Bankcode)
        {
            return await _queryParser.PayloadEncryption(ResponsData, Transaction_ID, Bankcode);
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
