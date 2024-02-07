namespace ETC
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
    using System.Security.Cryptography.Xml;
    using Azure;
    using Microsoft.VisualBasic;
    using System.Text;

    public class ETCExecution : IETCExecution
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

        private string Leadid, LeadAccountid, DDEId;

        private List<string> applicents = new List<string>();
    

        private ICommonFunction _commonFunc;

        public ETCExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {

            this._logger = logger;

            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

        }


        public async Task<ETCReturn> ValidateETCCustomertInput(dynamic RequestData)
        {
            ETCReturn ldRtPrm = new ETCReturn();
            RequestData = await this.getRequestData(RequestData, "CreateETCCustomer");

            if (RequestData.ErrorNo != null && RequestData.ErrorNo.ToString() == "Error99")
            {
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = "API do not have access permission!";
                return ldRtPrm;
            }

            try
            {

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "CreateETCCustomerappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        List<string> error = new List<string>();
                        if (string.IsNullOrEmpty(RequestData.etcid?.ToString()))
                        {
                            error.Add("Etcid");                            
                        }
                        if (string.IsNullOrEmpty(RequestData.firstname?.ToString()))
                        {
                            error.Add("Firstname");                            
                        }
                        if (string.IsNullOrEmpty(RequestData.mobilenumber?.ToString()))
                        {
                            error.Add("Mobilenumber");                            
                        }

                        if (error.Count>0)
                        {
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = string.Join(",", error) + " fields should not be null!";
                            return ldRtPrm;
                        }
                        else
                        {
                            ldRtPrm = await this.CreateEtcCustmers(RequestData);
                        }                        

                    }
                    else
                    {
                        this._logger.LogInformation("ValidateETCCustomertInput", "Transaction_ID or  Channel_ID is incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or  Channel_ID is incorrect.";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateETCCustomertInput", "Appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Appkey is incorrect";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateLeadtInput", ex.Message);
                throw ex;
            }

        }

        public async Task<ETCReturn> ValidateUpETCCustomertInput(dynamic RequestData)
        {
            ETCReturn ldRtPrm = new ETCReturn();
            RequestData = await this.getRequestData(RequestData, "UpdateETCCustomer");

            if (RequestData.ErrorNo != null && RequestData.ErrorNo.ToString() == "Error99")
            {
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = "API do not have access permission!";
                return ldRtPrm;
            }

            try
            {

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "UpdateETCCustomerappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        List<string> error = new List<string>();
                        if (string.IsNullOrEmpty(RequestData.etcid?.ToString()))
                        {
                            error.Add("Etcid");
                        }
                       
                        if (error.Count > 0)
                        {
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = string.Join(",", error) + " fields should not be null!";
                            return ldRtPrm;
                        }
                        else
                        {
                            ldRtPrm = await this.UpdateEtcCustmers(RequestData);
                        }

                    }
                    else
                    {
                        this._logger.LogInformation("ValidateETCCustomertInput", "Transaction_ID or  Channel_ID is incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or  Channel_ID is incorrect.";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateETCCustomertInput", "Appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Appkey is incorrect";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateLeadtInput", ex.Message);
                throw ex;
            }

        }


        private async Task<ETCReturn> CreateEtcCustmers(dynamic RequestData)
        {
            ETCReturn etcReturn = new ETCReturn();
            Dictionary<string, string> odatab = new Dictionary<string, string>();
            try
            {
                odatab.Add("firstname", RequestData.firstname.ToString());
                odatab.Add("mobilephone", RequestData.mobilenumber.ToString());
                odatab.Add("eqs_etcid", RequestData.etcid.ToString());
                odatab.Add("ccs_mf_customertype", await this._queryParser.getOptionSetTextToValue("contact", "ccs_mf_customertype", "Non UCIC"));
                odatab.Add("eqs_nonuciccustomertype", await this._queryParser.getOptionSetTextToValue("contact", "eqs_nonuciccustomertype", "FASTAG"));


                if (!string.IsNullOrEmpty(RequestData.middlename.ToString()))
                {
                    odatab.Add("middlename", RequestData.middlename?.ToString());
                }
                if (!string.IsNullOrEmpty(RequestData.middlename.ToString()))
                {
                    odatab.Add("lastname", RequestData.lastname?.ToString());
                }
                if (!string.IsNullOrEmpty(RequestData.emailid.ToString()))
                {
                    odatab.Add("emailaddress1", RequestData.emailid?.ToString());
                }
                if (!string.IsNullOrEmpty(RequestData.DOB.ToString()))
                {
                    odatab.Add("birthdate", RequestData.DOB?.ToString());
                }
                if (!string.IsNullOrEmpty(RequestData.ParentUCIC.ToString()))
                {
                    string ParentId = await this._commonFunc.getetcParentId(RequestData.ParentUCIC.ToString());
                    if (ParentId != null)
                    {
                        odatab.Add("parentcustomerid_contact@odata.bind", $"contacts({ParentId})");
                    }                    
                }

                string postDataParametr = JsonConvert.SerializeObject(odatab);

                var Customer_details = await this._queryParser.HttpApiCall("contacts()?$select=eqs_etcid", HttpMethod.Post, postDataParametr);               
                var ETCid = CommonFunction.GetIdFromPostRespons201(Customer_details[0]["responsebody"], "eqs_etcid");
                etcReturn.ETCID = ETCid;
                etcReturn.ReturnCode = "CRM - SUCCESS";
                etcReturn.Message = OutputMSG.Case_Success;
            }
            catch(Exception ex)
            {
                etcReturn.ReturnCode = "CRM-ERROR-102";
                etcReturn.Message = ex.Message;
            }

            return etcReturn;
        }

        private async Task<ETCReturn> UpdateEtcCustmers(dynamic RequestData)
        {
            ETCReturn etcReturn = new ETCReturn();
            Dictionary<string, string> odatab = new Dictionary<string, string>();
            try
            {
                string CustomerId = await this._commonFunc.getetcCustomerId(RequestData.etcid.ToString());
                if (string.IsNullOrEmpty(CustomerId))
                {
                    etcReturn.ReturnCode = "CRM-ERROR-101";
                    etcReturn.Message = "ETC ID not exist in CRM!";
                    return etcReturn;
                }
                if (!string.IsNullOrEmpty(RequestData.firstname.ToString()))
                {
                    odatab.Add("firstname", RequestData.firstname.ToString());
                }

                if (!string.IsNullOrEmpty(RequestData.mobilenumber.ToString()))
                {
                    odatab.Add("mobilephone", RequestData.mobilenumber.ToString());
                }       

                if (!string.IsNullOrEmpty(RequestData.middlename.ToString()))
                {
                    odatab.Add("middlename", RequestData.middlename?.ToString());
                }
                if (!string.IsNullOrEmpty(RequestData.middlename.ToString()))
                {
                    odatab.Add("lastname", RequestData.lastname?.ToString());
                }
                if (!string.IsNullOrEmpty(RequestData.emailid.ToString()))
                {
                    odatab.Add("emailaddress1", RequestData.emailid?.ToString());
                }
                if (!string.IsNullOrEmpty(RequestData.DOB.ToString()))
                {
                    odatab.Add("birthdate", RequestData.DOB?.ToString());
                }
                if (!string.IsNullOrEmpty(RequestData.ParentUCIC.ToString()))
                {
                    string ParentId = await this._commonFunc.getetcParentId(RequestData.ParentUCIC.ToString());
                    if (ParentId != null)
                    {
                        odatab.Add("parentcustomerid_contact@odata.bind", $"contacts({ParentId})");
                    }
                }

                string postDataParametr = JsonConvert.SerializeObject(odatab);

                var Customer_details = await this._queryParser.HttpApiCall($"contacts({CustomerId})?$select=eqs_etcid", HttpMethod.Patch, postDataParametr);               
                var ETCid = CommonFunction.GetIdFromPostRespons201(Customer_details[0]["responsebody"], "eqs_etcid");
                etcReturn.ETCID = ETCid;
                etcReturn.ReturnCode = "CRM - SUCCESS";
                etcReturn.Message = OutputMSG.Case_Success;
            }
            catch (Exception ex)
            {
                etcReturn.ReturnCode = "CRM-ERROR-102";
                etcReturn.Message = ex.Message;
            }

            return etcReturn;
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
