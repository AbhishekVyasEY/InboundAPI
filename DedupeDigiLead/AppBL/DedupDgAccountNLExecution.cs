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
using Microsoft.Identity.Client;

namespace DedupeDigiLead
{
    public class DedupDgAccountNLExecution : IDedupDgAccountNLExecution
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

        public DedupDgAccountNLExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

        }


        public async Task<dynamic> ValidateDedupDgAccNL(dynamic RequestData, string type)
        {

            dynamic ldRtPrm = (type == "NLTR") ? new DedupDgAccNLTRReturn() : new DedupDgAccNLReturn();
            dynamic dedupDgChk = (type == "NLTR") ? new List<DedupDgChkNLTR>() : new List<DedupDgChkNL>();
            try
            {
                RequestData = await this.getRequestData(RequestData, "DedupeDigiAccount" + type);

                if (RequestData.ErrorNo != null && RequestData.ErrorNo.ToString() == "Error99")
                {
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "API do not have access permission!";
                    return ldRtPrm;
                }

                string LeadAccount = RequestData.LeadAccount;

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "DedupDgLdNLappkey"))
                {
                    if (!string.IsNullOrEmpty(LeadAccount) && LeadAccount != "")
                    {
                        var AccLead_data = await this._commonFunc.getLeadAccData(LeadAccount);
                        foreach (var AccId in AccLead_data)
                        {
                            var Account_data = await this.getDedupDgAccNLStatus(AccId, type);
                            if (Account_data!=null)
                            {
                                dedupDgChk.Add(Account_data);
                            }
                            
                        }
                        ldRtPrm.accountData = dedupDgChk;

                        if (AccLead_data.Count>0)
                        {
                            ldRtPrm.ReturnCode = "CRM-SUCCESS";
                            ldRtPrm.Message = OutputMSG.Case_Success;
                        }
                        else
                        {
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "No LeadAccount data found.";
                            
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateDedupDgAccNL", "LeadAccount is incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "LeadAccount is incorrect";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateDedupDgAccNL", "Appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Appkey is incorrect";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateDedupDgAccNL", ex.Message);               
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


        public async Task<dynamic> getDedupDgAccNLStatus(string ApplicantId, string type)
        {
            dynamic ldRtPrm = (type == "NLTR") ? new DedupDgChkNLTR() : new DedupDgChkNL();

            DedupDgChkNLTR dedupDgChkNLTR;
            DedupDgChkNL dedupDgChkNL;
            JArray NLTR_data;
            int mData = 0;
            if (type == "NLTR" && this._commonFunc.GetMvalue<DedupDgChkNLTR>("NLTR" + ApplicantId, out dedupDgChkNLTR))
            {
                mData = 1;
                ldRtPrm = dedupDgChkNLTR;
            }
            else if (type == "NL" && this._commonFunc.GetMvalue<DedupDgChkNL>("NL" + ApplicantId, out dedupDgChkNL))
            {
                mData = 1;
                ldRtPrm = dedupDgChkNL;
            }
            else
            {

                try
                {

                    var Lead_data = await this._commonFunc.getLeadData(ApplicantId);
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
                                NLTR_data = await this._commonFunc.getNLTRData("Corporate", LeadData.eqs_internalpan.ToString(), LeadData.eqs_aadhar.ToString(), LeadData.eqs_passportnumber.ToString(), LeadData.eqs_cinnumber.ToString(), LeadData.eqs_companynamepart1.ToString(), LeadData.eqs_companynamepart2.ToString(), LeadData.eqs_companynamepart3.ToString(), LeadData.eqs_dateofregistration.ToString());
                            }
                            
                        }
                        else
                        {
                            NLTR_data = await this._commonFunc.getNLData(LeadData.eqs_internalpan.ToString(), LeadData.eqs_aadhar.ToString(), LeadData.eqs_passportnumber.ToString(), LeadData.eqs_cinnumber.ToString());
                        }

                        if (NLTR_data.Count > 0)
                        {
                            ldRtPrm.ApplicantID = ApplicantId;
                            if (type == "NLTR")
                            {
                                ldRtPrm.decideNLTR = true;
                                ldRtPrm.Message = $"Applicant {ApplicantId} has been matched with UID {NLTR_data[0]["eqs_uid"].ToString()}";
                            }
                            else if (type == "NL")
                            {
                                ldRtPrm.decideNL = true;
                                ldRtPrm.Message = $"Applicant {ApplicantId} has been matched with recordid {NLTR_data[0]["eqs_recordid"].ToString()}";
                            }

                                                       
                        }
                        else
                        {
                            ldRtPrm.ApplicantID = ApplicantId;
                            if (type == "NLTR")
                            {
                                ldRtPrm.decideNLTR = false;
                            }
                            else if (type == "NL")
                            {
                                ldRtPrm.decideNL = false;
                            }

                           
                        }

                        if (type == "NLTR")
                        {
                            this._commonFunc.SetMvalue<DedupDgChkNLTR>("NLTR" + ApplicantId, 2, ldRtPrm);
                        }
                        else if (type == "NL")
                        {
                            this._commonFunc.SetMvalue<DedupDgChkNL>("NL" + ApplicantId, 2, ldRtPrm);
                        }

                    }
                   
                }
                catch (Exception ex)
                {
                    this._logger.LogError("getDedupDgAccNLStatus", ex.Message);
                    ldRtPrm =null;
                }

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
