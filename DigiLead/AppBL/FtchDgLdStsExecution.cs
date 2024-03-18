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
    using Swashbuckle.AspNetCore.SwaggerGen;

    public class FtchDgLdStsExecution : IFtchDgLdStsExecution
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


        Dictionary<string, string> IdentityType = new Dictionary<string, string>();

        private readonly IKeyVaultService _keyVaultService;

                
        private ICommonFunction _commonFunc;

        public FtchDgLdStsExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;


            this.IdentityType.Add("615290000", "PAN Card");
            this.IdentityType.Add("615290001", "8Form 60 + Form 49 A");
            this.IdentityType.Add("615290002", "Form 60");
            this.IdentityType.Add("615290003", "Minor -NA");
            this.IdentityType.Add("615290004", "Not Applicable");

        }


        public async Task<FtchDgLdStsReturn> ValidateFtchDgLdSts(dynamic RequestData)
        {
            FtchDgLdStsReturn ldRtPrm = new FtchDgLdStsReturn();
            RequestData = await this.getRequestData(RequestData, "FetchDigiLeadStatus");

            if (RequestData.ErrorNo != null && RequestData.ErrorNo.ToString() == "Error99")
            {
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = "API do not have access permission!";
                return ldRtPrm;
            }

            try
            {
                string LeadID = RequestData.LeadID;
                if (!string.IsNullOrEmpty(this.appkey) && this.appkey != "" && checkappkey(this.appkey, "FetchDigiLeadStatusappkey"))
                {
                    if (!string.IsNullOrEmpty(this.Transaction_ID) && !string.IsNullOrEmpty(this.Channel_ID))
                    {
                        if (!string.IsNullOrEmpty(LeadID) && LeadID != "")
                        {
                            ldRtPrm = await this.getDigiLeadStatus(RequestData);
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateFtchDgLdSts", "LeadID is mandatory.");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "LeadID is mandatory.";
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateFtchDgLdSts", "Transaction_ID or Channel_ID is incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or Channel_ID is incorrect.";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateFtchDgLdSts", "Appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Appkey is incorrect";
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
                var Applicant_data = await getAccountApplicantData(RequestData.LeadID.ToString());
                if (Applicant_data.Count > 0)
                {
                    dynamic ApplicantData = Applicant_data[0];
                    string Entity_type = await this._commonFunc.getLeadType(ApplicantData._eqs_entitytypeid_value.ToString());
                    csRtPrm.LeadID = RequestData.LeadID;

                    //csRtPrm.Status = await this._queryParser.getOptionSetValuToText("lead", "statuscode", ApplicantData.statuscode.ToString());
                    csRtPrm.Status = await this._commonFunc.getLeadStatus(ApplicantData._eqs_leadid_value.ToString());
                    csRtPrm.EntityType = await this._commonFunc.getEntityType(ApplicantData._eqs_entitytypeid_value.ToString());
                    csRtPrm.SubEntityType = await this._commonFunc.getSubEntityType(ApplicantData._eqs_subentity_value.ToString()); 

                    if (Entity_type == "Individual")
                    {
                        csRtPrm.individualDetails = new IndividualDetails();
                        csRtPrm.individualDetails.firstName = ApplicantData.eqs_firstname;
                        csRtPrm.individualDetails.lastName = ApplicantData.eqs_lastname;
                        csRtPrm.individualDetails.middleName = ApplicantData.eqs_middlename;
                       //csRtPrm.individualDetails.shortName = LeadData.eqs_shortname;
                        csRtPrm.individualDetails.mobilePhone = ApplicantData.eqs_mobilenumber;
                        DateTime tempDate = Convert.ToDateTime(ApplicantData.eqs_dob);
                        csRtPrm.individualDetails.dob = tempDate.ToString("dd/MM/yyyy");
                        csRtPrm.individualDetails.aadhar = ApplicantData.eqs_aadhaarreference;                     
                        csRtPrm.individualDetails.PAN = ApplicantData.eqs_internalpan;
                        csRtPrm.individualDetails.motherMaidenName = ApplicantData.eqs_mothermaidenname;
                        csRtPrm.individualDetails.identityType = (!string.IsNullOrEmpty(ApplicantData.eqs_panform60code.ToString())) ? this.IdentityType[ApplicantData.eqs_panform60code.ToString()] : "";
                        csRtPrm.individualDetails.NLFound = await this._queryParser.getOptionSetValuToText("eqs_accountapplicant", "eqs_nlvalidatedcode", ApplicantData.eqs_nlvalidatedcode.ToString());
                        csRtPrm.individualDetails.reasonNotApplicable = ApplicantData.eqs_reasonforna;
                        csRtPrm.individualDetails.voterid = ApplicantData.eqs_voterid;
                        csRtPrm.individualDetails.drivinglicense = ApplicantData.eqs_dlnumber;
                        csRtPrm.individualDetails.passport = ApplicantData.eqs_passportnumber;
                        csRtPrm.individualDetails.ckycnumber = ApplicantData.eqs_ckycnumber;

                     
                        if (ApplicantData._eqs_titleid_value != null)
                        {
                            csRtPrm.individualDetails.title = await this._commonFunc.getTitle(ApplicantData._eqs_titleid_value.ToString());
                        }
                        if (ApplicantData._eqs_purposeofcreationid_value != null)
                        {
                            csRtPrm.individualDetails.purposeOfCreation = await this._commonFunc.getPurposeOfCreation(ApplicantData._eqs_purposeofcreationid_value.ToString());
                            csRtPrm.individualDetails.OtherPurpose = ApplicantData.eqs_otherpurpose;
                        }

                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }

                    if (Entity_type == "Corporate")
                    {
                        csRtPrm.individualDetails = null;
                        csRtPrm.corporateDetails = new CorporateDetails();
                        csRtPrm.corporateDetails.companyName = ApplicantData.eqs_companynamepart1;
                        csRtPrm.corporateDetails.companyName2 = ApplicantData.eqs_companynamepart2;
                        csRtPrm.corporateDetails.companyName3 = ApplicantData.eqs_companynamepart3;
                        csRtPrm.corporateDetails.companyPhone = ApplicantData.mobilephone;
                       
                        csRtPrm.corporateDetails.pocNumber = ApplicantData.eqs_contactmobilenumber;
                        csRtPrm.corporateDetails.pocName = ApplicantData.eqs_contactperson;
                        csRtPrm.corporateDetails.cinNumber = ApplicantData.eqs_cinnumber;
                        csRtPrm.corporateDetails.dateOfIncorporation = ApplicantData.eqs_dateofincorporation;                
                        csRtPrm.corporateDetails.tanNumber = ApplicantData.eqs_tannumber;
                        csRtPrm.corporateDetails.NLFound = await this._queryParser.getOptionSetValuToText("eqs_accountapplicant", "eqs_nlvalidatedcode", ApplicantData.eqs_nlvalidatedcode.ToString());
                        csRtPrm.corporateDetails.identityType = (!string.IsNullOrEmpty(ApplicantData.eqs_panform60code.ToString())) ? this.IdentityType[ApplicantData.eqs_panform60code.ToString()] : "";
                        csRtPrm.corporateDetails.gstNumber = ApplicantData.eqs_gstnumber;
                        csRtPrm.corporateDetails.alternateMandatoryCheck = (ApplicantData.eqs_deferalcode.ToString()== "615290000") ? "Yes" : "No";
                        csRtPrm.corporateDetails.cstNumber = ApplicantData.eqs_cstvatnumber;
                        csRtPrm.corporateDetails.ckycnumber = ApplicantData.eqs_ckycnumber;


                      
                        if (ApplicantData._eqs_purposeofcreationid_value != null)
                        {
                            csRtPrm.corporateDetails.purposeOfCreation = await this._commonFunc.getPurposeOfCreation(ApplicantData._eqs_purposeofcreationid_value.ToString());
                            csRtPrm.corporateDetails.OtherPurpose = ApplicantData.eqs_otherpurpose;
                        }


                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }
                    else if(csRtPrm.ReturnCode==null)
                    {
                        this._logger.LogInformation("getDigiLeadStatus", "Entity type is incorrect");
                        csRtPrm.ReturnCode = "CRM-ERROR-102";
                        csRtPrm.Message = "Entity type is incorrect";
                    }


                }
                else
                {
                    this._logger.LogInformation("getDigiLeadStatus", "Lead data not found");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = "Lead data not found";
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

        private async Task<JArray> getAccountApplicantData(string ApplicantId)
        {
            try
            {
                string query_url = $"eqs_accountapplicants()?$filter=eqs_applicantid eq '{ApplicantId}'";
                var Leaddtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Lead_dtails = await this._commonFunc.getDataFromResponce(Leaddtails);
                return Lead_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getLeadData", ex.Message);
                throw ex;
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
                string xmlData = await this._queryParser.PayloadDecryption(EncryptedData.ToString(), BankCode, APIname);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlData);
                string xpath = "PIDBlock/payload";
                var nodes = xmlDoc.SelectSingleNode(xpath);
                foreach (XmlNode childrenNode in nodes)
                {
                    rejusetJson = JsonConvert.DeserializeObject(childrenNode.Value);

                    var payload = rejusetJson.FetchDigiLeadStatus;
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
