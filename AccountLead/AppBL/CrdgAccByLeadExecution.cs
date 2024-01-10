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
    using Microsoft.Identity.Client;
    using Microsoft.Azure.KeyVault.Models;

    public class CrdgAccByLeadExecution : ICrdgAccByLeadExecution
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

        private List<string> applicents = new List<string>();
        private AccountLead _accountLead;
        private LeadParam _leadParam;
        private List<AccountApplicant> _accountApplicants;

        private ICommonFunction _commonFunc;

        public CrdgAccByLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {

            this._logger = logger;

            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

            _leadParam = new LeadParam();
            _accountLead = new AccountLead();
            _accountApplicants = new List<AccountApplicant>();


        }


        public async Task<AccountByLeadReturn> ValidateLeadtInput(dynamic RequestData)
        {
            AccountByLeadReturn ldRtPrm = new AccountByLeadReturn();
            RequestData = await this.getRequestData(RequestData, "CreateDigiAccountByLead");
            try
            {

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "CreateDigiAccountByLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        if (!string.IsNullOrEmpty(RequestData.AccountLeadId.ToString()))
                        {
                            ldRtPrm = await this.CreateAccountByLead(RequestData.AccountLeadId.ToString());
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateLeadtInput", "AccountLeadId can not be null.");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "AccountLeadId can not be null.";
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateLeadtInput", "Transaction_ID or Channel_ID in incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or Channel_ID in incorrect.";
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


        private async Task<AccountByLeadReturn> CreateAccountByLead(string accountLead)
        {
            AccountByLeadReturn accountLeadReturn = new AccountByLeadReturn();
            try
            {

                string Token = await this._queryParser.getAccessToken();
                string RequestTemplate = "{\"createAccountRequest\":{\"msgHdr\":{\"channelID\":\"FOS\",\"transactionType\":\"string\",\"transactionSubType\":\"string\",\"conversationID\":\"944dafbfe5904613bb9ef84d6ae59f42\",\"externalReferenceId\":\"944dafbfe5904613bb9ef84d6ae59f42\",\"isAsync\":false,\"authInfo\":{\"branchID\":\"1001\",\"userID\":\"WIZARDAUTH3\",\"token\":\"1001\"}},\"msgBdy\":{\"accountNo\":\"\",\"accountNominee\":{\"city\":\"Erode\",\"country\":\"IN\",\"dateOfBirth\":\"20161104\",\"isNomineeDisplay\":false,\"isNomineeBankCustomer\":false,\"nominee\":{\"phone\":{\"country\":\"9834\",\"area\":\"91\",\"number\":\"916345343425\",\"extn\":\"2342\"},\"address\":{\"line1\":\"qsdv\",\"line2\":\"sdsvsdv\",\"line3\":\"dvxdx\"},\"emailId\":\"sjfjg@hgsd.com\",\"name\":\"PAAVAI\"},\"guardian\":{\"phone\":{\"area\":\"293\",\"number\":\"916352673569\"},\"address\":{\"line1\":\"qsdv\",\"line2\":\"sdsvsdv\",\"line3\":\"dvxdx\",\"line4\":\"string\",\"city\":\"Erode\",\"state\":\"TAMILNADU\",\"country\":\"IN\",\"zip\":\"638103\"},\"emailId\":\"jnas@s1.com\",\"name\":\"Podiya\"},\"refGuardPhnCountry\":\"424\",\"refGuardPhnExtn\":\"334534\",\"relAcctHolder\":3,\"relationGuardian\":2,\"shareAmount\":100,\"sharePercentage\":100,\"state\":\"TAMILNADU\",\"zip\":\"638103\"},\"branchCode\":9999,\"customerAndRelation\":[{\"customerId\":\"137770003\",\"customerName\":\"IlangoSelvam\",\"relation\":\"SOW\"}],\"customerID\":\"137770003\",\"isJointHolder\":false,\"isRestrictAcct\":false,\"transactionType\":\"A\",\"minorAcctStatus\":false,\"productCode\":2005,\"tdaccountPayinRequest\":{\"depositAmount\":0,\"fromAccountNo\":\"\",\"branchCodeGL\":\"\",\"referenceNoGL\":\"\",\"termDays\":0,\"termMonths\":0},\"rdaccountPayinRequest\":{\"installmentAmount\":0,\"payoutAccountNo\":\"\",\"providerAccountNo\":\"\",\"branchCodeGL\":\"\",\"referenceNoGL\":\"\",\"termMonths\":0}}}}";
                dynamic Request_Template = JsonConvert.DeserializeObject(RequestTemplate);
                dynamic msgHdr = Request_Template.createAccountRequest.msgHdr;
                dynamic msgBdy = Request_Template.createAccountRequest.msgBdy;
                Guid ReferenceId = Guid.NewGuid();
                msgHdr.conversationID = ReferenceId.ToString().Replace("-", "");
                msgHdr.externalReferenceId = ReferenceId.ToString().Replace("-", "");
                Request_Template.createAccountRequest.msgHdr = msgHdr;

                Dictionary<string, string> odatab = new Dictionary<string, string>();

                var AccountDDE = await this._commonFunc.getAccountLeadData(accountLead);
                if (AccountDDE.Count > 0)
                {
                    if (!string.IsNullOrEmpty(AccountDDE[0]["eqs_readyforonboarding"].ToString()) && Convert.ToBoolean(AccountDDE[0]["eqs_readyforonboarding"].ToString()))
                    {
                        string Lead_details = "";
                        dynamic responsD = "";


                        string Instrakit_Text = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_instakitcode", AccountDDE[0]["eqs_instakitcode"].ToString());
                        if (Instrakit_Text == "Insta Kit")
                        {
                            var ApplicentDDE = await this._commonFunc.getApplicentFinalDDEbyAccountLead((AccountDDE[0]["eqs_ddeaccountid"].ToString()));
                            if (ApplicentDDE.Count > 0)
                            {
                                RequestTemplate = "{\"activateWizInstantAccountReq\":{\"msgHdr\":{\"channelID\":\"FOS\",\"transactionType\":\"string\",\"transactionSubType\":\"string\",\"conversationID\":\"string\",\"externalReferenceId\":\"\",\"isAsync\":false,\"authInfo\":{\"branchID\":\"9999\",\"userID\":\"WIZARDAUTH3\"}},\"msgBdy\":{\"accountId\":\"100048413491\",\"accountTitle\":\"NandhaKannan\",\"acctOperatingInstr\":\"Single\",\"custDtls\":{\"businessType\":\"\",\"countryOfResidence\":\"IN\",\"custEducation\":\"\",\"custIC\":\"13971357\",\"custType\":\"I\",\"customerMobilePhone\":\"918667266490\",\"dateOfBirthReg\":\"1988-12-17\",\"designation\":\"\",\"empID\":\"\",\"firstName\":\"KARTHIKA\",\"gender\":\"F\",\"icType\":\"\",\"incomeTaxNo\":\"PEZEZ9616J\",\"lastName\":\"C\",\"mailAddEmail\":\"a_karthikac@equitasbank.com\",\"mailPhoneRes\":\"0918667266490\",\"mailPhoneoff\":\"\",\"mailingAddress\":{\"city\":\"PERUNGUDI.\",\"country\":\"IN\",\"line1\":\"Westmambalam\",\"line2\":\"Westmambalam\",\"line3\":\"Ramnagar\",\"state\":\"TAMIL NADU\",\"zip\":\"600096\"},\"maritalStatus\":\"2\",\"middleName\":\"\",\"motherMaidenName\":\"Dfgh\",\"namPrefix\":\"MRS.\",\"nationality\":\"IN\",\"professionCode\":\"15\",\"repPermAdd\":\"N\",\"shortName\":\"KARTHIKAC\",\"signType\":\"1\",\"staff\":\"N\"},\"custId\":\"13971357\",\"dateAcctOpen\":\"2023-09-21\",\"flgBA525\":\"Y\",\"flgCM01\":\"N\",\"limitProfile\":\"\",\"nomineeDtls\":{\"guardianAddress\":{\"city\":\"WESTMAMBALAM\",\"country\":\"IN\",\"line1\":\"34/65\",\"line2\":\"Ramcolony\",\"line3\":\"Westmambalam\",\"state\":\"TAMIL NADU\",\"zip\":\"600033\"},\"guardianEmailId\":\"\",\"guardianMobile\":\"\",\"guardianName\":\"Chandru\",\"guardianPhoneArea\":\"91\",\"guardianPhoneCntry\":\"91\",\"guardianPhoneExt\":\"91\",\"guardianRel\":\"1\",\"isBankCustomer\":\"N\",\"nomAddress\":{\"city\":\"WESTMAMBALAM\",\"country\":\"IN\",\"line1\":\"32/45\",\"line2\":\"Ramcolony\",\"line3\":\"Westmambalam\",\"state\":\"TAMILNADU\",\"zip\":\"600033\"},\"nomCustId\":\"\",\"nomDOB\":\"1998-07-16\",\"nomEmailId\":\"\",\"nomMobile\":\"\",\"nomName\":\"Mithraan\",\"nomPhoneArea\":\"91\",\"nomPhoneCntry\":\"91\",\"nomPhoneExt\":\"91\",\"nomRegNo\":\"\",\"nomRel\":\"4\"}}}}";
                                Request_Template = JsonConvert.DeserializeObject(RequestTemplate);
                                msgHdr = Request_Template.activateWizInstantAccountReq.msgHdr;
                                msgBdy = Request_Template.activateWizInstantAccountReq.msgBdy;

                                msgHdr.conversationID = ReferenceId.ToString().Replace("-", "");
                                msgHdr.externalReferenceId = ReferenceId.ToString().Replace("-", "");
                                Request_Template.activateWizInstantAccountReq.msgHdr = msgHdr;

                                msgBdy.accountId = AccountDDE[0]["eqs_instakitaccountnumber"].ToString();
                                msgBdy.custId = ApplicentDDE[0]["eqs_instakitcustomerid"].ToString();
                                msgBdy.custDtls.custIC = ApplicentDDE[0]["eqs_instakitcustomerid"].ToString();


                                msgBdy.accountTitle = ApplicentDDE[0]["eqs_firstname"].ToString();
                                string dd = AccountDDE[0]["eqs_applicationdate"].ToString().Substring(0, 2);
                                string mm = AccountDDE[0]["eqs_applicationdate"].ToString().Substring(3, 2);
                                string yy = AccountDDE[0]["eqs_applicationdate"].ToString().Substring(6, 4);
                                msgBdy.dateAcctOpen = yy + mm + dd;


                                msgBdy.custDtls.customerMobilePhone = ApplicentDDE[0]["eqs_mobilenumber"].ToString();
                                dd = ApplicentDDE[0]["eqs_dob"].ToString().Substring(0, 2);
                                mm = ApplicentDDE[0]["eqs_dob"].ToString().Substring(3, 2);
                                yy = ApplicentDDE[0]["eqs_dob"].ToString().Substring(6, 4);
                                msgBdy.custDtls.dateOfBirthReg = yy + mm + dd;
                                msgBdy.custDtls.firstName = ApplicentDDE[0]["eqs_firstname"].ToString();
                                msgBdy.custDtls.incomeTaxNo = ApplicentDDE[0]["eqs_pannumber"].ToString();
                                msgBdy.custDtls.lastName = ApplicentDDE[0]["eqs_lastname"].ToString();
                                msgBdy.custDtls.mailAddEmail = ApplicentDDE[0]["eqs_emailid"].ToString();
                                msgBdy.custDtls.mailPhoneRes = ApplicentDDE[0]["eqs_mobilenumber"].ToString();
                                // msgBdy.custDtls.namPrefix = ApplicentDDE[0]["custid"].ToString();
                                msgBdy.custDtls.shortName = ApplicentDDE[0]["eqs_shortname"].ToString();

                                var address = await this._commonFunc.getAddressData(ApplicentDDE[0]["eqs_ddeindividualcustomerid"].ToString());
                                if (address.Count > 0)
                                {
                                    msgBdy.custDtls.mailingAddress.line1 = address[0]["eqs_addressline1"].ToString();
                                    msgBdy.custDtls.mailingAddress.line2 = address[0]["eqs_addressline2"].ToString();
                                    msgBdy.custDtls.mailingAddress.line3 = address[0]["eqs_addressline3"].ToString();
                                    msgBdy.custDtls.mailingAddress.city = await this._commonFunc.getCityName(address[0]["_eqs_cityid_value"].ToString());
                                    msgBdy.custDtls.mailingAddress.state = await this._commonFunc.getStateName(address[0]["_eqs_stateid_value"].ToString());
                                    msgBdy.custDtls.mailingAddress.zip = address[0]["eqs_pincode"].ToString();
                                }

                                var Nominee = await this._commonFunc.getAccountNominee(AccountDDE[0]["eqs_ddeaccountid"].ToString());
                                if (Nominee.Count > 0)
                                {
                                    dd = Nominee[0]["eqs_nomineedob"].ToString().Substring(0, 2);
                                    mm = Nominee[0]["eqs_nomineedob"].ToString().Substring(3, 2);
                                    yy = Nominee[0]["eqs_nomineedob"].ToString().Substring(6, 4);

                                    msgBdy.nomineeDtls.nomDOB = yy + mm + dd;
                                    msgBdy.nomineeDtls.nomName = Nominee[0]["eqs_nomineename"].ToString();


                                    msgBdy.nomineeDtls.nomAddress.line1 = Nominee[0]["eqs_addressline1"].ToString();
                                    msgBdy.nomineeDtls.nomAddress.line2 = Nominee[0]["eqs_addressline2"].ToString();
                                    msgBdy.nomineeDtls.nomAddress.line3 = Nominee[0]["eqs_addressline3"].ToString();
                                    msgBdy.nomineeDtls.nomAddress.city = await this._commonFunc.getCityName(Nominee[0]["_eqs_city_value"].ToString());
                                    msgBdy.nomineeDtls.nomAddress.state = await this._commonFunc.getStateName(Nominee[0]["_eqs_state_value"].ToString());
                                    msgBdy.nomineeDtls.nomAddress.zip = Nominee[0]["eqs_pincode"].ToString();

                                    msgBdy.nomineeDtls.guardianName = Nominee[0]["eqs_guardianname"].ToString();


                                    msgBdy.nomineeDtls.guardianAddress.line1 = Nominee[0]["eqs_guardianaddressline1"].ToString();
                                    msgBdy.nomineeDtls.guardianAddress.line2 = Nominee[0]["eqs_guardianaddressline2"].ToString();
                                    msgBdy.nomineeDtls.guardianAddress.line3 = Nominee[0]["eqs_guardianaddressline3"].ToString();
                                    msgBdy.nomineeDtls.guardianAddress.city = await this._commonFunc.getCityName(Nominee[0]["_eqs_guardiancity_value"].ToString());
                                    msgBdy.nomineeDtls.guardianAddress.state = await this._commonFunc.getStateName(Nominee[0]["_eqs_guardianstate_value"].ToString());
                                    msgBdy.nomineeDtls.guardianAddress.zip = Nominee[0]["eqs_guardianpincode"].ToString();
                                }
                                else
                                {
                                    msgBdy.Remove("nomineeDtls");
                                }


                                Request_Template.activateWizInstantAccountReq.msgBdy = msgBdy;
                                string wso_request = JsonConvert.SerializeObject(Request_Template);
                                string postDataParametr = await EncriptRespons(wso_request, "FI0060");
                                Lead_details = await this._queryParser.HttpCBSApiCall(Token, HttpMethod.Post, "CBSInstaAcct", postDataParametr);
                                responsD = JsonConvert.DeserializeObject(Lead_details);
                            }
                            else
                            {
                                accountLeadReturn.Message = "ApplicentDDE Final not found!";
                                accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                            }

                        }

                        else
                        {
                            var Nominee = await this._commonFunc.getAccountNominee(AccountDDE[0]["eqs_ddeaccountid"].ToString());
                            var AccApplicent = await this._commonFunc.getAccountApplicd(AccountDDE[0]["_eqs_leadaccountid_value"].ToString());


                            if (Nominee.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(Nominee[0]["_eqs_city_value"].ToString()))
                                {
                                    //  msgBdy.accountNominee.city = await this._commonFunc.getCityName(Nominee[0]["_eqs_city_value"].ToString());
                                }

                                if (!string.IsNullOrEmpty(Nominee[0]["_eqs_state_value"].ToString()))
                                {
                                    msgBdy.accountNominee.state = "TAMIL NADU";  //await this._commonFunc.getStateName(Nominee[0]["_eqs_state_value"].ToString());
                                }

                                if (!string.IsNullOrEmpty(Nominee[0]["_eqs_country_value"].ToString()))
                                {
                                    msgBdy.accountNominee.country = "IN";    //await this._commonFunc.getCountryName(Nominee[0]["_eqs_country_value"].ToString());
                                }

                                string dd = Nominee[0]["eqs_nomineedob"].ToString().Substring(0, 2);
                                string mm = Nominee[0]["eqs_nomineedob"].ToString().Substring(3, 2);
                                string yy = Nominee[0]["eqs_nomineedob"].ToString().Substring(6, 4);
                                msgBdy.accountNominee.dateOfBirth = yy + mm + dd;
                                msgBdy.accountNominee.nominee.phone.number = Nominee[0]["eqs_mobile"].ToString();

                                msgBdy.accountNominee.nominee.address.line1 = Nominee[0]["eqs_addressline1"].ToString();
                                msgBdy.accountNominee.nominee.address.line2 = Nominee[0]["eqs_addressline2"].ToString();
                                msgBdy.accountNominee.nominee.address.line3 = Nominee[0]["eqs_addressline3"].ToString();

                                msgBdy.accountNominee.nominee.emailId = Nominee[0]["eqs_emailid"].ToString();
                                msgBdy.accountNominee.nominee.name = Nominee[0]["eqs_nomineename"].ToString();

                                if (!string.IsNullOrEmpty(Nominee[0]["eqs_guardianname"].ToString()))
                                {
                                    msgBdy.accountNominee.guardian.name = Nominee[0]["eqs_guardianname"].ToString();
                                    msgBdy.accountNominee.guardian.phone.number = Nominee[0]["eqs_guardianmobile"].ToString();

                                    msgBdy.accountNominee.guardian.address.line1 = Nominee[0]["eqs_guardianaddressline1"].ToString();
                                    msgBdy.accountNominee.guardian.address.line2 = Nominee[0]["eqs_guardianaddressline2"].ToString();
                                    msgBdy.accountNominee.guardian.address.line3 = Nominee[0]["eqs_guardianaddressline3"].ToString();

                                    if (!string.IsNullOrEmpty(Nominee[0]["_eqs_guardiancity_value"].ToString()))
                                    {
                                        msgBdy.accountNominee.guardian.address.city = await this._commonFunc.getCityName(Nominee[0]["_eqs_guardiancity_value"].ToString());
                                    }

                                    if (!string.IsNullOrEmpty(Nominee[0]["_eqs_guardianstate_value"].ToString()))
                                    {
                                        msgBdy.accountNominee.guardian.address.state = "TAMIL NADU"; //await this._commonFunc.getStateName(Nominee[0]["_eqs_guardianstate_value"].ToString());
                                    }

                                    if (!string.IsNullOrEmpty(Nominee[0]["_eqs_guardiancountry_value"].ToString()))
                                    {
                                        msgBdy.accountNominee.guardian.address.country = "IN";   //await this._commonFunc.getCountryName(Nominee[0]["_eqs_guardiancountry_value"].ToString());
                                    }
                                    msgBdy.accountNominee.guardian.address.zip = Nominee[0]["eqs_guardianpincode"].ToString();
                                }

                                msgBdy.accountNominee.zip = Nominee[0]["eqs_pincode"].ToString();

                            }
                            else
                            {
                                msgBdy.Remove("accountNominee");
                            }

                            List<ApplicentRelation> relationList = new List<ApplicentRelation>();
                            foreach (var item in AccApplicent)
                            {
                                ApplicentRelation applicentRelation = new ApplicentRelation();
                                applicentRelation.customerId = item["eqs_customer"].ToString();
                                applicentRelation.customerName = item["eqs_name"].ToString();
                                applicentRelation.relation = await this._commonFunc.getAccountRelation(item["_eqs_accountrelationship_value"].ToString());

                                if (item["eqs_isprimaryholder"].ToString() == "789030001")
                                {
                                    msgBdy.customerID = item["eqs_customer"].ToString();
                                }
                                relationList.Add(applicentRelation);
                            }

                            string productCat = await this._commonFunc.getProductCategory(AccountDDE[0]["_eqs_productcategoryid_value"].ToString());

                            msgBdy.customerAndRelation = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(relationList));
                            msgBdy.isJointHolder = (AccountDDE[0]["eqs_accountownershipcode"].ToString() == "615290001") ? true : false;
                            msgBdy.productCode = Convert.ToInt32(await this._commonFunc.getProductCode(AccountDDE[0]["_eqs_productid_value"].ToString()));


                            msgBdy.tdaccountPayinRequest.depositAmount = AccountDDE[0]["eqs_depositamountslot"].ToString();
                            msgBdy.tdaccountPayinRequest.termDays = AccountDDE[0]["eqs_tenureindays"].ToString();
                            msgBdy.tdaccountPayinRequest.termMonths = AccountDDE[0]["eqs_tenureinmonths"].ToString();
                            if (!string.IsNullOrEmpty(AccountDDE[0]["eqs_fromesfbaccountnumber"].ToString()))
                                msgBdy.tdaccountPayinRequest.fromAccountNo = AccountDDE[0]["eqs_fromesfbaccountnumber"].ToString();
                            else
                            {
                                msgBdy.tdaccountPayinRequest.branchCodeGL = AccountDDE[0]["eqs_branchcodegl"].ToString();
                                msgBdy.tdaccountPayinRequest.referenceNoGL = AccountDDE[0]["eqs_fromesfbglaccount"].ToString();
                            }

                            msgBdy.rdaccountPayinRequest.installmentAmount = AccountDDE[0]["eqs_depositamountslot"].ToString();
                            msgBdy.rdaccountPayinRequest.termMonths = AccountDDE[0]["eqs_tenureinmonths"].ToString();
                            if (!string.IsNullOrEmpty(AccountDDE[0]["eqs_fromesfbaccountnumber"].ToString()))
                                msgBdy.rdaccountPayinRequest.providerAccountNo = AccountDDE[0]["eqs_fromesfbaccountnumber"].ToString();
                            else
                            {
                                msgBdy.rdaccountPayinRequest.branchCodeGL = AccountDDE[0]["eqs_branchcodegl"].ToString();
                                msgBdy.rdaccountPayinRequest.referenceNoGL = AccountDDE[0]["eqs_fromesfbglaccount"].ToString();
                            }

                            if (productCat == "PCAT04")
                            {
                                msgBdy.Remove("rdaccountPayinRequest");
                            }
                            else if (productCat == "PCAT05")
                            {
                                msgBdy.Remove("tdaccountPayinRequest");
                            }
                            else
                            {
                                msgBdy.Remove("tdaccountPayinRequest");
                                msgBdy.Remove("rdaccountPayinRequest");

                            }

                            Request_Template.createAccountRequest.msgBdy = msgBdy;



                            string input_payload = JsonConvert.SerializeObject(Request_Template);
                            string postDataParametr = await EncriptRespons(input_payload, "FI0060");
                            Lead_details = await this._queryParser.HttpCBSApiCall(Token, HttpMethod.Post, "CBSCreateAccount", postDataParametr);
                            responsD = JsonConvert.DeserializeObject(Lead_details);
                        }

                        if (responsD.msgHdr != null && responsD.msgHdr.result.ToString() == "ERROR")
                        {
                            accountLeadReturn.Message = responsD.msgHdr.error[0].reason.ToString();
                            accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                        }
                        else if (responsD.createAccountResponse != null && responsD.createAccountResponse.msgBdy != null)
                        {
                            Dictionary<string, string> fieldInput = new Dictionary<string, string>();

                            accountLeadReturn.AccountNo = responsD.createAccountResponse.msgBdy.accountNo.ToString();
                            fieldInput.Add("eqs_accountnocreated", accountLeadReturn.AccountNo);
                            string postDataParametr = JsonConvert.SerializeObject(fieldInput);

                            await this._queryParser.HttpApiCall($"eqs_ddeaccounts({AccountDDE[0]["eqs_ddeaccountid"].ToString()})", HttpMethod.Patch, postDataParametr);

                            accountLeadReturn.Message = OutputMSG.Case_Success;
                            accountLeadReturn.ReturnCode = "CRM-SUCCESS";
                        }
                        else
                        {
                            accountLeadReturn.Message = Lead_details;
                            accountLeadReturn.ReturnCode = "CRM-ERROR-101";
                        }
                    }
                    else
                    {
                        accountLeadReturn.Message = "Lead cannot be onboarded. " + AccountDDE[0]["eqs_onboardingvalidationmessage"].ToString().Replace("\r\n", ", ").Trim().Trim(',');
                        accountLeadReturn.ReturnCode = "CRM-ERROR-101";
                    }
                }
                else
                {
                    accountLeadReturn.Message = OutputMSG.Resource_n_Found;
                    accountLeadReturn.ReturnCode = "CRM-ERROR-101";
                }

            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateAccountByLead", ex.Message);
                accountLeadReturn.Message = $"error {ex.Message} {ex.InnerException!} {ex.StackTrace!}";
                accountLeadReturn.ReturnCode = "CRM-ERROR-102";
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
