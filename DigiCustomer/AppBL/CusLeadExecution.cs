namespace CustomerLead
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

    public class CusLeadExecution : ICusLeadExecution
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

        private string Leadid, LeadAccountid, DDEId;

        private List<string> applicents = new List<string>();
       

   

        private ICommonFunction _commonFunc;

        public CusLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

           

        }


        public async Task<CustomerLeadReturn> ValidateInput(dynamic RequestData)
        {
            CustomerLeadReturn ldRtPrm = new CustomerLeadReturn();
            RequestData = await this.getRequestData(RequestData, "GetDigiCustomerLeadNSDL");
            try
            { 

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "GetDigiCustomerLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        
                        if (!string.IsNullOrEmpty(RequestData.customerLeadID.ToString()))
                        {
                            ldRtPrm = await this.getCustomerLead(RequestData.customerLeadID.ToString());
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateInput", "customerLeadID is incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "customerLeadID is incorrect";
                        }
                        
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateInput", "Transaction_ID or Channel_ID in incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or Channel_ID in incorrect.";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateInput", "Appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Appkey is incorrect";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateInput", ex.Message);
                throw ex;
            }
            
        }


        private async Task<CustomerLeadReturn> getCustomerLead(string customerLeadID)
        {
            CustomerLeadReturn accountLeadReturn = new CustomerLeadReturn();

            var Customerdtl  = await this._commonFunc.getApplicentDetail(customerLeadID);

            if (Customerdtl.Count>0)
            {
                if (Customerdtl[0]["eqs_customertypecode"].ToString() == "789030001")
                {
                    string typeofcustomer = await this._commonFunc.getCustomerType(Customerdtl[0]["_eqs_entitytypeid_value"].ToString());
                    if (typeofcustomer == "I")
                    {
                        AccountApplicantIndv accountApplicantIndv = new AccountApplicantIndv();

                        accountApplicantIndv.title = await this._commonFunc.getTitle(Customerdtl[0]["_eqs_titleid_value"].ToString());
                        accountApplicantIndv.firstname = Customerdtl[0]["eqs_firstname"].ToString();
                        accountApplicantIndv.middlename = Customerdtl[0]["eqs_middlename"].ToString();
                        accountApplicantIndv.lastname = Customerdtl[0]["eqs_lastname"].ToString();
                        accountApplicantIndv.pan = Customerdtl[0]["eqs_internalpan"].ToString();

                        accountLeadReturn.AccountApplicants = accountApplicantIndv;
                    }
                    else
                    {
                        AccountApplicantCorp accountApplicantCorp = new AccountApplicantCorp();
                        accountApplicantCorp.title = await this._commonFunc.getTitle(Customerdtl[0]["_eqs_titleid_value"].ToString());
                        accountApplicantCorp.Companynamepart1 = Customerdtl[0]["eqs_companynamepart1"].ToString();
                        accountApplicantCorp.Companynamepart2 = Customerdtl[0]["eqs_companynamepart2"].ToString();
                        accountApplicantCorp.Companynamepart3 = Customerdtl[0]["eqs_companynamepart3"].ToString();
                        accountApplicantCorp.pan = Customerdtl[0]["eqs_tannumber"].ToString();

                        accountLeadReturn.AccountApplicants = accountApplicantCorp;
                    }
                    accountLeadReturn.ReturnCode = "CRM-SUCCESS";
                    accountLeadReturn.Message = OutputMSG.Case_Success;
                }
                else
                {
                    string CustomerId = Customerdtl[0]["_eqs_customerid_value"].ToString();
                    if (!string.IsNullOrEmpty(CustomerId))
                    {

                        var customerDetail = await this._commonFunc.getCustomerDetails(CustomerId);
                        string typeofcustomer = await this._commonFunc.getCustomerType(customerDetail[0]["_eqs_entitytypeid_value"].ToString());
                        if (typeofcustomer == "I")
                        {
                            AccountApplicantIndv accountApplicantIndv = new AccountApplicantIndv();

                            accountApplicantIndv.title = await this._commonFunc.getTitle(customerDetail[0]["_eqs_titleid_value"].ToString());
                            accountApplicantIndv.firstname = customerDetail[0]["firstname"].ToString();
                            accountApplicantIndv.middlename = customerDetail[0]["middlename"].ToString();
                            accountApplicantIndv.lastname = customerDetail[0]["lastname"].ToString();
                            accountApplicantIndv.pan = customerDetail[0]["eqs_pan"].ToString();

                            accountLeadReturn.AccountApplicants = accountApplicantIndv;
                        }
                        else
                        {
                            AccountApplicantCorp accountApplicantCorp = new AccountApplicantCorp();
                            accountApplicantCorp.Companynamepart1 = customerDetail[0]["eqs_companyname"].ToString();
                            accountApplicantCorp.Companynamepart2 = customerDetail[0]["eqs_companyname2"].ToString();
                            accountApplicantCorp.Companynamepart3 = customerDetail[0]["eqs_companyname3"].ToString();
                            accountApplicantCorp.pan = customerDetail[0]["eqs_tannumber"].ToString();

                            accountLeadReturn.AccountApplicants = accountApplicantCorp;
                        }
                        accountLeadReturn.ReturnCode = "CRM-SUCCESS";
                        accountLeadReturn.Message = OutputMSG.Case_Success;


                    }
                }

                //Set PAN Validation Mode = "System"
                Dictionary<string, string> odatab = new Dictionary<string, string>();
                odatab.Add("eqs_panvalidationmode", "958570001");
                string postDataParametr = JsonConvert.SerializeObject(odatab);
                var resp = await this._queryParser.HttpApiCall($"eqs_accountapplicants({Customerdtl[0]["eqs_accountapplicantid"].ToString()})", HttpMethod.Patch, postDataParametr);
            }            
            else
            {
                this._logger.LogInformation("getCustomerLead", "Applicent Detail not found");
                accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                accountLeadReturn.Message = "Applicent Detail not found";
            }

            return accountLeadReturn;
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
