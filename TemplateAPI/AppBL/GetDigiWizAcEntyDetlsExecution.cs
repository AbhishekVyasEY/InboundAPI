namespace DigiWiz
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

    public class GetDigiWizAcEntyDetlsExecution : IGetDigiWizAcEntyDetlsExecution
    {

        private ILoggers _logger;
        private IQueryParser _queryParser;

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

        public GetDigiWizAcEntyDetlsExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
           
           
        }


        public async Task<WizAcEntyReturn> ValidateWizAcEntyDetls(dynamic RequestData, string appkey)
        {
            WizAcEntyReturn ldRtPrm = new WizAcEntyReturn();
            RequestData = await this.getRequestData(RequestData);
            try
            {
                string AccountNumber = RequestData.AccountNumber;
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "GetDigiWizAcEntyDetlsappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        if (!string.IsNullOrEmpty(AccountNumber) && AccountNumber != "")
                        {

                            ldRtPrm = await this.getWizAcEntyDetls(AccountNumber);

                        }
                        else
                        {
                            this._logger.LogInformation("ValidateFtchDgLdSts", "Input parameters are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                        }
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateFtchDgLdSts", "Input parameters are incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateFtchDgLdSts", ex.Message);
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

        

        public async Task<WizAcEntyReturn> getWizAcEntyDetls(string AccountNumber)
        {
            WizAcEntyReturn csRtPrm = new WizAcEntyReturn();
            try
            {
                var Account_data = await this._commonFunc.getAccountData(AccountNumber);
                if (Account_data.Count > 0)
                {
                    dynamic AccountData = Account_data[0];
                    csRtPrm.accountNumber = AccountNumber;

                    if (AccountData.createdon.ToString().Length > 1)
                    {
                        csRtPrm.accountCreatedOn = AccountData.createdon;
                        csRtPrm.productVariant = AccountData.eqs_productcode;

                        string product_Cat_Id = AccountData._eqs_productcategoryid_value;
                        csRtPrm.productCategory = await this._commonFunc.getProductCatName(product_Cat_Id);

                        string product_customer_Id = AccountData._eqs_customeridvalue_value;

                        csRtPrm.customerInfo = new CustomerInfo();
                        csRtPrm.customerInfo.accountTitle = AccountData.eqs_name;
                        var Contact_data = await this._commonFunc.getContactData(product_customer_Id);

                        foreach (var cu_Data in Contact_data)
                        {
                            csRtPrm.customerInfo.UCICCreatedOn = (cu_Data["createdon"].ToString()==null)? "" : cu_Data["createdon"].ToString();
                            csRtPrm.customerInfo.entityFlag = (cu_Data["eqs_entityflag"].ToString() == null)? "" : cu_Data["eqs_entityflag"].ToString(); 
                            csRtPrm.customerInfo.entityType = (AccountData["eqs_subentitytypeid"] == null)? "" : AccountData["eqs_subentitytypeid"].ToString();                           
                            csRtPrm.customerInfo.phoneNumber = (cu_Data["mobilephone"].ToString() == null)? "" : cu_Data["mobilephone"].ToString();
                            csRtPrm.customerInfo.ucic = (cu_Data["eqs_customerid"].ToString() == null) ? "" : cu_Data["eqs_customerid"].ToString();
                        }                        

                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }

                    
                    

                }
                else
                {
                    this._logger.LogInformation("getDigiLeadStatus", "Input parameters are incorrect");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = OutputMSG.Incorrect_Input;
                }
            }
            catch(Exception ex)
            {
                this._logger.LogError("getDigiLeadStatus", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = OutputMSG.Incorrect_Input;
            }
            
            

            return csRtPrm;
        }


        public async Task<string> EncriptRespons(string ResponsData)
        {
            return await _queryParser.PayloadEncryption(ResponsData, Transaction_ID);
        }

        public async Task CRMLog(string InputRequest, string OutputRespons, string CallStatus)
        {
            Dictionary<string, string> CRMProp = new Dictionary<string, string>();
            CRMProp.Add("eqs_name", this.Transaction_ID);
            CRMProp.Add("eqs_requestbody", InputRequest);
            CRMProp.Add("eqs_responsebody", OutputRespons);
            CRMProp.Add("eqs_requeststatus", (CallStatus.Contains("ERROR")) ? "615290001" : "615290000");
            string postDataParametr = JsonConvert.SerializeObject(CRMProp);
            await this._queryParser.HttpApiCall("eqs_apilogs", HttpMethod.Post, postDataParametr);
        }

        private async Task<dynamic> getRequestData(dynamic inputData)
        {
            var EncryptedData = inputData.req_root.body.payload;
            string xmlData = await this._queryParser.PayloadDecryption(EncryptedData.ToString());
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlData);
            string xpath = "PIDBlock/payload";
            var nodes = xmlDoc.SelectSingleNode(xpath);
            foreach (XmlNode childrenNode in nodes)
            {
                dynamic rejusetJson = JsonConvert.DeserializeObject(childrenNode.Value);
                return rejusetJson;
            }

            return "";
        }

    }
}
