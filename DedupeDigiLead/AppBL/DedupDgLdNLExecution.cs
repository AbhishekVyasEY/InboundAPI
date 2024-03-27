using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Collections.Generic;
using CRMConnect;
using System.Xml;

namespace DedupeDigiLead
{
    public class DedupDgLdNLExecution : IDedupDgLdNLExecution
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
        
        private ICommonFunction _commonFunc;

        public DedupDgLdNLExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

        }


        public async Task<dynamic> ValidateDedupDgLdNL(dynamic RequestData, string type)
        {

            dynamic ldRtPrm = (type == "NLTR") ? new DedupDgLdNLTRReturn() : new DedupDgLdNLReturn();
            try
            {
                RequestData = await this.getRequestData(RequestData,"DedupeDigiCustomer" + type);

                if (RequestData.ErrorNo != null && RequestData.ErrorNo.ToString() == "Error99")
                {
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "API do not have access permission!";
                    return ldRtPrm;
                }

                string ApplicantId = RequestData.ApplicantId;
                if (!string.IsNullOrEmpty(this.Transaction_ID) && !string.IsNullOrEmpty(this.Channel_ID) && !string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "DedupDgLdNLappkey"))
                {
                    if (!string.IsNullOrEmpty(ApplicantId) && ApplicantId != "")
                    {

                        ldRtPrm = await this.getDedupDgLdNLStatus(RequestData, type);

                    }
                    else
                    {
                        this._logger.LogInformation("ValidateFtchDgLdSts", "ApplicantId is incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "ApplicantId is incorrect";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateFtchDgLdSts", "Transaction_ID or Channel_ID or AppKey is incorrect.");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Transaction_ID or Channel_ID or AppKey is incorrect.";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateFtchDgLdSts", ex.Message);               
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


        public async Task<dynamic> getDedupDgLdNLStatus(dynamic RequestData, string type)
        {
            dynamic ldRtPrm = (type == "NLTR") ? new DedupDgLdNLTRReturn() : new DedupDgLdNLReturn();
            JArray NLTR_data;
            try
            {
                var Lead_data = await this._commonFunc.getLeadData(RequestData.ApplicantId.ToString());
                if (Lead_data.Count > 0)
                {
                    dynamic LeadData = Lead_data[0];

                    if (type == "NLTR")
                    {
                        if (LeadData.eqs_entitytypeid.eqs_name.ToString() == "Individual")
                        {
                            NLTR_data = await this._commonFunc.getNLTRData("Individual", LeadData.eqs_internalpan.ToString(), LeadData.eqs_aadhar.ToString(), LeadData.eqs_passportnumber.ToString(), LeadData.eqs_cinnumber.ToString(), LeadData.eqs_firstname.ToString(), LeadData.eqs_middlename.ToString(), LeadData.eqs_lastname.ToString(), LeadData.eqs_dob.ToString());
                        }
                        else
                        {
                            NLTR_data = await this._commonFunc.getNLTRData("Corporate", LeadData.eqs_internalpan.ToString(), LeadData.eqs_aadhar.ToString(), LeadData.eqs_passportnumber.ToString(), LeadData.eqs_cinnumber.ToString(), LeadData.eqs_companynamepart1.ToString(), LeadData.eqs_companynamepart2.ToString(), LeadData.eqs_companynamepart3.ToString(), LeadData.eqs_dateofincorporation.ToString());
                        }   
                    }
                    else
                    {
                        NLTR_data = await this._commonFunc.getNLData(LeadData.eqs_internalpan.ToString(), LeadData.eqs_aadhar.ToString(), LeadData.eqs_passportnumber.ToString(), LeadData.eqs_cinnumber.ToString());
                    }

                    if (NLTR_data.Count > 0)
                    {
                        if (type == "NLTR")
                        {
                            ldRtPrm.decideNLTR = true;
                            ldRtPrm.Message = $"Applicant {RequestData.ApplicantId.ToString()} has been matched with UID {NLTR_data[0]["eqs_uid"].ToString()}";
                        }
                        else if (type == "NL")
                        {
                            ldRtPrm.decideNL = true;
                            ldRtPrm.Message = $"Applicant {RequestData.ApplicantId.ToString()} has been matched with recordid {NLTR_data[0]["eqs_recordid"].ToString()}";
                        }
                       
                        ldRtPrm.ReturnCode = "CRM - SUCCESS";
                        
                    }
                    else
                    {
                        if (type == "NLTR")
                        {
                            ldRtPrm.decideNLTR = false;
                        }
                        else if (type == "NL")
                        {
                            ldRtPrm.decideNL = false;
                        }
                     
                        ldRtPrm.ReturnCode = "CRM - SUCCESS";
                        ldRtPrm.Message = "";
                    }

                    
                }
                else
                {
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "No Lead data found.";
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDedupDgLdNLStatus", ex.Message);
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = OutputMSG.Resource_n_Found;
            }
            
                return ldRtPrm;
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
