namespace UpdateAccountLead
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
    using Microsoft.VisualBasic;

    public partial class UpdgAccLeadExecution : IUpdgAccLeadExecution
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

        private string Leadid, LeadAccountid, DDEId;

        private List<string> applicents = new List<string>();
        private LeadAccount _accountLead;
        private LeadDetails _leadParam;
        private List<AccountApplicant> _accountApplicants;

       
       
        private ICommonFunction _commonFunc;

        public UpdgAccLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

            _leadParam = new LeadDetails();
            _accountLead = new LeadAccount();
            _accountApplicants = new List<AccountApplicant>();

          
        }


        public async Task<UpAccountLeadReturn> ValidateLeadtInput(dynamic RequestData)
        {
            UpAccountLeadReturn ldRtPrm = new UpAccountLeadReturn();
            RequestData = await this.getRequestData(RequestData, "UpdateDigiAccountLead");
            try
            { 

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "UpdateDigiAccountLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        
                        if (!string.IsNullOrEmpty(RequestData.General?.AccountLeadId?.ToString()))
                        {
                            ldRtPrm = await this.CreateDDELeadAccount(RequestData);
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateLeadtInput", "Input LeadAccount Id is incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "Input LeadAccount Id is incorrect";
                        }
                        
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateLeadtInput", "Transaction_ID or  Channel_ID is incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or  Channel_ID is incorrect.";
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


                

        private async Task<bool> SetDocumentDDE(dynamic ddeDocuments)
        {           
            try
            {
                var res = await this._queryParser.DeleteFromTable("eqs_leaddocuments", "", "_eqs_leadaccountdde_value", this.DDEId, "eqs_leaddocumentid");
                foreach(var item in ddeDocuments) {
                    Dictionary<string, string> odatab = new Dictionary<string, string>();
                    odatab.Add("eqs_leadaccountid@odata.bind", $"eqs_leadaccounts({this.LeadAccountid})");
                    odatab.Add("eqs_leadaccountdde@odata.bind", $"eqs_ddeaccounts({this.DDEId})");

                    odatab.Add("eqs_doccategory@odata.bind", $"eqs_doccategories({await this._commonFunc.getDocCategoryId(item.CategoryCode.ToString())})");
                    odatab.Add("eqs_docsubcategory@odata.bind", $"eqs_docsubcategories({await this._commonFunc.getDocSubcatId(item.SubCategoryCode.ToString())})");
                    odatab.Add("eqs_doctypemasterid@odata.bind", $"eqs_doctypemasters({await this._commonFunc.getDocTypeId(item.TypeMasterID.ToString())})");

                    if (!string.IsNullOrEmpty(item.Status.ToString()))
                    {
                        odatab.Add("eqs_docstatuscode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_docstatuscode", item.Status.ToString()));
                    }
                   
                    string postDataParametr = JsonConvert.SerializeObject(odatab);
                    var resp = await this._queryParser.HttpApiCall("eqs_leaddocuments()?$select=eqs_leaddocumentid", HttpMethod.Post, postDataParametr);
                    if (resp[0]["responsecode"].ToString()=="400")
                    {
                        this._logger.LogError("SetDocumentDDE", JsonConvert.SerializeObject(resp), postDataParametr);
                        return false;
                    }
                }
                

                return true;
            }catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<List<string>> SetPreferencesDDE(dynamic preferenceData)
        {
            List<string> PreferenceIds = new List<string>();
            foreach (var item in preferenceData)
            {
                string PreferenceID = "";
                Dictionary<string,string> inputItem = new Dictionary<string,string>();  
                if (!string.IsNullOrEmpty(item["PreferenceID"].ToString()))
                {
                    PreferenceID = this._commonFunc.getPreferenceID(item["PreferenceID"].ToString(),this.DDEId);
                    PreferenceIds.Add(PreferenceID);
                }
                inputItem.Add("eqs_leadaccountdde@odata.bind", $"eqs_ddeaccounts({this.DDEId})");

                if (!string.IsNullOrEmpty(item["UCIC"].ToString()))
                {
                    inputItem.Add("eqs_applicantid@odata.bind", $"eqs_accountapplicants({await this._commonFunc.getApplicentID(item["UCIC"].ToString())})");
                }
                if (!string.IsNullOrEmpty(item["DebitCardFlag"].ToString()))
                {
                    inputItem.Add("eqs_debitcardflag", (item["DebitCardFlag"].ToString() == "Yes") ? "true" : "false");
                }
                if (!string.IsNullOrEmpty(item["DebitCardID"].ToString()))
                {
                    inputItem.Add("eqs_debitcard@odata.bind", $"eqs_debitcards({await this._commonFunc.getDebitCardID(item["DebitCardID"].ToString())})");
                }
                if (!string.IsNullOrEmpty(item["NameonCard"].ToString()))
                {
                    inputItem.Add("eqs_nameoncard", item["NameonCard"].ToString());
                }
                if (!string.IsNullOrEmpty(item["SMS"].ToString()))
                {
                    inputItem.Add("eqs_sms", item["SMS"].ToString());
                }
                if (!string.IsNullOrEmpty(item["NetBanking"].ToString()))
                {
                    inputItem.Add("eqs_netbanking", item["NetBanking"].ToString());
                }
                if (!string.IsNullOrEmpty(item["MobileBanking"].ToString()))
                {
                    inputItem.Add("eqs_mobilebanking", item["MobileBanking"].ToString());
                }
                if (!string.IsNullOrEmpty(item["EmailStatement"].ToString()))
                {
                    inputItem.Add("eqs_emailstatement", item["EmailStatement"].ToString());
                }
                if (!string.IsNullOrEmpty(item["InternationalDCLimitAct"].ToString()))
                {
                    inputItem.Add("eqs_internationaldclimitact", item["InternationalDCLimitAct"].ToString());
                }
                                           
                

                string postDataParametr = JsonConvert.SerializeObject(inputItem);

                if (string.IsNullOrEmpty(PreferenceID))
                {
                    var resp = await this._queryParser.HttpApiCall("eqs_customerpreferences()?$select=eqs_preferenceid", HttpMethod.Post, postDataParametr);
                    if (resp[0]["responsecode"].ToString() == "400")
                    {
                        this._logger.LogError("SetPreferencesDDE", JsonConvert.SerializeObject(resp), postDataParametr);                       
                    }
                    var Preference_data = CommonFunction.GetIdFromPostRespons201(resp[0]["responsebody"], "eqs_preferenceid");
                    PreferenceIds.Add(Preference_data);
                }
                else
                {
                    await this._queryParser.HttpApiCall($"eqs_customerpreferences({PreferenceID})", HttpMethod.Patch, postDataParametr);
                }
                

            }
            
            return PreferenceIds;
        }
               

        private async Task<bool> SetNomineeDDE(dynamic ddeNominee)
        {
            try
            {
                    string nomineeID = await this._commonFunc.getNomineeID(this.DDEId);
                
                    Dictionary<string, string> odatab = new Dictionary<string, string>();
                    
                    odatab.Add("eqs_nomineename", ddeNominee?.name?.ToString());
                    odatab.Add("eqs_nomineedob", ddeNominee?.DOB?.ToString());
                    odatab.Add("eqs_nomineerelationshipwithaccountholder@odata.bind", $"eqs_relationships({await this._commonFunc.getRelationShipId(ddeNominee?.NomineeRelationship?.ToString())})");
                    //odatab.Add("eqs_nomineeage", ddeNominee?.age?.ToString());
                    odatab.Add("eqs_nomineeucic", ddeNominee?.nomineeUCICIfCustomer?.ToString());
                    odatab.Add("eqs_nomineedisplayname", ddeNominee?.NomineeDisplayName?.ToString());
                    odatab.Add("eqs_nomineeaddresssameasprospectcurrentaddres", (Convert.ToBoolean(ddeNominee?.AddresssameasProspects?.ToString())) ? "true" : "false");
                    odatab.Add("eqs_emailid", ddeNominee?.email?.ToString());
                    odatab.Add("eqs_mobile", ddeNominee?.mobile?.ToString());
                    odatab.Add("eqs_landlinenumber", ddeNominee?.Landline?.ToString());

                    odatab.Add("eqs_addressline1", ddeNominee?.Address1?.ToString());
                    odatab.Add("eqs_addressline2", ddeNominee?.Address2?.ToString());
                    odatab.Add("eqs_addressline3", ddeNominee?.Address3?.ToString());
                    odatab.Add("eqs_pincode", ddeNominee?.Pin?.ToString());
                    odatab.Add("eqs_district", ddeNominee?.District?.ToString());
                    odatab.Add("eqs_pobox", ddeNominee?.PO?.ToString());
                    odatab.Add("eqs_landmark", ddeNominee?.Landmark?.ToString());

                    odatab.Add("eqs_leadaccountddeid@odata.bind", $"eqs_ddeaccounts({this.DDEId})");
                    
                    odatab.Add("eqs_city@odata.bind", $"eqs_cities({await this._commonFunc.getCityId(ddeNominee?.CityCode?.ToString())})");
                    odatab.Add("eqs_state@odata.bind", $"eqs_states({await this._commonFunc.getStateId(ddeNominee?.State?.ToString())})");
                    odatab.Add("eqs_country@odata.bind", $"eqs_countries({await this._commonFunc.getCuntryId(ddeNominee?.CountryCode?.ToString())})");

                if (ddeNominee?.Guardian?.Name != null)
                {
                    odatab.Add("eqs_guardianname", ddeNominee?.Guardian?.Name?.ToString());
                    odatab.Add("eqs_guardianucic", ddeNominee?.Guardian?.GuardianUCIC?.ToString());
                    odatab.Add("eqs_guardianmobile", ddeNominee?.Guardian?.GuardianMobile?.ToString());
                    odatab.Add("eqs_guardianlandlinenumber", ddeNominee?.Guardian?.GuardianLandline?.ToString());
                    odatab.Add("eqs_guardianaddressline1", ddeNominee?.Guardian?.GuardianAddress1?.ToString());
                    odatab.Add("eqs_guardianaddressline2", ddeNominee?.Guardian?.GuardianAddress2?.ToString());
                    odatab.Add("eqs_guardianaddressline3", ddeNominee?.Guardian?.GuardianAddress3?.ToString());
                    odatab.Add("eqs_guardianpincode", ddeNominee?.Guardian?.GuardianPin?.ToString());
                    odatab.Add("eqs_guardiandistrict", ddeNominee?.Guardian?.GuardianDistrict?.ToString());
                    odatab.Add("eqs_guardianpobox", ddeNominee?.Guardian?.GuardianPO?.ToString());
                    odatab.Add("eqs_guardianlandmark", ddeNominee?.Guardian?.GuardianLandmark?.ToString());

                    odatab.Add("eqs_guardianrelationshiptominor@odata.bind", $"eqs_relationships({await this._commonFunc.getRelationShipId(ddeNominee?.Guardian?.RelationshipToMinor?.ToString())})");
                    odatab.Add("eqs_guardiancity@odata.bind", $"eqs_cities({await this._commonFunc.getCityId(ddeNominee?.Guardian?.GuardianCityCode?.ToString())})");
                    odatab.Add("eqs_guardiancountry@odata.bind", $"eqs_countries({await this._commonFunc.getCuntryId(ddeNominee?.Guardian?.GuardianCountryCode?.ToString())})");
                    odatab.Add("eqs_guardianstate@odata.bind", $"eqs_states({await this._commonFunc.getStateId(ddeNominee?.Guardian?.GuardianState?.ToString())})");
                }
                


                string postDataParametr = JsonConvert.SerializeObject(odatab);
                if (string.IsNullOrEmpty(nomineeID))
                {
                    await this._queryParser.HttpApiCall("eqs_ddeaccountnominees()?$select=eqs_ddeaccountnomineeid", HttpMethod.Post, postDataParametr);
                }
                else
                {
                    await this._queryParser.HttpApiCall($"eqs_ddeaccountnominees({nomineeID})", HttpMethod.Patch, postDataParametr);
                }
                    
                

                return true;
            }
            catch (Exception ex)
            {
                return false;
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
