namespace GFSProduct
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

    public class FetchGFSProductExecution : IFetchGFSProductExecution
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

                
        private ICommonFunction _commonFunc;

        public FetchGFSProductExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
           
           
        }


        public async Task<GFSProducrListReturn> ValidateProductInput(dynamic RequestData)
        {
            GFSProducrListReturn ldRtPrm = new GFSProducrListReturn();
            RequestData = await this.getRequestData(RequestData, "FetchGFSProductList");
            try
            {
                string CategoryCode = RequestData.CategoryCode;
                string leadId = RequestData.LeadID;
                string customerID = RequestData.UCIC;

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "FetchGFSProductappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        if (!string.IsNullOrEmpty(CategoryCode) && CategoryCode != "")
                        {
                            if (!string.IsNullOrEmpty(leadId) && leadId != "")
                            {
                                ldRtPrm = await this.getApplicentToProduct(leadId, CategoryCode);
                            }
                            else if (!string.IsNullOrEmpty(customerID) && customerID != "")
                            {
                                ldRtPrm = await this.getUcicToProduct(customerID, CategoryCode);
                            }
                            else
                            {
                                this._logger.LogInformation("ValidateProductInput", "leadId or customerID is incorrect");
                                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                                ldRtPrm.Message = "leadId or customerID is incorrect";
                            }
                            

                        }
                        else
                        {
                            this._logger.LogInformation("ValidateProductInput", "CategoryCode is incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "CategoryCode is incorrect";
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateProductInput", "Transaction_ID or  Channel_ID is incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or  Channel_ID is incorrect.";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateProductInput", "AppKey incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "AppKey incorrect";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateProductInput", ex.Message);
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

        

        public async Task<GFSProducrListReturn> getUcicToProduct(string customerID, string CategoryCode)
        {
            GFSProducrListReturn csRtPrm = new GFSProducrListReturn();
            try
            {
                string CategoryId = await this._commonFunc.getCategoryId(CategoryCode);
                JArray applicentDetails = await this._commonFunc.getCustomerDetails(customerID);
                if (applicentDetails.Count > 0)
                {
                    productFilter product_Filter = new productFilter();

                    if (!string.IsNullOrEmpty(applicentDetails[0]["eqs_gender"].ToString()) && applicentDetails[0]["eqs_gender"].ToString() == "789030001")
                    {
                        product_Filter.gender = "Woman";
                    }

                    if (!string.IsNullOrEmpty(applicentDetails[0]["eqs_age"].ToString()))
                    {
                        product_Filter.age = applicentDetails[0]["eqs_age"].ToString();
                    }

                    if (!string.IsNullOrEmpty(applicentDetails[0]["_eqs_subentitytypeid_value"].ToString()))
                    {
                        string subentity = await this._commonFunc.getSubentity(applicentDetails[0]["_eqs_subentitytypeid_value"].ToString());
                        if (subentity.ToLower() == "foreigners")
                        {
                            product_Filter.subentity = "NRI";
                        }
                    }

                    if (!string.IsNullOrEmpty(applicentDetails[0]["eqs_customersegment"].ToString()) && applicentDetails[0]["eqs_customersegment"].ToString() == "789030002")
                    {
                        product_Filter.customerSegment = "elite";
                    }

                    if (!string.IsNullOrEmpty(applicentDetails[0]["eqs_isstafffcode"].ToString()))
                    {
                        product_Filter.gender = "staff";
                    }

                    product_Filter.productCategory = CategoryId;

                    csRtPrm = await this.getProductDetails(product_Filter, CategoryCode);
                }
                else
                {
                    this._logger.LogInformation("getUcicToProduct", "Applicent Details not found.");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = "Applicent Details not found.";
                }
            }
            catch(Exception ex)
            {
                this._logger.LogError("getUcicToProduct", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = OutputMSG.Incorrect_Input;
            }
            
            

            return csRtPrm;
        }

        public async Task<GFSProducrListReturn> getApplicentToProduct(string ApplicantId, string CategoryCode)
        {
            GFSProducrListReturn csRtPrm = new GFSProducrListReturn();
            try
            {
                string CategoryId = await this._commonFunc.getCategoryId(CategoryCode);
                JArray applicentDetails = await this._commonFunc.getApplicantDetails(ApplicantId);
                
                if (applicentDetails.Count > 0)
                {
                    productFilter product_Filter = new productFilter();

                    if (!string.IsNullOrEmpty(applicentDetails[0]["eqs_gendercode"].ToString()) && applicentDetails[0]["eqs_gendercode"].ToString() == "789030001")
                    {
                        product_Filter.gender = "Woman";
                    }

                    if (!string.IsNullOrEmpty(applicentDetails[0]["eqs_leadage"].ToString()))
                    {
                        product_Filter.age = applicentDetails[0]["eqs_leadage"].ToString();
                    }

                    if (!string.IsNullOrEmpty(applicentDetails[0]["_eqs_subentity_value"].ToString()))
                    {
                        string subentity = await this._commonFunc.getSubentity(applicentDetails[0]["_eqs_subentity_value"].ToString());
                        if (subentity.ToLower() == "foreigners" || subentity.ToLower() == "non resident individual")
                        {
                            product_Filter.subentity = "NRI";
                        }                        
                    }

                    if (!string.IsNullOrEmpty(applicentDetails[0]["eqs_customersegment"].ToString()) && applicentDetails[0]["eqs_customersegment"].ToString() == "789030002")
                    {
                        product_Filter.customerSegment = "elite";
                    }

                    if (!string.IsNullOrEmpty(applicentDetails[0]["eqs_isstaffcode"].ToString()))
                    {
                        product_Filter.gender = "staff";
                    }

                    product_Filter.productCategory = CategoryId;

                    csRtPrm = await this.getProductDetails(product_Filter, CategoryCode);
                }
                else
                {
                    this._logger.LogInformation("getUcicToProduct", "Applicent Details not found.");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = "Applicent Details not found.";
                }

            }
            catch (Exception ex)
            {
                this._logger.LogError("getApplicentToProduct", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = OutputMSG.Incorrect_Input;
            }

            return csRtPrm;
        }

        public async Task<GFSProducrListReturn> getProductDetails(productFilter product_Filter, string CategoryCode)
        {
            GFSProducrListReturn csRtPrm = new GFSProducrListReturn();
            var productDetails = await this._commonFunc.getProductData(product_Filter);
            if (productDetails.Count >0 )
            {
                csRtPrm.fdproductsApplicable = new List<FDProductsApplicable>();
                csRtPrm.rdproductsApplicable = new List<RDProductsApplicable>();
                csRtPrm.applicableCASAProducts = new List<ApplicableCASAProducts>();

            }
            foreach(dynamic product_detail in productDetails)
            {
                if (CategoryCode == "PCAT04")
                {
                    FDProductsApplicable fDProductsApplicable = new FDProductsApplicable();
                    fDProductsApplicable.productCode = product_detail.eqs_productcode;
                    fDProductsApplicable.productName = product_detail.eqs_name;
                  
                    fDProductsApplicable.minTenureDays = product_detail.eqs_mintenuredays;
                    fDProductsApplicable.maxTenureDays = product_detail.eqs_maxtenuredays;
                    fDProductsApplicable.minTenureMonths = product_detail.eqs_mintenuremonths;
                    fDProductsApplicable.maxTenureMonths = product_detail.eqs_maxtenuremonths;
                    fDProductsApplicable.minAmount = product_detail.eqs_minamount;
                    fDProductsApplicable.maxAmount = product_detail.eqs_maxamount;
                    fDProductsApplicable.depositVariance = product_detail.eqs_depositvariance;
                    fDProductsApplicable.payoutFrequency = product_detail.eqs_payoutfrequency;
                    fDProductsApplicable.compoundingFrequency = product_detail.eqs_compoundingfrequency;
                    fDProductsApplicable.renewalOptions = product_detail.eqs_renewaloptions;
                    fDProductsApplicable.payoutFrequencyType = product_detail.eqs_payoutfrequencytype;
                    fDProductsApplicable.compoundingFrequencyType = product_detail.eqs_compoundingfrequencytype;
                    fDProductsApplicable.isElite = product_detail.eqs_iselite;

                    csRtPrm.fdproductsApplicable.Add(fDProductsApplicable);
                }
                else if (CategoryCode == "PCAT05")
                {
                    RDProductsApplicable rdProductsApplicable = new RDProductsApplicable();
                    rdProductsApplicable.productCode = product_detail.eqs_productcode;
                    rdProductsApplicable.productName = product_detail.eqs_name;
                   
                    rdProductsApplicable.minTenureDays = product_detail.eqs_mintenuredays;
                    rdProductsApplicable.maxTenureDays = product_detail.eqs_maxtenuredays;
                    rdProductsApplicable.minTenureMonths = product_detail.eqs_mintenuremonths;
                    rdProductsApplicable.maxTenureMonths = product_detail.eqs_maxtenuremonths;
                    rdProductsApplicable.minAmount = product_detail.eqs_minamount;
                    rdProductsApplicable.maxAmount = product_detail.eqs_maxamount;
                    rdProductsApplicable.depositVariance = product_detail.eqs_depositvariance;
                    rdProductsApplicable.payoutFrequency = product_detail.eqs_payoutfrequency;
                    rdProductsApplicable.compoundingFrequency = product_detail.eqs_compoundingfrequency;
                    rdProductsApplicable.renewalOptions = product_detail.eqs_renewaloptions;
                    rdProductsApplicable.payoutFrequencyType = product_detail.eqs_payoutfrequencytype;
                    rdProductsApplicable.compoundingFrequencyType = product_detail.eqs_compoundingfrequencytype;
                    rdProductsApplicable.isElite = product_detail.eqs_iselite;

                    csRtPrm.rdproductsApplicable.Add(rdProductsApplicable);
                }
                else if (CategoryCode == "PCAT02" || CategoryCode == "PCAT01")
                {
                    ApplicableCASAProducts applicableCASAProducts = new ApplicableCASAProducts();
                    applicableCASAProducts.productCode = product_detail.eqs_productcode;
                    applicableCASAProducts.productName = product_detail.eqs_name;
                  
                    applicableCASAProducts.chequeBook = product_detail.eqs_chequebook;
                    applicableCASAProducts.debitCard = product_detail.eqs_debitcard;
                    applicableCASAProducts.applicableDebitCard = product_detail.eqs_applicabledebitcard;
                    applicableCASAProducts.defaultDebitCard = product_detail.eqs_defaultdebitcard;
                    applicableCASAProducts.instaKit = product_detail.eqs_InstaKit;
                    applicableCASAProducts.doorStep = product_detail.eqs_doorstep;
                  
                    applicableCASAProducts.PMAY = product_detail.eqs_pmay;
                    applicableCASAProducts.isElite = product_detail.eqs_iselite;
                    applicableCASAProducts.srnoofchequeleaves = product_detail.eqs_srnoofchequeleaves;
                    applicableCASAProducts.noofchequeleaves = product_detail.eqs_noofchequeleaves;
                    applicableCASAProducts.srdefaultchequeleaves = product_detail.eqs_srdefaultchequeleaves;
                    applicableCASAProducts.defaultchequeleaves = product_detail.eqs_defaultchequeleaves;


                    csRtPrm.applicableCASAProducts.Add(applicableCASAProducts);
                }

            }

            csRtPrm.ReturnCode = "CRM-SUCCESS";
            csRtPrm.Message = OutputMSG.Case_Success;

            return csRtPrm;
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
