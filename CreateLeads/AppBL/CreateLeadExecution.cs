﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;
using CRMConnect;
using System.Xml;

namespace CreateLeads
{
    public class CreateLeadExecution: ICreateLeadExecution
    {

        public ILoggers _logger;       
        public IQueryParser _queryParser;
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
       
        Dictionary<string, int> LeadStatus = new Dictionary<string, int>();
        private ICommonFunction _commonFunc;

        public CreateLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {            
           
            this._logger = logger;
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

           
            this.LeadStatus.Add("Open", 0);
            this.LeadStatus.Add("Onboarded", 1);
            this.LeadStatus.Add("Not Onboarded", 2);

        }


        public async Task<LeadReturnParam> ValidateLeade(dynamic LeadData)
        {
            LeadData = await this.getRequestData(LeadData);
            LeadReturnParam ldRtPrm = new LeadReturnParam();
            try
            {
                
                string channel = LeadData.ChannelType; 

                if (!string.IsNullOrEmpty(this.Transaction_ID) && !string.IsNullOrEmpty(this.Channel_ID) && !string.IsNullOrEmpty(this.appkey) && this.appkey != "" && checkappkey(this.appkey))
                {
                    if (!string.IsNullOrEmpty(channel) && channel != "")
                    {
                        int ValidationError = 0;
                        string errorText = "";

                        if (string.Equals(LeadData.ChannelType.ToString(), "ESFB Website"))
                        {
                            if (LeadData.FirstName == null || string.IsNullOrEmpty(LeadData.FirstName.ToString()) || LeadData.FirstName.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText = "FirstName";
                            }
                            if (LeadData.LastName == null || string.IsNullOrEmpty(LeadData.LastName.ToString()) || LeadData.LastName.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText = "LastName";
                            }
                            if (LeadData.MobileNumber == null || string.IsNullOrEmpty(LeadData.MobileNumber.ToString()) || LeadData.MobileNumber.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText = "MobileNumber";
                            }
                            if (LeadData.ProductCode == null || string.IsNullOrEmpty(LeadData.ProductCode.ToString()) || LeadData.ProductCode.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText = "ProductCode";
                            }
                            

                        }
                        else if (string.Equals(LeadData.ChannelType.ToString(), "Internet Banking") || string.Equals(LeadData.ChannelType.ToString(), "Mobile Banking"))
                        {
                            if (LeadData.CustomerID == null || string.IsNullOrEmpty(LeadData.CustomerID.ToString()) || LeadData.CustomerID.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText = "CustomerID";
                            }
                        }
                        else if (string.Equals(LeadData.ChannelType.ToString(), "ChatBot"))
                        {
                            if (LeadData.Email == null || string.IsNullOrEmpty(LeadData.Email.ToString()) || LeadData.Email.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText = "Email";
                            }

                            if (LeadData.MobileNumber == null || string.IsNullOrEmpty(LeadData.MobileNumber.ToString()) || LeadData.MobileNumber.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText = "MobileNumber";
                            }

                            if (LeadData.Transcript == null || string.IsNullOrEmpty(LeadData.Transcript.ToString()) || LeadData.Transcript.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText = "Transcript";
                            }

                        }
                        else if (string.Equals(LeadData.ChannelType.ToString(), "Email"))
                        {
                            if (LeadData.Email == null || string.IsNullOrEmpty(LeadData.Email.ToString()) || LeadData.Email.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText = "Email";
                            }

                            if (LeadData.EmailBody == null || string.IsNullOrEmpty(LeadData.EmailBody.ToString()) || LeadData.EmailBody.ToString() == "")
                            {
                                ValidationError = 1;
                                errorText = "EmailBody";
                            }
                        }


                        if (ValidationError == 1)
                        {                           
                            this._logger.LogInformation("ValidateLeade", $"{errorText} is mandatory");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = $"{errorText} is mandatory";
                        }
                        else
                        {
                            ldRtPrm = await this.CreateLead(LeadData);
                        }                      


                    }
                    else
                    {
                        this._logger.LogInformation("ValidateLeade", "Channel is incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Channel is incorrect";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateLeade", "Transaction_ID or appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Transaction_ID or appkey is incorrect";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateLeade", ex.Message);
                ldRtPrm.ReturnCode = "CRM-ERROR-101";
                ldRtPrm.Message = OutputMSG.Resource_n_Found;
                return ldRtPrm;
            }
            
        }

        public bool checkappkey(string appkey)
        {
            if (this._keyVaultService.ReadSecret("CreateLeadappkey") == appkey)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        

        public async Task<LeadReturnParam> CreateLead(dynamic LeadData)
        {
            LeadReturnParam ldRtPrm = new LeadReturnParam();
            LeadMsdProperty lead_Property = new LeadMsdProperty();
            LeadProperty ldProperty = new LeadProperty();
            Dictionary<string,string> odatab= new Dictionary<string,string>();
            string postDataParametr, postDataParametr1;
            List<JObject> Lead_details = new List<JObject>();

            lead_Property.eqs_leadchannel = await this._queryParser.getOptionSetTextToValue("lead", "eqs_leadchannel", LeadData.ChannelType.ToString());
            try
            {
                if (string.Equals(LeadData.ChannelType.ToString(), "ESFB Website"))
                {                
                    var productDetails = await this._commonFunc.getProductId(LeadData.ProductCode.ToString());
                    ldProperty.ProductId = productDetails["ProductId"];
                    ldProperty.Businesscategoryid = productDetails["businesscategoryid"];
                    ldProperty.Productcategoryid = productDetails["productcategory"];
                    lead_Property.eqs_crmproductcategorycode = productDetails["crmproductcategorycode"];

                    if (ldProperty.ProductId != "")
                    {
                        lead_Property.firstname = LeadData.FirstName;
                        lead_Property.lastname = LeadData.LastName;
                        lead_Property.mobilephone = LeadData.MobileNumber;
                        lead_Property.emailaddress1 = LeadData.Email;
                        odatab.Add("eqs_productid@odata.bind", $"eqs_products({ldProperty.ProductId})");
                        odatab.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({ldProperty.Productcategoryid})");
                        odatab.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({ldProperty.Businesscategoryid})");

                        ldProperty.CityId = await this._commonFunc.getCityId(LeadData.CityCode.ToString());
                        if (ldProperty.CityId != null && ldProperty.CityId != "")
                            odatab.Add("eqs_cityid@odata.bind", $"eqs_cities({ldProperty.CityId})");

                        ldProperty.BranchId = await this._commonFunc.getBranchId(LeadData.BranchCode.ToString());
                        if (ldProperty.BranchId != null && ldProperty.BranchId != "")
                            odatab.Add("eqs_branchid@odata.bind", $"eqs_branchs({ldProperty.BranchId})");


                        if (LeadData.CustomerID != null && LeadData.CustomerID.ToString() != "")
                        {                            
                            var Customer_Detail = await this._commonFunc.getCustomerDetail(LeadData.CustomerID.ToString());
                            if (Customer_Detail.Count > 0)
                            {
                                ldProperty.ETBCustomerID = Customer_Detail[0]["contactid"];
                                lead_Property.firstname = Customer_Detail[0]["firstname"];
                                lead_Property.lastname = Customer_Detail[0]["lastname"];

                                odatab.Add("eqs_titleid@odata.bind", $"eqs_titles({Customer_Detail[0]["_eqs_titleid_value"]})");
                                if (!string.IsNullOrEmpty(Customer_Detail[0]["_eqs_businesstypeid_value"].ToString()))
                                {
                                    odatab.Add("eqs_businesstypeid@odata.bind", $"eqs_businesstypes({Customer_Detail[0]["_eqs_businesstypeid_value"]})");
                                }

                                lead_Property.eqs_ucic = Customer_Detail[0]["eqs_customerid"];
                                lead_Property.eqs_companynamepart1 = Customer_Detail[0]["eqs_companyname"];
                                lead_Property.eqs_companynamepart2 = Customer_Detail[0]["eqs_companyname2"];
                                lead_Property.eqs_companynamepart3 = Customer_Detail[0]["eqs_companyname3"];
                                lead_Property.eqs_dateofincorporation = Customer_Detail[0]["eqs_dateofincorporation"];
                                lead_Property.eqs_dob = Customer_Detail[0]["birthdate"];
                                lead_Property.eqs_gendercode = Customer_Detail[0]["eqs_gender"];
                                if (!string.IsNullOrEmpty(Customer_Detail[0]["mobilephone"].ToString()))
                                {
                                    lead_Property.mobilephone = Customer_Detail[0]["mobilephone"];
                                }
                                if (!string.IsNullOrEmpty(Customer_Detail[0]["emailaddress1"].ToString()))
                                {
                                    lead_Property.emailaddress1 = Customer_Detail[0]["emailaddress1"];
                                }

                                odatab.Add("eqs_etbcustomerid@odata.bind", $"contacts({ldProperty.ETBCustomerID})");

                                odatab.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({Customer_Detail[0]["_eqs_entitytypeid_value"].ToString()})");
                                odatab.Add("eqs_subentitytypeid@odata.bind", $"eqs_subentitytypes({Customer_Detail[0]["_eqs_subentitytypeid_value"].ToString()})");
                            }
                        }
                        else
                        {
                            string EntityTypeId = await this._commonFunc.getEntityTypeId("I");
                            string SubEntityTypeId = await this._commonFunc.getSubEntityTypeId("I");

                            odatab.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({EntityTypeId})");
                            odatab.Add("eqs_subentitytypeid@odata.bind", $"eqs_subentitytypes({SubEntityTypeId})");
                        }
                        

                        if (LeadData.Pincode != null && LeadData.Pincode.ToString() != "")
                            lead_Property.eqs_pincode = LeadData.Pincode;

                        if (LeadData.MiddleName != null && LeadData.MiddleName.ToString() != "")
                            lead_Property.middlename = LeadData.MiddleName;

                        odatab.Add("eqs_createdfromonline", "true");


                        postDataParametr = JsonConvert.SerializeObject(lead_Property);
                        postDataParametr1 = JsonConvert.SerializeObject(odatab);

                        postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                        Lead_details = await this._queryParser.HttpApiCall("leads?$select=eqs_crmleadid", HttpMethod.Post, postDataParametr);
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateLeade", "Input parameters are incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }

                }
                else if (string.Equals(LeadData.ChannelType.ToString(), "Mobile Banking") || string.Equals(LeadData.ChannelType.ToString(), "Internet Banking"))
                {
                    var productDetails = await this._commonFunc.getProductId(LeadData.ProductCode.ToString());
                    ldProperty.ProductId = productDetails["ProductId"];
                    ldProperty.Businesscategoryid = productDetails["businesscategoryid"];
                    ldProperty.Productcategoryid = productDetails["productcategory"];
                    lead_Property.eqs_crmproductcategorycode = productDetails["crmproductcategorycode"];

                    if (ldProperty.ProductId != "")
                    {
                        lead_Property.firstname = LeadData.FirstName;
                        lead_Property.lastname = LeadData.LastName;
                        lead_Property.mobilephone = LeadData.MobileNumber;
                        lead_Property.emailaddress1 = LeadData.Email;

                        odatab.Add("eqs_productid@odata.bind", $"eqs_products({ldProperty.ProductId})");
                        odatab.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({ldProperty.Productcategoryid})");
                        odatab.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({ldProperty.Businesscategoryid})");

                        ldProperty.CityId = await this._commonFunc.getCityId(LeadData.CityCode.ToString());
                        if (ldProperty.CityId != null && ldProperty.CityId != "")
                            odatab.Add("eqs_cityid@odata.bind", $"eqs_cities({ldProperty.CityId})");

                        ldProperty.BranchId = await this._commonFunc.getBranchId(LeadData.BranchCode.ToString());
                        if (ldProperty.BranchId != null && ldProperty.BranchId != "")
                            odatab.Add("eqs_branchid@odata.bind", $"eqs_branchs({ldProperty.BranchId})");

                        var Customer_Detail = await this._commonFunc.getCustomerDetail(LeadData.CustomerID.ToString());
                        if (Customer_Detail.Count > 0) {
                            ldProperty.ETBCustomerID = Customer_Detail[0]["contactid"];
                            lead_Property.firstname = Customer_Detail[0]["firstname"];
                            lead_Property.lastname = Customer_Detail[0]["lastname"];

                            odatab.Add("eqs_titleid@odata.bind", $"eqs_titles({Customer_Detail[0]["_eqs_titleid_value"]})");
                            if (!string.IsNullOrEmpty(Customer_Detail[0]["_eqs_businesstypeid_value"].ToString()))
                            {
                                odatab.Add("eqs_businesstypeid@odata.bind", $"eqs_businesstypes({Customer_Detail[0]["_eqs_businesstypeid_value"]})");
                            }                            

                            lead_Property.eqs_companynamepart1 = Customer_Detail[0]["eqs_companyname"];
                            lead_Property.eqs_companynamepart2 = Customer_Detail[0]["eqs_companyname2"];
                            lead_Property.eqs_companynamepart3 = Customer_Detail[0]["eqs_companyname3"];
                            lead_Property.eqs_dateofincorporation = Customer_Detail[0]["eqs_dateofincorporation"];
                            lead_Property.eqs_dob = Customer_Detail[0]["birthdate"];
                            lead_Property.eqs_gendercode = Customer_Detail[0]["eqs_gender"];
                            lead_Property.eqs_ucic = Customer_Detail[0]["eqs_customerid"];

                            if (!string.IsNullOrEmpty(Customer_Detail[0]["mobilephone"].ToString()))
                            {
                                lead_Property.mobilephone = Customer_Detail[0]["mobilephone"];
                            }
                            if (!string.IsNullOrEmpty(Customer_Detail[0]["emailaddress1"].ToString()))
                            {
                                lead_Property.emailaddress1 = Customer_Detail[0]["emailaddress1"];
                            }

                            odatab.Add("eqs_etbcustomerid@odata.bind", $"contacts({ldProperty.ETBCustomerID})");

                            odatab.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({Customer_Detail[0]["_eqs_entitytypeid_value"].ToString()})");
                            odatab.Add("eqs_subentitytypeid@odata.bind", $"eqs_subentitytypes({Customer_Detail[0]["_eqs_subentitytypeid_value"].ToString()})");

                            if (LeadData.Pincode != null && LeadData.Pincode.ToString() != "")
                                lead_Property.eqs_pincode = LeadData.Pincode;

                            if (LeadData.MiddleName != null && LeadData.MiddleName.ToString() != "")
                                lead_Property.middlename = LeadData.MiddleName;

                            odatab.Add("eqs_createdfromonline", "true");

                            postDataParametr = JsonConvert.SerializeObject(lead_Property);
                            postDataParametr1 = JsonConvert.SerializeObject(odatab);

                            postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                            Lead_details = await this._queryParser.HttpApiCall("leads?$select=eqs_crmleadid", HttpMethod.Post, postDataParametr);
                        }
                        else
                        {
                            this._logger.LogInformation("CreateLead", "Input parameters are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                        }

                        
                    }
                    else
                    {
                        this._logger.LogInformation("CreateLead", "Input parameters are incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else if (string.Equals(LeadData.ChannelType.ToString(), "ChatBot"))
                {
                    if (LeadData.FirstName != null && LeadData.FirstName.ToString() != "")
                        lead_Property.firstname = LeadData.FirstName;

                    if (LeadData.LastName != null && LeadData.FirstName.ToString() != "")
                        lead_Property.lastname = LeadData.LastName;

                    if (LeadData.ProductCode != null && LeadData.ProductCode.ToString() != "")
                    {
                        var productDetails = await this._commonFunc.getProductId(LeadData.ProductCode.ToString());
                        ldProperty.ProductId = productDetails["ProductId"];
                        ldProperty.Businesscategoryid = productDetails["businesscategoryid"];
                        ldProperty.Productcategoryid = productDetails["productcategory"];
                        lead_Property.eqs_crmproductcategorycode = productDetails["crmproductcategorycode"];

                        odatab.Add("eqs_productid@odata.bind", $"eqs_products({ldProperty.ProductId})");
                        odatab.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({ldProperty.Productcategoryid})");
                        odatab.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({ldProperty.Businesscategoryid})");
                    }

                    lead_Property.mobilephone = LeadData.MobileNumber;
                    lead_Property.emailaddress1 = LeadData.Email;

                    if (LeadData.CityCode != null && LeadData.CityCode.ToString() != "")
                    {
                        ldProperty.CityId = await this._commonFunc.getCityId(LeadData.CityCode.ToString());
                        if (ldProperty.CityId != null && ldProperty.CityId != "")
                            odatab.Add("eqs_cityid@odata.bind", $"eqs_cities({ldProperty.CityId})");
                    }
                    if (LeadData.BranchCode != null && LeadData.BranchCode.ToString() != "")
                    {
                        ldProperty.BranchId = await this._commonFunc.getBranchId(LeadData.BranchCode.ToString());
                        if (ldProperty.BranchId != null && ldProperty.BranchId != "")
                            odatab.Add("eqs_branchid@odata.bind", $"eqs_branchs({ldProperty.BranchId})");
                    }

                                        
                    if (LeadData.CustomerID != null && LeadData.CustomerID.ToString() != "")
                    {
                        var Customer_Detail = await this._commonFunc.getCustomerDetail(LeadData.CustomerID.ToString());
                        if (Customer_Detail.Count > 0)
                        {
                            ldProperty.ETBCustomerID = Customer_Detail[0]["contactid"];
                            lead_Property.firstname = Customer_Detail[0]["firstname"];
                            lead_Property.lastname = Customer_Detail[0]["lastname"];

                            odatab.Add("eqs_titleid@odata.bind", $"eqs_titles({Customer_Detail[0]["_eqs_titleid_value"]})");
                            if (!string.IsNullOrEmpty(Customer_Detail[0]["_eqs_businesstypeid_value"].ToString()))
                            {
                                odatab.Add("eqs_businesstypeid@odata.bind", $"eqs_businesstypes({Customer_Detail[0]["_eqs_businesstypeid_value"]})");
                            }

                            lead_Property.eqs_companynamepart1 = Customer_Detail[0]["eqs_companyname"];
                            lead_Property.eqs_companynamepart2 = Customer_Detail[0]["eqs_companyname2"];
                            lead_Property.eqs_companynamepart3 = Customer_Detail[0]["eqs_companyname3"];
                            lead_Property.eqs_dateofincorporation = Customer_Detail[0]["eqs_dateofincorporation"];
                            lead_Property.eqs_dob = Customer_Detail[0]["birthdate"];
                            lead_Property.eqs_gendercode = Customer_Detail[0]["eqs_gender"];
                            lead_Property.eqs_ucic = Customer_Detail[0]["eqs_customerid"];

                            if (!string.IsNullOrEmpty(Customer_Detail[0]["mobilephone"].ToString()))
                            {
                                lead_Property.mobilephone = Customer_Detail[0]["mobilephone"];
                            }
                            if (!string.IsNullOrEmpty(Customer_Detail[0]["emailaddress1"].ToString()))
                            {
                                lead_Property.emailaddress1 = Customer_Detail[0]["emailaddress1"];
                            }

                            odatab.Add("eqs_etbcustomerid@odata.bind", $"contacts({ldProperty.ETBCustomerID})");

                            odatab.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({Customer_Detail[0]["_eqs_entitytypeid_value"].ToString()})");
                            odatab.Add("eqs_subentitytypeid@odata.bind", $"eqs_subentitytypes({Customer_Detail[0]["_eqs_subentitytypeid_value"].ToString()})");
                        }
                    }
                    else
                    {
                        string EntityTypeId = await this._commonFunc.getEntityTypeId("I");
                        string SubEntityTypeId = await this._commonFunc.getSubEntityTypeId("I");

                        odatab.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({EntityTypeId})");
                        odatab.Add("eqs_subentitytypeid@odata.bind", $"eqs_subentitytypes({SubEntityTypeId})");
                    }

                   
                    lead_Property.description = LeadData.Transcript;

                    if (LeadData.Pincode != null && LeadData.Pincode.ToString() != "")
                        lead_Property.eqs_pincode = LeadData.Pincode;

                    if (LeadData.MiddleName != null && LeadData.MiddleName.ToString() != "")
                        lead_Property.middlename = LeadData.MiddleName;

                    odatab.Add("eqs_createdfromonline", "true");

                    postDataParametr = JsonConvert.SerializeObject(lead_Property);
                    postDataParametr1 = JsonConvert.SerializeObject(odatab);

                    postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                    Lead_details = await this._queryParser.HttpApiCall("leads?$select=eqs_crmleadid", HttpMethod.Post, postDataParametr);

                }
                else if (string.Equals(LeadData.ChannelType.ToString(), "Email"))
                {
                    if (LeadData.FirstName != null && LeadData.FirstName.ToString() != "")
                        lead_Property.firstname = LeadData.FirstName;

                    if (LeadData.LastName != null && LeadData.FirstName.ToString() != "")
                        lead_Property.lastname = LeadData.LastName;

                    if (LeadData.MobileNumber != null && LeadData.MobileNumber.ToString() != "")
                        lead_Property.mobilephone = LeadData.MobileNumber;

                    lead_Property.emailaddress1 = LeadData.Email;
                    lead_Property.description = LeadData.EmailBody;

                    if (LeadData.ProductCode != null && LeadData.ProductCode.ToString() != "")
                    {
                        
                    }
                    else
                    {
                        var productDetails = await this._commonFunc.getProductId("1005");
                        ldProperty.ProductId = productDetails["ProductId"];
                        ldProperty.Businesscategoryid = productDetails["businesscategoryid"];
                        ldProperty.Productcategoryid = productDetails["productcategory"];
                        lead_Property.eqs_crmproductcategorycode = productDetails["crmproductcategorycode"];

                        odatab.Add("eqs_productid@odata.bind", $"eqs_products({ldProperty.ProductId})");
                        odatab.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({ldProperty.Productcategoryid})");
                        odatab.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({ldProperty.Businesscategoryid})");

                    }

                   

                    if (LeadData.CityCode != null && LeadData.CityCode.ToString() != "")
                    {
                        ldProperty.CityId = await this._commonFunc.getCityId(LeadData.CityCode.ToString());
                        if (ldProperty.CityId != null && ldProperty.CityId != "")
                           odatab.Add("eqs_cityid@odata.bind", $"eqs_cities({ldProperty.CityId})");
                    }
                    if (LeadData.BranchCode != null && LeadData.BranchCode.ToString() != "")
                    {
                        ldProperty.BranchId = await this._commonFunc.getBranchId(LeadData.BranchCode.ToString());
                        if (ldProperty.BranchId != null && ldProperty.BranchId != "")
                            odatab.Add("eqs_branchid@odata.bind", $"eqs_branchs({ldProperty.BranchId})");
                    }

                    if (LeadData.CustomerID != null && LeadData.CustomerID.ToString() != "")
                    {
                        var Customer_Detail = await this._commonFunc.getCustomerDetail(LeadData.CustomerID.ToString());
                        if (Customer_Detail.Count > 0)
                        {
                            ldProperty.ETBCustomerID = Customer_Detail[0]["contactid"];
                            lead_Property.firstname = Customer_Detail[0]["firstname"];
                            lead_Property.lastname = Customer_Detail[0]["lastname"];

                            odatab.Add("eqs_titleid@odata.bind", $"eqs_titles({Customer_Detail[0]["_eqs_titleid_value"]})");
                            if (!string.IsNullOrEmpty(Customer_Detail[0]["_eqs_businesstypeid_value"].ToString()))
                            {
                                odatab.Add("eqs_businesstypeid@odata.bind", $"eqs_businesstypes({Customer_Detail[0]["_eqs_businesstypeid_value"]})");
                            }

                            lead_Property.eqs_companynamepart1 = Customer_Detail[0]["eqs_companyname"];
                            lead_Property.eqs_companynamepart2 = Customer_Detail[0]["eqs_companyname2"];
                            lead_Property.eqs_companynamepart3 = Customer_Detail[0]["eqs_companyname3"];
                            lead_Property.eqs_dateofincorporation = Customer_Detail[0]["eqs_dateofincorporation"];
                            lead_Property.eqs_dob = Customer_Detail[0]["birthdate"];
                            lead_Property.eqs_gendercode = Customer_Detail[0]["eqs_gender"];
                            lead_Property.eqs_ucic = Customer_Detail[0]["eqs_customerid"];

                            if (!string.IsNullOrEmpty(Customer_Detail[0]["mobilephone"].ToString()))
                            {
                                lead_Property.mobilephone = Customer_Detail[0]["mobilephone"];
                            }
                            if (!string.IsNullOrEmpty(Customer_Detail[0]["emailaddress1"].ToString()))
                            {
                                lead_Property.emailaddress1 = Customer_Detail[0]["emailaddress1"];
                            }

                            odatab.Add("eqs_etbcustomerid@odata.bind", $"contacts({ldProperty.ETBCustomerID})");

                            odatab.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({Customer_Detail[0]["_eqs_entitytypeid_value"].ToString()})");
                            odatab.Add("eqs_subentitytypeid@odata.bind", $"eqs_subentitytypes({Customer_Detail[0]["_eqs_subentitytypeid_value"].ToString()})");
                        }
                    }
                    else
                    {
                        string EntityTypeId = await this._commonFunc.getEntityTypeId("I");
                        string SubEntityTypeId = await this._commonFunc.getSubEntityTypeId("I");

                        odatab.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({EntityTypeId})");
                        odatab.Add("eqs_subentitytypeid@odata.bind", $"eqs_subentitytypes({SubEntityTypeId})");
                    }

                    odatab.Add("eqs_createdfromonline", "true");


                    if (LeadData.Pincode != null && LeadData.Pincode.ToString() != "")
                        lead_Property.eqs_pincode = LeadData.Pincode;

                    if (LeadData.MiddleName != null && LeadData.MiddleName.ToString() != "")
                        lead_Property.middlename = LeadData.MiddleName;

                    postDataParametr = JsonConvert.SerializeObject(lead_Property);
                    postDataParametr1 = JsonConvert.SerializeObject(odatab);

                    postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                    Lead_details = await this._queryParser.HttpApiCall("leads?$select=eqs_crmleadid", HttpMethod.Post, postDataParametr);
                }
                       


                if (Lead_details.Count >0 )
                {
                    dynamic respons_code = Lead_details[0];
                    if (respons_code.responsecode == 204)
                    {
                        ldRtPrm.LeadID = CommonFunction.GetIdFromPostRespons(respons_code.responsebody.ToString());
                        ldRtPrm.ReturnCode = "CRM - SUCCESS";
                        ldRtPrm.Message = OutputMSG.Lead_Success;
                    }
                    else if (respons_code.responsecode == 201)
                    {
                        ldRtPrm.LeadID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "eqs_crmleadid");
                        ldRtPrm.ReturnCode = "CRM - SUCCESS";
                        ldRtPrm.Message = OutputMSG.Lead_Success;
                    }
                    else
                    {
                        this._logger.LogInformation("CreateLead", JsonConvert.SerializeObject(Lead_details));
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Lead creation failed";
                    }
                }
                else
                {
                    this._logger.LogInformation("CreateLead", JsonConvert.SerializeObject(Lead_details));
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Lead creation failed";
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateLead", ex.Message);
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = OutputMSG.Incorrect_Input;
            }

            return ldRtPrm;
        }



        public async Task<string> EncriptRespons(string ResponsData)
        {
            return await _queryParser.PayloadEncryption(ResponsData, Transaction_ID, this.Bank_Code);
        }

        private async Task<dynamic> getRequestData(dynamic inputData)
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
                    rejusetJson = JsonConvert.DeserializeObject(childrenNode.Value);

                    var payload = rejusetJson.CreateLead;
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