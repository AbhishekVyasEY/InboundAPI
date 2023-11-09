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

    public class FtdgAccLeadExecution : IFtdgAccLeadExecution
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

        private string Leadid, LeadAccountid;

        private List<string> applicents = new List<string>();
        private LeadAccount _accountLead;
        private LeadDetails _leadParam;
        private List<AccountApplicant> _accountApplicants;

              
     
        Dictionary<string, string> genderc = new Dictionary<string, string>();

        private ICommonFunction _commonFunc;

        public FtdgAccLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

            _leadParam = new LeadDetails();
            _accountLead = new LeadAccount();
            _accountApplicants = new List<AccountApplicant>();

           
          

            genderc.Add("789030000", "Male");
            genderc.Add("789030001", "Female");
            genderc.Add("789030002", "Third Gender");

        }


        public async Task<FtAccountLeadReturn> ValidateLeadtInput(dynamic RequestData)
        {
            FtAccountLeadReturn ldRtPrm = new FtAccountLeadReturn();
            RequestData = await this.getRequestData(RequestData, "FetchDigiAccountLead");
            try
            { 

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "FetchDigiAccountLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        
                        if (!string.IsNullOrEmpty(RequestData.AccountLeadId.ToString()))
                        {
                            ldRtPrm = await this.FetLeadAccount(RequestData.AccountLeadId.ToString());
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateLeadtInput", "Account LeadId is incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "Account LeadId is incorrect";
                        }
                        
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateLeadtInput", "Transaction_ID or  Channel_ID is incorrect.");
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


        private async Task<FtAccountLeadReturn> FetLeadAccount(string LeadAccountId)
        {
            FtAccountLeadReturn accountLeadReturn = new FtAccountLeadReturn();

            var _leadDetails  = await this._commonFunc.getLeadAccountDetails(LeadAccountId);
            if (_leadDetails.Count>0)
            {
                if(await getLeadAccount(_leadDetails))
                {
                    if (await getLeadData())
                    {
                        if (await getApplicent())
                        {
                            accountLeadReturn.AccountLead = _accountLead;
                            accountLeadReturn.leadDetails = _leadParam;
                            accountLeadReturn.Applicants = _accountApplicants;
                            accountLeadReturn.ReturnCode = "CRM-SUCCESS";
                            accountLeadReturn.Message = OutputMSG.Case_Success;

                        }
                    }
                }
            }
            else
            {
                this._logger.LogInformation("FetLeadAccount", "Lead Account Details not found.");
                accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                accountLeadReturn.Message = "Lead Account Details not found.";
            }


            

            return accountLeadReturn;
        }


        private async Task<bool> getLeadData()
        {

            var _leadDetails = await this._commonFunc.getLeadDetails(this.Leadid);

            _leadParam.ucic = _leadDetails[0]["eqs_ucic"].ToString();
            _leadParam.leadid = _leadDetails[0]["eqs_crmleadid"].ToString();
            _leadParam.title = await this._commonFunc.getTitleCode(_leadDetails[0]["_eqs_titleid_value"].ToString());
            _leadParam.Firstname = _leadDetails[0]["firstname"].ToString();
            _leadParam.Lastname = _leadDetails[0]["lastname"].ToString();
            _leadParam.CustomerPhoneNumber = _leadDetails[0]["mobilephone"].ToString();
            _leadParam.CustomerEmailID = _leadDetails[0]["emailaddress1"].ToString();
            _leadParam.companynamepart1 = _leadDetails[0]["eqs_companynamepart1"].ToString();
            _leadParam.companynamepart2 = _leadDetails[0]["eqs_companynamepart2"].ToString();
            _leadParam.companynamepart3 = _leadDetails[0]["eqs_companynamepart3"].ToString();

            _leadParam.dateofincorporation = _leadDetails[0]["eqs_dateofincorporation"].ToString();
            _leadParam.DOB = _leadDetails[0]["eqs_dob"].ToString();
            _leadParam.PAN = _leadDetails[0]["eqs_pan"].ToString();
            _leadParam.gender = _leadDetails[0]["eqs_gendercode"].ToString();

            _leadParam.branchid = await this._commonFunc.getBranchCode(_leadDetails[0]["_eqs_branchid_value"].ToString());
            _leadParam.productid = await this._commonFunc.getProductCode(_leadDetails[0]["_eqs_productid_value"].ToString());
            _leadParam.productCategory = await this._commonFunc.getProductCategoryCode(_leadDetails[0]["_eqs_productcategoryid_value"].ToString());
            _leadParam.EntityType = await this._commonFunc.getEntityCode(_leadDetails[0]["_eqs_entitytypeid_value"].ToString());
            _leadParam.SubEntityType = await this._commonFunc.getSubEntityCode(_leadDetails[0]["_eqs_subentitytypeid_value"].ToString());
            

            return true;
        }

        private async Task<bool> getLeadAccount(JArray LeadData)
        {                

            this.LeadAccountid = LeadData[0]["eqs_leadaccountid"].ToString();
            this.Leadid = LeadData[0]["_eqs_lead_value"].ToString();

            _accountLead.productCode = await this._commonFunc.getProductCode(LeadData[0]["_eqs_productid_value"].ToString());

            if (!string.IsNullOrEmpty(LeadData[0]["_eqs_branchid_value"].ToString())) {
                _accountLead.sourceBranch = await this._commonFunc.getBranchCode(LeadData[0]["_eqs_branchid_value"].ToString());
            }

            _accountLead.productCategory = await this._commonFunc.getProductCategoryCode(LeadData[0]["_eqs_typeofaccountid_value"].ToString()); 

            _accountLead.LeadAccountID = LeadData[0]["eqs_crmleadaccountid"].ToString();
            _accountLead.accountType =  await this._queryParser.getOptionSetValuToText("eqs_leadaccount", "eqs_accountownershipcode", LeadData[0]["eqs_accountownershipcode"].ToString());
            _accountLead.accountOpeningFlow =  await this._queryParser.getOptionSetValuToText("eqs_leadaccount", "eqs_instakitoptioncode", LeadData[0]["eqs_instakitoptioncode"].ToString());
            _accountLead.initialDepositType = await this._queryParser.getOptionSetValuToText("eqs_leadaccount", "eqs_initialdepositmodecode", LeadData[0]["eqs_initialdepositmodecode"].ToString());
            _accountLead.fdAccOpeningDate = LeadData[0]["eqs_fdvaluedate"].ToString();
            _accountLead.tenureinmonths = LeadData[0]["eqs_tenureinmonths"].ToString();
            _accountLead.tenureindays = LeadData[0]["eqs_tenureindays"].ToString();
            _accountLead.fieldEmployeeCode = LeadData[0]["eqs_sourcebyemployeecode"].ToString();
            _accountLead.applicationDate = LeadData[0]["eqs_applicationdate"].ToString();
            _accountLead.fundsTobeDebitedFrom = LeadData[0]["eqs_fundstobedebitedfrom"].ToString();
            _accountLead.mopRemarks = LeadData[0]["eqs_modeofoperationremarks"].ToString();
            _accountLead.initialDeposit = (LeadData[0]["eqs_initialdepositamountcode"].ToString() == "789030000") ? "Up To Rs. 500000" : "Above Rs. 500000";
            _accountLead.rateOfInterest = LeadData[0]["eqs_rateofinterest"].ToString();
            _accountLead.depositAmount = LeadData[0]["eqs_depositamount"].ToString();
            _accountLead.sweepFacility = Convert.ToBoolean(LeadData[0]["eqs_sweepfacility"].ToString());


            return true;      

        }

        private async Task<bool> getApplicent()
        {
            var _leadAcDetails = await this._commonFunc.getApplicentsSetails(this.LeadAccountid);
            foreach (var applicant in _leadAcDetails)
            {
                AccountApplicant _accountApplicant = new AccountApplicant();

                _accountApplicant.applicantid = applicant["eqs_applicantid"].ToString();
                _accountApplicant.UCIC = applicant["eqs_customer"].ToString();
                _accountApplicant.title = await this._commonFunc.getTitleCode(applicant["_eqs_titleid_value"].ToString());
                _accountApplicant.firstname = applicant["eqs_firstname"].ToString();
                _accountApplicant.lastname = applicant["eqs_lastname"].ToString();
                _accountApplicant.customerPhoneNumber = applicant["eqs_mobilenumber"].ToString();
                _accountApplicant.customerEmailID = applicant["eqs_emailaddress"].ToString();
                _accountApplicant.dob = applicant["eqs_dob"].ToString();
                _accountApplicant.pan = applicant["eqs_pan"].ToString();
                _accountApplicant.age = applicant["eqs_leadage"].ToString();
                _accountApplicant.gender = await this._queryParser.getOptionSetValuToText("eqs_accountapplicant", "eqs_gendercode", applicant["eqs_gendercode"].ToString());

                _accountApplicant.eqs_companynamepart1 = applicant["eqs_companynamepart1"].ToString();
                _accountApplicant.eqs_companynamepart2 = applicant["eqs_companynamepart2"].ToString();
                _accountApplicant.eqs_companynamepart3 = applicant["eqs_companynamepart3"].ToString();
                _accountApplicant.eqs_dateofincorporation = applicant["eqs_dateofincorporation"].ToString();

                _accountApplicant.entityType = await this._commonFunc.getEntityCode(applicant["_eqs_entitytypeid_value"].ToString()); 
                _accountApplicant.subentityType = await this._commonFunc.getSubEntityCode(applicant["_eqs_subentity_value"].ToString());
                if (!string.IsNullOrEmpty(applicant["_eqs_accountrelationship_value"].ToString()))
                {
                    _accountApplicant.customerAccountRelation = await this._commonFunc.getAccRelationshipCode(applicant["_eqs_accountrelationship_value"].ToString());
                }
               
                _accountApplicant.isPrimaryHolder = (applicant["eqs_isprimaryholder"].ToString() == "789030001") ? true : false;
                _accountApplicant.isStaff = applicant["eqs_isstaffcode"].ToString();

                if (!string.IsNullOrEmpty(applicant["_eqs_relationship_value"].ToString()))
                {
                    _accountApplicant.relationToPrimaryHolder = await this._commonFunc.getRelationshipCode(applicant["_eqs_relationship_value"].ToString());
                }

                /*
                var applicentPreferences = await this._commonFunc.getPreferences(applicant["eqs_accountapplicantid"].ToString());

                if (applicentPreferences.Count > 0)
                {
                    Preferences preferences = new Preferences();

                    preferences.sms = Convert.ToBoolean(applicentPreferences[0]["eqs_sms"].ToString());
                    preferences.allSMSAlerts = Convert.ToBoolean(applicentPreferences[0]["eqs_allsmsalerts"].ToString());
                    preferences.onlyTransactionAlerts = Convert.ToBoolean(applicentPreferences[0]["eqs_onlytransactionalerts"].ToString());
                    preferences.passbook = Convert.ToBoolean(applicentPreferences[0]["eqs_passbook"].ToString());
                    preferences.physicalStatement = Convert.ToBoolean(applicentPreferences[0]["eqs_physicalstatement"].ToString());
                    preferences.emailStatement = Convert.ToBoolean(applicentPreferences[0]["eqs_emailstatement"].ToString());
                    preferences.netBanking = Convert.ToBoolean(applicentPreferences[0]["eqs_netbanking"].ToString());
                    preferences.bankGuarantee = Convert.ToBoolean(applicentPreferences[0]["eqs_bankguarantee"].ToString());
                    preferences.letterofCredit = Convert.ToBoolean(applicentPreferences[0]["eqs_letterofcredit"].ToString());
                    preferences.businessLoan = Convert.ToBoolean(applicentPreferences[0]["eqs_businessloan"].ToString());
                    preferences.doorStepBanking = Convert.ToBoolean(applicentPreferences[0]["eqs_doorstepbanking"].ToString());
                    preferences.doorStepBankingOnCall = Convert.ToBoolean(applicentPreferences[0]["eqs_doorstepbankingoncall"].ToString());
                    preferences.doorStepBankingBeat = Convert.ToBoolean(applicentPreferences[0]["eqs_doorstepbankingbeat"].ToString());
                    preferences.tradeForex = Convert.ToBoolean(applicentPreferences[0]["eqs_tradeforex"].ToString());
                    preferences.loanAgainstProperty = Convert.ToBoolean(applicentPreferences[0]["eqs_loanagainstproperty"].ToString());
                    preferences.overdraftsagainstFD = Convert.ToBoolean(applicentPreferences[0]["eqs_overdraftagainstfd"].ToString());
                    preferences.preferencesCopied = Convert.ToBoolean(applicentPreferences[0]["eqs_preferencescopied"].ToString());
                    preferences.bankLevelAlerts = Convert.ToBoolean(applicentPreferences[0]["eqs_banklevelalerts"].ToString());
                 

                    _accountApplicant.preferences = preferences;
                }
                */

                _accountApplicants.Add(_accountApplicant);


            }

       
            return true;

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
