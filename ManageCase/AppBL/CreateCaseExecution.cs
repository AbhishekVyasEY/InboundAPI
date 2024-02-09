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

namespace ManageCase
{
    public class CreateCaseExecution : ICreateCaseExecution
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


        public async Task<CaseReturnParam> ValidateCreateCase(dynamic CaseData)
        {
            CaseData = await this.getRequestData(CaseData, "CreateCase");
            CaseReturnParam ldRtPrm = new CaseReturnParam();
            ldRtPrm.TransactionID = Transaction_ID;

            if (CaseData.ErrorNo != null && CaseData.ErrorNo.ToString() == "Error99")
            {
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = "API do not have access permission!";
                return ldRtPrm;
            }

            try
            {
                //string channel = CaseData.Channel;
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "CreateCaseappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        int ValidationError = 0;
                        List<string> errors = new List<string>();

                        if (CaseData.Channel == null || string.IsNullOrEmpty(CaseData.Channel.ToString()) || CaseData.Channel.ToString() == "")
                        {
                            ValidationError = 1;
                            errors.Add("Channel");
                        }
                        if (CaseData.Source == null || string.IsNullOrEmpty(CaseData.Source.ToString()) || CaseData.Source.ToString() == "")
                        {
                            ValidationError = 1;
                            errors.Add("Source");
                        }
                        if (CaseData.UCIC == null || string.IsNullOrEmpty(CaseData.UCIC.ToString()) || CaseData.UCIC.ToString() == "")
                        {
                            ValidationError = 1;
                            errors.Add("UCIC");                           
                        }
                        if (CaseData.CaseType == null || string.IsNullOrEmpty(CaseData.CaseType.ToString()) || CaseData.CaseType.ToString() == "")
                        {
                            ValidationError = 1;
                            errors.Add("CaseType");
                        }
                        if (CaseData.Subject == null || string.IsNullOrEmpty(CaseData.Subject.ToString()) || CaseData.Subject.ToString() == "")
                        {
                            ValidationError = 1;
                            errors.Add("Subject");
                        }
                        if (CaseData.Classification == null || string.IsNullOrEmpty(CaseData.Classification.ToString()) || CaseData.Classification.ToString() == "")
                        {
                            ValidationError = 1;
                            errors.Add("Classification");
                        }                      
                        if (CaseData.Category == null || string.IsNullOrEmpty(CaseData.Category.ToString()) || CaseData.Category.ToString() == "")
                        {
                            ValidationError = 1;
                            errors.Add("Category");
                        }
                        if (CaseData.SubCategory == null || string.IsNullOrEmpty(CaseData.SubCategory.ToString()) || CaseData.SubCategory.ToString() == "")
                        {
                            ValidationError = 1;
                            errors.Add("SubCategory");
                        }
                        

                        if (ValidationError == 1)
                        {
                            this._logger.LogInformation("ValidateCreateCase", $"{string.Join(",", errors.ToArray())} is mandatory");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            if (errors.Count == 1)
                            {
                                ldRtPrm.Message = $" {string.Join(",", errors.ToArray())} is mandatory";
                            }
                            if (errors.Count > 1)
                            {
                                ldRtPrm.Message = $" {string.Join(", ", errors.ToArray())} are mandatory";
                            }
                        }
                        else if (await this._commonFunc.checkDuplicate(CaseData.UCIC.ToString(), CaseData.AccountNumber.ToString(), CaseData.Classification.ToString(), CaseData.Category.ToString(), CaseData.SubCategory.ToString()))
                        {
                            this._logger.LogInformation("ValidateCreateCase", "Case already exists in the system");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "Case already exists in the system";
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateCreateCase", "No case with the same Combination. Before Create Case");
                            ldRtPrm = await this.CreateCase(CaseData);
                            this._logger.LogInformation("ValidateCreateCase", $"Case created successfully. {ldRtPrm.CaseID}");
                        }                   
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateCreateCase", "Transaction_ID or Channel is incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or Channel is incorrect";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateCreateCase", "AppKey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "AppKey is incorrect";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateCreateCase", ex.Message);
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

        public async Task<CaseStatusRtParam> ValidategetCaseStatus(dynamic CaseData)
        {
            CaseStatusRtParam CSRtPrm = new CaseStatusRtParam();
            CaseData = await this.getRequestData(CaseData, "getCaseStatus");

            if (CaseData.ErrorNo != null && CaseData.ErrorNo.ToString() == "Error99")
            {
                CSRtPrm.ReturnCode = "CRM-ERROR-102";
                CSRtPrm.Message = "API do not have access permission!";
                return CSRtPrm;
            }

            try
            {
                if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID) && !string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "GetCaseStatusappkey"))
                {
                   

                    if (CaseData.CaseID == null || string.IsNullOrEmpty(CaseData.CaseID.ToString()) || CaseData.CaseID.ToString() == "")
                    {                       
                        CSRtPrm.Message = "CaseId is incorrect";
                        CSRtPrm.ReturnCode = "CRM-ERROR-102";
                    }
                    else
                    {                       
                        var statusCodeId = await this._commonFunc.getCaseStatus(CaseData.CaseID.ToString());
                        
                        if (statusCodeId == null || statusCodeId.Count<1)
                        {                          
                            CSRtPrm.Message = "Case status not found.";
                            CSRtPrm.ReturnCode = "CRM-ERROR-102";
                        }
                        else
                        {
                            CaseStatusRtParam case_dtl;
                            if (!this._commonFunc.GetMvalue<CaseStatusRtParam>("CsSt" + CaseData.CaseID.ToString() + statusCodeId, out case_dtl))
                            {
                                CSRtPrm.CaseID = statusCodeId[0]["ticketnumber"];
                                CSRtPrm.CaseStatus = this.StatusCodes[statusCodeId[0]["statuscode"].ToString()];
                                CSRtPrm.Casetype = this._CaseType[statusCodeId[0]["eqs_casetype"].ToString()];
                                CSRtPrm.Subject = statusCodeId[0]["title"];
                                CSRtPrm.openDate = statusCodeId[0]["createdon"];
                                CSRtPrm.modifiedDate = statusCodeId[0]["modifiedon"];
                                CSRtPrm.closeDate = statusCodeId[0]["ccs_resolveddate"];

                                await this._commonFunc.getClassificationName(statusCodeId[0]["_ccs_classification_value"].ToString());
                                await this._commonFunc.getCategoryName(statusCodeId[0]["_ccs_category_value"].ToString());
                                await this._commonFunc.getSubCategoryName(statusCodeId[0]["_ccs_subcategory_value"].ToString());

                                var Batch_results1 = await this._queryParser.GetBatchResult();

                                CSRtPrm.Classification = (Batch_results1[0]["ccs_name"] != null) ? Batch_results1[0]["ccs_name"].ToString() : "";
                                CSRtPrm.category = (Batch_results1[1]["ccs_name"] != null) ? Batch_results1[1]["ccs_name"].ToString() : "";
                                CSRtPrm.subcategory = (Batch_results1[2]["ccs_name"] != null) ? Batch_results1[2]["ccs_name"].ToString() : "";

                                CSRtPrm.AdditionalField = JsonConvert.DeserializeObject(statusCodeId[0]["eqs_casepayload"].ToString());

                                dynamic obj = new ExpandoObject();
                                if (CSRtPrm.AdditionalField == null)
                                {
                                    var additionalFields = await this._commonFunc.getCaseAdditionalFields(statusCodeId[0]["_ccs_subcategory_value"].ToString());
                                    if (additionalFields.Count > 0)
                                    {
                                        var fields = additionalFields[0]["eqs_showfield"].ToString();
                                        var caseAdditionalData = await this._commonFunc.getCaseAdditionalDetails(CaseData.CaseID.ToString(), fields);
                                        if (caseAdditionalData.Count > 0)
                                        {
                                            string[] str = fields.Split(',');

                                            for (int i = 0; i < str.Length; i++)
                                            {
                                                int j = i; j++;
                                                string value = null;
                                                //lookup
                                                if (caseAdditionalData[0][$"_{str[i].ToString()}_value@OData.Community.Display.V1.FormattedValue"] != null)
                                                    value = caseAdditionalData[0][$"_{str[i].ToString()}_value@OData.Community.Display.V1.FormattedValue"];

                                                //Option set
                                                if (caseAdditionalData[0][$"{str[i].ToString()}@OData.Community.Display.V1.FormattedValue"] != null)
                                                    value = caseAdditionalData[0][$"{str[i].ToString()}@OData.Community.Display.V1.FormattedValue"];
                                                else
                                                    value = caseAdditionalData[0][str[i].ToString()];

                                                ((IDictionary<string, object>)obj).Add("FieldName" + j, str[i].ToString().Replace("eqs_", ""));

                                                ((IDictionary<string, object>)obj).Add("FieldValue" + j, value);
                                            }
                                            CSRtPrm.AdditionalField = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(obj));
                                        }
                                    }
                                }

                                CSRtPrm.Description = statusCodeId[0]["description"];
                                CSRtPrm.Priority = await this._queryParser.getOptionSetValuToText("incident", "eqs_casepriority", statusCodeId[0]["eqs_casepriority"].ToString());

                                await this._commonFunc.getChannelCode(statusCodeId[0]["_eqs_casechannel_value"].ToString());
                                await this._commonFunc.getSourceCode(statusCodeId[0]["_eqs_casesource_value"].ToString());
                                await this._commonFunc.getAccountNumber(statusCodeId[0]["_eqs_account_value"].ToString());
                                await this._commonFunc.getCustomerCode(statusCodeId[0]["_customerid_value"].ToString());
                                var Batch_results2 = await this._queryParser.GetBatchResult();
                                CSRtPrm.Channel = (Batch_results2[0]["eqs_channelid"] != null) ? Batch_results2[0]["eqs_channelid"].ToString() : "";
                                CSRtPrm.Source = (Batch_results2[1]["eqs_sourceid"] != null) ? Batch_results2[1]["eqs_sourceid"].ToString() : "";
                                CSRtPrm.Accountid = (Batch_results2[2]["eqs_accountno"] != null) ? Batch_results2[2]["eqs_accountno"].ToString() : "";
                                CSRtPrm.customerid = (Batch_results2[3]["eqs_customerid"] != null) ? Batch_results2[3]["eqs_customerid"].ToString() : "";

                                CSRtPrm.ReturnCode = "CRM-SUCCESS";

                                this._commonFunc.SetMvalue<CaseStatusRtParam>("CaseStatus" + CaseData.CaseID.ToString() + statusCodeId, 1400, CSRtPrm);
                            }
                            else
                            {
                                CSRtPrm = case_dtl;
                            }
                        }
                        
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidategetCaseStatus", "Transaction_ID or Channel or AppKey is incorrect");
                    CSRtPrm.ReturnCode = "CRM-ERROR-102";
                    CSRtPrm.Message = "Transaction_ID or Channel or AppKey is incorrect";
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
                Dictionary<string, double> odatab1 = new Dictionary<string, double>();
                string postDataParametr, postDataParametr1;
                List<JObject> case_details = new List<JObject>();
                //Channel
                await this._commonFunc.getChannelId(CaseData.Channel.ToString());
                await this._commonFunc.getSourceId(CaseData.Source.ToString());
                await this._commonFunc.getCustomer_Id(CaseData.UCIC?.ToString());
                await this._commonFunc.getCategoryId(CaseData.Category?.ToString());
                await this._commonFunc.getAccount_Id(CaseData.AccountNumber.ToString());
                await this._commonFunc.getclassificationId(CaseData.Classification.ToString());

                var Batch_results1 = await this._queryParser.GetBatchResult();
                csProperty.channelId = (Batch_results1[0]["eqs_casechannelid"] != null) ? Batch_results1[0]["eqs_casechannelid"].ToString() : "";
                csProperty.SourceId = (Batch_results1[1]["eqs_casesourceid"] != null) ? Batch_results1[1]["eqs_casesourceid"].ToString() : "";
                csProperty.customerid = (Batch_results1[2]["contactid"] != null) ? Batch_results1[2]["contactid"].ToString() : "";
                csProperty.CategoryId = (Batch_results1[3]["ccs_categoryid"] != null) ? Batch_results1[3]["ccs_categoryid"].ToString() : "";
                csProperty.Accountid = (Batch_results1[4]["eqs_accountid"] != null) ? Batch_results1[4]["eqs_accountid"].ToString() : "";
                csProperty.ccs_classification = (Batch_results1[5]["ccs_classificationid"] != null) ? Batch_results1[5]["ccs_classificationid"].ToString() : "";

                if (csProperty.channelId != null && csProperty.channelId.Length > 4)
                {
                    odatab.Add("eqs_CaseChannel@odata.bind", $"eqs_casechannels({csProperty.channelId})");
                }
                else
                {
                    this._logger.LogInformation("CreateCase", "Channel not found.");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = "Channel not found.";
                    return csRtPrm;
                }

                //Source
                
                if (csProperty.SourceId != null && csProperty.SourceId.Length > 4)
                {
                    odatab.Add("eqs_CaseSource@odata.bind", $"eqs_casesources({csProperty.SourceId})");
                }
                else
                {
                    this._logger.LogInformation("CreateCase", "Source not found.");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = "Source not found.";
                    return csRtPrm;
                }

                //UCIC
                csProperty.eqs_customerid = CaseData.UCIC?.ToString();
                odatab.Add("eqs_customercode", csProperty.eqs_customerid);
                
                if (!string.IsNullOrEmpty(csProperty.customerid))
                {
                    odatab.Add("customerid_contact@odata.bind", $"contacts({csProperty.customerid})");
                    
                }
                else
                {
                    this._logger.LogError("CreateCase", "Customer not found!");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = "Customer not found!";
                    return csRtPrm;
                }
                
                case_Property.eqs_casetype = this.CaseType[CaseData.CaseType?.ToString()];
                case_Property.title = CaseData.Subject?.ToString();

               
                if (csProperty.CategoryId != null && csProperty.CategoryId.Length > 4)
                {
                    odatab.Add("ccs_category@odata.bind", $"ccs_categories({csProperty.CategoryId})");
                }
                else
                {
                    this._logger.LogError("CreateCase", "Category not found!");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = "Category not found!";
                    return csRtPrm;
                }

                csProperty.SubCategoryId = await this._commonFunc.getSubCategoryId(CaseData.SubCategory.ToString(), csProperty.CategoryId);
                if (csProperty.SubCategoryId != null && csProperty.SubCategoryId.Length > 4)
                {
                    odatab.Add("ccs_subcategory@odata.bind", $"ccs_subcategories({csProperty.SubCategoryId})");
                }
                else
                {
                    this._logger.LogError("CreateCase", "SubCategory not found!");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = "SubCategory not found!";
                    return csRtPrm;
                }

                if (!string.IsNullOrEmpty(CaseData.Priority?.ToString()))
                {
                    case_Property.eqs_casepriority = await this._queryParser.getOptionSetTextToValue("incident", "eqs_casepriority", CaseData.Priority.ToString());
                }
                if (!string.IsNullOrEmpty(CaseData.Description?.ToString()))
                {
                    case_Property.description = CaseData.Description.ToString();
                }               
                if (!string.IsNullOrEmpty(CaseData.AccountNumber?.ToString()))
                {
                   
                    if (csProperty.Accountid != null && csProperty.Accountid.Length > 4)
                    {
                        odatab.Add("eqs_account@odata.bind", $"eqs_accounts({csProperty.Accountid})");
                    }
                    else
                    {
                            this._logger.LogError("CreateCase", "Account not found!");
                            csRtPrm.ReturnCode = "CRM-ERROR-102";
                            csRtPrm.Message = "Account not found!";
                            return csRtPrm;
                    }
                }
                if (!string.IsNullOrEmpty(CaseData.Classification?.ToString()))
                {
                    
                    if (csProperty.ccs_classification.Length > 4)
                    {
                        odatab.Add("ccs_classification@odata.bind", $"ccs_classifications({csProperty.ccs_classification})");
                    }
                }

                if (csProperty.SubCategoryId == "" || csProperty.SubCategoryId?.Length < 4)
                {
                    this._logger.LogInformation("CreateCase", "SubCategoryId not found.");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = "SubCategoryId not found.";
                    return csRtPrm;
                }
                else
                {
                    if (CaseData.AdditionalField != null)
                    {
                        JObject items_J_Arr = (JObject)CaseData.AdditionalField;
                        int no_Itemes = (items_J_Arr.Count) / 2;
                        var mandatoryFields = await this._commonFunc.getMandatoryFields(csProperty.SubCategoryId);
                        if (mandatoryFields.Count > 0)
                        {
                            for (int i = 1; i <= no_Itemes; i++)
                            {
                                string keyName = CaseData.AdditionalField["FieldName" + i];
                                string ValName = CaseData.AdditionalField["FieldValue" + i];
                                mandatoryFields.Where(x => (x.InputField == keyName)).Single().CRMValue = ValName;
                            }
                        }
                        var value = await this._queryParser.getOptionSetTextToValue("incident", "statuscode", "Researching");
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
                                    if (field.CRMType == "615290001")
                                    {
                                        odatab.Add(field.CRMField, await this._queryParser.getOptionSetTextToValue("incident", field.CRMField, field.CRMValue));
                                    }
                                    else if (field.CRMType == "615290007")
                                    {
                                        odatab1.Add(field.CRMField, Convert.ToDouble(field.CRMValue.ToString()));
                                    }
                                    else if (field.CRMType == "615290006")
                                    {
                                        string dd, mm, yyyy;
                                        dd = field.CRMValue.Substring(0, 2);
                                        mm = field.CRMValue.Substring(3, 2);
                                        yyyy = field.CRMValue.Substring(6, 4);
                                        odatab.Add(field.CRMField, yyyy + "-" + mm + "-" + dd);
                                    }
                                    else
                                    {
                                        odatab.Add(field.CRMField, field.CRMValue);
                                    }

                                }

                            }
                            else
                            {
                                this._logger.LogInformation("CreateCase", $"{field.CRMField} can not be null.");
                                csRtPrm.ReturnCode = "CRM-ERROR-102";
                                csRtPrm.Message = $"{field.CRMField} can not be null.";
                                return csRtPrm;
                            }
                        }
                    }
                }                             
                
                if (!string.IsNullOrEmpty(CaseData.AdditionalField?.ToString()))
                {
                    odatab.Add("eqs_casepayload", JsonConvert.SerializeObject(CaseData.AdditionalField));
                }
                odatab.Add("caseorigincode", "3");
                

                postDataParametr = JsonConvert.SerializeObject(case_Property);
                postDataParametr1 = JsonConvert.SerializeObject(odatab);

                postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);
                if (odatab1 != null && odatab1.Count >0)
                {
                    postDataParametr1 = JsonConvert.SerializeObject(odatab1);
                    postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);
                }

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
                        this._logger.LogError("CreateCase", JsonConvert.SerializeObject(case_details));
                        csRtPrm.ReturnCode = "CRM-ERROR-101";
                        csRtPrm.Message = "Unable to create case.";
                    }
                }
                else
                {
                    this._logger.LogError("CreateCase", JsonConvert.SerializeObject(case_details));
                    csRtPrm.ReturnCode = "CRM-ERROR-101";
                    csRtPrm.Message = "Unable to create case.";
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
                      

        public async Task<CaseListParam> getCaseList(dynamic CaseData)
        {
            CaseListParam CSRtPrm = new CaseListParam();
            CSRtPrm.AllCases = new List<CaseDetails>();
            CaseData = await this.getRequestData(CaseData, "getCaseList");

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
                        this._logger.LogInformation("getCaseList", "CustomerID or AccountID is incorrect");                       
                        CSRtPrm.Message = "CustomerID or AccountID is incorrect";
                    }
                    else
                    {
                        string query_url = "";
                        if (custId==1)
                        {
                            string customerid = await this._commonFunc.getCustomerId(CaseData.CustomerID.ToString());
                            query_url = $"incidents()?$top=50&$orderby=ticketnumber desc&$select=ticketnumber,statuscode,title,createdon,modifiedon,ccs_resolveddate,eqs_casecancellationdate,eqs_casetype,_ccs_classification_value,_ccs_category_value,_ccs_subcategory_value,eqs_casepayload,description,eqs_casepriority,_eqs_casechannel_value,_eqs_casesource_value,_eqs_account_value,_customerid_value&$filter=_customerid_value eq '{customerid}'";
                        }
                        if (AccId == 1)
                        {
                            string Accountid = await this._commonFunc.getAccountId(CaseData.AccountID.ToString());
                            query_url = $"incidents()?$top=50&$orderby=ticketnumber desc&$select=ticketnumber,statuscode,title,createdon,modifiedon,ccs_resolveddate,eqs_casecancellationdate,eqs_casetype,_ccs_classification_value,_ccs_category_value,_ccs_subcategory_value,eqs_casepayload,description,eqs_casepriority,_eqs_casechannel_value,_eqs_casesource_value,_eqs_account_value,_customerid_value&$filter=_eqs_account_value eq '{Accountid}'";
                        }

                        var caseresponsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                        var CaseList = await this._queryParser.getDataFromResponce(caseresponsdtails);
                        foreach (var caseDetails in CaseList)
                        {
                            CaseDetails case_details = new CaseDetails();
                            CaseDetails case_dtl;
                            if (!this._commonFunc.GetMvalue<CaseDetails>("Case" + caseDetails["ticketnumber"].ToString(), out case_dtl))
                            {
                                case_details.CaseID = caseDetails["ticketnumber"].ToString();
                                case_details.CaseStatus = this.StatusCodes[caseDetails["statuscode"].ToString()];
                                case_details.Subject = caseDetails["title"].ToString();
                                case_details.openDate = caseDetails["createdon"].ToString();
                                case_details.modifiedDate = caseDetails["modifiedon"].ToString();
                                case_details.closeDate = caseDetails["ccs_resolveddate"].ToString();
                                case_details.cancellationDate = caseDetails["eqs_casecancellationdate"].ToString();

                                if (!string.IsNullOrEmpty(caseDetails["eqs_casetype"].ToString()))
                                {
                                    case_details.Casetype = this._CaseType[caseDetails["eqs_casetype"].ToString()];
                                }

                                await this._commonFunc.getClassificationName(caseDetails["_ccs_classification_value"].ToString());
                                await this._commonFunc.getCategoryName(caseDetails["_ccs_category_value"].ToString());
                                await this._commonFunc.getSubCategoryName(caseDetails["_ccs_subcategory_value"].ToString());
                                var Batch_results1 = await this._queryParser.GetBatchResult();
                                case_details.Classification = (Batch_results1[0]["ccs_name"] != null) ? Batch_results1[0]["ccs_name"].ToString() : "";
                                case_details.category = (Batch_results1[1]["ccs_name"] != null) ? Batch_results1[1]["ccs_name"].ToString() : "";
                                case_details.subcategory = (Batch_results1[2]["ccs_name"] != null) ? Batch_results1[2]["ccs_name"].ToString() : "";

                                case_details.AdditionalField = (JObject)JsonConvert.DeserializeObject(caseDetails["eqs_casepayload"].ToString());

                                case_details.Description = caseDetails["description"].ToString();

                                if (!string.IsNullOrEmpty(caseDetails["eqs_casepriority"].ToString()))
                                {
                                    case_details.Priority = await this._queryParser.getOptionSetValuToText("incident", "eqs_casepriority", caseDetails["eqs_casepriority"].ToString()); 
                                }
                                
                                await this._commonFunc.getChannelCode(caseDetails["_eqs_casechannel_value"].ToString());
                                await this._commonFunc.getSourceCode(caseDetails["_eqs_casesource_value"].ToString());
                                await this._commonFunc.getAccountNumber(caseDetails["_eqs_account_value"].ToString());
                                await this._commonFunc.getCustomerCode(caseDetails["_customerid_value"].ToString());
                                var Batch_results2 = await this._queryParser.GetBatchResult();
                                case_details.Channel = (Batch_results2[0]["eqs_channelid"] != null) ? Batch_results2[0]["eqs_channelid"].ToString() : "";
                                case_details.Source = (Batch_results2[1]["eqs_sourceid"] != null) ? Batch_results2[1]["eqs_sourceid"].ToString() : "";
                                case_details.Accountid = (Batch_results2[2]["eqs_accountno"] != null) ? Batch_results2[2]["eqs_accountno"].ToString() : "";
                                case_details.customerid = (Batch_results2[3]["eqs_customerid"] != null) ? Batch_results2[3]["eqs_customerid"].ToString() : "";

                                this._commonFunc.SetMvalue<CaseDetails>("Case" + caseDetails["ticketnumber"].ToString(), 60, case_details);
                            }
                            else
                            {
                                case_details = case_dtl;
                            }

                            CSRtPrm.AllCases.Add(case_details);
                        }
                        CSRtPrm.ReturnCode = "CRM-SUCCESS";
                    }
                }
                else
                {
                    this._logger.LogInformation("getCaseList", "Appkey is incorrect");
                    CSRtPrm.ReturnCode = "CRM-ERROR-102";
                    CSRtPrm.Message = "Appkey is incorrect";
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
            return await _queryParser.PayloadEncryption(ResponsData, Transaction_ID, this.Bank_Code);
        }

       

        private async Task<dynamic> getRequestData(dynamic inputData,string APIname)
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
