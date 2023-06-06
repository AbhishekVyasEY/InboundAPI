using Microsoft.Extensions.Logging;
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
        Dictionary<string, int> Channel = new Dictionary<string, int>();
        Dictionary<string, int> LeadStatus = new Dictionary<string, int>();
        private ICommonFunction _commonFunc;

        public CreateLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {            
           
            this._logger = logger;
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

            this.Channel.Add("ESFBWebsite", 6);
            this.Channel.Add("ChatBot", 8);
            this.Channel.Add("Email", 3);
            this.Channel.Add("MobileBanking", 4);
            this.Channel.Add("InternetBanking", 5);
           // this.Channel.Add("Selfie", 15);

            this.LeadStatus.Add("Open", 0);
            this.LeadStatus.Add("Onboarded", 1);
            this.LeadStatus.Add("Not Onboarded", 2);

        }


        public async Task<LeadReturnParam> ValidateLeade(dynamic LeadData,string appkey)
        {
            LeadData = await this.getRequestData(LeadData);
            LeadReturnParam ldRtPrm = new LeadReturnParam();
            try
            {
                string channel = LeadData.ChannelType;

                if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID) && !string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey))
                {
                    if (!string.IsNullOrEmpty(channel) && channel != "")
                    {
                        int ValidationError = 0;

                        if (string.Equals(LeadData.ChannelType.ToString(), "InternetBanking") || string.Equals(LeadData.ChannelType.ToString(), "MobileBanking") || string.Equals(LeadData.ChannelType.ToString(), "ESFBWebsite"))
                        {
                            if (LeadData.FirstName == null || string.IsNullOrEmpty(LeadData.FirstName.ToString()) || LeadData.FirstName.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (LeadData.LastName == null || string.IsNullOrEmpty(LeadData.LastName.ToString()) || LeadData.LastName.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (LeadData.MobileNumber == null || string.IsNullOrEmpty(LeadData.MobileNumber.ToString()) || LeadData.MobileNumber.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (LeadData.ProductCode == null || string.IsNullOrEmpty(LeadData.ProductCode.ToString()) || LeadData.ProductCode.ToString() == "")
                            {
                                ValidationError = 1;
                            }

                            if (string.Equals(LeadData.ChannelType.ToString(), "InternetBanking") || string.Equals(LeadData.ChannelType.ToString(), "MobileBanking"))
                            {
                                if (LeadData.CustomerID == null || string.IsNullOrEmpty(LeadData.CustomerID.ToString()) || LeadData.CustomerID.ToString() == "")
                                {
                                    ValidationError = 1;
                                }
                            }

                        }
                        else if (string.Equals(LeadData.ChannelType.ToString(), "ChatBot"))
                        {
                            if (LeadData.Email == null || string.IsNullOrEmpty(LeadData.Email.ToString()) || LeadData.Email.ToString() == "")
                            {
                                ValidationError = 1;
                            }

                            if (LeadData.MobileNumber == null || string.IsNullOrEmpty(LeadData.MobileNumber.ToString()) || LeadData.MobileNumber.ToString() == "")
                            {
                                ValidationError = 1;
                            }

                            if (LeadData.Transcript == null || string.IsNullOrEmpty(LeadData.Transcript.ToString()) || LeadData.Transcript.ToString() == "")
                            {
                                ValidationError = 1;
                            }

                        }
                        else if (string.Equals(LeadData.ChannelType.ToString(), "Email"))
                        {
                            if (LeadData.Email == null || string.IsNullOrEmpty(LeadData.Email.ToString()) || LeadData.Email.ToString() == "")
                            {
                                ValidationError = 1;
                            }

                            if (LeadData.EmailBody == null || string.IsNullOrEmpty(LeadData.EmailBody.ToString()) || LeadData.EmailBody.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                        }


                        if (ValidationError == 1)
                        {                           
                            this._logger.LogInformation("ValidateLeade", "Input parameters are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                        }
                        else
                        {
                            ldRtPrm = await this.CreateLead(LeadData);
                        }                      


                    }
                    else
                    {
                        this._logger.LogInformation("ValidateLeade", "Input parameters are incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateLeade", "Input parameters are incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateLead", ex.Message);
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

        public async Task<LeadReturnParam> ValidateLeadeStatus(dynamic LeadStatus)
        {
            LeadReturnParam ldRtPrm = new LeadReturnParam();
            int ValidationError = 0;
            try
            {

                if (LeadStatus.LeadId == null || string.IsNullOrEmpty(LeadStatus.LeadId.ToString()) || LeadStatus.LeadId.ToString() == "")
                {
                    ValidationError = 1;
                }

                if (LeadStatus.Status == null || string.IsNullOrEmpty(LeadStatus.Status.ToString()) || LeadStatus.Status.ToString() == "")
                {
                    ValidationError = 1;
                }

                if (ValidationError == 1)
                {
                    this._logger.LogInformation("ValidateLeadeStatus", "Input parameters are incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                }

                ldRtPrm = await this.UpdateLead(LeadStatus);

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("LeadeStatus", ex.Message);
                ldRtPrm.ReturnCode = "CRM-ERROR-101";
                ldRtPrm.Message = OutputMSG.Resource_n_Found;
                return ldRtPrm;
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

            lead_Property.leadsourcecode = this.Channel[LeadData.ChannelType.ToString()];
            try
            {
                if (string.Equals(LeadData.ChannelType.ToString(), "ESFBWebsite"))
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

                        ldProperty.CityId = await this._commonFunc.getCityId(LeadData.CityName.ToString());
                        if (ldProperty.CityId != null && ldProperty.CityId != "")
                            odatab.Add("eqs_cityid@odata.bind", $"eqs_cities({ldProperty.CityId})");

                        ldProperty.BranchId = await this._commonFunc.getBranchId(LeadData.BranchCode.ToString());
                        if (ldProperty.BranchId != null && ldProperty.BranchId != "")
                            odatab.Add("eqs_branchid@odata.bind", $"eqs_branchs({ldProperty.BranchId})");


                        if (LeadData.CustomerID != null && LeadData.CustomerID.ToString() != "")
                        {
                            ldProperty.ETBCustomerID = await this._commonFunc.getCustomerId(LeadData.CustomerID.ToString());
                            if (ldProperty.ETBCustomerID != null && ldProperty.ETBCustomerID != "")
                                odatab.Add("eqs_etbcustomerid@odata.bind", $"contacts({ldProperty.ETBCustomerID})");
                        }


                        if (LeadData.Pincode != null && LeadData.Pincode.ToString() != "")
                            lead_Property.eqs_pincode = LeadData.Pincode;

                        if (LeadData.MiddleName != null && LeadData.MiddleName.ToString() != "")
                            lead_Property.middlename = LeadData.MiddleName;


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
                else if (string.Equals(LeadData.ChannelType.ToString(), "MobileBanking") || string.Equals(LeadData.ChannelType.ToString(), "InternetBanking"))
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

                        ldProperty.CityId = await this._commonFunc.getCityId(LeadData.CityName.ToString());
                        if (ldProperty.CityId != null && ldProperty.CityId != "")
                            odatab.Add("eqs_cityid@odata.bind", $"eqs_cities({ldProperty.CityId})");

                        ldProperty.BranchId = await this._commonFunc.getBranchId(LeadData.BranchCode.ToString());
                        if (ldProperty.BranchId != null && ldProperty.BranchId != "")
                            odatab.Add("eqs_branchid@odata.bind", $"eqs_branchs({ldProperty.BranchId})");

                        ldProperty.ETBCustomerID = await this._commonFunc.getCustomerId(LeadData.CustomerID.ToString());
                        if (ldProperty.ETBCustomerID != null && ldProperty.ETBCustomerID != "")
                            odatab.Add("eqs_etbcustomerid@odata.bind", $"contacts({ldProperty.ETBCustomerID})");

                        if (LeadData.Pincode != null && LeadData.Pincode.ToString() != "")
                            lead_Property.eqs_pincode = LeadData.Pincode;

                        if (LeadData.MiddleName != null && LeadData.MiddleName.ToString() != "")
                            lead_Property.middlename = LeadData.MiddleName;

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

                    if (LeadData.CustomerID != null && LeadData.CustomerID.ToString() != "")
                    {
                        ldProperty.ETBCustomerID = await this._commonFunc.getCustomerId(LeadData.CustomerID.ToString());
                        if (ldProperty.ETBCustomerID != null && ldProperty.ETBCustomerID != "")
                            odatab.Add("eqs_etbcustomerid@odata.bind", $"contacts({ldProperty.ETBCustomerID})");
                    }

                    if (LeadData.CityName != null && LeadData.CityName.ToString() != "")
                    {
                        ldProperty.CityId = await this._commonFunc.getCityId(LeadData.CityName.ToString());
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
                        ldProperty.ETBCustomerID = await this._commonFunc.getCustomerId(LeadData.CustomerID.ToString());
                        if (ldProperty.ETBCustomerID != null && ldProperty.ETBCustomerID != "")
                            odatab.Add("eqs_etbcustomerid@odata.bind", $"contacts({ldProperty.ETBCustomerID})");
                    }

                    lead_Property.mobilephone = LeadData.MobileNumber;
                    lead_Property.emailaddress1 = LeadData.Email;
                    lead_Property.description = LeadData.Transcript;

                    if (LeadData.Pincode != null && LeadData.Pincode.ToString() != "")
                        lead_Property.eqs_pincode = LeadData.Pincode;

                    if (LeadData.MiddleName != null && LeadData.MiddleName.ToString() != "")
                        lead_Property.middlename = LeadData.MiddleName;

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
                        var productDetails = await this._commonFunc.getProductId(LeadData.ProductCode.ToString());
                        ldProperty.ProductId = productDetails["ProductId"];
                        ldProperty.Businesscategoryid = productDetails["businesscategoryid"];
                        ldProperty.Productcategoryid = productDetails["productcategory"];
                        lead_Property.eqs_crmproductcategorycode = productDetails["crmproductcategorycode"];

                        odatab.Add("eqs_productid@odata.bind", $"eqs_products({ldProperty.ProductId})");
                        odatab.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({ldProperty.Productcategoryid})");
                        odatab.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({ldProperty.Businesscategoryid})");
                    }

                    if (LeadData.CustomerID != null && LeadData.CustomerID.ToString() != "")
                    {
                        ldProperty.ETBCustomerID = await this._commonFunc.getCustomerId(LeadData.CustomerID.ToString());
                        if (ldProperty.ETBCustomerID != null && ldProperty.ETBCustomerID != "")
                            odatab.Add("eqs_etbcustomerid@odata.bind", $"contacts({ldProperty.ETBCustomerID})");
                    }

                    if (LeadData.CityName != null && LeadData.CityName.ToString() != "")
                    {
                        ldProperty.CityId = await this._commonFunc.getCityId(LeadData.CityName.ToString());
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
                        ldProperty.ETBCustomerID = await this._commonFunc.getCustomerId(LeadData.CustomerID.ToString());
                        if (ldProperty.ETBCustomerID != null && ldProperty.ETBCustomerID != "")
                            odatab.Add("eqs_etbcustomerid@odata.bind", $"contacts({ldProperty.ETBCustomerID})");
                    }

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
                        this._logger.LogInformation("ValidateLeade", "Input parameters are incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateLeade", "Input parameters are incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateLeade", ex.Message);
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = OutputMSG.Incorrect_Input;
            }

            return ldRtPrm;
        }


        public async Task<LeadReturnParam> UpdateLead(dynamic LeadData)
        {
            LeadReturnParam ldRtPrm = new LeadReturnParam();
            List<JObject> Lead_details = new List<JObject>();
            Dictionary<string, string> odatab = new Dictionary<string, string>();

            odatab.Add("eqs_leadstatus", this.LeadStatus[LeadData.Status.ToString()]);

            string postDataParametr = JsonConvert.SerializeObject(odatab);

            if (LeadData.LeadID != null && LeadData.LeadID.ToString() != "")
            {
                ldRtPrm.LeadID = LeadData.LeadID.ToString();
                Lead_details = await this._queryParser.HttpApiCall($"leads({ldRtPrm.LeadID})", HttpMethod.Patch, postDataParametr);
            }

            return ldRtPrm;
        }
        

        public List<JObject> getLeads()
        {
            
            try
            {
                var output = this._queryParser.HttpApiCall("leads", HttpMethod.Get, "").Result;
                return output;
            }
            catch(Exception ex) 
            {
                throw ex;
            }
            
        }

        public async Task<string> EncriptRespons(string ResponsData)
        {
            return await _queryParser.PayloadEncryption(ResponsData, Transaction_ID);
        }

        private async Task<dynamic> getRequestData(dynamic inputData)
        {
            var EncryptedData = inputData.req_root.body.payload;
            string xmlData = await this._queryParser.PayloadDecryption(EncryptedData.ToString());
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlData);
            string xpath = "PIDBlock/payload";
            var nodes = xmlDoc.SelectSingleNode(xpath);
            foreach (XmlNode childrenNode in nodes)
            {
                dynamic rejusetJson = JsonConvert.DeserializeObject(childrenNode.Value);
                return rejusetJson;
            }

            return "";
        }

    }
}
