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
        
        private ICommonFunction _commonFunc;

        public DedupDgLdNLExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

        }


        public async Task<dynamic> ValidateDedupDgLdNL(dynamic RequestData, string appkey, string type)
        {

            dynamic ldRtPrm = (type == "NLTR") ? new DedupDgLdNLTRReturn() : new DedupDgLdNLReturn();
            try
            {
                RequestData = await this.getRequestData(RequestData);

                string ApplicantId = RequestData.ApplicantId;
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "DedupDgLdNLappkey"))
                {
                    if (!string.IsNullOrEmpty(ApplicantId) && ApplicantId != "")
                    {

                        ldRtPrm = await this.getDedupDgLdNLStatus(RequestData, type);

                    }
                    else
                    {
                        this._logger.LogInformation("ValidateFtchDgLdSts", "Input parameters are incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateFtchDgLdSts", "Input parameters are incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
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
                        NLTR_data = await this._commonFunc.getNLTRData(LeadData.eqs_internalpan.ToString(), LeadData.eqs_aadhar.ToString(), LeadData.eqs_passportnumber.ToString(), LeadData.eqs_cinnumber.ToString());
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
                        }
                        else if (type == "NL")
                        {
                            ldRtPrm.decideNL = true;
                        }
                       
                        ldRtPrm.ReturnCode = "CRM - SUCCESS";
                        ldRtPrm.Message = "";
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
            }
            catch (Exception ex)
            {
                
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = OutputMSG.Resource_n_Found;
            }
            
                return ldRtPrm;
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
