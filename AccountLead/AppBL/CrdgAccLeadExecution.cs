namespace AccountLead
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

    public class CrdgAccLeadExecution : ICrdgAccLeadExecution
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

        private List<string> applicents = new List<string>();
        private AccountLead _accountLead;
        private LeadParam _leadParam;
        private List<AccountApplicant> _accountApplicants;
    
      
        private ICommonFunction _commonFunc;

        public CrdgAccLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

            _leadParam = new LeadParam();
            _accountLead = new AccountLead();
            _accountApplicants = new List<AccountApplicant>();

           
        }


        public async Task<AccountLeadReturn> ValidateLeadtInput(dynamic RequestData)
        {
            AccountLeadReturn ldRtPrm = new AccountLeadReturn();
            RequestData = await this.getRequestData(RequestData, "CreateDigiAccountLead");
            try
            { 

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "CreateDigiAccountLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        if (this.ValidateAccountLead(RequestData.accountLead) && this.ValidateAccountApplicent(RequestData.CustomerAccountLeadRelation))
                        {
                            if (await this.ValidateUCIC())
                            {
                                ldRtPrm = await this.CreateAccountLead();
                            }
                            else
                            {
                                this._logger.LogInformation("ValidateLeadtInput", "Input UCIC are incorrect");
                                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                                ldRtPrm.Message = "Input UCIC are incorrect";
                            }
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateLeadtInput", "AccountLead or AccountApplicent is incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "AccountLead or AccountApplicent is incorrect";
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateLeadtInput", "Transaction_ID or Channel_ID is incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or Channel_ID is incorrect.";
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


        private async Task<AccountLeadReturn> CreateAccountLead()
        {
            AccountLeadReturn accountLeadReturn = new AccountLeadReturn();
            if(await CreateLead())
            {
                if (await LeadAccountCreation())
                {
                    if (await AddAccountApplicent())
                    {
                        accountLeadReturn.AccountLeadId = _leadParam.LeadAccount_id;
                        accountLeadReturn.Applicants = this.applicents;
                        accountLeadReturn.ReturnCode = "CRM - SUCCESS";
                        accountLeadReturn.Message = OutputMSG.Case_Success;
                    }
                    else
                    {
                        this._logger.LogInformation("CreateAccountLead", "Account Applicent creation fail.");
                        accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                        accountLeadReturn.Message = "Account Applicent creation fail.";
                    }
                }
                else
                {
                    this._logger.LogInformation("CreateAccountLead", "Lead Account creation fail.");
                    accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                    accountLeadReturn.Message = "Lead Account creation fail.";
                }
            }
            else
            {
                this._logger.LogInformation("CreateAccountLead", "Lead creation fail.");
                accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                accountLeadReturn.Message = "Lead creation fail.";
            }

            return accountLeadReturn;
        }


        private async Task<bool> CreateLead()
        {
            Dictionary<string, string> odatab = new Dictionary<string, string>();
            var productDetails = await this._commonFunc.getProductId(_accountLead.productCode);
            string ProductId = productDetails["ProductId"];
            _leadParam.productid = ProductId;
            string Businesscategoryid = productDetails["businesscategoryid"];
            string Productcategoryid = productDetails["productcategory"];
            _leadParam.productCategory = Productcategoryid;
            string eqs_crmproductcategorycode = productDetails["crmproductcategorycode"];

            if (ProductId != "")
            {
                var appitem = _accountApplicants.Where(x => x.UCIC == _leadParam.eqs_ucic).FirstOrDefault();

                string BranchId = await this._commonFunc.getBranchId(_accountLead.sourceBranch);
                if (BranchId != null && BranchId != "")
                {
                    odatab.Add("eqs_branchid@odata.bind", $"eqs_branchs({BranchId})");
                    _leadParam.branchid = BranchId;
                }

                odatab.Add("eqs_ucic", appitem.UCIC);
                odatab.Add("eqs_etbcustomerid@odata.bind", $"contacts({appitem.contactid})");
                odatab.Add("eqs_titleid@odata.bind", $"eqs_titles({appitem.title})");
                
                odatab.Add("leadsourcecode", "15");
                odatab.Add("firstname", appitem.firstname);
                odatab.Add("lastname", appitem.lastname);
                odatab.Add("mobilephone", appitem.customerPhoneNumber);
                odatab.Add("emailaddress1", appitem.customerEmailID);

                odatab.Add("eqs_companynamepart1", appitem.eqs_companynamepart1);
                odatab.Add("eqs_companynamepart2", appitem.eqs_companynamepart2);
                odatab.Add("eqs_companynamepart3", appitem.eqs_companynamepart3);

                if(!string.IsNullOrEmpty(appitem.eqs_dateofincorporation))
                    odatab.Add("eqs_dateofincorporation", appitem.eqs_dateofincorporation);

                if (!string.IsNullOrEmpty(appitem.dob))
                    odatab.Add("eqs_dob", appitem.dob);

                odatab.Add("eqs_gendercode", appitem.gender);
                odatab.Add("eqs_createdfromonline", "true");
                if (!string.IsNullOrEmpty(_accountLead.leadsource.ToString()))
                {
                    odatab.Add("eqs_leadsourceid@odata.bind", $"eqs_leadsources({await this._commonFunc.getLeadSourceId(_accountLead.leadsource)})");
                }
                if (!string.IsNullOrEmpty(ProductId))
                {
                    odatab.Add("eqs_productid@odata.bind", $"eqs_products({ProductId})");
                }
                if (!string.IsNullOrEmpty(Productcategoryid))
                {
                    odatab.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({Productcategoryid})");
                }
                if (!string.IsNullOrEmpty(Businesscategoryid))
                {
                    odatab.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({Businesscategoryid})");
                }
                if (!string.IsNullOrEmpty(appitem.entityType))
                {
                    odatab.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({appitem.entityType})");
                }
                if (!string.IsNullOrEmpty(appitem.subentityType))
                {
                    odatab.Add("eqs_subentitytypeid@odata.bind", $"eqs_subentitytypes({appitem.subentityType})");
                }
                    
               
                

                string postDataParametr = JsonConvert.SerializeObject(odatab);

                var Lead_details = await this._queryParser.HttpApiCall("leads?$select=eqs_crmleadid", HttpMethod.Post, postDataParametr);
                if (Lead_details.Count > 0)
                {
                    dynamic respons_code = Lead_details[0];
                    if (respons_code.responsecode == 201)
                    {
                        _leadParam.Lead_id = respons_code.responsebody["eqs_crmleadid"];
                        _leadParam.leadid = respons_code.responsebody["leadid"];
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                
            }
            else
            {
                return false;
            }

            return true;
        }

        private async Task<bool> LeadAccountCreation()
        {
            Dictionary<string, string> odatab = new Dictionary<string, string>();
            Dictionary<string, double> odatab1 = new Dictionary<string, double>();

            odatab.Add("eqs_typeofaccountid@odata.bind", $"eqs_productcategories({_leadParam.productCategory})");
            odatab.Add("eqs_productid@odata.bind", $"eqs_products({_leadParam.productid})");
            odatab.Add("eqs_Lead@odata.bind", $"leads({_leadParam.leadid})");

            odatab.Add("eqs_accountownershipcode", await this._queryParser.getOptionSetTextToValue("eqs_leadaccount", "eqs_accountownershipcode", _accountLead.accountType.ToString()));

            if (!string.IsNullOrEmpty(_leadParam.branchid))
                odatab.Add("eqs_branchid@odata.bind", $"eqs_branchs({_leadParam.branchid})");


            odatab.Add("eqs_instakitoptioncode", await this._queryParser.getOptionSetTextToValue("eqs_leadaccount", "eqs_instakitoptioncode", _accountLead.accountOpeningFlow.ToString()));
            odatab.Add("eqs_initialdepositmodecode",  await this._queryParser.getOptionSetTextToValue("eqs_leadaccount", "eqs_initialdepositmodecode", _accountLead.initialDepositType.ToString()));

            odatab.Add("eqs_sourcebyemployeecode", _accountLead.fieldEmployeeCode);

            if(!string.IsNullOrEmpty(_accountLead.applicationDate))
                odatab.Add("eqs_applicationdate", _accountLead.applicationDate);            

            odatab.Add("eqs_fundstobedebitedfrom", _accountLead.fundsTobeDebitedFrom);           
            odatab.Add("eqs_modeofoperationremarks", _accountLead.mopRemarks);

            odatab.Add("eqs_initialdepositamountcode", await this._queryParser.getOptionSetTextToValue("eqs_leadaccount", "eqs_initialdepositamountcode", _accountLead.initialDeposit.ToString()));

            if (!string.IsNullOrEmpty(_accountLead.fdAccOpeningDate))
                odatab.Add("eqs_fdvaluedate", _accountLead.fdAccOpeningDate);

            odatab.Add("eqs_sweepfacility", _accountLead.sweepFacility.ToString().ToLower());

            odatab.Add("eqs_tenureinmonths", _accountLead.tenureInMonths);
            odatab.Add("eqs_tenureindays", _accountLead.tenureInDays);

            string postDataParametr = JsonConvert.SerializeObject(odatab);

            odatab1.Add("eqs_rateofinterest", Convert.ToDouble(_accountLead.rateOfInterest.ToString()));
            odatab1.Add("eqs_depositamount", Convert.ToDouble(_accountLead.depositAmount.ToString()));

            string postDataParametr1 = JsonConvert.SerializeObject(odatab1);
            postDataParametr = await _commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

            var LeadAccount_details = await this._queryParser.HttpApiCall("eqs_leadaccounts()?$select=eqs_crmleadaccountid", HttpMethod.Post, postDataParametr);
            if (LeadAccount_details.Count > 0)
            {
                dynamic respons_code = LeadAccount_details[0];
                if (respons_code.responsecode == 201)
                {
                    _leadParam.LeadAccount_id = respons_code.responsebody["eqs_crmleadaccountid"];
                    _leadParam.LeadAccountid = respons_code.responsebody["eqs_leadaccountid"];
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;

        }

        private async Task<bool> AddAccountApplicent()
        {           
            foreach (var applicant in _accountApplicants)
            {
                Dictionary<string, string> odatab = new Dictionary<string, string>();

                odatab.Add("eqs_leadaccountid@odata.bind", $"eqs_leadaccounts({_leadParam.LeadAccountid})");
                odatab.Add("eqs_ucic", applicant.UCIC);
                odatab.Add("eqs_customer", applicant.UCIC);
                odatab.Add("eqs_customerid@odata.bind", $"contacts({applicant.contactid})");
                odatab.Add("eqs_titleid@odata.bind", $"eqs_titles({applicant.title})");
                odatab.Add("eqs_leadchannel", "15");

                odatab.Add("eqs_firstname", applicant.firstname);
                odatab.Add("eqs_lastname", applicant.lastname);
                odatab.Add("eqs_name", applicant.firstname + " " + applicant.lastname);
                odatab.Add("eqs_emailaddress", applicant.customerEmailID);
                odatab.Add("eqs_mobilenumber", applicant.customerPhoneNumber);
                odatab.Add("eqs_gendercode", applicant.gender);
                odatab.Add("eqs_dob", applicant.dob);
                odatab.Add("eqs_pan", applicant.pan);
              

                odatab.Add("eqs_companynamepart1", applicant.eqs_companynamepart1);
                odatab.Add("eqs_companynamepart2", applicant.eqs_companynamepart2);
                odatab.Add("eqs_companynamepart3", applicant.eqs_companynamepart3);

                if (!string.IsNullOrEmpty(applicant.eqs_dateofincorporation))
                    odatab.Add("eqs_dateofincorporation", applicant.eqs_dateofincorporation);

                if (!string.IsNullOrEmpty(_leadParam.branchid))
                {
                    odatab.Add("eqs_branchid@odata.bind", $"eqs_branchs({_leadParam.branchid})");
                }
                    

                odatab.Add("eqs_leadid@odata.bind", $"leads({_leadParam.leadid})");

                odatab.Add("eqs_accountrelationship@odata.bind", $"eqs_accountrelationships({await this._commonFunc.getAccRelationshipId(applicant.customerAccountRelation)})");
                odatab.Add("eqs_isprimaryholder", (applicant.isPrimaryHolder == true) ? "789030001" : "789030000");

                odatab.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({applicant.entityType})");
                odatab.Add("eqs_subentity@odata.bind", $"eqs_subentitytypes({applicant.subentityType})");

               if (applicant.isPrimaryHolder == false && !string.IsNullOrEmpty(applicant.relationToPrimaryHolder))
                {

                    odatab.Add("eqs_relationship@odata.bind", $"eqs_relationships({await this._commonFunc.getRelationshipId(applicant.relationToPrimaryHolder)})");
                }

                string postDataParametr = JsonConvert.SerializeObject(odatab);
                var Applicent_details = await this._queryParser.HttpApiCall("eqs_accountapplicants()?$select=eqs_applicantid", HttpMethod.Post, postDataParametr);
                dynamic respons_code = Applicent_details[0];
                applicents.Add(respons_code.responsebody["eqs_applicantid"].ToString());
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

               
        private async Task<bool> ValidateUCIC()
        {
            int nuid = _accountApplicants.Count;
            if (nuid>0)
            {
                string query_url = $"contacts()?$select=contactid,eqs_customerid,_eqs_titleid_value,firstname,lastname,eqs_companyname,eqs_companyname2,eqs_companyname3,eqs_pan,mobilephone,eqs_gender,emailaddress1,birthdate,eqs_dateofincorporation,_eqs_entitytypeid_value,_eqs_subentitytypeid_value&$filter=";
                foreach (var applicent in _accountApplicants)
                {
                    query_url += $"eqs_customerid eq '{applicent.UCIC}' or ";
                    if (applicent.isPrimaryHolder==true)
                    {
                        _leadParam.eqs_ucic = applicent.UCIC;
                    }
                }
                query_url = query_url.Substring(0, query_url.Length-4);
                var Applicantdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Applicant_dtails = await this._commonFunc.getDataFromResponce(Applicantdtails);
                if (Applicant_dtails.Count == nuid)
                {
                    List<AccountApplicant> accountApplicants1 = new List<AccountApplicant>();
                    foreach (var item in Applicant_dtails)
                    {
                       var appitem = _accountApplicants.Where(x => x.UCIC == item["eqs_customerid"].ToString()).FirstOrDefault();
                       
                        appitem.pan = item["eqs_pan"].ToString();
                        appitem.customerPhoneNumber = item["mobilephone"].ToString();
                        appitem.customerEmailID = item["emailaddress1"].ToString();
                        appitem.dob = item["birthdate"].ToString();
                        appitem.gender = item["eqs_gender"].ToString();
                        appitem.entityType = item["_eqs_entitytypeid_value"].ToString();
                        appitem.subentityType = item["_eqs_subentitytypeid_value"].ToString();

                        appitem.contactid = item["contactid"].ToString();
                        appitem.title = item["_eqs_titleid_value"].ToString();
                        appitem.firstname = item["firstname"].ToString();
                        appitem.lastname = item["lastname"].ToString();
                        appitem.eqs_companynamepart1 = item["eqs_companyname"].ToString();
                        appitem.eqs_companynamepart2 = item["eqs_companyname2"].ToString();
                        appitem.eqs_companynamepart3 = item["eqs_companyname3"].ToString();
                        appitem.eqs_dateofincorporation = item["eqs_dateofincorporation"].ToString();


                        accountApplicants1.Add(appitem);
                    }
                    _accountApplicants = accountApplicants1;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private bool ValidateAccountLead(dynamic AccountData)
        {
            if (string.IsNullOrEmpty(AccountData.accountType.ToString()))
            {
                return false;                
            }
            else
            {
                _accountLead.accountType = AccountData.accountType.ToString();
            }

            if (string.IsNullOrEmpty(AccountData.leadsource.ToString()))
            {
                return false;
            }
            else
            {
                _accountLead.leadsource = AccountData.leadsource.ToString();
            }

            if (string.IsNullOrEmpty(AccountData.productCode.ToString()))
            {
                return false;
            }
            else
            {
                _accountLead.productCode = AccountData.productCode.ToString();
            }

            if (string.IsNullOrEmpty(AccountData.sourceBranch.ToString()))
            {
                return false;
            }
            else
            {
                _accountLead.sourceBranch = AccountData.sourceBranch.ToString();
            }
            
            if (string.IsNullOrEmpty(AccountData.accountOpeningFlow.ToString()))
            {
                return false;
            }
            else
            {
                _accountLead.accountOpeningFlow = AccountData.accountOpeningFlow.ToString();
            }

            if (string.IsNullOrEmpty(AccountData.depositAmount.ToString()))
            {
                return false;
            }
            else
            {
                _accountLead.depositAmount = AccountData.depositAmount.ToString();
            }

            if (string.IsNullOrEmpty(AccountData.initialDepositType.ToString()))
            {
                return false;
            }
            else
            {
                _accountLead.initialDepositType = AccountData.initialDepositType;
            }

            if (string.IsNullOrEmpty(AccountData.InitialDeposit.ToString()))
            {
                return false;
            }
            else
            {
                _accountLead.initialDeposit = AccountData.InitialDeposit;
            }

            _accountLead.productCategory = AccountData.productCategory;

          
            _accountLead.fieldEmployeeCode = AccountData.fieldEmployeeCode;
            _accountLead.applicationDate = AccountData.applicationDate;

            if (_accountLead.productCategory == "PCAT04" || _accountLead.productCategory == "PCAT05")
            {

                if (string.IsNullOrEmpty(AccountData.tenureInMonths.ToString()))
                {
                    return false;
                }
                else
                {
                    _accountLead.tenureInMonths = AccountData.tenureInMonths;
                }

                if (string.IsNullOrEmpty(AccountData.tenureInDays.ToString()))
                {
                    return false;
                }
                else
                {
                    _accountLead.tenureInDays = AccountData.tenureInDays;
                }
               
            }
            else
            {
                _accountLead.tenureInMonths = AccountData.tenureInMonths;
                _accountLead.tenureInDays = AccountData.tenureInDays;
            }
            
            _accountLead.rateOfInterest = AccountData.rateOfInterest;
            _accountLead.fundsTobeDebitedFrom = AccountData.fundsTobeDebitedFrom;
            _accountLead.mopRemarks = AccountData.mopRemarks;
            _accountLead.fdAccOpeningDate = AccountData.fdAccOpeningDate;
            _accountLead.sweepFacility = AccountData.sweepFacility;
            

            return true;
        }

        private bool ValidateAccountApplicent(dynamic ApplicentData)
        {
            try
            {
                if (ApplicentData.Count > 0)
                {
                    foreach (var item in ApplicentData)
                    {
                        AccountApplicant accountApplicant = new AccountApplicant();
                        if (string.IsNullOrEmpty(item.UCIC.ToString()))
                        {
                            return false;
                        }
                        else
                        {
                            accountApplicant.UCIC = item.UCIC.ToString();
                        }

                        if (string.IsNullOrEmpty(item.customerAccountRelation.ToString()))
                        {
                            return false;
                        }
                        else
                        {
                            accountApplicant.customerAccountRelation = item.customerAccountRelation;
                        }

                        if (string.IsNullOrEmpty(item.isPrimaryHolder.ToString()))
                        {
                            return false;
                        }
                        else
                        {
                            accountApplicant.isPrimaryHolder = item.isPrimaryHolder;
                        }

                        if (!Convert.ToBoolean(item.isPrimaryHolder.ToString()))
                        {
                            if (string.IsNullOrEmpty(item.relationToPrimaryHolder.ToString()))
                            {
                                return false;
                            }
                            else
                            {
                                accountApplicant.relationToPrimaryHolder = item.relationToPrimaryHolder;
                            }

                        }


                        _accountApplicants.Add(accountApplicant);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch(Exception ex) {
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
