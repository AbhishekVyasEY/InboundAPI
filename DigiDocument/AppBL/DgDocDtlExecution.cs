﻿namespace DigiDocument
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
    using System.Reflection.Metadata;
    using Microsoft.Identity.Client;
    using Microsoft.Azure.KeyVault.Models;

    public class DgDocDtlExecution : IDgDocDtlExecution
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


        private ICommonFunction _commonFunc;

        public DgDocDtlExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {

            this._logger = logger;

            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;


        }

        public async Task<UpdateDgDocDtlReturn> ValidateDocumentInput(dynamic RequestData)
        {
            UpdateDgDocDtlReturn ldRtPrm = new UpdateDgDocDtlReturn();
            List<DocUpdateStatus> updateDocumentReturn = new List<DocUpdateStatus>();
            RequestData = await this.getRequestData(RequestData, "UpdateDigiDocumentDetails");
            try
            {
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "UpdateDigiDocumentappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        var catId = await this._commonFunc.getDocumentDateConfig();
                        if (RequestData.Document?.Count > 0)
                        {
                            foreach (var item in RequestData.Document)
                            {
                                DocUpdateStatus updateDocument = new DocUpdateStatus();
                                var Fieldvalidation = this.ValidateDocumentData(item, catId);
                                if (Fieldvalidation == "ok")
                                {
                                    if (!string.IsNullOrEmpty(item.CRMDocumentID.ToString()))
                                    {
                                        updateDocument = await this.UpdateDocument(item);
                                    }
                                    else
                                    {
                                        updateDocument = await this.CreateDocument(item);
                                    }
                                    updateDocumentReturn.Add(updateDocument);
                                }
                                else
                                {
                                    updateDocument.Status = "Error";
                                    updateDocument.SubCategoryCode = item.SubcategoryCode.ToString();
                                    updateDocument.DocId = item.DocId;
                                    updateDocument.ErrorMessage = Fieldvalidation;
                                    updateDocumentReturn.Add(updateDocument);
                                }
                            }
                        
                        ldRtPrm.DocUpdateStatus = updateDocumentReturn;
                        ldRtPrm.ReturnCode = "CRM-SUCCESS";
                        ldRtPrm.Message = OutputMSG.Case_Success;
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateDocumentInput", "Documents data is incorrect.");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "Documents data is incorrect.";
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateDocumentInput", "Transaction_ID or Channel_ID is incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or Channel_ID is incorrect.";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateDocumentInput", "Appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Appkey is incorrect";
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateDocumentInput", ex.Message);
                this._logger.LogInformation("ValidateDocumentInput", "Input parameters are incorrect");
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = OutputMSG.Incorrect_Input;
            }
            return ldRtPrm;
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

        private async Task<DocUpdateStatus> CreateDocument(dynamic documentdtl)
        {
            Dictionary<string, string> odatab = new Dictionary<string, string>();
            DocUpdateStatus createDocument = new DocUpdateStatus();
            try
            {
                int error = 0;
                List<string> errorT = new List<string>();
                string catId = await this._commonFunc.getDocCategoryId(documentdtl.CategoryCode.ToString());
                if (!string.IsNullOrEmpty(catId))
                {
                    odatab.Add("eqs_doccategory@odata.bind", $"eqs_doccategories({catId})");
                }
                else
                {
                    error = 1;
                    errorT.Add("Category");
                }
                string subcatId = await this._commonFunc.getDocSubentityId(documentdtl.SubcategoryCode.ToString(), documentdtl.CategoryCode.ToString());
                if (!string.IsNullOrEmpty(subcatId))
                {
                    odatab.Add("eqs_docsubcategory@odata.bind", $"eqs_docsubcategories({subcatId})");
                }
                else
                {
                    error = 1;
                    errorT.Add("Sub-Category");
                }
                string doctype = await this._commonFunc.getDocTypeId(documentdtl.DocumentType.ToString());
                if (!string.IsNullOrEmpty(doctype))
                {
                    odatab.Add("eqs_doctype@odata.bind", $"eqs_doctypes({doctype})");
                }
                else
                {
                    error = 1;
                    errorT.Add("Doctype");
                }

                if (error==1)
                {
                    createDocument.ErrorMessage = string.Join(",", errorT) + " not found in CRM!";
                    createDocument.Status = "CRM-ERROR-102";
                    return createDocument;
                }

                odatab.Add("eqs_dmsrequestid", documentdtl.DmsDocumentID.ToString());
                odatab.Add("eqs_issuedat", documentdtl.IssuedAt.ToString());

                string dd = ""; string mm = ""; string yy = "";

                if (!string.IsNullOrEmpty(documentdtl.IssueDate.ToString()))
                {
                    dd = documentdtl.IssueDate.ToString().Substring(0, 2);
                    mm = documentdtl.IssueDate.ToString().Substring(3, 2);
                    yy = documentdtl.IssueDate.ToString().Substring(6, 4);

                    odatab.Add("eqs_issuedate", yy + "-" + mm + "-" + dd);
                }
                if (!string.IsNullOrEmpty(documentdtl.ExpiryDate.ToString()))
                {
                    dd = documentdtl.ExpiryDate.ToString().Substring(0, 2);
                    mm = documentdtl.ExpiryDate.ToString().Substring(3, 2);
                    yy = documentdtl.ExpiryDate.ToString().Substring(6, 4);

                    odatab.Add("eqs_expirydate", yy + "-" + mm + "-" + dd);
                }

                odatab.Add("eqs_verificationstatus", documentdtl.VerificationStatus.ToString());

                if (!string.IsNullOrEmpty(documentdtl.VerifiedOn.ToString()))
                {
                    dd = documentdtl.VerifiedOn.ToString().Substring(0, 2);
                    mm = documentdtl.VerifiedOn.ToString().Substring(3, 2);
                    yy = documentdtl.VerifiedOn.ToString().Substring(6, 4);

                    odatab.Add("eqs_verifiedon", yy + "-" + mm + "-" + dd);
                }

                string validateby = await this._commonFunc.getSystemuserId(documentdtl.VerifiedBy.ToString());
                if (!string.IsNullOrEmpty(validateby))
                {
                    odatab.Add("eqs_verifiedbyid@odata.bind", $"systemusers({validateby})");
                }

                string leadid = await this._commonFunc.getLeadId(documentdtl.MappedCustomerLead.ToString());
                if (!string.IsNullOrEmpty(leadid))
                {
                    odatab.Add("eqs_leadid@odata.bind", $"leads({leadid})");
                }
                string LeadAccount = await this._commonFunc.getLeadAccountId(documentdtl.MappedAccountLead.ToString());
                if (!string.IsNullOrEmpty(LeadAccount))
                {
                    odatab.Add("eqs_leadaccountid@odata.bind", $"eqs_leadaccounts({LeadAccount})");
                }
                string customerid = await this._commonFunc.getCustomerId(documentdtl.MappedUCIC.ToString());
                if (!string.IsNullOrEmpty(customerid))
                {
                    odatab.Add("eqs_ucicid@odata.bind", $"contacts({customerid})");
                }
                string accountid = await this._commonFunc.getAccountId(documentdtl.MappedAccount.ToString());
                if (!string.IsNullOrEmpty(accountid))
                {
                    odatab.Add("eqs_accountnumberid@odata.bind", $"eqs_accounts({accountid})");
                }
                string caseid = await this._commonFunc.getCaseId(documentdtl.MappedServiceRequest.ToString());
                if (!string.IsNullOrEmpty(caseid))
                {
                    odatab.Add("eqs_CaseId@odata.bind", $"incidents({caseid})");
                }

                string postDataParametr = JsonConvert.SerializeObject(odatab);
                var Document_details = await this._queryParser.HttpApiCall($"eqs_leaddocuments()?$select=eqs_documentid", HttpMethod.Post, postDataParametr);
                var Documentid = CommonFunction.GetIdFromPostRespons201(Document_details[0]["responsebody"], "eqs_documentid");
                createDocument.DocId = Documentid.ToString();
                createDocument.SubCategoryCode = documentdtl.SubcategoryCode.ToString();
                createDocument.Status = "Success";
                return createDocument;

            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateDocument", ex.Message);
                throw ex;
            }

        }

        private async Task<DocUpdateStatus> UpdateDocument(dynamic documentdtl)
        {
            try
            {
                int error = 0;
                List<string> errorT = new List<string>();
                DocUpdateStatus updateDocument = new DocUpdateStatus();
                Dictionary<string, string> odatab = new Dictionary<string, string>();
                string DocumentID = await this._commonFunc.getDocumentID(documentdtl.CRMDocumentID.ToString());
                if (!string.IsNullOrEmpty(DocumentID))
                {
                    string catId = await this._commonFunc.getDocCategoryId(documentdtl.CategoryCode.ToString());
                    if (!string.IsNullOrEmpty(catId))
                    {
                        odatab.Add("eqs_doccategory@odata.bind", $"eqs_doccategories({catId})");
                    }
                    else
                    {
                        error = 1;
                        errorT.Add("Category");
                    }

                    string subcatId = await this._commonFunc.getDocSubentityId(documentdtl.SubcategoryCode.ToString(), documentdtl.CategoryCode.ToString());
                    if (!string.IsNullOrEmpty(subcatId))
                    {
                        odatab.Add("eqs_docsubcategory@odata.bind", $"eqs_docsubcategories({subcatId})");
                    }
                    else
                    {
                        error = 1;
                        errorT.Add("Sub-Category");
                    }

                    string doctype = await this._commonFunc.getDocTypeId(documentdtl.DocumentType.ToString());
                    if (!string.IsNullOrEmpty(doctype))
                    {
                        odatab.Add("eqs_doctype@odata.bind", $"eqs_doctypes({doctype})");
                    }
                    else
                    {
                        error = 1;
                        errorT.Add("Doctype");
                    }

                    if (error == 1)
                    {
                        updateDocument.ErrorMessage = string.Join(",", errorT) + " not found in CRM!";
                        updateDocument.Status = "CRM-ERROR-102";
                        return updateDocument;
                    }


                    odatab.Add("eqs_dmsrequestid", documentdtl.DmsDocumentID.ToString());
                    odatab.Add("eqs_issuedat", documentdtl.IssuedAt.ToString());

                    string dd = ""; string mm = ""; string yy = "";

                    if (!string.IsNullOrEmpty(documentdtl.IssueDate.ToString()))
                    {
                        dd = documentdtl.IssueDate.ToString().Substring(0, 2);
                        mm = documentdtl.IssueDate.ToString().Substring(3, 2);
                        yy = documentdtl.IssueDate.ToString().Substring(6, 4);

                        odatab.Add("eqs_issuedate", yy + "-" + mm + "-" + dd);
                    }
                    if (!string.IsNullOrEmpty(documentdtl.ExpiryDate.ToString()))
                    {
                        dd = documentdtl.ExpiryDate.ToString().Substring(0, 2);
                        mm = documentdtl.ExpiryDate.ToString().Substring(3, 2);
                        yy = documentdtl.ExpiryDate.ToString().Substring(6, 4);

                        odatab.Add("eqs_expirydate", yy + "-" + mm + "-" + dd);
                    }
                    odatab.Add("eqs_verificationstatus", documentdtl.VerificationStatus.ToString());

                    if (!string.IsNullOrEmpty(documentdtl.VerifiedOn.ToString()))
                    {
                        dd = documentdtl.VerifiedOn.ToString().Substring(0, 2);
                        mm = documentdtl.VerifiedOn.ToString().Substring(3, 2);
                        yy = documentdtl.VerifiedOn.ToString().Substring(6, 4);

                        odatab.Add("eqs_verifiedon", yy + "-" + mm + "-" + dd);
                    }

                    string validateby = await this._commonFunc.getSystemuserId(documentdtl.VerifiedBy.ToString());
                    if (!string.IsNullOrEmpty(validateby))
                    {
                        odatab.Add("eqs_verifiedbyid@odata.bind", $"systemusers({validateby})");
                    }

                    string leadid = await this._commonFunc.getLeadId(documentdtl.MappedCustomerLead.ToString());
                    if (!string.IsNullOrEmpty(leadid))
                    {
                        odatab.Add("eqs_leadid@odata.bind", $"leads({leadid})");
                    }
                    string LeadAccount = await this._commonFunc.getLeadAccountId(documentdtl.MappedAccountLead.ToString());
                    if (!string.IsNullOrEmpty(LeadAccount))
                    {
                        odatab.Add("eqs_leadaccountid@odata.bind", $"eqs_leadaccounts({LeadAccount})");
                    }
                    string customerid = await this._commonFunc.getCustomerId(documentdtl.MappedUCIC.ToString());
                    if (!string.IsNullOrEmpty(customerid))
                    {
                        odatab.Add("eqs_ucicid@odata.bind", $"contacts({customerid})");
                    }
                    string accountid = await this._commonFunc.getAccountId(documentdtl.MappedAccount.ToString());
                    if (!string.IsNullOrEmpty(accountid))
                    {
                        odatab.Add("eqs_accountnumberid@odata.bind", $"eqs_accounts({accountid})");
                    }
                    string caseid = await this._commonFunc.getCaseId(documentdtl.MappedServiceRequest.ToString());
                    if (!string.IsNullOrEmpty(caseid))
                    {
                        odatab.Add("eqs_CaseId@odata.bind", $"incidents({caseid})");
                    }

                    string postDataParametr = JsonConvert.SerializeObject(odatab);
                    var Document_details = await this._queryParser.HttpApiCall($"eqs_leaddocuments({DocumentID})", HttpMethod.Patch, postDataParametr);
                    updateDocument.DocId = documentdtl.CRMDocumentID.ToString();
                    updateDocument.SubCategoryCode = documentdtl.SubcategoryCode.ToString();
                    updateDocument.Status = "Success";
                    return updateDocument;
                }
                else
                {
                    return await this.CreateDocument(documentdtl);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("UpdateDocument", ex.Message);
                throw ex;
            }

        }

        public async Task<GetDgDocDtlReturn> GetDocumentList(dynamic RequestData)
        {
            GetDgDocDtlReturn ldRtPrm = new GetDgDocDtlReturn();

            RequestData = await this.getRequestData(RequestData, "GetDigiDocumentDetails");
            try
            {
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "GetDigiDocumentappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        string query_url = $"eqs_leaddocuments()?$select=eqs_documentid,_eqs_doccategory_value,_eqs_docsubcategory_value,_eqs_doctype_value,eqs_dmsrequestid,eqs_issuedat,eqs_issuedate,eqs_expirydate,eqs_verificationstatus,eqs_verifiedon,_eqs_verifiedbyid_value,_eqs_leadid_value,_eqs_leadaccountid_value,_eqs_ucicid_value,_eqs_accountnumberid_value,_eqs_caseid_value&$filter=";
                        int IdExist = 0;
                        if (!string.IsNullOrEmpty(RequestData.CustomerLead.ToString()))
                        {
                            string LeadId = await this._commonFunc.getLeadId(RequestData.CustomerLead.ToString());
                            if (!string.IsNullOrEmpty(LeadId))
                            {
                                IdExist = 1;
                                query_url = query_url + $"_eqs_leadid_value eq '{LeadId}'";
                            }
                        }
                        else if (!string.IsNullOrEmpty(RequestData.AccountLead.ToString()))
                        {
                            string LeadAcc = await this._commonFunc.getLeadAccountId(RequestData.AccountLead.ToString());
                            if (!string.IsNullOrEmpty(LeadAcc))
                            {
                                IdExist = 1;
                                query_url = query_url + $"_eqs_leadaccountid_value eq '{LeadAcc}'";
                            }
                        }
                        else if (!string.IsNullOrEmpty(RequestData.UCIC.ToString()))
                        {
                            string custonerId = await this._commonFunc.getCustomerId(RequestData.UCIC.ToString());
                            if (!string.IsNullOrEmpty(custonerId))
                            {
                                IdExist = 1;
                                query_url = query_url + $"_eqs_ucicid_value eq '{custonerId}'";
                            }
                        }
                        else if (!string.IsNullOrEmpty(RequestData.AccountNumbe.ToString()))
                        {
                            string AccountId = await this._commonFunc.getAccountId(RequestData.AccountNumbe.ToString());
                            if (!string.IsNullOrEmpty(AccountId))
                            {
                                IdExist = 1;
                                query_url = query_url + $"_eqs_accountnumberid_value eq '{AccountId}'";

                            }
                        }
                        else if (!string.IsNullOrEmpty(RequestData.CaseNumber.ToString()))
                        {
                            string caseId = await this._commonFunc.getCaseId(RequestData.CaseNumber.ToString());
                            if (!string.IsNullOrEmpty(caseId))
                            {
                                IdExist = 1;
                                query_url = query_url + $"_eqs_caseid_value eq '{caseId}'";
                            }
                        }
                        else
                        {
                            this._logger.LogInformation("GetDocumentList", "Input parameters are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                            return ldRtPrm;
                        }

                        if (IdExist == 1)
                        {
                            var documentList = await this._commonFunc.getDocumentList(query_url);
                            ldRtPrm.DocumentIDs = documentList;
                            ldRtPrm.ReturnCode = "CRM-SUCCESS";
                            ldRtPrm.Message = OutputMSG.Case_Success;
                        }
                        else
                        {
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                        }

                    }
                    else
                    {
                        this._logger.LogInformation("GetDocumentList", "Input parameters are incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("GetDocumentList", "Input parameters are incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("GetDocumentList", ex.Message);
                this._logger.LogInformation("GetDocumentList", "Input parameters are incorrect");
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = OutputMSG.Incorrect_Input;
            }

            return ldRtPrm;
        }

        private string ValidateDocumentData(dynamic documentdtl, List<MasterConfiguration> configurations)
        {
            int ValidationError = 0;
            string errorText = string.Empty;

            var expirydateCategories = (from c in configurations where c.Key == "docid_issue_expirydate" select c.Value)?.FirstOrDefault()?.ToString();
            var issuedateCategories = (from c in configurations where c.Key == "docid_issuedat" select c.Value)?.FirstOrDefault()?.ToString();
            if (string.IsNullOrEmpty(documentdtl.DocumentType?.ToString()))
            {
                errorText += "DocumentType is mandatory";
                ValidationError = 1;
            }
            if (string.IsNullOrEmpty(documentdtl.DmsDocumentID?.ToString()))
            {
                errorText += "DmsDocumentID is mandatory";
                ValidationError = 1;
            }
            if (string.IsNullOrEmpty(documentdtl.CategoryCode?.ToString()))
            {
                errorText += "CategoryCode is mandatory";
                ValidationError = 1;
            }
            if (string.IsNullOrEmpty(documentdtl.SubcategoryCode?.ToString()))
            {
                errorText += "SubcategoryCode is mandatory";
                ValidationError = 1;
            }
            else
            {
                if (expirydateCategories.Contains(documentdtl.SubcategoryCode.ToString()) || issuedateCategories.Contains(documentdtl.SubcategoryCode.ToString()))
                {
                    if ((string.IsNullOrEmpty(documentdtl.ExpiryDate.ToString()) || (string.IsNullOrEmpty(documentdtl.IssueDate.ToString()))))
                    {
                        if ((string.IsNullOrEmpty(documentdtl.ExpiryDate?.ToString()) && (string.IsNullOrEmpty(documentdtl.IssueDate?.ToString()))))
                        {
                            errorText += $"ExpiryDate && IssueDate cannot be empty for Sub Category {documentdtl.SubcategoryCode.ToString()}";
                            ValidationError = 1;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(documentdtl.ExpiryDate?.ToString()))
                            {
                                errorText += $"ExpiryDate cannot be empty for Sub Category {documentdtl.SubcategoryCode.ToString()}";
                                ValidationError = 1;
                            }
                            if (string.IsNullOrEmpty(documentdtl.IssueDate?.ToString()))
                            {
                                errorText += $"IssueDate cannot be empty for Sub Category {documentdtl.SubcategoryCode.ToString()}";
                                ValidationError = 1;
                            }
                        }
                    }
                    else if ((string.IsNullOrEmpty(documentdtl.ExpiryDate?.ToString()) && (string.IsNullOrEmpty(documentdtl.IssueDate?.ToString()))))
                    {
                        errorText += $"ExpiryDate && IssueDate cannot be empty for Sub Category {documentdtl.SubcategoryCode.ToString()}";
                        ValidationError = 1;
                    }
                }
            }
            
            if (ValidationError > 0)
            {
                return string.Join(",", errorText);
            }
            else
            {
                return "ok";
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