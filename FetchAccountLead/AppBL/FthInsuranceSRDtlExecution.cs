namespace FetchAccountLead
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

    public class FthInsuranceSRDtlExecution : IFthInsuranceSRDtlExecution
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

        private string customerid, accountid;

        private List<string> applicents = new List<string>();


        Dictionary<string, string> genderc = new Dictionary<string, string>();

        private ICommonFunction _commonFunc;

        public FthInsuranceSRDtlExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
            this._logger = logger;
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
        }


        public async Task<FthInsuranceSRDtlReturn> ValidateSRDInput(dynamic RequestData)
        {
            FthInsuranceSRDtlReturn ldRtPrm = new FthInsuranceSRDtlReturn();
            RequestData = await this.getRequestData(RequestData, "FetchInsuranceSRDetails");

            if (RequestData.ErrorNo != null && RequestData.ErrorNo.ToString() == "Error99")
            {
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = "API do not have access permission!";
                return ldRtPrm;
            }

            if (RequestData.ErrorNo != null && RequestData.ErrorNo.ToString() == "Error99")
            {
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = "API do not have access permission!";
                return ldRtPrm;
            }

            try
            {

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "FetchInsuranceSRDetailsappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {

                        if (!string.IsNullOrEmpty(RequestData.CustomerID.ToString()))
                        {                            
                            ldRtPrm = await this.FetInsuSRDtl(RequestData.CustomerID.ToString());
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateLeadtInput", "Customer ID is incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "Customer ID is incorrect";
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateLeadtInput", "Transaction_ID or Channel_ID is incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or  Channel_ID is incorrect.";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateLeadtInput", "Appkey is incorrect");
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


        private async Task<FthInsuranceSRDtlReturn> FetInsuSRDtl(string CustomerID)
        {
            FthInsuranceSRDtlReturn customerDetailReturn = new FthInsuranceSRDtlReturn();
            customerDetailReturn.lifeInsuranceVerificationSRListRp = new List<LifeInsuranceVerificationSRL>();
            try
            {
                var CuatomerDetails = await this._commonFunc.getCustomerCaseDetails(CustomerID);
                if (CuatomerDetails.Count > 0)
                {
                    foreach (var item in CuatomerDetails)
                    {
                        LifeInsuranceVerificationSRL lifeInsuranceVerSRL = new LifeInsuranceVerificationSRL();
                        lifeInsuranceVerSRL.dependentname = item["eqs_dependentname"].ToString();
                        if (item["eqs_incident_eqs_leaddocument"].Count() > 0)
                        {
                            lifeInsuranceVerSRL.dmsDocumentID = item["eqs_incident_eqs_leaddocument"][0]["eqs_documentid"].ToString();
                        }
                        if (!string.IsNullOrEmpty(item["_eqs_planname_value"].ToString()))
                        {
                            lifeInsuranceVerSRL.insuranceproduct = item["eqs_PlanName"]["eqs_name"].ToString();
                        }
                        
                        lifeInsuranceVerSRL.policyCoverage = item["eqs_policycoverage"].ToString();
                        lifeInsuranceVerSRL.riskprofile = item["eqs_tppriskprofile"].ToString();
                        lifeInsuranceVerSRL.spcode = item["eqs_spcode"].ToString();
                        lifeInsuranceVerSRL.srNumber = item["ticketnumber"].ToString();

                        customerDetailReturn.lifeInsuranceVerificationSRListRp.Add(lifeInsuranceVerSRL);
                    }


                    customerDetailReturn.ReturnCode = "CRM-SUCCESS";
                    customerDetailReturn.Message = OutputMSG.Case_Success;
                }
                else
                {
                    customerDetailReturn.ReturnCode = "CRM-ERROR-101";
                    customerDetailReturn.Message = OutputMSG.Resource_n_Found;
                }
            }
            catch (Exception ex)
            {
                customerDetailReturn.ReturnCode = "CRM-ERROR-101";
                customerDetailReturn.Message = OutputMSG.Resource_n_Found;
            }           

            return customerDetailReturn;
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
