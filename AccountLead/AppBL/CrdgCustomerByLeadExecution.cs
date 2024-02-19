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
    using System.Security.Cryptography.X509Certificates;

    public class CrdgCustomerByLeadExecution : ICrdgCustomerByLeadExecution
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

        public CrdgCustomerByLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
            this._logger = logger;

            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

            _leadParam = new LeadParam();
            _accountLead = new AccountLead();
            _accountApplicants = new List<AccountApplicant>();
        }


        public async Task<CustomerByLeadReturn> ValidateLeadtInput(dynamic RequestData)
        {
            CustomerByLeadReturn ldRtPrm = new CustomerByLeadReturn();
            RequestData = await this.getRequestData(RequestData, "CreateDigiCustomerByLead");

            if (RequestData.ErrorNo != null && RequestData.ErrorNo.ToString() == "Error99")
            {
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = "API do not have access permission!";
                return ldRtPrm;
            }
            try
            {
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "CreateDigiCustomerByLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        if (!string.IsNullOrEmpty(RequestData.ApplicantID.ToString()))
                        {
                            ldRtPrm = await this.CreateCustomer(RequestData.ApplicantID.ToString());
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateLeadtInput", "ApplicantID can not be null.");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "ApplicentID can not be null.";
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

        private async Task<CustomerByLeadReturn> CreateCustomer(string applicantId)
        {
            CustomerByLeadReturn customerLeadReturn = new CustomerByLeadReturn();
            string EntityType = string.Empty;
            try
            {
                var AccountDDE = await this._commonFunc.getApplicentData(applicantId);
                if (AccountDDE.Count > 0)
                {
                    if (AccountDDE[0]["_eqs_entitytypeid_value@OData.Community.Display.V1.FormattedValue"] != null)
                        EntityType = AccountDDE[0]["_eqs_entitytypeid_value@OData.Community.Display.V1.FormattedValue"].ToString();
                    if (EntityType == "Individual")
                    {
                        customerLeadReturn = await this.CreateCustomerByLeadIndiv(applicantId);
                    }
                    else
                    {
                        customerLeadReturn = await this.CreateCustomerByLeadCorp(applicantId);
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateCustomer", ex.Message);
                customerLeadReturn.Message = ex.Message;
                customerLeadReturn.ReturnCode = "CRM-ERROR-102";
            }
            return customerLeadReturn;
        }

        private async Task<CustomerByLeadReturn> CreateCustomerByLeadIndiv(string applicantId)
        {
            CustomerByLeadReturn customerLeadReturn = new CustomerByLeadReturn();
            try
            {
                string Token = await this._queryParser.getAccessToken();
                Dictionary<string, string> odatab = new Dictionary<string, string>();
                var AccountDDE = await this._commonFunc.getApplicantIndivDDE(applicantId);

                if (AccountDDE.Count > 0)
                {
                    if (string.IsNullOrEmpty(AccountDDE[0]["eqs_customeridcreated"].ToString()))
                    {
                        if (!string.IsNullOrEmpty(AccountDDE[0]["eqs_readyforonboarding"].ToString()) && Convert.ToBoolean(AccountDDE[0]["eqs_readyforonboarding"].ToString()))
                        {
                            dynamic responsD = "";
                            string Lead_details = "";
                            var address = await this._commonFunc.getAddressData(AccountDDE[0]["eqs_ddeindividualcustomerid"].ToString());

                            string RequestTemplate = "{\"createCustomerRequest\":{\"msgHdr\":{\"channelID\":\"VFLOS\",\"transactionType\":\"create\",\"transactionSubType\":\"customer\",\"conversationID\":\"string\",\"externalReferenceId\":\"kjdcbskj9c123424\",\"isAsync\":false,\"authInfo\":{\"branchID\":\"1001\",\"userID\":\"IBUSER\",\"token\":\"1001\"}},\"msgBdy\":{\"misClass\":\"DIVISION\",\"misCode\":\"0\",\"individualCustomer\":{\"address\":{\"line1\":\"TEST1\",\"line2\":\"TEST2\",\"line3\":\"TEST3\",\"line4\":\"TEST4\",\"city\":\"CHENNAI\",\"state\":\"TAMILNADU\",\"country\":\"IN\",\"zip\":\"565556\"},\"category\":\"I\",\"cifType\":\"C\",\"countryOfResidence\":\"IN\",\"customerMobilePhone\":\"919887899899\",\"dateOfBirthOrRegistration\":\"20001205\",\"emailId\":\"emaill@e.com\",\"adhrNo\":\"\",\"incomeTaxNumber\":\"\",\"homeBranchCode\":9999,\"language\":\"ENG\",\"name\":{\"firstName\":\"sall\",\"lastName\":\"SWAMI\",\"midName\":\"\",\"prefix\":\"MR.\",\"shortName\":\"salllu\"},\"customerEducation\":\"5\",\"employeeId\":\"33454\",\"isStaff\":\"Y\",\"maritalStatus\":\"1\",\"motherMaidenName\":\"KOMAL\",\"professionCode\":0,\"sex\":\"M\",\"nationalIdentificationCode\":\"1293\",\"nationality\":\"IN\"}}}}";
                            dynamic Request_Template = JsonConvert.DeserializeObject(RequestTemplate);
                            dynamic msgHdr = Request_Template.createCustomerRequest.msgHdr;
                            dynamic msgBdy = Request_Template.createCustomerRequest.msgBdy;
                            Guid ReferenceId = Guid.NewGuid();
                            msgHdr.externalReferenceId = ReferenceId.ToString().Replace("-", "");
                            Request_Template.createCustomerRequest.msgHdr = msgHdr;

                            if (address.Count > 0)
                            {
                                msgBdy.individualCustomer.address.line1 = address[0]["eqs_addressline1"].ToString();
                                msgBdy.individualCustomer.address.line2 = address[0]["eqs_addressline2"].ToString();
                                msgBdy.individualCustomer.address.line3 = address[0]["eqs_addressline3"].ToString();
                                msgBdy.individualCustomer.address.line4 = address[0]["eqs_addressline4"].ToString();
                                msgBdy.individualCustomer.address.zip = address[0]["eqs_pincode"].ToString();
                                msgBdy.individualCustomer.address.city = await this._commonFunc.getCityName(address[0]["_eqs_cityid_value"].ToString());  //"CHENNAI";
                                msgBdy.individualCustomer.address.state = await this._commonFunc.getStateName(address[0]["_eqs_stateid_value"].ToString());  //"TAMILNADU";
                                msgBdy.individualCustomer.address.country = "IN";
                            }

                            DateTime dob = Convert.ToDateTime(AccountDDE[0]["eqs_dob"]);
                            string yyyy = dob.Year.ToString().PadLeft(4, '0');
                            string mm = dob.Month.ToString().PadLeft(2, '0');
                            string dd = dob.Day.ToString().PadLeft(2, '0');

                            //string dd = AccountDDE[0]["eqs_dob"].ToString().Substring(0, 2);
                            //string mm = AccountDDE[0]["eqs_dob"].ToString().Substring(3, 2);
                            //string yy = AccountDDE[0]["eqs_dob"].ToString().Substring(6, 4);
                            msgBdy.individualCustomer.dateOfBirthOrRegistration = yyyy + mm + dd;

                            string subEntityType = string.Empty; string mobileNumber = "91";
                            if(!string.IsNullOrEmpty(AccountDDE[0]["_eqs_subentitytypeid_value"]?.ToString()))
                            {
                                subEntityType = AccountDDE[0]["eqs_subentitytypeId"]["eqs_name"].ToString();
                            }
                            if (!string.IsNullOrEmpty(subEntityType) && !(subEntityType.ToUpper() == "NON RESIDENT INDIVIDUAL" || subEntityType.ToLower() == "foreigners"))
                            {
                                mobileNumber = "91" + AccountDDE[0]["eqs_mobilenumber"].ToString();
                            }
                            else
                            {
                                mobileNumber = AccountDDE[0]["eqs_mobilenumber"].ToString();
                            }
                            msgBdy.individualCustomer.customerMobilePhone = mobileNumber;
                            msgBdy.individualCustomer.emailId = AccountDDE[0]["eqs_emailid"].ToString();

                            msgBdy.individualCustomer.name.firstName = AccountDDE[0]["eqs_firstname"].ToString();
                            msgBdy.individualCustomer.name.lastName = AccountDDE[0]["eqs_lastname"].ToString();
                            msgBdy.individualCustomer.name.midName = AccountDDE[0]["eqs_middlename"].ToString();
                            msgBdy.individualCustomer.name.shortName = AccountDDE[0]["eqs_shortname"].ToString();

                            msgBdy.individualCustomer.adhrNo = AccountDDE[0]["eqs_aadharreference"].ToString();
                            msgBdy.individualCustomer.incomeTaxNumber = AccountDDE[0]["eqs_pannumber"].ToString();

                            //msgBdy.individualCustomer.employeeId = "";
                            msgBdy.individualCustomer.nationalIdentificationCode = applicantId;
                            msgBdy.individualCustomer.motherMaidenName = AccountDDE[0]["eqs_mothermaidenname"].ToString();
                            msgBdy.individualCustomer.isStaff = "Y";
                            if (!string.IsNullOrEmpty(AccountDDE[0]["eqs_gendercode"].ToString()))
                            {
                                if (AccountDDE[0]["eqs_gendercode"].ToString() == "789030000")
                                {
                                    msgBdy.individualCustomer.sex = "M";
                                }
                                else if (AccountDDE[0]["eqs_gendercode"].ToString() == "789030001")
                                {
                                    msgBdy.individualCustomer.sex = "F";
                                }
                                else if (AccountDDE[0]["eqs_gendercode"].ToString() == "789030002")
                                {
                                    msgBdy.individualCustomer.sex = "T";
                                }
                            }

                            if (!string.IsNullOrEmpty(AccountDDE[0]["_eqs_corporatecompanyid_value"]?.ToString()))
                            {
                                msgBdy.individualCustomer.employerCode = AccountDDE[0]["eqs_corporatecompanyid"]["eqs_corporatecode"]?.ToString();
                            }
                            if (!string.IsNullOrEmpty(AccountDDE[0]["_eqs_custpreferredbranchid_value"]?.ToString()))
                            {
                                msgBdy.individualCustomer.homeBranchCode = AccountDDE[0]["eqs_custpreferredbranchId"]["eqs_branchidvalue"]?.ToString();
                            }
                            //msgBdy.individualCustomer.homeBranchCode = await this._commonFunc.getBranchCode(AccountDDE[0]["_eqs_sourcebranchid_value"].ToString());

                            Request_Template.createCustomerRequest.msgBdy = msgBdy;
                            string wso_request = JsonConvert.SerializeObject(Request_Template);
                            string postDataParametr = await EncriptRespons(wso_request, "FI0060");
                            Lead_details = await this._queryParser.HttpCBSApiCall(Token, HttpMethod.Post, "CBSCreateCustomer", postDataParametr);
                            responsD = JsonConvert.DeserializeObject(Lead_details);


                            if (responsD.msgHdr != null && responsD.msgHdr.result.ToString() == "ERROR")
                            {
                                customerLeadReturn.Message = responsD.msgHdr.error[0].reason.ToString();
                                customerLeadReturn.ReturnCode = "CRM-ERROR-102";
                            }
                            else if (responsD.createCustomerResponse != null && responsD.createCustomerResponse.msgHdr != null && responsD.createCustomerResponse.msgHdr.result.ToString() == "ERROR")
                            {
                                customerLeadReturn.Message = responsD.createCustomerResponse.msgHdr.error[0].reason.ToString();
                                customerLeadReturn.ReturnCode = "CRM-ERROR-102";
                            }
                            else if (responsD.createCustomerResponse != null && responsD.createCustomerResponse.msgBdy != null)
                            {
                                Dictionary<string, string> fieldInput = new Dictionary<string, string>();

                                customerLeadReturn.customerId = responsD.createCustomerResponse.msgBdy.customerId.ToString();
                                if (!string.IsNullOrEmpty(customerLeadReturn.customerId) && customerLeadReturn.customerId != "")
                                {
                                    fieldInput.Add("eqs_customeridcreated", customerLeadReturn.customerId);
                                    postDataParametr = JsonConvert.SerializeObject(fieldInput);

                                    var resp1 = await this._queryParser.HttpApiCall($"eqs_accountapplicants({AccountDDE[0]["_eqs_accountapplicantid_value"].ToString()})", HttpMethod.Patch, postDataParametr);

                                    var resp2 = await this._queryParser.HttpApiCall($"eqs_ddeindividualcustomers({AccountDDE[0]["eqs_ddeindividualcustomerid"].ToString()})", HttpMethod.Patch, postDataParametr);

                                    fieldInput = new Dictionary<string, string>();
                                     string OnboardingStatus = await this._queryParser.getOptionSetTextToValue("lead", "eqs_onboardingstatus", "Completed");
                                    fieldInput.Add("eqs_onboardingstatus", OnboardingStatus);
                                    postDataParametr = JsonConvert.SerializeObject(fieldInput);
                                    var resp3 = await this._queryParser.HttpApiCall($"leads({AccountDDE[0]["_eqs_leadid_value"].ToString()})", HttpMethod.Patch, postDataParametr);

                                    customerLeadReturn.Message = OutputMSG.Case_Success;
                                    customerLeadReturn.ReturnCode = "CRM-SUCCESS";
                                }
                            }
                            else
                            {
                                this._logger.LogInformation("HttpCBSApiCall output", Lead_details);
                                customerLeadReturn.Message = Lead_details;
                                customerLeadReturn.ReturnCode = "CRM-ERROR-101";
                            }
                        }
                        else
                        {
                            customerLeadReturn.Message = "Lead cannot be onboarded. " + AccountDDE[0]["eqs_onboardingvalidationmessage"].ToString();
                            customerLeadReturn.ReturnCode = "CRM-ERROR-101";
                        }
                    }
                    else
                    {
                        customerLeadReturn.Message = "Lead has been onboarded already. Customer No " + AccountDDE[0]["eqs_customeridcreated"].ToString();
                        customerLeadReturn.ReturnCode = "CRM-ERROR-101";
                    }
                }
                else
                {
                    this._logger.LogInformation("CreateCustomerByLeadIndiv", "DDE final data not found.");
                    customerLeadReturn.Message = "DDE final data not found.";
                    customerLeadReturn.ReturnCode = "CRM-ERROR-101";
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateCustomerByLeadIndiv", ex.Message);
                customerLeadReturn.Message = ex.Message;
                customerLeadReturn.ReturnCode = "CRM-ERROR-102";
            }
            return customerLeadReturn;
        }

        private async Task<CustomerByLeadReturn> CreateCustomerByLeadCorp(string applicantId)
        {
            CustomerByLeadReturn customerLeadReturn = new CustomerByLeadReturn();
            try
            {
                string Token = await this._queryParser.getAccessToken();
                Dictionary<string, string> odatab = new Dictionary<string, string>();
               var AccountDDE = await this._commonFunc.getApplicantCorpDDE(applicantId);

                if (AccountDDE.Count > 0)
                {
                    if (string.IsNullOrEmpty(AccountDDE[0]["eqs_customeridcreated"].ToString()))
                    {
                        if (!string.IsNullOrEmpty(AccountDDE[0]["eqs_readyforonboarding"].ToString()) && Convert.ToBoolean(AccountDDE[0]["eqs_readyforonboarding"].ToString()))
                        {
                            dynamic responsD = "";
                            string Lead_details = "";
                            var address = await this._commonFunc.getAddressData(AccountDDE[0]["eqs_ddecorporatecustomerid"].ToString(), "corp");
                            string RequestTemplate = "{\"createCustomerRequest\":{\"msgHdr\":{\"channelID\":\"VFLOS\",\"transactionType\":\"create\",\"transactionSubType\":\"customer\",\"conversationID\":\"string\",\"externalReferenceId\":\"kjdcbskj9c123424\",\"isAsync\":false,\"authInfo\":{\"branchID\":\"1001\",\"userID\":\"IBUSER\",\"token\":\"1001\"}},\"msgBdy\":{\"misClass\":\"DIVISION\",\"misCode\":\"0\",\"corporateCustomer\":{\"address\":{\"line1\":\"TEST1\",\"line2\":\"TEST2\",\"line3\":\"TEST3\",\"line4\":\"TEST4\",\"city\":\"CHENNAI\",\"state\":\"TAMILNADU\",\"country\":\"IN\",\"zip\":\"565556\"},\"category\":\"C\",\"cifType\":\"C\",\"countryOfResidence\":\"IN\",\"customerMobilePhone\":\"919887899899\",\"dateOfBirthOrRegistration\":\"20001205\",\"emailId\":\"emaill@e.com\",\"homeBranchCode\":9999,\"language\":\"ENG\",\"name\":{\"firstName\":\"sall\",\"lastName\":\"SWAMI\",\"midName\":\"\",\"prefix\":\"MR.\",\"shortName\":\"salllu\"},\"nationalIdentificationCode\":\"234325\",\"businessCode\":\"\",\"alternateEmailId\":\"\",\"annualTurnover\":\"\",\"businessRegistrationNumber\":\"\",\"businessType\":\"\",\"copyMailAddToPermAdd\":\"\",\"dateRegistered\":\"\",\"fax\":\"\",\"telexHandPhone\":\"\",\"riskCategory\":\"\",\"riskCategoryChangeReason\":\"\",\"incomeTaxNumber\":\"\",\"gstNumber\":\"\",\"tan\":\"\",\"cinOrRegNo\":\"\",\"tin\":\"\",\"din\":\"\",\"nrega\":\"\",\"districtCommunication\":\"\",\"nationality\":\"IN\"}}}}";
                            dynamic Request_Template = JsonConvert.DeserializeObject(RequestTemplate);
                            dynamic msgHdr = Request_Template.createCustomerRequest.msgHdr;
                            dynamic msgBdy = Request_Template.createCustomerRequest.msgBdy;
                            Guid ReferenceId = Guid.NewGuid();
                            msgHdr.externalReferenceId = ReferenceId.ToString().Replace("-", "");
                            Request_Template.createCustomerRequest.msgHdr = msgHdr;

                            if (address.Count > 0)
                            {
                                msgBdy.corporateCustomer.address.line1 = address[0]["eqs_addressline1"].ToString();
                                msgBdy.corporateCustomer.address.line2 = address[0]["eqs_addressline2"].ToString();
                                msgBdy.corporateCustomer.address.line3 = address[0]["eqs_addressline3"].ToString();
                                msgBdy.corporateCustomer.address.line4 = address[0]["eqs_addressline4"].ToString();
                                msgBdy.corporateCustomer.address.zip = address[0]["eqs_pincode"].ToString();
                                msgBdy.corporateCustomer.address.city = await this._commonFunc.getCityName(address[0]["_eqs_cityid_value"].ToString());  //"CHENNAI";
                                msgBdy.corporateCustomer.address.state = await this._commonFunc.getStateName(address[0]["_eqs_stateid_value"].ToString());  //"TAMILNADU";
                                msgBdy.corporateCustomer.address.country = "IN";
                            }

                            string dd = AccountDDE[0]["eqs_dateofincorporation"].ToString().Substring(0, 2);
                            string mm = AccountDDE[0]["eqs_dateofincorporation"].ToString().Substring(3, 2);
                            string yy = AccountDDE[0]["eqs_dateofincorporation"].ToString().Substring(6, 4);
                            msgBdy.corporateCustomer.dateOfBirthOrRegistration = yy + mm + dd;
                            msgBdy.corporateCustomer.customerMobilePhone = AccountDDE[0]["eqs_pocphonenumber"].ToString();
                            msgBdy.corporateCustomer.emailId = AccountDDE[0]["eqs_emailid"].ToString();

                            msgBdy.corporateCustomer.name.firstName = AccountDDE[0]["eqs_companyname1"].ToString();
                            msgBdy.corporateCustomer.name.lastName = AccountDDE[0]["eqs_companyname3"].ToString();
                            msgBdy.corporateCustomer.name.midName = AccountDDE[0]["eqs_companyname2"].ToString();
                            msgBdy.corporateCustomer.name.shortName = AccountDDE[0]["eqs_companyname1"].ToString();

                            msgBdy.corporateCustomer.nationalIdentificationCode = applicantId;                            
                            if (!string.IsNullOrEmpty(AccountDDE[0]["_eqs_preferredhomebranchid_value"]?.ToString()))
                            {
                                msgBdy.corporateCustomer.homeBranchCode = AccountDDE[0]["eqs_preferredhomebranchId"]["eqs_branchidvalue"]?.ToString();
                            }
                            //msgBdy.corporateCustomer.homeBranchCode = await this._commonFunc.getBranchCode(AccountDDE[0]["_eqs_sourcebranchterritoryid_value"].ToString());

                            msgBdy.corporateCustomer.annualTurnover = AccountDDE[0]["eqs_companyturnovervalue"].ToString();
                            string businessregno = "";
                            string gstnum = AccountDDE[0]["eqs_gstnumber"].ToString();
                            string cstvat = AccountDDE[0]["eqs_cstvatnumber"].ToString();
                            string tan = AccountDDE[0]["eqs_tannumber"].ToString();
                            if (cstvat != string.Empty)
                            {
                                businessregno = cstvat;
                            }
                            else if (gstnum != string.Empty)
                            {
                                businessregno = gstnum;
                            }
                            else if (tan != string.Empty)
                            {
                                businessregno = tan;
                            }
                            msgBdy.corporateCustomer.businessRegistrationNumber = businessregno;

                            msgBdy.corporateCustomer.copyMailAddToPermAdd = "Y";
                            dd = AccountDDE[0]["eqs_dateofincorporation"].ToString().Substring(0, 2);
                            mm = AccountDDE[0]["eqs_dateofincorporation"].ToString().Substring(3, 2);
                            yy = AccountDDE[0]["eqs_dateofincorporation"].ToString().Substring(6, 4);
                            msgBdy.corporateCustomer.dateRegistered = yy + mm + dd;
                            msgBdy.corporateCustomer.fax = AccountDDE[0]["eqs_faxnumber"].ToString();

                            msgBdy.corporateCustomer.incomeTaxNumber = AccountDDE[0]["eqs_pannumber"].ToString();
                            msgBdy.corporateCustomer.gstNumber = gstnum;
                            msgBdy.corporateCustomer.tan = tan;
                            msgBdy.corporateCustomer.cinOrRegNo = AccountDDE[0]["eqs_cinregisterednumber"].ToString();
                            msgBdy.corporateCustomer.riskCategory = "1";
                            msgBdy.corporateCustomer.districtCommunicationmsgBdy = "Distr";

                            Request_Template.createCustomerRequest.msgBdy = msgBdy;
                            string wso_request = JsonConvert.SerializeObject(Request_Template);
                            string postDataParametr = await EncriptRespons(wso_request, "FI0060");
                            Lead_details = await this._queryParser.HttpCBSApiCall(Token, HttpMethod.Post, "CBSCreateCustomer", postDataParametr);
                            responsD = JsonConvert.DeserializeObject(Lead_details);

                            if (responsD.msgHdr != null && responsD.msgHdr.result.ToString() == "ERROR")
                            {
                                customerLeadReturn.Message = responsD.msgHdr.error[0].reason.ToString();
                                customerLeadReturn.ReturnCode = "CRM-ERROR-102";
                            }
                            else if (responsD.createCustomerResponse != null && responsD.createCustomerResponse.msgBdy != null)
                            {
                                Dictionary<string, string> fieldInput = new Dictionary<string, string>();

                                customerLeadReturn.customerId = responsD.createCustomerResponse.msgBdy.customerId.ToString();
                                if (!string.IsNullOrEmpty(customerLeadReturn.customerId) && customerLeadReturn.customerId != "")
                                {
                                    fieldInput.Add("eqs_customeridcreated", customerLeadReturn.customerId);
                                    postDataParametr = JsonConvert.SerializeObject(fieldInput);

                                    var resp1 = await this._queryParser.HttpApiCall($"eqs_accountapplicants({AccountDDE[0]["_eqs_accountapplicantid_value"].ToString()})", HttpMethod.Patch, postDataParametr);

                                    var resp2 = await this._queryParser.HttpApiCall($"eqs_ddecorporatecustomers({AccountDDE[0]["eqs_ddecorporatecustomerid"].ToString()})", HttpMethod.Patch, postDataParametr);

                                    fieldInput = new Dictionary<string, string>();
                                    string OnboardingStatus = await this._queryParser.getOptionSetTextToValue("lead", "eqs_onboardingstatus", "Completed");
                                    fieldInput.Add("eqs_onboardingstatus", OnboardingStatus);
                                    postDataParametr = JsonConvert.SerializeObject(fieldInput);
                                    await this._queryParser.HttpApiCall($"leads({AccountDDE[0]["_eqs_leadid_value"].ToString()})", HttpMethod.Patch, postDataParametr);

                                    customerLeadReturn.Message = OutputMSG.Case_Success;
                                    customerLeadReturn.ReturnCode = "CRM-SUCCESS";
                                }
                            }
                            else
                            {
                                this._logger.LogInformation("HttpCBSApiCall output", Lead_details);
                                customerLeadReturn.Message = Lead_details;
                                customerLeadReturn.ReturnCode = "CRM-ERROR-101";
                            }
                        }
                        else
                        {
                            customerLeadReturn.Message = "Lead cannot be onboarded. " + AccountDDE[0]["eqs_onboardingvalidationmessage"].ToString();
                            customerLeadReturn.ReturnCode = "CRM-ERROR-101";
                        }
                    }
                    else
                    {
                        customerLeadReturn.Message = "Lead has been onboarded already. Customer No " + AccountDDE[0]["eqs_customeridcreated"].ToString();
                        customerLeadReturn.ReturnCode = "CRM-ERROR-101";
                    }
                }
                else
                {
                    this._logger.LogInformation("CreateCustomerByLeadCorp", "DDE final data not found.");
                    customerLeadReturn.Message = "DDE final data not found.";
                    customerLeadReturn.ReturnCode = "CRM-ERROR-101";
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateCustomerByLeadCorp", ex.Message);
                customerLeadReturn.Message = ex.Message;
                customerLeadReturn.ReturnCode = "CRM-ERROR-102";
            }
            return customerLeadReturn;
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