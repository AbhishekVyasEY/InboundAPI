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

namespace ManageCase
{
    public class CreateCaseExecution : ICreateCaseExecution
    {

        private ILoggers _logger;
        private IQueryParser _queryParser;
        
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

       
        Dictionary<string, string> CaseType = new Dictionary<string, string>();
        Dictionary<string, string> _CaseType = new Dictionary<string, string>();
        Dictionary<string, int> Priority = new Dictionary<string, int>();
        Dictionary<int, string> _Priority = new Dictionary<int, string>();
        Dictionary<string, string> StatusCodes = new Dictionary<string, string>();
        
        private ICommonFunction _commonFunc;

        public CreateCaseExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
           
            
            this.CaseType.Add("Request", "789030001");
            this.CaseType.Add("Complaint", "789030003");
            this.CaseType.Add("Query", "1");
            this.CaseType.Add("Suggestion", "789030002");

            this._CaseType.Add("789030001", "Request");
            this._CaseType.Add("789030003", "Complaint");
            this._CaseType.Add("1", "Query");
            this._CaseType.Add("789030002", "Suggestion");

            this.Priority.Add("High", 1);
            this.Priority.Add("Normal", 2);
            this.Priority.Add("Low", 3);

            this._Priority.Add(1, "High");
            this._Priority.Add(2, "Normal");
            this._Priority.Add(3, "Low");

            this.StatusCodes.Add("5", "Problem Solved");
            this.StatusCodes.Add("1000", "Information Provided");
            this.StatusCodes.Add("2000", "Merged");
            this.StatusCodes.Add("1", "In Progress");
            this.StatusCodes.Add("2", "On Hold");
            this.StatusCodes.Add("3", "Waiting for Details");
            this.StatusCodes.Add("4", "Researching");
            this.StatusCodes.Add("6", "Cancelled");
            this.StatusCodes.Add("615290000", "Auto Closed");

        }


