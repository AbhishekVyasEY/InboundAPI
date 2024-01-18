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


        public async Task<WizAcEntyReturn> ValidateWizAcEntyDetls(dynamic RequestData)
        {
            WizAcEntyReturn ldRtPrm = new WizAcEntyReturn();
            RequestData = await this.getRequestData(RequestData, "GetDigiWizAccountEntityDetails");

            if (RequestData.ErrorNo != null && RequestData.ErrorNo.ToString() == "Error99")
            {
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = "API do not have access permission!";
                return ldRtPrm;
            }

            try
            {
                string AccountNumber = RequestData.AccountNumber;
                if (!string.IsNullOrEmpty(this.appkey) && this.appkey != "" && checkappkey(this.appkey, "GetDigiWizAcEntyDetlsappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        if (!string.IsNullOrEmpty(AccountNumber) && AccountNumber != "")
                        {

                            ldRtPrm = await this.getWizAcEntyDetls(AccountNumber);

                        }
                        else
                        {
                            this._logger.LogInformation("ValidateWizAcEntyDetls", "Transaction ID or Channel ID is incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "Transaction ID or Channel ID is incorrect";
                        }
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateWizAcEntyDetls", "Appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Appkey is incorrect";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateWizAcEntyDetls", ex.Message);
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
                        csRtPrm.accountTitle = AccountData.eqs_name;
                        string product_Cat_Id = AccountData._eqs_productcategoryid_value;
                        csRtPrm.productCategory = await this._commonFunc.getProductCatName(product_Cat_Id);

                        var all_customers = await this._commonFunc.getAllCustomers(AccountData.eqs_accountid.ToString()); 

                        csRtPrm.customerInfo = new List<CustomerInfo>();
                        
                       
                        

                        foreach (var cu_Data in all_customers)
                        {
                            CustomerInfo customerInfo = new CustomerInfo();
                            var Contact_data = await this._commonFunc.getContactData(cu_Data._eqs_customeridvalue_value.ToString());
                            
                            customerInfo.UCICCreatedOn = (Contact_data[0]["createdon"].ToString()==null)? "" : Contact_data[0]["createdon"].ToString();                                                     
                            customerInfo.phoneNumber = (Contact_data[0]["mobilephone"].ToString() == null)? "" : Contact_data[0]["mobilephone"].ToString();
                            customerInfo.ucic = (Contact_data[0]["eqs_customerid"].ToString() == null) ? "" : Contact_data[0]["eqs_customerid"].ToString();

                            customerInfo.entityFlag = (Contact_data[0]["_eqs_entitytypeid_value"].ToString() == null) ? "" : await this._commonFunc.getEntityType(Contact_data[0]["_eqs_entitytypeid_value"].ToString());
                            customerInfo.subentityFlag = (Contact_data[0]["_eqs_subentitytypeid_value"].ToString() == null) ? "" : await this._commonFunc.getSubEntityType(Contact_data[0]["_eqs_subentitytypeid_value"].ToString());

                            csRtPrm.customerInfo.Add(customerInfo);
                        }                        
                        
                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }

                    
                    

                }
                else
                {
                    this._logger.LogInformation("getWizAcEntyDetls", "Account data not found.");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = "Account data not found.";
                }
            }
            catch(Exception ex)
            {
                this._logger.LogError("getWizAcEntyDetls", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = OutputMSG.Incorrect_Input;
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
                string xmlData = await this._queryParser.PayloadDecryption(EncryptedData.ToString(), BankCode, APIname);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlData);
                string xpath = "PIDBlock/payload";
                var nodes = xmlDoc.SelectSingleNode(xpath);
                foreach (XmlNode childrenNode in nodes)
                {
                    rejusetJson = JsonConvert.DeserializeObject(childrenNode.Value);

                    var payload = rejusetJson.GetDigiWizAccountEntityDetails;
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
