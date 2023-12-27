namespace DigiCustLead
{

    using Microsoft.Extensions.Caching.Memory;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json.Linq;
    using System.Net;
    using System.Diagnostics.Metrics;
    using CRMConnect;


    public class CommonFunction : ICommonFunction
    {
        public IQueryParser _queryParser;
        private ILoggers _logger;
        public IMemoryCache _cache;

        public CommonFunction(IMemoryCache cache, ILoggers logger, IQueryParser queryParser)
        {
            this._queryParser = queryParser;
            this._logger = logger;
            this._cache = cache;
        }
        public async Task<string> AcquireNewTokenAsync()
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                //string tenantID = "62c7002c-da9e-4e54-86e7-4c53c52518ad";
                //string ClientID = "6cded931-9ddc-47d4-a31b-c4c135036c3b";
                //string ClientSecrets = "OQC8Q~gx9aRsgmf.Q-v.2nfsrgwsdhF1VadXCc_H";

                string tenantID = "e22e4eaa-623f-4164-abb8-ac89a5a17e13";
                string ClientID = "fa5dddfc-4e66-4522-93aa-9e46e78d2c00";
                string ClientSecrets = "Aym8Q~LzrJIqAGQ9CtkmMWkYU3~-ACdfoDFdubjX";

                var authority = $"https://login.microsoftonline.com/{tenantID}";
                var app =
                    ConfidentialClientApplicationBuilder.Create(ClientID)
                                                        .WithClientSecret(ClientSecrets)
                                                        .WithAuthority(authority)
                                                        .Build();

                var authResult = await app.AcquireTokenForClient(new[] { "https://orgc39e5cd7.crm8.dynamics.com/.default" })
                    .ExecuteAsync(cancellationTokenSource.Token).ConfigureAwait(true);

                string bearerHeaderValue = authResult.AccessToken;
                cancellationTokenSource.Cancel();
                return bearerHeaderValue;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            
        }

        public static string GetIdFromPostRespons(string PResponseData)
        {
           
           string Respons_Id = PResponseData.Substring(PResponseData.IndexOf('(') + 1, PResponseData.IndexOf(')') - PResponseData.IndexOf('(') - 1);
            return Respons_Id;
        }

        public static string GetIdFromPostRespons201(dynamic PResponseData, string datakey)
        {
            string Respons_Id = PResponseData[datakey];
            return Respons_Id;
        }

        public async Task<string> getIDFromGetResponce(string primaryField ,List<JObject> RsponsData)
        {
            string resourceID = "";
            foreach (JObject item in RsponsData)
            {
                if (Enum.TryParse(item["responsecode"].ToString(), out HttpStatusCode responseStatus) && responseStatus == HttpStatusCode.OK)
                {
                    dynamic responseValue = item["responsebody"];
                    JArray resArray = new JArray();
                    string urlMetaData = string.Empty;
                    if (responseValue?.value != null)
                    {
                        resArray = (JArray)responseValue?.value;
                        urlMetaData = responseValue["@odata.context"];
                    }
                    else if (responseValue is JArray)
                    {
                        resArray = responseValue;

                    }
                    else
                    {
                        resArray.Add(responseValue);
                        urlMetaData = responseValue["@odata.context"];
                    }

                    if (resArray != null && resArray.Any())
                    {
                        //int startIndex = urlMetaData.IndexOf("metadata#", StringComparison.Ordinal) + "metadata#".Length;
                        //int endIndex = urlMetaData.IndexOf("(", StringComparison.Ordinal);
                        //string entityName = urlMetaData.Substring(startIndex, endIndex - startIndex);

                        foreach (var record in resArray)
                        {
                            resourceID = record[primaryField]?.ToString();
                        }

                    }
                }
            }
                return resourceID;
        }

        public async Task<JArray> getDataFromResponce(List<JObject> RsponsData)
        {
            string resourceID = "";
            foreach (JObject item in RsponsData)
            {
                if (Enum.TryParse(item["responsecode"].ToString(), out HttpStatusCode responseStatus) && responseStatus == HttpStatusCode.OK)
                {
                    dynamic responseValue = item["responsebody"];
                    JArray resArray = new JArray();
                    string urlMetaData = string.Empty;
                    if (responseValue?.value != null)
                    {
                        resArray = (JArray)responseValue?.value;
                        urlMetaData = responseValue["@odata.context"];
                    }
                    else if (responseValue is JArray)
                    {
                        resArray = responseValue;

                    }
                    else
                    {
                        resArray.Add(responseValue);
                        urlMetaData = responseValue["@odata.context"];
                    }

                    if (resArray != null && resArray.Any())
                    {

                        return resArray;

                    }
                }
            }
            return new JArray();
        }

        public async Task<string> getIDfromMSDTable(string tablename, string idfield, string filterkey, string filtervalue)
        {
            try
            {
                string Table_Id;
                string TableId;
                if (!this.GetMvalue<string>(tablename + filtervalue, out Table_Id))
                {
                    string query_url = $"{tablename}()?$select={idfield}&$filter={filterkey} eq '{filtervalue}'";
                    var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                    TableId = await this.getIDFromGetResponce(idfield, responsdtails);

                    this.SetMvalue<string>(tablename + filtervalue, 1400, TableId);
                }
                else
                {
                    TableId = Table_Id;
                }
                return TableId;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getIDfromMSDTable", ex.Message, $"Table {tablename} filterkey {filterkey} filtervalue {filtervalue}");
                throw;
            }
        }

        public async Task<Dictionary<string, string>> getProductId(string ProductCode)
        {
            try
            {
                string query_url = $"eqs_products()?$select=eqs_productid,_eqs_businesscategoryid_value,_eqs_productcategory_value,eqs_crmproductcategorycode&$filter=eqs_productcode eq '{ProductCode}'";
                var productdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                string ProductId = await this.getIDFromGetResponce("eqs_productid", productdtails);
                string businesscategoryid = await this.getIDFromGetResponce("_eqs_businesscategoryid_value", productdtails);
                string productcategory = await this.getIDFromGetResponce("_eqs_productcategory_value", productdtails);
                string crmproductcategorycode = await this.getIDFromGetResponce("eqs_crmproductcategorycode", productdtails);
                Dictionary<string, string> ProductData = new Dictionary<string, string>() {
                    { "ProductId", ProductId },
                    { "businesscategoryid", businesscategoryid },
                    { "productcategory", productcategory },
                    { "crmproductcategorycode", crmproductcategorycode }
                };
                return ProductData;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getProductId", ex.Message);
                throw ex;
            }
        }

        public async Task<string> getBranchId(string BranchCode)
        {
            return await this.getIDfromMSDTable("eqs_branchs", "eqs_branchid", "eqs_branchidvalue", BranchCode);
        }

        public async Task<string> getRMId(string Code)
        {
            return await this.getIDfromMSDTable("eqs_rmemployees", "eqs_rmemployeeid", "eqs_rmempidslot", Code);
        }

        public async Task<string> getBranchText(string BranchwId)
        {
            return await this.getIDfromMSDTable("eqs_branchs", "eqs_branchidvalue", "eqs_branchid", BranchwId);
        }

        public async Task<string> getTitleId(string Title)
        {
            return await this.getIDfromMSDTable("eqs_titles", "eqs_titleid", "eqs_name", Title);
        }
        public async Task<string> getTitleText(string TitleID)
        {
            return await this.getIDfromMSDTable("eqs_titles", "eqs_name", "eqs_titleid", TitleID);
        }
        public async Task<string> getEntityID(string Entity)
        {
            return await this.getIDfromMSDTable("eqs_entitytypes", "eqs_entitytypeid", "eqs_name", Entity);
        }
        public async Task<string> getEntityName(string EntityId)
        {
            return await this.getIDfromMSDTable("eqs_entitytypes", "eqs_name", "eqs_entitytypeid", EntityId);
        }
        public async Task<string> getSubentitytypeID(string SubEntityType, string SubEntityKey)
        {
            return await this.getIDfromMSDTable("eqs_subentitytypes", "eqs_subentitytypeid", $"eqs_flagtype eq '{SubEntityType}' and eqs_key", SubEntityKey);
        }
        public async Task<string> getSubentitytypeText(string SubentitytypeID)
        {
            return await this.getIDfromMSDTable("eqs_subentitytypes", "eqs_key", "eqs_subentitytypeid", SubentitytypeID);
        }

        public async Task<string> getRelationshipID(string relationshipCode)
        {
            return await this.getIDfromMSDTable("eqs_relationships", "eqs_relationshipid", "eqs_relationship", relationshipCode);
        }
        public async Task<string> getRelationshipText(string relationshipId)
        {
            return await this.getIDfromMSDTable("eqs_relationships", "eqs_relationship", "eqs_relationshipid", relationshipId);
        }
        public async Task<string> getAccRelationshipID(string accrelationshipCode)
        {
            return await this.getIDfromMSDTable("eqs_accountrelationshipses", "eqs_accountrelationshipsid", "eqs_key", accrelationshipCode);
        }
        public async Task<string> getAccRelationshipText(string accrelationshipId)
        {
            return await this.getIDfromMSDTable("eqs_accountrelationshipses", "eqs_key", "eqs_accountrelationshipsid", accrelationshipId);
        }
        public async Task<string> getCountryID(string CountryCode)
        {
            return await this.getIDfromMSDTable("eqs_countries", "eqs_countryid", "eqs_countrycode", CountryCode);
        }
        public async Task<string> getCountryText(string CountryId)
        {
            return await this.getIDfromMSDTable("eqs_countries", "eqs_countrycode", "eqs_countryid", CountryId);
        }
        public async Task<string> getcorporatemasterID(string CorporateCode)
        {
            return await this.getIDfromMSDTable("eqs_corporatemasters", "eqs_corporatemasterid", "eqs_corporatecode", CorporateCode);
        }
        public async Task<string> getcorporatemasterText(string CorporateId)
        {
            return await this.getIDfromMSDTable("eqs_corporatemasters", "eqs_corporatecode", "eqs_corporatemasterid",  CorporateId);
        }
        public async Task<string> getdesignationmasterID(string DesignatioCode)
        {
            return await this.getIDfromMSDTable("eqs_designationmasters", "eqs_designationmasterid", "eqs_name", DesignatioCode);
        }
        public async Task<string> getdesignationmasterText(string DesignatioId)
        {
            return await this.getIDfromMSDTable("eqs_designationmasters", "eqs_name", "eqs_designationmasterid", DesignatioId);
        }

        public async Task<string> getStateID(string StateCode)
        {
            return await this.getIDfromMSDTable("eqs_states", "eqs_stateid", "eqs_statecode", StateCode);
        }
        public async Task<string> getStateText(string StateID)
        {
            return await this.getIDfromMSDTable("eqs_states", "eqs_statecode", "eqs_stateid", StateID);
        }
        public async Task<string> getCityID(string CityCode)
        {
            return await this.getIDfromMSDTable("eqs_cities", "eqs_cityid", "eqs_citycode", CityCode);
        }
        public async Task<string> getCityText(string CityId)
        {
            return await this.getIDfromMSDTable("eqs_cities", "eqs_citycode", "eqs_cityid", CityId);
        }
        public async Task<string> getPincodeID(string PincodeCode)
        {
            return await this.getIDfromMSDTable("eqs_pincodes", "eqs_pincodeid", "eqs_pincode", PincodeCode);
        }
        public async Task<string> getPurposeID(string Purpose)
        {
            return await this.getIDfromMSDTable("eqs_purposeofcreations", "eqs_purposeofcreationid", "eqs_name", Purpose);
        }
        public async Task<string> getPurposeText(string Purpose)
        {
            return await this.getIDfromMSDTable("eqs_purposeofcreations", "eqs_name", "eqs_purposeofcreationid", Purpose);
        }
        public async Task<string> getFatcaAddressID(string FatcaID, string AddressID)
        {
            if (string.IsNullOrEmpty(AddressID))
                return string.Empty;

            string query_url = $"eqs_leadaddresses()?$select=eqs_leadaddressid&$filter=_eqs_applicantfatca_value eq '{FatcaID}' and eqs_applicantaddressid eq '{AddressID}'";

            var adddtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
            var add_dtails = await this.getDataFromResponce(adddtails);
            if (add_dtails.Count > 0)
            {
                return add_dtails[0]["eqs_leadaddressid"].ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        public async Task<string> getAddressID(string DDEID, string AddressID, string types)
        {
            if (string.IsNullOrEmpty(AddressID)) 
                return string.Empty;
            
            string DDEField;
            if (types == "indv")
                DDEField = "_eqs_individualdde_value";
            else
                DDEField = "_eqs_corporatedde_value";
            
            string query_url = $"eqs_leadaddresses()?$select=eqs_leadaddressid&$filter={DDEField} eq '{DDEID}' and eqs_applicantaddressid eq '{AddressID}'";

            var adddtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
            var add_dtails = await this.getDataFromResponce(adddtails);
            if (add_dtails.Count > 0)
            {
                return add_dtails[0]["eqs_leadaddressid"].ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        public async Task<string> getFatcaID(string DDEID,string types)
        {
            if(types == "indv"){
                return await this.getIDfromMSDTable("eqs_customerfactcaothers", "eqs_customerfactcaotherid", "_eqs_indivapplicantddeid_value" , DDEID);
            }
            else
            {
                return await this.getIDfromMSDTable("eqs_customerfactcaothers", "eqs_customerfactcaotherid", "_eqs_ddecorporatecustomerid_value" , DDEID);
            }
            
        }
        public async Task<string> getDocumentId(string docId)
        {
            return await this.getIDfromMSDTable("eqs_leaddocuments", "eqs_leaddocumentid", "eqs_documentid", docId);            
        }

        public async Task<string> getDocCategoryId(string doccatCode)
        {
            return await this.getIDfromMSDTable("eqs_doccategories", "eqs_doccategoryid", "eqs_doccategorycode", doccatCode);
        }
        public async Task<string> getDocCategoryText(string doccatId)
        {
            return await this.getIDfromMSDTable("eqs_doccategories", "eqs_name", "eqs_doccategoryid", doccatId);
        }
        public async Task<string> getDocSubCategoryId(string docsubcatCode)
        {
            return await this.getIDfromMSDTable("eqs_docsubcategories", "eqs_docsubcategoryid", "eqs_docsubcategorycode", docsubcatCode);
        }
        public async Task<string> getDocSubCategoryText(string docsubcatId)
        {
            return await this.getIDfromMSDTable("eqs_docsubcategories", "eqs_name", "eqs_docsubcategoryid", docsubcatId);
        }
        public async Task<string> getDocTypeId(string docTypeCode)
        {
            return await this.getIDfromMSDTable("eqs_doctypes", "eqs_doctypeid", "eqs_name", docTypeCode);
        }
        public async Task<string> getDocTypeText(string docTypeId)
        {
            return await this.getIDfromMSDTable("eqs_doctypes", "eqs_name", "eqs_doctypeid", docTypeId);
        }
        public async Task<string> getBusinessTypeId(string businessTypeCode)
        {
            return await this.getIDfromMSDTable("eqs_businesstypes", "eqs_businesstypeid", "eqs_name", businessTypeCode);
        }
        public async Task<string> getBusinessTypeText(string businessTypeId)
        {
            return await this.getIDfromMSDTable("eqs_businesstypes", "eqs_name", "eqs_businesstypeid", businessTypeId);
        }
        public async Task<string> getIndustryId(string industryName)
        {
            return await this.getIDfromMSDTable("eqs_businessnatures", "eqs_businessnatureid", "eqs_name", industryName);
        }
        public async Task<string> getIndustryText(string industryId)
        {
            return await this.getIDfromMSDTable("eqs_businessnatures", "eqs_name", "eqs_businessnatureid", industryId);
        }
        public async Task<string> getBOId(string BOid)
        {
            return await this.getIDfromMSDTable("eqs_customerbos", "eqs_customerboid", "eqs_boid", BOid);
        }
        public async Task<string> getCPId(string CPid)
        {
            return await this.getIDfromMSDTable("eqs_customercps", "eqs_customercpid", "eqs_cpid", CPid);
        }
        public async Task<string> getIndividualDDEText(string DDEID)
        {
            return await this.getIDfromMSDTable("eqs_ddeindividualcustomers", "eqs_dataentryoperator", "eqs_ddeindividualcustomerid", DDEID);
        }
        public async Task<string> getFatcaText(string FatcaID)
        {
            return await this.getIDfromMSDTable("eqs_customerfactcaothers", "eqs_name", "eqs_customerfactcaotherid", FatcaID);
        }
        public async Task<string> getCorporateDDEText(string DDEID)
        {
            return await this.getIDfromMSDTable("eqs_ddecorporatecustomers", "eqs_dataentryoperator", "eqs_ddecorporatecustomerid", DDEID);
        }
        public async Task<string> getNomineeText(string FatcaID)
        {
            return await this.getIDfromMSDTable("eqs_ddeaccountnominees", "eqs_nomineename", "eqs_ddeaccountnomineeid", FatcaID);
        }
        public async Task<string> getCustomerText(string customerId)
        {
            return await this.getIDfromMSDTable("contacts", "fullname", "contactid", customerId);
        }
        public async Task<string> getAccountapplicantName(string AccountapplicantId)
        {
            return await this.getIDfromMSDTable("eqs_accountapplicants", "eqs_name", "eqs_accountapplicantid", AccountapplicantId);
        }        
        public async Task<string> getLeadsourceName(string leadsourceid)
        {
            return await this.getIDfromMSDTable("eqs_leadsources", "eqs_name", "eqs_leadsourceid", leadsourceid);
        }
        public async Task<string> getLeadsourceId(string leadsourceid)
        {
            return await this.getIDfromMSDTable("eqs_leadsources", "eqs_leadsourceid", "eqs_leadsourceidvalue",  leadsourceid);
        }
        public async Task<string> getSystemuserName(string systemuserid)
        {
            return await this.getIDfromMSDTable("systemusers", "fullname", "systemuserid", systemuserid);
        }
        public async Task<string> getBankName(string bankid)
        {
            return await this.getIDfromMSDTable("eqs_bankmasters", "eqs_name", "eqs_ddecorporatecustomerid", bankid);
        }

        public async Task<string> getKYCVerificationID(string DDEId, string type)
        {
            if (type== "Corp")
            {
                return await this.getIDfromMSDTable("eqs_ddeindividualcustomers", "_eqs_kycverificationdetailid_value", "eqs_ddeindividualcustomerid", DDEId);
            }
            else
            {
                return await this.getIDfromMSDTable("eqs_ddecorporatecustomers", "_eqs_kycverificationdetailid_value", "eqs_ddecorporatecustomerid", DDEId);
            }
           
        }
        public async Task<string> getDDEEntry(string AccountapplicantID, string type)
        {
            if (type == "Individual")
            {
                return await this.getIDfromMSDTable("eqs_ddeindividualcustomers", "eqs_ddeindividualcustomerid", "_eqs_accountapplicantid_value", AccountapplicantID);
            }
            else
            {
                return await this.getIDfromMSDTable("eqs_ddecorporatecustomers", "eqs_ddecorporatecustomerid", "_eqs_accountapplicantid_value", AccountapplicantID);
            }
            
        }


        public async Task<string> getDDEFinalAccountIndvData(string AccountNumber)
        {
            string finalValue = await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_dataentrystage", "Final");
            return await this.getIDfromMSDTable("eqs_ddeindividualcustomers", "eqs_ddeindividualcustomerid", $"eqs_dataentrystage eq {finalValue} and _eqs_accountapplicantid_value", AccountNumber);
            
        }
                
        public async Task<string> getDDEFinalAccountCorpData(string AccountNumber)
        {
            string finalValue = await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_dataentrystage", "Final");
            return await this.getIDfromMSDTable("eqs_ddecorporatecustomers", "eqs_ddecorporatecustomerid", $"eqs_dataentrystage eq {finalValue} and _eqs_accountapplicantid_value", AccountNumber);
                       
        }

        public async Task<JArray> getApplicentData(string Applicent_id)
        {
            try
            {
                string query_url = $"eqs_accountapplicants()?$filter=eqs_applicantid eq '{Applicent_id}' &$expand=eqs_leadid($select=leadid)";
                var Accountdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Account_dtails = await this.getDataFromResponce(Accountdtails);
                return Account_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getApplicentData", ex.Message);
                throw ex;
            }
        }
        public async Task<JArray> getDDEFinalIndvDetail(string AccountNumber)
        {
            try
            {
                //string finalValue = await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_dataentrystage", "Final");
                string query_url = $"eqs_ddeindividualcustomers()?$filter=_eqs_accountapplicantid_value eq '{AccountNumber}'";
                var DDEdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var DDE_dtails = await this.getDataFromResponce(DDEdtails);
                return DDE_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDDEFinalIndvDetail", ex.Message);
                throw ex;
            }

        }

        public async Task<JArray> getDDEFinalIndvCustomerId(string ddeId)
        {
            try
            {
                string query_url = $"eqs_ddeindividualcustomers()?$select=eqs_customeridcreated&$filter=eqs_ddeindividualcustomerid eq '{ddeId}'";
                var DDEdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var DDE_dtails = await this.getDataFromResponce(DDEdtails);
                return DDE_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDDEFinalIndvCustomerId", ex.Message);
                throw ex;
            }

        }

        public async Task<JArray> getDDEFinalCorpCustomerId(string ddeId)
        {
            try
            {
                string query_url = $"eqs_ddecorporatecustomers()?$select=eqs_customeridcreated&$filter=eqs_ddecorporatecustomerid eq '{ddeId}'";
                var DDEdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var DDE_dtails = await this.getDataFromResponce(DDEdtails);
                return DDE_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDDEFinalCorpCustomerId", ex.Message);
                throw ex;
            }

        }


        public async Task<JArray> getkycverificationDetail(string kycverificationId)
        {
            try
            {              
                string query_url = $"eqs_kycverificationdetailses()?$filter=eqs_kycverificationdetailsid eq '{kycverificationId}'";
                var KYCdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var KYC_dtails = await this.getDataFromResponce(KYCdtails);
                return KYC_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getkycverificationDetail", ex.Message);
                throw ex;
            }

        }
        public async Task<JArray> getDDEFinalCorpDetail(string AccountNumber)
        {
            try
            {
                //string finalValue = await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_dataentrystage", "Final");
                string query_url = $"eqs_ddecorporatecustomers()?$filter=_eqs_accountapplicantid_value eq '{AccountNumber}'";
                var DDEdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "", true);
                var DDE_dtails = await this.getDataFromResponce(DDEdtails);
                return DDE_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDDEFinalCorpDetail", ex.Message);
                throw ex;
            }

        }

        public async Task<JArray> getAccountApplicantDetail(string AccountApplicantID)
        {
            try
            {
                string query_url = $"eqs_accountapplicants({AccountApplicantID})?$expand=eqs_productid($select=eqs_productcode),eqs_customerid($select=fullname,eqs_shortname),eqs_branchid($select=eqs_branchidvalue),eqs_subentity($select=eqs_key)";
                var AccountApplicantDetails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "", true);
                var AccountApplicant_Details = await this.getDataFromResponce(AccountApplicantDetails);
                return AccountApplicant_Details;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDDEFinalCorpDetail", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getDDEFinalAddressDetail(string DDEId, string type)
        {
            try
            {
                string query_url;
                if (type == "corp")
                {
                    query_url = $"eqs_leadaddresses()?$filter=_eqs_corporatedde_value eq '{DDEId}'";
                }
                else
                {
                    query_url = $"eqs_leadaddresses()?$filter=_eqs_individualdde_value eq '{DDEId}'";
                }

                var Addressdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Address_dtails = await this.getDataFromResponce(Addressdtails);
                return Address_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDDEFinalAddressDetail", ex.Message);
                throw ex;
            }

        }

        public async Task<JArray> getDDEFinalFatcaDetail(string DDEId, string type)
        {
            try
            {
                string query_url;
                if (type == "corp")
                {
                    query_url = $"eqs_customerfactcaothers()?$filter=_eqs_ddecorporatecustomerid_value eq '{DDEId}'";
                }
                else
                {
                    query_url = $"eqs_customerfactcaothers()?$filter=_eqs_indivapplicantddeid_value eq '{DDEId}'";
                }

                var fatcadtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var fatca_dtails = await this.getDataFromResponce(fatcadtails);
                return fatca_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDDEFinalFatcaDetail", ex.Message);
                throw ex;
            }

        }
        public async Task<JArray> getDDEFinalDocumentDetail(string DDEId, string type)
        {
            try
            {
                string query_url;
                if (type == "corp")
                {
                    query_url = $"eqs_leaddocuments()?$filter=_eqs_corporatedde_value eq '{DDEId}'";
                }
                else
                {
                    query_url = $"eqs_leaddocuments()?$filter=_eqs_individualddefinal_value eq '{DDEId}'";
                }

                var docdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var doc_dtails = await this.getDataFromResponce(docdtails);
                return doc_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDDEFinalDocumentDetail", ex.Message);
                throw ex;
            }


        }
        public async Task<JArray> getDDEFinalCPDetail(string DDEId)
        {
            try
            {
                string query_url = $"eqs_customercps()?$filter=_eqs_ddecorporatecustomerid_value eq '{DDEId}'";

                var CPdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var CP_dtails = await this.getDataFromResponce(CPdtails);
                return CP_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDDEFinalCPDetail", ex.Message);
                throw ex;
            }

        }
        public async Task<JArray> getDDEFinalBODetail(string DDEId)
        {
            try
            {
                string query_url = $"eqs_customerbos()?$filter=_eqs_ddecorporatecustomerid_value eq '{DDEId}'";

                var BOdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var BO_dtails = await this.getDataFromResponce(BOdtails);
                return BO_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDDEFinalBODetail", ex.Message);
                throw ex;
            }
        }
        public async Task<JArray> getFATCAAddress(string FatcaID)
        {
            try
            {
                string query_url = $"eqs_leadaddresses()?$filter=_eqs_applicantfatca_value eq '{FatcaID}'";

                var adddtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var add_dtails = await this.getDataFromResponce(adddtails);
                return add_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDDEFinalBODetail", ex.Message);
                throw ex;
            }
        }
        public async Task<JArray> getContactData(string UCIC_id)
        {
            try
            {
                string query_url = $"contacts()?$select=contactid,fullname,emailaddress1,mobilephone,mobilephone,eqs_customerid&$filter=eqs_customerid eq '{UCIC_id}'";
                var Accountdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Account_dtails = await this.getDataFromResponce(Accountdtails);
                return Account_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getContactData", ex.Message);
                throw ex;
            }
        }

        public async Task<string> MeargeJsonString(string json1, string json2)
        {
            string first = json1.Remove(json1.Length - 1, 1);
            string second = json2.Substring(1);
            return first + ", " + second;
        }

        public bool GetMvalue<T>(string keyname, out T? Outvalue)
        {
            if (!this._cache.TryGetValue<T>(keyname, out Outvalue))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(timevalid));

            this._cache.Set<T>(keyname, inputvalue, cacheEntryOptions);
        }

    }
}