        public async Task<CaseReturnParam> ValidateCreateCase(dynamic CaseData, string appkey)
        {
            CaseData = await this.getRequestData(CaseData);
            CaseReturnParam ldRtPrm = new CaseReturnParam();
            ldRtPrm.TransactionID = Transaction_ID;
            try
            {
                string channel = CaseData.Channel;
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "CreateCaseappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID) && !string.IsNullOrEmpty(channel) && channel != "")
                    {
                        int ValidationError = 0;

                        
                        
                        if (CaseData.UCIC == null || string.IsNullOrEmpty(CaseData.UCIC.ToString()) || CaseData.UCIC.ToString() == "")
                        {
                            ValidationError = 1;                                
                        }
                        if (CaseData.Classification == null || string.IsNullOrEmpty(CaseData.Classification.ToString()) || CaseData.Classification.ToString() == "")
                        {
                            ValidationError = 1;
                        }
                        if (CaseData.CaseType == null || string.IsNullOrEmpty(CaseData.CaseType.ToString()) || CaseData.CaseType.ToString() == "")
                        {
                            ValidationError = 1;
                        }
                        if (CaseData.Source == null || string.IsNullOrEmpty(CaseData.Source.ToString()) || CaseData.Source.ToString() == "")
                        {
                            ValidationError = 1;
                        }
                        if (CaseData.AccountNumber == null || string.IsNullOrEmpty(CaseData.AccountNumber.ToString()) || CaseData.AccountNumber.ToString() == "")
                        {
                            ValidationError = 1;
                        }


                        if (string.Equals(CaseData.CaseType.ToString(), "Request"))
                        {                               
                            if (CaseData.Category == null || string.IsNullOrEmpty(CaseData.Category.ToString()) || CaseData.Category.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (CaseData.SubCategory == null || string.IsNullOrEmpty(CaseData.SubCategory.ToString()) || CaseData.SubCategory.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                        }

                        if (string.Equals(CaseData.CaseType.ToString(), "Query"))
                        {
                            if (CaseData.Category == null || string.IsNullOrEmpty(CaseData.Category.ToString()) || CaseData.Category.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (CaseData.SubCategory == null || string.IsNullOrEmpty(CaseData.SubCategory.ToString()) || CaseData.SubCategory.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                        }                            

                        
                        


                        if (ValidationError == 1)
                        {
                            this._logger.LogInformation("ValidateCreateCase", "Input parameters are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                        }
                        else
                        {
                            ldRtPrm = await this.CreateCase(CaseData);
                        }                       


                    }
                    else
                    {
                        this._logger.LogInformation("ValidateCreateCase", "Input parameters are incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateCreateCase", "Input parameters are incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateCase", ex.Message);
                ldRtPrm.ReturnCode = "CRM-ERROR-101";
                ldRtPrm.Message = OutputMSG.Resource_n_Found;
                return ldRtPrm;
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

        public async Task<CaseStatusRtParam> ValidategetCaseStatus(dynamic CaseData, string appkey)
        {
            CaseStatusRtParam CSRtPrm = new CaseStatusRtParam();
            CaseData = await this.getRequestData(CaseData);

            int ValidationError = 0;
            try
            {
                if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID) && !string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "GetCaseStatusappkey"))
                {
                    if (CaseData.CaseID == null || string.IsNullOrEmpty(CaseData.CaseID.ToString()) || CaseData.CaseID.ToString() == "")
                    {
                        ValidationError = 1;
                    }

                    if (ValidationError == 1)
                    {                       
                        CSRtPrm.Message = OutputMSG.Incorrect_Input;
                        CSRtPrm.ReturnCode = "CRM-ERROR-102";
                    }
                    else
                    {                       
                        var statusCodeId = await this._commonFunc.getCaseStatus(CaseData.CaseID.ToString());
                        
                        if (statusCodeId == null || statusCodeId.Count<1)
                        {                          
                            CSRtPrm.Message = OutputMSG.Incorrect_Input;
                            CSRtPrm.ReturnCode = "CRM-ERROR-102";
                        }
                        else
                        {
                            CSRtPrm.CaseID = statusCodeId[0]["ticketnumber"];
                            CSRtPrm.CaseStatus = this.StatusCodes[statusCodeId[0]["statuscode"].ToString()];
                            CSRtPrm.Casetype = this._CaseType[statusCodeId[0]["eqs_casetype"].ToString()];
                            CSRtPrm.Subject = statusCodeId[0]["title"];
                            CSRtPrm.openDate = statusCodeId[0]["createdon"];
                            CSRtPrm.modifiedDate = statusCodeId[0]["modifiedon"];
                            CSRtPrm.closeDate = statusCodeId[0]["ccs_resolveddate"];
                            CSRtPrm.Classification = await this._commonFunc.getClassificationName(statusCodeId[0]["_ccs_classification_value"].ToString());
                            CSRtPrm.category = await this._commonFunc.getCategoryName(statusCodeId[0]["_ccs_category_value"].ToString());
                            CSRtPrm.subcategory = await this._commonFunc.getSubCategoryName(statusCodeId[0]["_ccs_subcategory_value"].ToString());
                            CSRtPrm.AdditionalField = JsonConvert.DeserializeObject(statusCodeId[0]["eqs_casepayload"].ToString());

                            CSRtPrm.Description = statusCodeId[0]["description"];
                            CSRtPrm.Priority = this._Priority[Convert.ToInt32(statusCodeId[0]["prioritycode"])];
                            CSRtPrm.Channel = await this._commonFunc.getChannelCode(statusCodeId[0]["_eqs_casechannel_value"].ToString());
                            CSRtPrm.Source = await this._commonFunc.getSourceCode(statusCodeId[0]["_eqs_casesource_value"].ToString());
                            CSRtPrm.Accountid = await this._commonFunc.getAccountNumber(statusCodeId[0]["_eqs_account_value"].ToString());
                            CSRtPrm.customerid = await this._commonFunc.getCustomerCode(statusCodeId[0]["_customerid_value"].ToString());

                            CSRtPrm.ReturnCode = "CRM-SUCCESS";
                        }
                        
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidategetCaseStatus", "Input parameters are incorrect");
                    CSRtPrm.ReturnCode = "CRM-ERROR-102";
                    CSRtPrm.Message = OutputMSG.Incorrect_Input;
                }


                return CSRtPrm;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<CaseReturnParam> CreateCase(dynamic CaseData)
        {
            CaseReturnParam csRtPrm = new CaseReturnParam();
            try
            {               
                LeadMsdProperty case_Property = new LeadMsdProperty();
                CaseProperty csProperty = new CaseProperty();
                Dictionary<string, string> odatab = new Dictionary<string, string>();
                string postDataParametr, postDataParametr1;
                List<JObject> case_details = new List<JObject>();

                case_Property.eqs_casetype = this.CaseType[CaseData.CaseType.ToString()];
                case_Property.title = CaseData.Subject.ToString();
                case_Property.prioritycode = this.Priority[CaseData.Priority.ToString()];
                case_Property.description = CaseData.Description.ToString();

                csProperty.eqs_customerid = CaseData.UCIC.ToString();
                csProperty.channelId = await this._commonFunc.getChannelId(CaseData.Channel.ToString());
                csProperty.customerid = await this._commonFunc.getCustomerId(csProperty.eqs_customerid);
                csProperty.Accountid = await this._commonFunc.getAccountId(CaseData.AccountNumber.ToString());
                csProperty.ccs_classification = await this._commonFunc.getclassificationId(CaseData.Classification.ToString());
                csProperty.CategoryId = await this._commonFunc.getCategoryId(CaseData.Category.ToString());
                csProperty.SubCategoryId = await this._commonFunc.getSubCategoryId(CaseData.SubCategory.ToString(), csProperty.CategoryId);
                csProperty.SourceId = await this._commonFunc.getSourceId(CaseData.Source.ToString());

                if (csProperty.SubCategoryId == "" || csProperty.SubCategoryId.Length < 4)
                {
                    this._logger.LogError("CreateCase", "Mandatory fields are required " + case_details.ToString());
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = OutputMSG.Incorrect_Input;
                    return csRtPrm;
                }
                else
                {
                    JObject items_J_Arr = (JObject)CaseData.AdditionalField;
                    int no_Itemes = (items_J_Arr.Count) / 2;
                    var mandatoryFields = await this._commonFunc.getMandatoryFields(csProperty.SubCategoryId);
                    if (mandatoryFields.Count>0)
                    {
                        for (int i = 1; i <= no_Itemes; i++)
                        {
                            string keyName = CaseData.AdditionalField["FieldName" + i];
                            string ValName = CaseData.AdditionalField["FieldValue" + i];
                            mandatoryFields.Where(x => (x.InputField == keyName)).Single().CRMValue = ValName;
                        }
                    }
                    
                    foreach (var field in mandatoryFields)
                    {
                        if (field.CRMValue != "" || !string.IsNullOrEmpty(field.CRMValue))
                        {
                            if (field.CRMType == "615290002")
                            {
                                string mandFieldID = await this._commonFunc.getIDfromMSDTable(field.CRMTable, field.IDFieldName, field.FilterField, field.CRMValue);
                                odatab.Add(field.CRMField + "@odata.bind", $"{field.CRMTable}({mandFieldID})");
                            }
                            else
                            {
                                odatab.Add(field.CRMField, field.CRMValue);
                            }
                            
                        }
                        else
                        {
                            this._logger.LogError("CreateCase", "Mandatory fields are required " + case_details.ToString());
                            csRtPrm.ReturnCode = "CRM-ERROR-102";
                            csRtPrm.Message = OutputMSG.Incorrect_Input;
                            return csRtPrm;
                        }

                    }
                }

                odatab.Add("customerid_contact@odata.bind", $"contacts({csProperty.customerid})");
                if (csProperty.Accountid.Length > 4)
                {
                    odatab.Add("eqs_account@odata.bind", $"eqs_accounts({csProperty.Accountid})");
                }
                else
                {
                    this._logger.LogError("CreateCase", "Mandatory fields are required " + case_details.ToString());
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = OutputMSG.Incorrect_Input;
                    return csRtPrm;
                }
                if (csProperty.channelId.Length > 4)
                {
                    odatab.Add("eqs_CaseChannel@odata.bind", $"eqs_casechannels({csProperty.channelId})");
                }
                if (csProperty.ccs_classification.Length > 4)
                {
                    odatab.Add("ccs_classification@odata.bind", $"ccs_classifications({csProperty.ccs_classification})");
                }
                if (csProperty.CategoryId.Length > 4)
                {
                    odatab.Add("ccs_category@odata.bind", $"ccs_categories({csProperty.CategoryId})");
                }
                if (csProperty.SubCategoryId.Length > 4)
                {
                    odatab.Add("ccs_subcategory@odata.bind", $"ccs_subcategories({csProperty.SubCategoryId})");
                }
                
                if (csProperty.SourceId.Length > 4)
                {
                    odatab.Add("eqs_CaseSource@odata.bind", $"eqs_casesources({csProperty.SourceId})");
                }

                odatab.Add("eqs_casepayload", JsonConvert.SerializeObject(CaseData.AdditionalField));

                odatab.Add("caseorigincode", "3");
                odatab.Add("eqs_customercode", csProperty.eqs_customerid);

                postDataParametr = JsonConvert.SerializeObject(case_Property);
                postDataParametr1 = JsonConvert.SerializeObject(odatab);

                postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                case_details = await this._queryParser.HttpApiCall("incidents?$select=ticketnumber", HttpMethod.Post, postDataParametr);




                if (case_details.Count > 0)
                {
                    dynamic respons_code = case_details[0];
                    if (respons_code.responsecode == 204)
                    {
                        csRtPrm.CaseID = CommonFunction.GetIdFromPostRespons(respons_code.responsebody.ToString());
                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }
                    else if (respons_code.responsecode == 201)
                    {
                        csRtPrm.CaseID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "ticketnumber");
                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }
                    else
                    {
                        this._logger.LogError("CreateCase", case_details.ToString());
                        csRtPrm.ReturnCode = "CRM-ERROR-101";
                        csRtPrm.Message = OutputMSG.Resource_n_Found;
                    }
                }
                else
                {
                    this._logger.LogError("CreateCase", case_details.ToString());
                    csRtPrm.ReturnCode = "CRM-ERROR-101";
                    csRtPrm.Message = OutputMSG.Resource_n_Found;
                }


                return csRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateCase", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-101";
                csRtPrm.Message = OutputMSG.Resource_n_Found;
                return csRtPrm;
            }
        }
                      

        public async Task<CaseListParam> getCaseList(dynamic CaseData, string appkey)
        {
            CaseListParam CSRtPrm = new CaseListParam();
            CSRtPrm.AllCases = new List<CaseDetails>();
            CaseData = await this.getRequestData(CaseData);

            int ValidationError = 0;
            int custId =0 , AccId = 0;
            try
            {
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "GetCaseListappkey"))
                {
                    if (CaseData.CustomerID != null && !string.IsNullOrEmpty(CaseData.CustomerID.ToString()) && CaseData.CustomerID.ToString() != "")
                    {
                        custId = 1;
                    }

                    if (CaseData.AccountID != null && !string.IsNullOrEmpty(CaseData.AccountID.ToString()) && CaseData.AccountID.ToString() != "")
                    {
                        AccId = 1;
                    }

                    if (custId==0 && AccId==0)
                    {
                        ValidationError = 1;
                    }

                    if (ValidationError == 1)
                    {
                        this._logger.LogInformation("getCaseList", "Input parameters are incorrect");                       
                        CSRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                    else
                    {
                        string query_url = "";
                        if (custId==1)
                        {
                            string customerid = await this._commonFunc.getCustomerId(CaseData.CustomerID.ToString());
                            query_url = $"incidents()?$select=ticketnumber,statuscode,title,createdon,modifiedon,ccs_resolveddate,eqs_casetype,_ccs_classification_value,_ccs_category_value,_ccs_subcategory_value,eqs_casepayload,description,prioritycode,_eqs_casechannel_value,_eqs_casesource_value,_eqs_account_value,_customerid_value&$filter=_customerid_value eq '{customerid}'";
                        }
                        if (AccId == 1)
                        {
                            string Accountid = await this._commonFunc.getAccountId(CaseData.AccountID.ToString());
                            query_url = $"incidents()?$select=ticketnumber,statuscode,title,createdon,modifiedon,ccs_resolveddate,eqs_casetype,_ccs_classification_value,_ccs_category_value,_ccs_subcategory_value,eqs_casepayload,description,prioritycode,_eqs_casechannel_value,_eqs_casesource_value,_eqs_account_value,_customerid_value&$filter=_eqs_account_value eq '{Accountid}'";
                        }

                        var caseresponsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                        var CaseList = await this._commonFunc.getDataFromResponce(caseresponsdtails);
                        foreach (var caseDetails in CaseList)
                        {
                            CaseDetails case_details = new CaseDetails();
                            case_details.CaseID = caseDetails["ticketnumber"].ToString();
                            case_details.CaseStatus = this.StatusCodes[caseDetails["statuscode"].ToString()];
                            case_details.Subject = caseDetails["title"].ToString();
                            case_details.openDate = caseDetails["createdon"].ToString();
                            case_details.modifiedDate = caseDetails["modifiedon"].ToString();
                            case_details.closeDate = caseDetails["ccs_resolveddate"].ToString();

                            if (!string.IsNullOrEmpty(caseDetails["eqs_casetype"].ToString()))
                            {
                                case_details.Casetype = this._CaseType[caseDetails["eqs_casetype"].ToString()];
                            }

                            case_details.Classification = await this._commonFunc.getClassificationName(caseDetails["_ccs_classification_value"].ToString());
                            case_details.category = await this._commonFunc.getCategoryName(caseDetails["_ccs_category_value"].ToString());
                            case_details.subcategory = await this._commonFunc.getSubCategoryName(caseDetails["_ccs_subcategory_value"].ToString());
                            case_details.AdditionalField = (JObject)JsonConvert.DeserializeObject(caseDetails["eqs_casepayload"].ToString());

                            case_details.Description = caseDetails["description"].ToString();
                            case_details.Priority = this._Priority[Convert.ToInt32(caseDetails["prioritycode"])];
                            case_details.Channel = await this._commonFunc.getChannelCode(caseDetails["_eqs_casechannel_value"].ToString());
                            case_details.Source = await this._commonFunc.getSourceCode(caseDetails["_eqs_casesource_value"].ToString());
                            case_details.Accountid = await this._commonFunc.getAccountNumber(caseDetails["_eqs_account_value"].ToString());
                            case_details.customerid = await this._commonFunc.getCustomerCode(caseDetails["_customerid_value"].ToString());

                            CSRtPrm.AllCases.Add(case_details);
                        }
                        CSRtPrm.ReturnCode = "CRM-SUCCESS";
                    }
                }
                else
                {
                    this._logger.LogInformation("getCaseList", "Input parameters are incorrect");
                    CSRtPrm.ReturnCode = "CRM-ERROR-102";
                    CSRtPrm.Message = OutputMSG.Incorrect_Input;
                }


                return CSRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getCaseList", ex.Message);
                CSRtPrm.ReturnCode = "CRM-ERROR-101";
                CSRtPrm.Message = OutputMSG.Resource_n_Found;
                return CSRtPrm;
            }

        }

        public async Task<string> EncriptRespons(string ResponsData)
        {
            return await _queryParser.PayloadEncryption(ResponsData, Transaction_ID);
        }

        public async Task CRMLog(string InputRequest, string OutputRespons, string CallStatus)
        {
            Dictionary<string, string> CRMProp = new Dictionary<string, string>();
            CRMProp.Add("eqs_name", this.Transaction_ID);
            CRMProp.Add("eqs_requestbody", InputRequest);
            CRMProp.Add("eqs_responsebody", OutputRespons);
            CRMProp.Add("eqs_requeststatus", (CallStatus.Contains("ERROR")) ? "615290001" : "615290000");
            string postDataParametr = JsonConvert.SerializeObject(CRMProp);
            await this._queryParser.HttpApiCall("eqs_apilogs", HttpMethod.Post, postDataParametr);
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
