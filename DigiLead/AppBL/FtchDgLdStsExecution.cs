namespace DigiLead
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

    public class FtchDgLdStsExecution : IFtchDgLdStsExecution
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

        public FtchDgLdStsExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
           
           
        }


        public async Task<FtchDgLdStsReturn> ValidateFtchDgLdSts(dynamic RequestData, string appkey)
        {
            FtchDgLdStsReturn ldRtPrm = new FtchDgLdStsReturn();
            RequestData = await this.getRequestData(RequestData);
            try
            {
                string LeadID = RequestData.LeadID;
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "FetchDigiLeadStatusappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID) && !string.IsNullOrEmpty(LeadID) && LeadID != "")
                    {                       
                        
                       ldRtPrm = await this.getDigiLeadStatus(RequestData);

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

        

        public async Task<FtchDgLdStsReturn> getDigiLeadStatus(dynamic RequestData)
        {
            FtchDgLdStsReturn csRtPrm = new FtchDgLdStsReturn();
            try
            {
                var Lead_data = await getLeadData(RequestData.LeadID.ToString());
                if (Lead_data.Count > 0)
                {
                    dynamic LeadData = Lead_data[0];
                    csRtPrm.LeadID = RequestData.LeadID;

                    if (LeadData.firstname.ToString().Length > 1)
                    {
                        csRtPrm.individualDetails = new IndividualDetails();
                        csRtPrm.individualDetails.firstName = LeadData.firstname;
                        csRtPrm.individualDetails.lastName = LeadData.lastname;
                        csRtPrm.individualDetails.middleName = LeadData.middlename;
                        csRtPrm.individualDetails.shortName = LeadData.eqs_shortname;
                        csRtPrm.individualDetails.mobilePhone = LeadData.mobilephone;
                        csRtPrm.individualDetails.dob = LeadData.eqs_dob;
                        csRtPrm.individualDetails.aadhar = LeadData.eqs_aadhaarreference;
                        csRtPrm.individualDetails.PAN = LeadData.eqs_pan;
                        csRtPrm.individualDetails.motherMaidenName = LeadData.eqs_mothermaidenname;
                        csRtPrm.individualDetails.identityType = LeadData.eqs_panform60code;
                        csRtPrm.individualDetails.NLFound = LeadData.eqs_nlmatchcode;
                        csRtPrm.individualDetails.reasonNotApplicable = LeadData.eqs_reasonforna;
                        csRtPrm.individualDetails.voterid = LeadData.eqs_voterid;
                        csRtPrm.individualDetails.drivinglicense = LeadData.eqs_dlnumber;
                        csRtPrm.individualDetails.passport = LeadData.eqs_passportnumber;
                        csRtPrm.individualDetails.ckycnumber = LeadData.eqs_ckycnumber;

                        // csRtPrm.individualDetails.reason = TBC;
                        if (LeadData._eqs_titleid_value != null)
                        {
                            csRtPrm.individualDetails.title = await this._commonFunc.getTitle(LeadData._eqs_titleid_value.ToString());
                        }
                        if (LeadData._eqs_purposeofcreationid_value != null)
                        {
                            csRtPrm.individualDetails.purposeOfCreation = await this._commonFunc.getPurposeOfCreation(Lead_data._eqs_purposeofcreationid_value.ToString());
                        }

                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }

                    if (csRtPrm.ReturnCode == null && LeadData.eqs_companynamepart1.ToString().Length > 1)
                    {
                        csRtPrm.individualDetails = null;
                        csRtPrm.corporateDetails = new CorporateDetails();
                        csRtPrm.corporateDetails.companyName = LeadData.eqs_companynamepart1;
                        csRtPrm.corporateDetails.companyName2 = LeadData.eqs_companynamepart2;
                        csRtPrm.corporateDetails.companyName3 = LeadData.eqs_companynamepart3;
                        csRtPrm.corporateDetails.companyPhone = LeadData.mobilephone;
                        csRtPrm.corporateDetails.aadhar = LeadData.eqs_aadhaarreference;
                        csRtPrm.corporateDetails.pocNumber = LeadData.eqs_contactmobile;
                        csRtPrm.corporateDetails.pocName = LeadData.eqs_contactperson;
                        csRtPrm.corporateDetails.cinNumber = LeadData.eqs_cinnumber;
                        csRtPrm.corporateDetails.dateOfIncorporation = LeadData.eqs_dateofregistration;
                        csRtPrm.corporateDetails.pan = LeadData.eqs_pan;
                        csRtPrm.corporateDetails.tanNumber = LeadData.eqs_tannumber;
                        csRtPrm.corporateDetails.NLFound = LeadData.eqs_nlmatchcode;
                        csRtPrm.corporateDetails.identityType = LeadData.eqs_panform60code;
                        csRtPrm.corporateDetails.gstNumber = LeadData.eqs_gstnumber;
                        csRtPrm.corporateDetails.alternateMandatoryCheck = LeadData.eqs_deferalcode;
                        csRtPrm.corporateDetails.cstNumber = LeadData.eqs_cstvatnumber;

                        //csRtPrm.corporateDetails.tinNumber = TBC;
                        //csRtPrm.corporateDetails.reason = TBC;
                        if (LeadData._eqs_purposeofcreationid_value != null)
                        {
                            csRtPrm.corporateDetails.purposeOfCreation = await this._commonFunc.getPurposeOfCreation(Lead_data._eqs_purposeofcreationid_value.ToString());
                        }


                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }
                    else if(csRtPrm.ReturnCode==null)
                    {
                        this._logger.LogInformation("getDigiLeadStatus", "Input parameters are incorrect");
                        csRtPrm.ReturnCode = "CRM-ERROR-102";
                        csRtPrm.Message = OutputMSG.Incorrect_Input;
                    }


                }
                else
                {
                    this._logger.LogInformation("getDigiLeadStatus", "Input parameters are incorrect");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = OutputMSG.Incorrect_Input;
                }
            }
            catch(Exception ex)
            {
                this._logger.LogError("getDigiLeadStatus", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = OutputMSG.Incorrect_Input;
            }
            
            

            return csRtPrm;
        }
                      
        private async Task<JArray> getLeadData(string LeadID)
        {
            try
            {
                string query_url = $"leads()?$filter=eqs_crmleadid eq '{LeadID}'";
                var Leaddtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Lead_dtails = await this._commonFunc.getDataFromResponce(Leaddtails);
                return Lead_dtails;
            }
            catch(Exception ex) {
                this._logger.LogError("getLeadData", ex.Message);
                throw ex; 
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
