namespace DigiCustLead
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
    using Microsoft.VisualBasic;

    public class CrDgCustLeadExecution : ICrDgCustLeadExecution
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

        public CrDgCustLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {

            this._logger = logger;

            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;


        }


        public async Task<CreateCustLeadReturn> ValidateCustLeadDetls(dynamic RequestData)
        {
            CreateCustLeadReturn ldRtPrm = new CreateCustLeadReturn();
            RequestData = await this.getRequestData(RequestData, "CreateDigiCustLead");

            if (RequestData.ErrorNo != null && RequestData.ErrorNo.ToString() == "Error99")
            {
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = "API do not have access permission!";
                return ldRtPrm;
            }

            try
            {

                if (!string.IsNullOrEmpty(this.appkey) && this.appkey != "" && checkappkey(this.appkey, "CreateDigiCustLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(this.Transaction_ID) && !string.IsNullOrEmpty(this.Channel_ID))
                    {
                        int ValidationError = 0;
                        List<string> errorText = new List<string>();

                        if (RequestData.ProductCode == null || string.IsNullOrEmpty(RequestData.ProductCode?.ToString()) || RequestData.ProductCode?.ToString() == "")
                        {
                            //ValidationError = 1;
                            //errorText.Add("ProductCode");
                            RequestData.ProductCode = "9999";
                        }
                        if (RequestData.BranchCode == null || string.IsNullOrEmpty(RequestData.BranchCode?.ToString()) || RequestData.BranchCode?.ToString() == "")
                        {
                            ValidationError = 1;
                            errorText.Add("BranchCode");
                        }
                        if (string.IsNullOrEmpty(RequestData.LeadSource?.ToString()))
                        {
                            ValidationError = 1;
                            errorText.Add("LeadSource");
                        }
                        if (string.IsNullOrEmpty(RequestData.LeadChannel?.ToString()))
                        {
                            ValidationError = 1;
                            errorText.Add("LeadChannel");
                        }
                        if (string.IsNullOrEmpty(RequestData.EntityType?.ToString()))
                        {
                            ValidationError = 1;
                            errorText.Add("EntityType");
                        }
                        //if (string.IsNullOrEmpty(RequestData.EntityFlagType?.ToString()))
                        //{
                        //    ValidationError = 1;
                        //    errorText.Add("EntityFlagType");
                        //}
                        if (string.IsNullOrEmpty(RequestData.SubEntityType?.ToString()))
                        {
                            ValidationError = 1;
                            errorText.Add("SubEntityType");
                        }

                        if (string.Equals(RequestData.EntityType?.ToString(), "Individual"))
                        {
                            var IndvData = RequestData.IndividualEntry;
                            if (IndvData.Title == null || string.IsNullOrEmpty(IndvData.Title.ToString()) || IndvData.Title.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText.Add("Title");
                            }
                            if (IndvData.FirstName == null || string.IsNullOrEmpty(IndvData.FirstName.ToString()) || IndvData.FirstName.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText.Add("FirstName");
                            }
                            //if (IndvData.LastName == null || string.IsNullOrEmpty(IndvData.LastName.ToString()) || IndvData.LastName.ToString() == "")
                            //{
                            //    ValidationError = 1;
                            //    errorText.Add("LastName");
                            //}
                            if (IndvData.MobilePhone == null || string.IsNullOrEmpty(IndvData.MobilePhone.ToString()) || IndvData.MobilePhone.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText.Add("MobilePhone");
                            }
                            if (IndvData.Dob == null || string.IsNullOrEmpty(IndvData.Dob.ToString()) || IndvData.Dob.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText.Add("Dob");
                            }
                            if (IndvData.PANForm60 == null || string.IsNullOrEmpty(IndvData.PANForm60.ToString()) || IndvData.PANForm60.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText.Add("PANForm60");
                            }
                            else
                            {
                                if (IndvData.PANForm60 == "PAN Card")
                                {
                                    if (IndvData.PAN == null || string.IsNullOrEmpty(IndvData.PAN.ToString()) || IndvData.PAN.ToString() == "")
                                    {
                                        ValidationError = 1;
                                        errorText.Add("PAN");
                                    }
                                }

                            }
                            if (!string.IsNullOrEmpty(IndvData.Passport?.ToString()))
                            {
                                if (IndvData.Passport?.ToString()?.Length > 10)
                                {
                                    ValidationError = 1;
                                    errorText.Add("Passport should not be more than 10 characters");
                                }
                            }


                        }
                        else if (string.Equals(RequestData.EntityType?.ToString(), "Corporate"))
                        {
                            var CorpData = RequestData.CorporateEntry;
                            if (CorpData.CompanyName == null || string.IsNullOrEmpty(CorpData.CompanyName.ToString()) || CorpData.CompanyName.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText.Add("CompanyName");
                            }
                            if (CorpData.PocNumber == null || string.IsNullOrEmpty(CorpData.PocNumber.ToString()) || CorpData.PocNumber.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText.Add("PocNumber");
                            }
                            if (CorpData.PocName == null || string.IsNullOrEmpty(CorpData.PocName.ToString()) || CorpData.PocName.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText.Add("PocName");
                            }
                            if (CorpData.DateOfIncorporation == null || string.IsNullOrEmpty(CorpData.DateOfIncorporation.ToString()) || CorpData.DateOfIncorporation.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText.Add("DateOfIncorporation");
                            }
                            
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateCustLeadDetls", "EntityType is incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "EntityType is incorrect";
                            return ldRtPrm;
                        }


                        if (ValidationError == 1)
                        {
                            string errfield = string.Join(",", errorText);
                            this._logger.LogInformation("ValidateCustLeadDetls", $"{errfield} field can not be null.");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = $"{errfield} field can not be null.";
                        }
                        else
                        {

                            ldRtPrm = (string.Equals(RequestData.EntityType.ToString(), "Corporate")) ? await this.createDigiCustLeadCorp(RequestData) : await this.createDigiCustLeadIndv(RequestData);
                        }

                    }
                    else
                    {
                        this._logger.LogInformation("ValidateCustLeadDetls", "Transaction_ID or Channel_ID is incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or Channel_ID is incorrect.";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateCustLeadDetls", "Appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Appkey is incorrect";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateCustLeadDetls", ex.Message);
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



        public async Task<CreateCustLeadReturn> createDigiCustLeadIndv(dynamic CustLeadData)
        {
            string Applicent_ID = "", Lead_Id = "", Pan_Number = "", leadSourceId = "";
            CreateCustLeadReturn csRtPrm = new CreateCustLeadReturn();
            CustLeadElement custLeadElement = new CustLeadElement();
            Dictionary<string, string> CRMLeadmappingFields = new Dictionary<string, string>();
            Dictionary<string, string> CRMCustomermappingFields = new Dictionary<string, string>();
            try
            {
                var productDetails = await this._commonFunc.getProductId(CustLeadData.ProductCode.ToString());
                if (!string.IsNullOrEmpty(CustLeadData.IndividualEntry.ApplicantId?.ToString()))
                {
                    var applicentDtl = await this._commonFunc.getApplicentData(CustLeadData.IndividualEntry.ApplicantId.ToString());
                    Applicent_ID = applicentDtl[0]["eqs_accountapplicantid"].ToString();
                    Lead_Id = applicentDtl[0]["eqs_leadid"]["leadid"].ToString();
                    Pan_Number = applicentDtl[0]["eqs_internalpan"].ToString();
                }
                string ProductId = productDetails["ProductId"];
                string Businesscategoryid = productDetails["businesscategoryid"];
                string Productcategoryid = productDetails["productcategory"];
                if (!string.IsNullOrEmpty(productDetails["crmproductcategorycode"]))
                {
                    custLeadElement.eqs_crmproductcategorycode = productDetails["crmproductcategorycode"];
                }

                if (ProductId != "")
                {
                    string EntityID = await this._commonFunc.getEntityID(CustLeadData.EntityType.ToString());
                    string TitleId = await this._commonFunc.getTitleId(CustLeadData.IndividualEntry.Title.ToString());
                    string SubEntityID = await this._commonFunc.getSubentitytypeID(CustLeadData.EntityFlagType.ToString(), CustLeadData.SubEntityType.ToString());

                    custLeadElement.leadsourcecode = 15;
                    custLeadElement.firstname = CustLeadData.IndividualEntry.FirstName;
                    custLeadElement.middlename = CustLeadData.IndividualEntry.MiddleName;
                    custLeadElement.lastname = CustLeadData.IndividualEntry.LastName;
                    custLeadElement.mobilephone = CustLeadData.IndividualEntry.MobilePhone;
                    custLeadElement.eqs_dob = CustLeadData.IndividualEntry.Dob;
                    custLeadElement.eqs_internalpan = CustLeadData.IndividualEntry.PAN;

                    if (!string.IsNullOrEmpty(CustLeadData.LeadSource?.ToString()))
                    {
                        leadSourceId = await this._commonFunc.getLeadsourceId(CustLeadData.LeadSource?.ToString());
                    }
                    else
                    {
                        leadSourceId = await this._commonFunc.getLeadsourceId("4");
                    }

                    if (!string.IsNullOrEmpty(leadSourceId))
                    {
                        CRMLeadmappingFields.Add("eqs_leadsourceid@odata.bind", $"eqs_leadsources({leadSourceId})");
                    }
                    if (!string.IsNullOrEmpty(CustLeadData.LeadChannel?.ToString()))
                    {
                        CRMLeadmappingFields.Add("eqs_leadchannel", await this._queryParser.getOptionSetTextToValue("lead", "eqs_leadchannel", CustLeadData.LeadChannel.ToString()));
                    }
                    else
                    {
                        CRMLeadmappingFields.Add("eqs_leadchannel", "789030000");
                    }


                    CRMLeadmappingFields.Add("eqs_panform60code", await this._queryParser.getOptionSetTextToValue("lead", "eqs_panform60code", CustLeadData.IndividualEntry.PANForm60.ToString()));

                    if (!string.IsNullOrEmpty(CustLeadData?.IndividualEntry?.PAN?.ToString()) && !string.IsNullOrEmpty(CustLeadData?.IndividualEntry?.PANForm60?.ToString()))
                    {
                        if (CustLeadData?.IndividualEntry?.PANForm60?.ToString() == "PAN Card")
                        {
                            CRMLeadmappingFields.Add("eqs_pan", "**********");
                        }
                    }
                    CRMLeadmappingFields.Add("eqs_titleid@odata.bind", $"eqs_titles({TitleId})");
                    CRMLeadmappingFields.Add("eqs_productid@odata.bind", $"eqs_products({ProductId})");
                    CRMLeadmappingFields.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({Productcategoryid})");
                    CRMLeadmappingFields.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({Businesscategoryid})");
                    CRMLeadmappingFields.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({EntityID})");
                    CRMLeadmappingFields.Add("eqs_subentitytypeid@odata.bind", $"eqs_subentitytypes({SubEntityID})");

                    if (!string.IsNullOrEmpty(CustLeadData.IndividualEntry.AadharReference?.ToString()))
                    {
                        CRMLeadmappingFields.Add("eqs_aadhaarreference", CustLeadData.IndividualEntry.AadharReference.ToString());
                    }                    

                    CRMLeadmappingFields.Add("eqs_createdfrompartnerchannel", "true");
                    CRMLeadmappingFields.Add("eqs_createdfromonline", "true");

                    if (!string.IsNullOrEmpty(CustLeadData.IndividualEntry.Pincode?.ToString()))
                    {
                        custLeadElement.eqs_pincode = CustLeadData.IndividualEntry.Pincode;
                    }
                        

                    if (!string.IsNullOrEmpty(CustLeadData.IndividualEntry.Voterid?.ToString()))
                    {
                        custLeadElement.eqs_voterid = CustLeadData.IndividualEntry.Voterid;
                    }                        

                    if (!string.IsNullOrEmpty(CustLeadData.IndividualEntry.Drivinglicense?.ToString()))
                    {
                        custLeadElement.eqs_dlnumber = CustLeadData.IndividualEntry.Drivinglicense;
                    }
                        
                    if (!string.IsNullOrEmpty(CustLeadData.IndividualEntry.Passport?.ToString()))
                    {
                        custLeadElement.eqs_passportnumber = CustLeadData.IndividualEntry.Passport;
                    }                       

                    if (!string.IsNullOrEmpty(CustLeadData.IndividualEntry.CKYCNumber?.ToString()))
                    {
                        custLeadElement.eqs_ckycnumber = CustLeadData.IndividualEntry.CKYCNumber;
                    }
                       

                    string BranchId = await this._commonFunc.getBranchId(CustLeadData.BranchCode.ToString());
                    if (BranchId != null && BranchId != "")
                    {
                        CRMLeadmappingFields.Add("eqs_branchid@odata.bind", $"eqs_branchs({BranchId})");
                        CRMCustomermappingFields.Add("eqs_branchid@odata.bind", $"eqs_branchs({BranchId})");
                    }

                    if (!string.IsNullOrEmpty(CustLeadData.IndividualEntry?.MotherMaidenName?.ToString()))
                    {
                        CRMLeadmappingFields.Add("eqs_mothermaidenname", CustLeadData.IndividualEntry?.MotherMaidenName?.ToString());
                        CRMCustomermappingFields.Add("eqs_mothermaidenname", CustLeadData.IndividualEntry?.MotherMaidenName?.ToString());
                    }

                    string purpose = "";
                    if (!string.IsNullOrEmpty(CustLeadData.IndividualEntry.PurposeOfCreation?.ToString()))
                    {
                        purpose = await this._commonFunc.getPurposeID(CustLeadData.IndividualEntry.PurposeOfCreation.ToString());
                    }

                    if (!string.IsNullOrEmpty(purpose))
                    {
                        CRMLeadmappingFields.Add("eqs_purposeofcreationid@odata.bind", $"eqs_purposeofcreations({purpose})");
                        CRMCustomermappingFields.Add("eqs_purposeofcreationid@odata.bind", $"eqs_purposeofcreations({purpose})");
                    }

                    string postDataParametr = JsonConvert.SerializeObject(custLeadElement);
                    string postDataParametr1 = JsonConvert.SerializeObject(CRMLeadmappingFields);

                    postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);
                    List<JObject> Lead_details;
                    if (Lead_Id == "")
                    {
                        Lead_details = await this._queryParser.HttpApiCall("leads?$select=eqs_crmleadid", HttpMethod.Post, postDataParametr);
                    }
                    else
                    {
                        Lead_details = await this._queryParser.HttpApiCall($"leads({Lead_Id})?$select=eqs_crmleadid", HttpMethod.Patch, postDataParametr);
                    }
                                        

                    CRMCustomermappingFields.Add("eqs_titleid@odata.bind", $"eqs_titles({TitleId})");
                    CRMCustomermappingFields.Add("eqs_firstname", custLeadElement.firstname);                   
                    CRMCustomermappingFields.Add("eqs_lastname", custLeadElement.lastname);
                    CRMCustomermappingFields.Add("eqs_name", custLeadElement.firstname + " " + custLeadElement.middlename + " " + custLeadElement.lastname);
                    CRMCustomermappingFields.Add("eqs_mobilenumber", custLeadElement.mobilephone);
                    CRMCustomermappingFields.Add("eqs_dob", custLeadElement.eqs_dob);
                    if (!string.IsNullOrEmpty(custLeadElement.eqs_dob))
                    {
                        int age = GetAgeInYears(custLeadElement.eqs_dob);
                        CRMCustomermappingFields.Add("eqs_leadage", age.ToString());
                    }

                    if (!string.IsNullOrEmpty(leadSourceId))
                    {
                        CRMCustomermappingFields.Add("eqs_leadsourceid@odata.bind", $"eqs_leadsources({leadSourceId})");
                    }

                    if (!string.IsNullOrEmpty(CustLeadData.LeadChannel?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_leadchannelnew", await this._queryParser.getOptionSetTextToValue("eqs_accountapplicant", "eqs_leadchannelnew", CustLeadData.LeadChannel.ToString()));
                    }
                    else
                    {
                        CRMCustomermappingFields.Add("eqs_leadchannelnew", "789030000");
                    }
                    CRMCustomermappingFields.Add("eqs_panform60code", await this._queryParser.getOptionSetTextToValue("eqs_accountapplicant", "eqs_panform60code", CustLeadData.IndividualEntry.PANForm60.ToString()));

                    if (!string.IsNullOrEmpty(custLeadElement?.eqs_internalpan?.ToString()) && !string.IsNullOrEmpty(CustLeadData.IndividualEntry?.PANForm60?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_internalpan", custLeadElement.eqs_internalpan);

                        if (CustLeadData.IndividualEntry?.PANForm60?.ToString() == "PAN Card")
                        {
                            CRMCustomermappingFields.Add("eqs_pan", "**********");
                        }
                    }
                    if (!string.IsNullOrEmpty(Applicent_ID?.ToString()))
                    {
                        if (custLeadElement?.eqs_internalpan?.ToString() != Pan_Number)
                        {
                            CRMCustomermappingFields.Add("eqs_panvalidationmode", "958570001");
                        }
                    }
                    if (!string.IsNullOrEmpty(custLeadElement.middlename?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_middlename", custLeadElement.middlename);
                    }
                    if (!string.IsNullOrEmpty(custLeadElement.eqs_passportnumber?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_passportnumber", custLeadElement.eqs_passportnumber);
                    }
                    if (!string.IsNullOrEmpty(custLeadElement.eqs_voterid?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_voterid", custLeadElement.eqs_voterid);
                    }
                    if (!string.IsNullOrEmpty(custLeadElement.eqs_dlnumber?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_dlnumber", custLeadElement.eqs_dlnumber);
                    }
                    if (!string.IsNullOrEmpty(custLeadElement.eqs_ckycnumber?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_ckycnumber", custLeadElement.eqs_ckycnumber);
                    }
                    if (!string.IsNullOrEmpty(CustLeadData.IndividualEntry.AadharReference?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_aadhaarreference", CustLeadData.IndividualEntry.AadharReference.ToString());
                    }
                    

                    CRMCustomermappingFields.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({EntityID})");
                    CRMCustomermappingFields.Add("eqs_subentity@odata.bind", $"eqs_subentitytypes({SubEntityID})");
                    CRMCustomermappingFields.Add("eqs_productid@odata.bind", $"eqs_products({ProductId})");
                    CRMCustomermappingFields.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({Productcategoryid})");
                    CRMCustomermappingFields.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({Businesscategoryid})");

                    if (Lead_details.Count > 0)
                    {
                        dynamic respons_code = Lead_details[0];
                        if (respons_code.responsecode == 201 || respons_code.responsecode == 200)
                        {
                            string LeadID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "eqs_crmleadid");
                            string Lead_ID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "leadid");
                            CRMCustomermappingFields.Add("eqs_leadid@odata.bind", $"leads({Lead_ID})");
                            postDataParametr = JsonConvert.SerializeObject(CRMCustomermappingFields);
                            List<JObject> Customer_details;
                            if (string.IsNullOrEmpty(Applicent_ID))
                            {
                                Customer_details = await this._queryParser.HttpApiCall("eqs_accountapplicants()?$select=eqs_applicantid", HttpMethod.Post, postDataParametr);
                            }
                            else
                            {
                                Customer_details = await this._queryParser.HttpApiCall($"eqs_accountapplicants({Applicent_ID})?$select=eqs_applicantid", HttpMethod.Patch, postDataParametr);
                            }


                            if (Customer_details.Count > 0)
                            {
                                respons_code = Customer_details[0];
                                if (respons_code.responsecode == 201 || respons_code.responsecode == 200)
                                {
                                    string applicantID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "eqs_applicantid");
                                    csRtPrm.ReturnCode = "CRM-SUCCESS";
                                    csRtPrm.AccountapplicantID = applicantID;
                                    csRtPrm.LeadID = LeadID;
                                }
                                else
                                {
                                    this._logger.LogError("createDigiCustLeadIndv", Lead_details.ToString());
                                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                                    csRtPrm.Message = OutputMSG.Incorrect_Input;
                                }
                            }
                        }
                        else if (respons_code.responsecode == 400)
                        {
                            this._logger.LogInformation("CreateLead", JsonConvert.SerializeObject(Lead_details));
                            csRtPrm.ReturnCode = "CRM-ERROR-102";
                            csRtPrm.Message = $"Lead creation failed.{respons_code.responsebody.error.message.ToString()}";
                        }

                    }
                    else
                    {
                        this._logger.LogInformation("createDigiCustLeadIndv", "Input parameters are incorrect");
                        csRtPrm.ReturnCode = "CRM-ERROR-102";
                        csRtPrm.Message = OutputMSG.Incorrect_Input;
                    }

                }
                else
                {
                    this._logger.LogInformation("createDigiCustLeadIndv", "Input parameters are incorrect");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = OutputMSG.Incorrect_Input;
                }


            }
            catch (Exception ex)
            {
                this._logger.LogError("createDigiCustLeadIndv", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = OutputMSG.Incorrect_Input;
            }



            return csRtPrm;
        }

        public async Task<CreateCustLeadReturn> createDigiCustLeadCorp(dynamic CustLeadData)
        {
            string Applicent_ID = "", Pan_Number = "", leadSourceId = "";
            CreateCustLeadReturn csRtPrm = new CreateCustLeadReturn();
            CustLeadElementCorp custLeadElement = new CustLeadElementCorp();
            Dictionary<string, string> CRMLeadmappingFields = new Dictionary<string, string>();
            Dictionary<string, string> CRMCustomermappingFields = new Dictionary<string, string>();
            try
            {
                if (!string.IsNullOrEmpty(CustLeadData.CorporateEntry?.ApplicantId?.ToString()))
                {
                    var applicentDtl = await this._commonFunc.getApplicentData(CustLeadData.CorporateEntry?.ApplicantId?.ToString());
                    Applicent_ID = applicentDtl[0]["eqs_accountapplicantid"].ToString();
                    Pan_Number = applicentDtl[0]["eqs_internalpan"].ToString();
                }
                var productDetails = await this._commonFunc.getProductId(CustLeadData.ProductCode.ToString());
                string ProductId = productDetails["ProductId"];
                string Businesscategoryid = productDetails["businesscategoryid"];
                string Productcategoryid = productDetails["productcategory"];
                if (!string.IsNullOrEmpty(productDetails["crmproductcategorycode"]))
                {
                    custLeadElement.eqs_crmproductcategorycode = productDetails["crmproductcategorycode"];
                }

                if (ProductId != "")
                {
                    string EntityID = await this._commonFunc.getEntityID(CustLeadData.EntityType.ToString());
                    string TitleId = await this._commonFunc.getTitleId(CustLeadData.CorporateEntry?.Title?.ToString());
                    string SubEntityID = await this._commonFunc.getSubentitytypeID(CustLeadData.EntityFlagType.ToString(), CustLeadData.SubEntityType.ToString());
                    custLeadElement.leadsourcecode = 15;
                    custLeadElement.eqs_companynamepart1 = CustLeadData.CorporateEntry.CompanyName;
                    custLeadElement.eqs_companynamepart2 = CustLeadData.CorporateEntry.CompanyName2;
                    custLeadElement.eqs_companynamepart3 = CustLeadData.CorporateEntry.CompanyName3;
                    custLeadElement.eqs_contactmobile = CustLeadData.CorporateEntry.PocNumber;
                    custLeadElement.eqs_contactperson = CustLeadData.CorporateEntry.PocName;

                    CRMLeadmappingFields.Add("eqs_titleid@odata.bind", $"eqs_titles({TitleId})");
                    if (!string.IsNullOrEmpty(CustLeadData.CorporateEntry.CinNumber?.ToString()))
                    {
                        custLeadElement.eqs_cinnumber = CustLeadData.CorporateEntry.CinNumber;
                    }
                    if (!string.IsNullOrEmpty(CustLeadData.CorporateEntry.TanNumber?.ToString()))
                    {
                        custLeadElement.eqs_tannumber = CustLeadData.CorporateEntry.TanNumber;
                    }
                    if (!string.IsNullOrEmpty(CustLeadData.CorporateEntry.GstNumber?.ToString()))
                    {
                        custLeadElement.eqs_gstnumber = CustLeadData.CorporateEntry.GstNumber;
                    }
                    if (!string.IsNullOrEmpty(CustLeadData.CorporateEntry.CstNumber?.ToString()))
                    {   
                        custLeadElement.eqs_cstvatnumber = CustLeadData.CorporateEntry.CstNumber;
                    }
                    if (!string.IsNullOrEmpty(CustLeadData.CorporateEntry.PAN?.ToString()))
                    {
                        custLeadElement.eqs_internalpan = CustLeadData.CorporateEntry.PAN;
                    }

                    if (!string.IsNullOrEmpty(CustLeadData.LeadSource?.ToString()))
                    {
                        leadSourceId = await this._commonFunc.getLeadsourceId(CustLeadData.LeadSource?.ToString());
                    }
                    else
                    {
                        leadSourceId = await this._commonFunc.getLeadsourceId("4");
                    }

                    if (!string.IsNullOrEmpty(leadSourceId))
                    {
                        CRMLeadmappingFields.Add("eqs_leadsourceid@odata.bind", $"eqs_leadsources({leadSourceId})");
                    }
                    if (!string.IsNullOrEmpty(CustLeadData.LeadChannel?.ToString()))
                    {
                        CRMLeadmappingFields.Add("eqs_leadchannel", await this._queryParser.getOptionSetTextToValue("lead", "eqs_leadchannel", CustLeadData.LeadChannel.ToString()));
                    }
                    else
                    {
                        CRMLeadmappingFields.Add("eqs_leadchannel", "789030000");
                    }
                    CRMLeadmappingFields.Add("eqs_createdfrompartnerchannel", "true");
                    CRMLeadmappingFields.Add("firstname", CustLeadData.CorporateEntry.CompanyName.ToString());
                    CRMLeadmappingFields.Add("lastname", CustLeadData.CorporateEntry.CompanyName2.ToString());
                    // CRMLeadmappingFields.Add("yomifullname", CustLeadData.eqs_companynamepart1 + " " + CustLeadData.eqs_companynamepart2);
                    CRMLeadmappingFields.Add("eqs_panform60code", "615290000");

                    if (!string.IsNullOrEmpty(CustLeadData?.CorporateEntry?.PAN?.ToString()))
                    {

                        CRMLeadmappingFields.Add("eqs_pan", "**********");
                    }

                    CRMLeadmappingFields.Add("eqs_productid@odata.bind", $"eqs_products({ProductId})");
                    CRMLeadmappingFields.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({Productcategoryid})");
                    CRMLeadmappingFields.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({Businesscategoryid})");
                    CRMLeadmappingFields.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({EntityID})");
                    CRMLeadmappingFields.Add("eqs_subentitytypeid@odata.bind", $"eqs_subentitytypes({SubEntityID})");

                    string BranchId = await this._commonFunc.getBranchId(CustLeadData.BranchCode.ToString());
                    if (BranchId != null && BranchId != "")
                    {
                        CRMLeadmappingFields.Add("eqs_branchid@odata.bind", $"eqs_branchs({BranchId})");
                        CRMCustomermappingFields.Add("eqs_branchid@odata.bind", $"eqs_branchs({BranchId})");
                    }

                    string postDataParametr = JsonConvert.SerializeObject(custLeadElement);
                    string postDataParametr1 = JsonConvert.SerializeObject(CRMLeadmappingFields);

                    postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                    List<JObject> Lead_details = await this._queryParser.HttpApiCall("leads?$select=eqs_crmleadid", HttpMethod.Post, postDataParametr);

                    string purpose = await this._commonFunc.getPurposeID(CustLeadData.CorporateEntry.PurposeOfCreation.ToString());

                    CRMCustomermappingFields.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({EntityID})");
                    CRMCustomermappingFields.Add("eqs_subentity@odata.bind", $"eqs_subentitytypes({SubEntityID})");
                    CRMCustomermappingFields.Add("eqs_companynamepart1", CustLeadData.CorporateEntry.CompanyName?.ToString());
                    CRMCustomermappingFields.Add("eqs_companynamepart2", CustLeadData.CorporateEntry.CompanyName2?.ToString());
                    CRMCustomermappingFields.Add("eqs_companynamepart3", CustLeadData.CorporateEntry.CompanyName3?.ToString());
                    CRMCustomermappingFields.Add("eqs_contactperson", CustLeadData.CorporateEntry.PocName?.ToString());
                    CRMCustomermappingFields.Add("eqs_contactmobilenumber", CustLeadData.CorporateEntry.PocNumber?.ToString());
                    CRMCustomermappingFields.Add("eqs_dateofincorporation", CustLeadData.CorporateEntry.DateOfIncorporation?.ToString());
                    

                    if (!string.IsNullOrEmpty(CustLeadData.CorporateEntry.CinNumber?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_cinnumber", CustLeadData.CorporateEntry.CinNumber.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustLeadData.CorporateEntry.TanNumber?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_tannumber", CustLeadData.CorporateEntry.TanNumber.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustLeadData.CorporateEntry.GstNumber?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_gstnumber", CustLeadData.CorporateEntry.GstNumber.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustLeadData.CorporateEntry.CstNumber?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_cstvatnumber", CustLeadData.CorporateEntry.CstNumber.ToString());
                    }
                    if (!string.IsNullOrEmpty(leadSourceId))
                    {
                        CRMCustomermappingFields.Add("eqs_leadsourceid@odata.bind", $"eqs_leadsources({leadSourceId})");
                    }

                    if (!string.IsNullOrEmpty(CustLeadData.LeadChannel?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_leadchannelnew", await this._queryParser.getOptionSetTextToValue("eqs_accountapplicant", "eqs_leadchannelnew", CustLeadData.LeadChannel.ToString()));
                    }
                    else
                    {
                        CRMCustomermappingFields.Add("eqs_leadchannelnew", "789030000");
                    }
                    CRMCustomermappingFields.Add("eqs_productid@odata.bind", $"eqs_products({ProductId})");
                    CRMCustomermappingFields.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({Productcategoryid})");
                    CRMCustomermappingFields.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({Businesscategoryid})");
                    CRMCustomermappingFields.Add("eqs_titleid@odata.bind", $"eqs_titles({TitleId})");

                    CRMCustomermappingFields.Add("eqs_panform60code", "615290000");

                    if (!string.IsNullOrEmpty(Applicent_ID?.ToString()))
                    {
                        if (custLeadElement?.eqs_internalpan?.ToString() != Pan_Number)
                        {
                            CRMCustomermappingFields.Add("eqs_panvalidationmode", "958570001");
                        }
                    }
                    CRMCustomermappingFields.Add("eqs_internalpan", custLeadElement.eqs_internalpan);

                    if (!string.IsNullOrEmpty(custLeadElement?.eqs_internalpan?.ToString()))
                    {
                        CRMCustomermappingFields.Add("eqs_pan", "**********");
                    }

                    if (!string.IsNullOrEmpty(purpose) && purpose != "")
                    {
                        CRMCustomermappingFields.Add("eqs_purposeofcreationid@odata.bind", $"eqs_purposeofcreations({purpose})");
                    }


                    if (Lead_details.Count > 0)
                    {
                        dynamic respons_code = Lead_details[0];
                        if (respons_code.responsecode == 201)
                        {
                            string LeadID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "eqs_crmleadid");
                            string Lead_ID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "leadid");
                            CRMCustomermappingFields.Add("eqs_leadid@odata.bind", $"leads({Lead_ID})");
                            postDataParametr = JsonConvert.SerializeObject(CRMCustomermappingFields);
                            List<JObject> Customer_details = await this._queryParser.HttpApiCall("eqs_accountapplicants?$select=eqs_applicantid", HttpMethod.Post, postDataParametr);

                            if (Customer_details.Count > 0)
                            {
                                respons_code = Customer_details[0];
                                if (respons_code.responsecode == 201)
                                {
                                    string applicantID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "eqs_applicantid");
                                    csRtPrm.ReturnCode = "CRM-SUCCESS";
                                    csRtPrm.AccountapplicantID = applicantID;
                                    csRtPrm.LeadID = LeadID;
                                }
                                else
                                {
                                    this._logger.LogError("createDigiCustLeadCorp", Lead_details.ToString());
                                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                                    csRtPrm.Message = OutputMSG.Incorrect_Input;
                                }
                            }
                        }

                    }
                    else
                    {
                        this._logger.LogInformation("createDigiCustLeadCorp", "Input parameters are incorrect");
                        csRtPrm.ReturnCode = "CRM-ERROR-102";
                        csRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("createDigiCustLeadCorp", "Input parameters are incorrect");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = OutputMSG.Incorrect_Input;
                }

            }
            catch (Exception ex)
            {
                this._logger.LogError("createDigiCustLeadCorp", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = OutputMSG.Incorrect_Input;
            }

            return csRtPrm;
        }

        private int GetAgeInYears(string dobstring)
        {
            DateTime dob = Convert.ToDateTime(dobstring);
            TimeSpan diff = DateTime.Today - dob;

            DateTime zerodate = new DateTime(1, 1, 1);
            return (zerodate + diff).Year - 1;
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

                    var payload = rejusetJson.CreateDigiCustLead;
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