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
    using System.Net.Mail;
    using Microsoft.VisualBasic;

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

        public async Task<UpdateCaseReturnParam> ValidateUpdateCase(dynamic CaseData)
        {
            CaseData = await this.getRequestData(CaseData, "UpdateCase");
            UpdateCaseReturnParam caseRtPrm = new UpdateCaseReturnParam();
            try
            {

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "UpdateCaseappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        int ValidationError = 0;
                        List<string> errors = new List<string>();

                        if (CaseData.CaseId == null || string.IsNullOrEmpty(CaseData.CaseId.ToString()) || CaseData.CaseId.ToString() == "")
                        {
                            ValidationError = 1;
                            errors.Add("CaseId");
                        }
                        if (CaseData.CaseStatus == null || string.IsNullOrEmpty(CaseData.CaseStatus.ToString()) || CaseData.CaseStatus.ToString() == "")
                        {
                            ValidationError = 1;
                            errors.Add("CaseStatus");
                        }

                        if (!string.IsNullOrEmpty(CaseData.CRMModification?.Customer?.Account.ToString())
                            && string.IsNullOrEmpty(CaseData.CRMModification?.Customer?.Account?.AccountNumber.ToString()))
                        {
                            ValidationError = 1;
                            errors.Add("AccountNumber");
                        }

                        if (!string.IsNullOrEmpty(CaseData.CRMModification?.Customer?.Account.ToString())
                            && string.IsNullOrEmpty(CaseData.CRMModification?.Customer?.Account?.AccountNumber.ToString()))
                        {
                            ValidationError = 1;
                            errors.Add("AccountNumber");
                        }

                        if (ValidationError == 1)
                        {
                            this._logger.LogInformation("ValidateUpdateCase", $"{string.Join(",", errors.ToArray())} is mandatory");
                            caseRtPrm.ReturnCode = "CRM-ERROR-102";
                            if (errors.Count == 1)
                            {
                                caseRtPrm.Message = $" {string.Join(", ", errors.ToArray())} is mandatory";
                            }
                            else if (errors.Count > 1)
                            {
                                caseRtPrm.Message = $" {string.Join(", ", errors.ToArray())} are mandatory";
                            }
                        }
                        else
                        {
                            caseRtPrm = await this.UpdateCase(CaseData);
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateUpdateCase", "Transaction_ID or Channel_ID in incorrect.");
                        caseRtPrm.ReturnCode = "CRM-ERROR-102";
                        caseRtPrm.Message = "Transaction_ID or Channel_ID in incorrect.";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateUpdateCase", "Appkey is incorrect");
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

            dynamic caseData = await this._commonFunc.getExistingCase(RequestData.CaseId.ToString());
            if (caseData.Count > 0)
            {
                string caseid = caseData[0]["incidentid"].ToString();
                string statuscode = await this._queryParser.getOptionSetTextToValue("incident", "statuscode", RequestData.CaseStatus.ToString());


                if (statuscode == "615290000") //Auto Closed
                {
                    Dictionary<string, string> incidentresolution = new Dictionary<string, string>
                    {
                        { "subject", "Case Auto Closed" },
                        { "incidentid@odata.bind", $"/incidents({caseid})" },
                        { "description", "Resolved via Inbound API" }
                    };

                    Dictionary<string, object> odatab = new Dictionary<string, object>();
                    odatab.Add("IncidentResolution", incidentresolution);
                    odatab.Add("Status", statuscode);

                    string caseDataParametr = JsonConvert.SerializeObject(odatab);
                    var Case_details = await this._queryParser.HttpApiCall($"CloseIncident", HttpMethod.Post, caseDataParametr);                    
                }
                else
                {
                    Dictionary<string, string> odatab = new Dictionary<string, string>();
                    odatab.Add("statuscode", statuscode);
                    string caseDataParametr = JsonConvert.SerializeObject(odatab);
                    var Case_details = await this._queryParser.HttpApiCall($"incidents({caseid})?$select=incidentid", HttpMethod.Patch, caseDataParametr);
                    caseid = CommonFunction.GetIdFromPostRespons201(Case_details[0]["responsebody"], "incidentid");

                    if (string.IsNullOrEmpty(caseid))
                    {
                        this._logger.LogError("UpdateCase", JsonConvert.SerializeObject(Case_details), caseDataParametr);
                        caseRtPrm.ReturnCode = "CRM-ERROR-102";
                        caseRtPrm.Message = "Error occurred while updating case.";
                        return caseRtPrm;
                    }

                    if (!string.IsNullOrEmpty(RequestData.CRMModification?.Customer?.ToString()))
                    {
                        string customerid = caseData[0]["_customerid_value"].ToString();
                        dynamic CustomerData = await this._commonFunc.getCustomerData(customerid);
                        string entityType = CustomerData[0]["_eqs_entitytypeid_value@OData.Community.Display.V1.FormattedValue"].ToString();

                        dynamic CustData = RequestData.CRMModification.Customer;
                        odatab = new Dictionary<string, string>();
                        string dd, mm, yyyy;

                        if (entityType.ToLower() == "individual")
                        {
                            if (!string.IsNullOrEmpty(CustData.Individual?.Title.ToString()))
                            {
                                odatab.Add("eqs_titleidslot", CustData.Individual?.Title.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.FirstName.ToString()))
                            {
                                odatab.Add("firstname", CustData.Individual?.FirstName.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.MiddleName.ToString()))
                            {
                                odatab.Add("middlename", CustData.Individual?.MiddleName.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.LastName.ToString()))
                            {
                                odatab.Add("lastname", CustData.Individual?.LastName.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.ShortName.ToString()))
                            {
                                odatab.Add("eqs_shortname", CustData.Individual?.ShortName.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.MobilePhone.ToString()))
                            {
                                odatab.Add("mobilephone", CustData.Individual?.MobilePhone.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.DOB.ToString()))
                            {
                                dd = CustData.Individual?.DOB.ToString().Substring(0, 2);
                                mm = CustData.Individual?.DOB.ToString().Substring(3, 2);
                                yyyy = CustData.Individual?.DOB.ToString().Substring(6, 4);
                                odatab.Add("birthdate", yyyy + "-" + mm + "-" + dd);
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.EmailID.ToString()))
                            {
                                odatab.Add("emailaddress1", CustData.Individual?.EmailID.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.PAN.ToString()))
                            {
                                odatab.Add("eqs_pan", CustData.Individual?.PAN.ToString());
                                //odatab.Add("eqs_pan", "**********");
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.Form60.ToString()))
                            {
                                odatab.Add("eqs_panformslot", CustData.Individual?.Form60.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.Aadhar.ToString()))
                            {
                                odatab.Add("eqs_aadharreference", CustData.Individual?.Aadhar.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.Gender.ToString()))
                            {
                                odatab.Add("eqs_genderslot", CustData.Individual?.Gender.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.MotherMaidenName.ToString()))
                            {
                                odatab.Add("eqs_mothermaidenname", CustData.Individual?.MotherMaidenName.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.IfStaffThenEmployeeID.ToString()))
                            {
                                odatab.Add("eqs_isstafffcode", await this._queryParser.getOptionSetTextToValue("contact", "eqs_isstafffcode", CustData.Individual?.IfStaffThenEmployeeID.ToString()));
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.Nationality.ToString()))
                            {
                                odatab.Add("eqs_nationalityid", CustData.Individual?.Nationality.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.PoliticallyExposedPerson.ToString()))
                            {
                                odatab.Add("eqs_politicallyexposedperson", CustData.Individual?.PoliticallyExposedPerson.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.Purposeofcreation.ToString()))
                            {
                                odatab.Add("eqs_purposeofcreation", CustData.Individual?.Purposeofcreation.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.Nrikycmode.ToString()))
                            {
                                odatab.Add("eqs_nrikycmode", CustData.Individual?.Nrikycmode.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.Nrimobilenopref.ToString()))
                            {
                                odatab.Add("eqs_nrimobilenopref", CustData.Individual?.Nrimobilenopref.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.Voterid.ToString()))
                            {
                                odatab.Add("eqs_voterid", CustData.Individual?.Voterid.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.Drivinglicense.ToString()))
                            {
                                odatab.Add("eqs_dlnumber", CustData.Individual?.Drivinglicense.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.Passport.ToString()))
                            {
                                odatab.Add("eqs_passportnumber", CustData.Individual?.Passport.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Individual?.Nrivisatype.ToString()))
                            {
                                odatab.Add("eqs_nrivisatype", CustData.Individual?.Nrivisatype.ToString());
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(CustData.Corporate?.Title.ToString()))
                            {
                                odatab.Add("eqs_titleidslot", CustData.Corporate?.Title.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.CompanyName1.ToString()))
                            {
                                odatab.Add("eqs_companyname", CustData.Corporate?.CompanyName1.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.CompanyName2.ToString()))
                            {
                                odatab.Add("eqs_companyname2", CustData.Corporate?.CompanyName2.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.CompanyName3.ToString()))
                            {
                                odatab.Add("eqs_companyname3", CustData.Corporate?.CompanyName3.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.Dateofincorprotation.ToString()))
                            {
                                dd = CustData.Corporate?.Dateofincorprotation.ToString().Substring(0, 2);
                                mm = CustData.Corporate?.Dateofincorprotation.ToString().Substring(3, 2);
                                yyyy = CustData.Corporate?.Dateofincorprotation.ToString().Substring(6, 4);
                                odatab.Add("eqs_dateofincorporation", yyyy + "-" + mm + "-" + dd);
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.PANNumber.ToString()))
                            {
                                odatab.Add("eqs_pan", CustData.Corporate?.PANNumber.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.Form60.ToString()))
                            {
                                odatab.Add("eqs_panformslot", CustData.Corporate?.Form60.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.CSTOrVATNumber.ToString()))
                            {
                                odatab.Add("eqs_cstvatnumber", CustData.Corporate?.CSTOrVATNumber.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.CorporateIdentificationNumber.ToString()))
                            {
                                odatab.Add("eqs_corporateidentificationnumber", CustData.Corporate?.CorporateIdentificationNumber.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.GSTNumber.ToString()))
                            {
                                odatab.Add("eqs_gstnumber", CustData.Corporate?.GSTNumber.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.TANNumber.ToString()))
                            {
                                odatab.Add("eqs_tannumber", CustData.Corporate?.TANNumber.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.CompanyPhoneNumber.ToString()))
                            {
                                odatab.Add("eqs_companyphonenumber", CustData.Corporate?.CompanyPhoneNumber.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.CompanyEmailID.ToString()))
                            {
                                odatab.Add("eqs_companyemailid", CustData.Corporate?.CompanyEmailID.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.CompanyCoordinatorUCIC.ToString()))
                            {
                                odatab.Add("eqs_companycoordinatorucic", CustData.Corporate?.CompanyCoordinatorUCIC.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.CompanyCoordinatorName.ToString()))
                            {
                                odatab.Add("eqs_companycoordinatorname", CustData.Corporate?.CompanyCoordinatorName.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.CompanyCoordinatorEmail.ToString()))
                            {
                                odatab.Add("eqs_companycoordinatoremail", CustData.Corporate?.CompanyCoordinatorEmail.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.CompanyCoordinatorPhone.ToString()))
                            {
                                odatab.Add("eqs_companycoordinatorphone", CustData.Corporate?.CompanyCoordinatorPhone.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Corporate?.purposeofcreation.ToString()))
                            {
                                odatab.Add("eqs_purposeofcreation", CustData.Corporate?.purposeofcreation.ToString());
                            }
                        }

                        if (odatab.Count > 0)
                        {
                            string postDataParametr = JsonConvert.SerializeObject(odatab);
                            var Customer_details = await this._queryParser.HttpApiCall($"contacts({customerid})?$select=contactid", HttpMethod.Patch, postDataParametr);
                            customerid = CommonFunction.GetIdFromPostRespons201(Customer_details[0]["responsebody"], "contactid");

                            if (string.IsNullOrEmpty(customerid))
                            {
                                this._logger.LogError("UpdateCase", JsonConvert.SerializeObject(Customer_details), postDataParametr);
                                caseRtPrm.ReturnCode = "CRM-ERROR-102";
                                caseRtPrm.Message = "Error occurred while updating customer information.";
                                return caseRtPrm;
                            }
                        }

                        if (!string.IsNullOrEmpty(CustData.Account?.ToString()))
                        {
                            string accountNumber = CustData.Account.AccountNumber.ToString();
                            string accountid = await this._commonFunc.getAccountId(accountNumber);

                            odatab = new Dictionary<string, string>();
                            if (!string.IsNullOrEmpty(CustData.Account.AccountTitle.ToString()))
                            {
                                odatab.Add("eqs_accountcustomertitle", CustData.Account.AccountTitle.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustData.Account.ProductCode.ToString()))
                            {
                                odatab.Add("eqs_productcode", CustData.Account.ProductCode.ToString());
                                string productid = await this._commonFunc.getProductId(CustData.Account.ProductCode.ToString());
                                odatab.Add("eqs_productid@odata.bind", $"/eqs_products({productid})");
                            }

                            if (!string.IsNullOrEmpty(CustData.Account.BranchID.ToString()))
                            {
                                odatab.Add("eqs_branch", CustData.Account.BranchID.ToString());
                                string branchid = await this._commonFunc.getBranchId(CustData.Account.BranchID.ToString());
                                odatab.Add("eqs_branchid@odata.bind", $"/eqs_branchs({branchid})");
                            }

                            if (!string.IsNullOrEmpty(CustData.Account.ModeofOperations.ToString()))
                            {
                                odatab.Add("eqs_modeofperations", CustData.Account.ModeofOperations.ToString());
                                odatab.Add("eqs_modeofoperationcode", await this._queryParser.getOptionSetTextToValue("eqs_account", "eqs_modeofoperationcode", CustData.Account.ModeofOperations.ToString()));
                            }

                            if (!string.IsNullOrEmpty(CustData.Account.ModeofOperationRemarks.ToString()))
                            {
                                odatab.Add("eqs_modeofoperationremarks", CustData.Account.ModeofOperationRemarks.ToString());
                            }

                            if (odatab.Count > 0)
                            {
                                string postDataParametr = JsonConvert.SerializeObject(odatab);
                                var Account_details = await this._queryParser.HttpApiCall($"eqs_accounts({accountid})?$select=eqs_accountid", HttpMethod.Patch, postDataParametr);
                                accountid = CommonFunction.GetIdFromPostRespons201(Account_details[0]["responsebody"], "eqs_accountid");

                                if (string.IsNullOrEmpty(accountid))
                                {
                                    this._logger.LogError("UpdateCase", JsonConvert.SerializeObject(Account_details), postDataParametr);
                                    caseRtPrm.ReturnCode = "CRM-ERROR-102";
                                    caseRtPrm.Message = "Error occurred while updating account information.";
                                }
                            }

                        }
                        else
                        {
                            caseRtPrm.ReturnCode = "CRM-ERROR-102";
                            caseRtPrm.Message = "Account details are not provided";
                        }

                        if (!string.IsNullOrEmpty(CustData.Address?.ToString()))
                        {
                            dynamic addresses = CustData.Address;
                            foreach (dynamic address in ((JArray)addresses))
                            {
                                string addresstypecode = await this._queryParser.getOptionSetTextToValue("eqs_address", "eqs_addresstypeid", address.AddressType.ToString());
                                if (string.IsNullOrEmpty(addresstypecode))
                                {
                                    caseRtPrm.ReturnCode = "CRM-ERROR-102";
                                    caseRtPrm.Message = $"Invalid Address Type '{address.AddressType.ToString()}'";
                                    return caseRtPrm;
                                }
                                string addressid = await this._commonFunc.getCustomerAddressId(customerid, addresstypecode);

                                odatab = new Dictionary<string, string>();
                                if (!string.IsNullOrEmpty(address.AddressLine1.ToString()))
                                {
                                    odatab.Add("eqs_addressline1", address.AddressLine1.ToString());
                                }

                                if (!string.IsNullOrEmpty(address.AddressLine2.ToString()))
                                {
                                    odatab.Add("eqs_addressline2", address.AddressLine2.ToString());
                                }

                                if (!string.IsNullOrEmpty(address.AddressLine3.ToString()))
                                {
                                    odatab.Add("eqs_addressline3", address.AddressLine3.ToString());
                                }

                                if (!string.IsNullOrEmpty(address.Pincode.ToString()))
                                {
                                    odatab.Add("eqs_pincode", address.Pincode.ToString());
                                }

                                if (!string.IsNullOrEmpty(address.CityId.ToString()))
                                {
                                    odatab.Add("eqs_city", address.CityId.ToString());
                                }

                                if (!string.IsNullOrEmpty(address.LandlineNumber.ToString()))
                                {
                                    odatab.Add("eqs_landlinenumber", address.LandlineNumber.ToString());
                                }

                                if (!string.IsNullOrEmpty(address.MobileNumber.ToString()))
                                {
                                    odatab.Add("eqs_mobilenumber", address.MobileNumber.ToString());
                                }

                                if (!string.IsNullOrEmpty(address.FaxNumber.ToString()))
                                {
                                    odatab.Add("eqs_faxnumber", address.FaxNumber.ToString());
                                }

                                string postDataParametr = JsonConvert.SerializeObject(odatab);
                                if (string.IsNullOrEmpty(addressid))
                                {
                                    //create
                                    var Customer_details = await this._queryParser.HttpApiCall($"eqs_addresses()?$select=eqs_addressid", HttpMethod.Post, postDataParametr);
                                    addressid = CommonFunction.GetIdFromPostRespons201(Customer_details[0]["responsebody"], "eqs_addressid");
                                }
                                else
                                {
                                    //update
                                    var Customer_details = await this._queryParser.HttpApiCall($"eqs_addresses({customerid})?$select=eqs_addressid", HttpMethod.Patch, postDataParametr);
                                    addressid = CommonFunction.GetIdFromPostRespons201(Customer_details[0]["responsebody"], "eqs_addressid");
                                }

                                if (string.IsNullOrEmpty(addressid))
                                {
                                    caseRtPrm.ReturnCode = "CRM-ERROR-102";
                                    caseRtPrm.Message = "Error occurred while updating address.";
                                    return caseRtPrm;
                                }
                            }
                        }
                    }
                    else
                    {
                        caseRtPrm.ReturnCode = "CRM-ERROR-102";
                        caseRtPrm.Message = "Customer details are not provided";
                    }

                    if (!string.IsNullOrEmpty(RequestData.OthersModification?.ToString()))
                    {
                        odatab = new Dictionary<string, string>();
                        odatab.Add("objectid_incident@odata.bind", $"/incidents({caseid})");
                        odatab.Add("notetext", RequestData.OthersModification?.ToString());

                        string noteDataParameter = JsonConvert.SerializeObject(odatab);
                        var Note_details = await this._queryParser.HttpApiCall($"annotations?$select=annotationid", HttpMethod.Post, noteDataParameter);
                        string noteid = CommonFunction.GetIdFromPostRespons201(Note_details[0]["responsebody"], "annotationid");

                        //if (string.IsNullOrEmpty(noteid))
                        //{
                        //    this._logger.LogError("UpdateCase", JsonConvert.SerializeObject(Case_details), caseDataParametr);
                        //    caseRtPrm.ReturnCode = "CRM-ERROR-102";
                        //    caseRtPrm.Message = "Error occurred while other modifications in case.";
                        //    return caseRtPrm;
                        //}
                    }
                }

                caseRtPrm.ReturnCode = "CRM-SUCCESS";
                caseRtPrm.Message = "API executed successfully";
            }
            else
            {
                caseRtPrm.ReturnCode = "CRM-ERROR-102";
                caseRtPrm.Message = "Invalid CaseId";
            }

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
