namespace CustomerLead
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
    using System.Drawing;

    public class DdpDgCusomerExecution : IDdpDgCustomerExecution
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

        private string Leadid, LeadAccountid, DDEId;

        private List<string> applicents = new List<string>();

        private ICommonFunction _commonFunc;

        public DdpDgCusomerExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
            this._logger = logger;
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
        }


        public async Task<DedupeDigiCustomerReturn> ValidateInput(dynamic RequestData)
        {
            DedupeDigiCustomerReturn ldRtPrm = new DedupeDigiCustomerReturn();
            RequestData = await this.getRequestData(RequestData, "DedupeDigiCustomer");

            if (RequestData.ErrorNo != null && RequestData.ErrorNo.ToString() == "Error99")
            {
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = "API do not have access permission!";
                return ldRtPrm;
            }

            try
            {

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "DedupeDigiCustomerappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {

                        if (!string.IsNullOrEmpty(RequestData.ApplicantId.ToString()))
                        {
                            ldRtPrm = await this.getCustomerLead(RequestData.ApplicantId.ToString());
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateInput", "Input UCIC are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "Input UCIC are incorrect";
                        }

                    }
                    else
                    {
                        this._logger.LogInformation("ValidateInput", "Transaction_ID or Channel_ID in incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or Channel_ID in incorrect.";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateInput", "Appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Appkey is incorrect";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateInput", ex.Message);
                throw ex;
            }

        }


        private async Task<DedupeDigiCustomerReturn> getCustomerLead(string customerLeadID)
        {
            DedupeDigiCustomerReturn ddupdgCustomerReturn = new DedupeDigiCustomerReturn();
            try
            {
                string Token = await this._queryParser.getAccessToken();
                string RequestTemplate = "{\"searchOrUpdateDigiDedupeCustomerReq\":{\"msgHdr\":{\"channelID\":\"Posidex2023091207502836896407\",\"transactionType\":\"Posidex2023091207502836896407\",\"transactionSubType\":\"Posidex2023091207502836896407\",\"conversationID\":\"Posidex2023091207502836896407\",\"externalReferenceId\":\"Posidex2023091207502836896407\",\"authInfo\":{\"branchID\":\"1001\",\"userID\":\"Posidex\",\"token\":\"Posidex\"},\"isAsync\":false},\"msgBdy\":{\"CUSTOMER_CATEGORY\":\"I\",\"MATCHING_RULE_PROFILE\":1,\"LEAD_ID\":\"\",\"DATA_SOURCE\":\"wizard\",\"ACCOUNT_ID\":\"\",\"REQUEST_TYPE\":\"search\",\"UCIC\":\"\",\"PSX_REQUEST_ID\":\"\",\"DEMOGRAPHIC_INFORMATION\":{\"FIRST_NAME\":\"govcomp\",\"LAST_NAME\":\"\",\"MIDDLE_NAME\":\"\",\"FATHER_NAME\":\"\",\"MOTHER_NAME\":\"\",\"SPOUSE_NAME\":\"\",\"FATHER_SPOUSE_NAME\":\"\",\"DATE_OF_BIRTH\":\"\",\"DATE_OF_INC\":\"\",\"AADHAR_REFERENCE_NO\":\"\",\"PAN\":\"\",\"PASSPORT\":\"\",\"DRIVING_LICENSE\":\"\",\"TIN\":\"\",\"GSTIN\":\"\",\"VOTER_CARD_NO\":\"\",\"RATION_CARD_NO\":\"\"},\"ADDRESS_INFORMATION\":{\"ADDRESS1\":\"\",\"AREA1\":\"\",\"CITY1\":\"\",\"STATE1\":\"\",\"PINCODE1\":\"\",\"ADDRESS_TYPE1\":\"\",\"ADDRESS2\":\"\",\"AREA2\":\"\",\"CITY2\":\"\",\"STATE2\":\"\",\"PINCODE2\":\"\",\"ADDRESS_TYPE2\":\"\"},\"CONTACT_INFORMATION\":{\"CUSTOMER_CONTACT_1\":\"\",\"CUSTOMER_CONTACT_TYPE_1\":\"\",\"CUSTOMER_CONTACT_2\":\"\",\"CUSTOMER_CONTACT_TYPE_2\":\"\"}}}}";
                dynamic Request_Template = JsonConvert.DeserializeObject(RequestTemplate);

                dynamic msgBdy = Request_Template.searchOrUpdateDigiDedupeCustomerReq.msgBdy;

                var applicentDtls = await this._commonFunc.getApplicentDetail(customerLeadID);

                msgBdy.CUSTOMER_CATEGORY = await this._commonFunc.getCustomerType(applicentDtls[0]["_eqs_entitytypeid_value"].ToString());
                if (msgBdy.CUSTOMER_CATEGORY == "I")
                {
                    msgBdy.DEMOGRAPHIC_INFORMATION.FIRST_NAME = applicentDtls[0]["eqs_firstname"].ToString();
                    msgBdy.DEMOGRAPHIC_INFORMATION.LAST_NAME = applicentDtls[0]["eqs_lastname"].ToString();
                    msgBdy.DEMOGRAPHIC_INFORMATION.MIDDLE_NAME = applicentDtls[0]["eqs_middlename"].ToString();
                    if (!string.IsNullOrEmpty(applicentDtls[0]["eqs_dob"].ToString()))
                    {
                        string mm = applicentDtls[0]["eqs_dob"].ToString().Substring(0, 2);
                        string dd = applicentDtls[0]["eqs_dob"].ToString().Substring(3, 2);
                        string yy = applicentDtls[0]["eqs_dob"].ToString().Substring(6, 4);
                        //msgBdy.DEMOGRAPHIC_INFORMATION.DATE_OF_BIRTH = dd + "-" + mm + "-" + yy;
                        msgBdy.DEMOGRAPHIC_INFORMATION.DATE_OF_BIRTH = yy+ mm + dd;
                    }
                }
                else
                {
                    msgBdy.DEMOGRAPHIC_INFORMATION.FIRST_NAME = applicentDtls[0]["eqs_companynamepart1"].ToString();
                    msgBdy.DEMOGRAPHIC_INFORMATION.LAST_NAME = applicentDtls[0]["eqs_companynamepart2"].ToString();
                    msgBdy.DEMOGRAPHIC_INFORMATION.MIDDLE_NAME = applicentDtls[0]["eqs_companynamepart3"].ToString();
                    if (!string.IsNullOrEmpty(applicentDtls[0]["eqs_dateofincorporation"].ToString()))
                    {
                        string dd = applicentDtls[0]["eqs_dateofincorporation"].ToString().Substring(0, 2);
                        string mm = applicentDtls[0]["eqs_dateofincorporation"].ToString().Substring(3, 2);
                        string yy = applicentDtls[0]["eqs_dateofincorporation"].ToString().Substring(6, 4);
                        //msgBdy.DEMOGRAPHIC_INFORMATION.DATE_OF_INC = dd + "-" + mm + "-" + yy;
                        msgBdy.DEMOGRAPHIC_INFORMATION.DATE_OF_INC = yy + mm + dd;
                    }
                }

                msgBdy.DEMOGRAPHIC_INFORMATION.PAN = applicentDtls[0]["eqs_internalpan"].ToString();
                msgBdy.DEMOGRAPHIC_INFORMATION.AADHAR_REFERENCE_NO = applicentDtls[0]["eqs_aadhaarreference"].ToString();
                msgBdy.DEMOGRAPHIC_INFORMATION.PASSPORT = applicentDtls[0]["eqs_passportnumber"].ToString();
                msgBdy.DEMOGRAPHIC_INFORMATION.VOTER_CARD_NO = applicentDtls[0]["eqs_voterid"].ToString();



                Request_Template.searchOrUpdateDigiDedupeCustomerReq.msgBdy = msgBdy;

                string request_body = JsonConvert.SerializeObject(Request_Template);
                this._logger.LogInformation("getCustomerLead", request_body, "B4 calling CBS API");
                string postDataParametr = await EncriptRespons(request_body, "FI0060");
                string Lead_details = await this._queryParser.HttpCBSApiCall(Token, HttpMethod.Post, "CBSsearchOrUpdateDedupeCustomer", postDataParametr);
                //string Lead_details = "{\"searchOrUpdateDigiDedupeCustomerRes\":{\"msgHdr\":{\"result\":\"OK\"},\"msgBdy\":{\"REQUEST_ID\":\"1051065\",\"STATUS\":\"C\",\"RESPONSE_CODE\":\"200\",\"DESCRIPTION\":\"RequestProcessedSuccessfully\",\"CUSTOMER_MATCH_COUNT\":\"10\",\"EXACT_MATCH_COUNT\":\"10\",\"PROBABLE_MATCH_COUNT\":\"0\",\"SUCCESS_MESSAGE\":\"Customerinformationsearchedsuccessfully\",\"CUSTOMER_MATCHES\":{\"INDIVIDUAL_MATCHES\":[{\"NAME\":\"RAVANALANKAPRABU\",\"FATHER_NAME\":\"\",\"FATHER_SPOUSE_NAME\":\"\",\"MOTHER_NAME\":\"\",\"SPOUSE_NAME\":\"\",\"DATE_OF_BIRTH\":\"\",\"DATE_OF_INC\":\"\",\"AADHAR_REFERENCE_NO\":\"\",\"PAN\":\"EIXIX6982G\",\"PASSPORT_NO\":\"\",\"DRIVING_LICENSE_NO\":\"\",\"TIN\":\"\",\"TAN\":\"\",\"CIN\":\"\",\"DIN\":\"\",\"NREGA\":\"\",\"CKYC\":\"\",\"VOTER_CARD_NO\":\"\",\"GST_IN\":\"\",\"RATION_CARD\":\"\",\"ADDRESS_TYPE_0\":\"\",\"ADDRESS_0\":\"\",\"AREA_0\":\"\",\"CITY_0\":\"\",\"STATE_0\":\"\",\"PINCODE_0\":\"\",\"ADDRESS_TYPE_1\":\"\",\"ADDRESS_1\":\"\",\"AREA_1\":\"\",\"CITY_1\":\"\",\"STATE_1\":\"\",\"PINCODE_1\":\"\",\"ADDRESS_TYPE_2\":\"\",\"ADDRESS_2\":\"\",\"AREA_2\":\"\",\"CITY_2\":\"\",\"STATE_2\":\"\",\"PINCODE_2\":\"\",\"ADDRESS_TYPE_3\":\"\",\"ADDRESS_3\":\"\",\"AREA_3\":\"\",\"CITY_3\":\"\",\"STATE_3\":\"\",\"PINCODE_3\":\"\",\"ADDRESS_TYPE_4\":\"\",\"ADDRESS_4\":\"\",\"AREA_4\":\"\",\"CITY_4\":\"\",\"STATE_4\":\"\",\"PINCODE_4\":\"\",\"MOBILE_TYPE_0\":\"\",\"MOBILE_0\":\"\",\"MOBILE_TYPE_1\":\"\",\"MOBILE_1\":\"\",\"MOBILE_TYPE_2\":\"\",\"MOBILE_2\":\"\",\"MOBILE_TYPE_3\":\"\",\"MOBILE_3\":\"\",\"MOBILE_TYPE_4\":\"\",\"MOBILE_4\":\"\",\"RECORD_TYPE\":\"ONLINE\",\"MATCH_CRITERIA\":\"PAN\",\"IS_EXACT_MATCH\":\"True\",\"IS_PROBABLE_MATCH\":\"False\",\"MATCHED_ID\":\"12312\",\"UCIC\":\"12312\",\"LEAD_ID\":\"\",\"IND_NON_DETAILS\":\"I\"}],\"ORGANISATION_MATCHES\":[]}}}}";
                dynamic responsD = JsonConvert.DeserializeObject(Lead_details);
                this._logger.LogInformation("HttpCBSApiCall output", Lead_details, "After calling CBS API");
                if (responsD.msgHdr != null && responsD.msgHdr.result == "ERROR")
                {
                    ddupdgCustomerReturn.ReturnCode = "CRM-ERROR-101";
                    dynamic respons_code = responsD.msgHdr["error"];
                    ddupdgCustomerReturn.Message = respons_code[0].reason.ToString();
                }
                else
                {
                    if (responsD.searchOrUpdateDigiDedupeCustomerRes != null && responsD.searchOrUpdateDigiDedupeCustomerRes.msgBdy.CUSTOMER_MATCHES.INDIVIDUAL_MATCHES.Count > 0)
                    {
                        List<Individual> all_individuals = new List<Individual>();
                        var individuals = responsD.searchOrUpdateDigiDedupeCustomerRes.msgBdy.CUSTOMER_MATCHES.INDIVIDUAL_MATCHES;
                        foreach (var individual in individuals)
                        {
                            this._logger.LogInformation("getCustomerLead", JsonConvert.SerializeObject(individual), "Inside individual loop");
                            Individual individual_obj = new Individual();
                            individual_obj.NAME = individual["NAME"].ToString();
                            individual_obj.FATHER_NAME = individual["FATHER_NAME"].ToString();
                            individual_obj.FATHER_SPOUSE_NAME = individual["FATHER_SPOUSE_NAME"].ToString();
                            individual_obj.MOTHER_NAME = individual["MOTHER_NAME"].ToString();
                            individual_obj.SPOUSE_NAME = individual["SPOUSE_NAME"].ToString();
                            individual_obj.DATE_OF_BIRTH = individual["DATE_OF_BIRTH"].ToString();
                            individual_obj.DATE_OF_INC = individual["DATE_OF_INC"].ToString();
                            individual_obj.AADHAR_REFERENCE_NO = individual["AADHAR_REFERENCE_NO"].ToString();
                            individual_obj.PAN = individual["PAN"].ToString();
                            individual_obj.PASSPORT_NO = individual["PASSPORT_NO"].ToString();
                            individual_obj.DRIVING_LICENSE_NO = individual["DRIVING_LICENSE_NO"].ToString();
                            individual_obj.TIN = individual["TIN"].ToString();
                            individual_obj.TAN = individual["TAN"].ToString();
                            individual_obj.CIN = individual["CIN"].ToString();
                            individual_obj.DIN = individual["DIN"].ToString();
                            individual_obj.NREGA = individual["NREGA"].ToString();
                            individual_obj.CKYC = individual["CKYC"].ToString();
                            individual_obj.VOTER_CARD_NO = individual["VOTER_CARD_NO"].ToString();
                            individual_obj.GST_IN = individual["GST_IN"].ToString();
                            individual_obj.RATION_CARD = individual["RATION_CARD"].ToString();
                            individual_obj.MOBILE_TYPE_0 = individual["MOBILE_TYPE_0"].ToString();
                            individual_obj.MOBILE_0 = individual["MOBILE_0"].ToString();
                            individual_obj.MOBILE_TYPE_1 = individual["MOBILE_TYPE_1"].ToString();
                            individual_obj.MOBILE_1 = individual["MOBILE_1"].ToString();
                            individual_obj.MOBILE_TYPE_2 = individual["MOBILE_TYPE_2"].ToString();
                            individual_obj.MOBILE_2 = individual["MOBILE_2"].ToString();
                            individual_obj.MOBILE_TYPE_3 = individual["MOBILE_TYPE_3"].ToString();
                            individual_obj.MOBILE_3 = individual["MOBILE_3"].ToString();
                            individual_obj.MOBILE_TYPE_4 = individual["MOBILE_TYPE_4"].ToString();
                            individual_obj.MOBILE_4 = individual["MOBILE_4"].ToString();
                            individual_obj.RECORD_TYPE = individual["RECORD_TYPE"].ToString();
                            individual_obj.MATCH_CRITERIA = individual["MATCH_CRITERIA"].ToString();
                            individual_obj.IS_EXACT_MATCH = individual["IS_EXACT_MATCH"].ToString();
                            individual_obj.IS_PROBABLE_MATCH = individual["IS_PROBABLE_MATCH"].ToString();
                            individual_obj.MATCHED_ID = individual["MATCHED_ID"].ToString();
                            individual_obj.UCIC = individual["UCIC"].ToString();
                            individual_obj.LEAD_ID = individual["LEAD_ID"].ToString();
                            individual_obj.IND_NON_DETAILS = individual["IND_NON_DETAILS"].ToString();
                            all_individuals.Add(individual_obj);

                        }

                        responsD.searchOrUpdateDigiDedupeCustomerRes.msgBdy.CUSTOMER_MATCHES = "";
                        IndividualsData individualsData = new IndividualsData();
                        individualsData.INDIVIDUAL_MATCHES = all_individuals;

                        responsD.searchOrUpdateDigiDedupeCustomerRes.msgBdy.CUSTOMER_MATCHES = (JObject)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(individualsData));
                    }
                    else if (responsD.searchOrUpdateDigiDedupeCustomerRes != null && responsD.searchOrUpdateDigiDedupeCustomerRes.msgBdy.CUSTOMER_MATCHES.ORGANISATION_MATCHES.Count > 0)
                    {
                        List<Individual> all_organisations = new List<Individual>();
                        var Organisations = responsD.searchOrUpdateDigiDedupeCustomerRes.msgBdy.CUSTOMER_MATCHES.ORGANISATION_MATCHES;
                        foreach (var Organisation in Organisations)
                        {
                            this._logger.LogInformation("getCustomerLead", JsonConvert.SerializeObject(Organisation), "Inside Corporate loop");
                            Individual individual_obj = new Individual();
                            individual_obj.NAME = Organisation["NAME"].ToString();
                            individual_obj.FATHER_NAME = Organisation["FATHER_NAME"].ToString();
                            individual_obj.FATHER_SPOUSE_NAME = Organisation["FATHER_SPOUSE_NAME"].ToString();
                            individual_obj.MOTHER_NAME = Organisation["MOTHER_NAME"].ToString();
                            individual_obj.SPOUSE_NAME = Organisation["SPOUSE_NAME"].ToString();
                            individual_obj.DATE_OF_BIRTH = Organisation["DATE_OF_BIRTH"].ToString();
                            individual_obj.DATE_OF_INC = Organisation["DATE_OF_INC"].ToString();
                            individual_obj.AADHAR_REFERENCE_NO = Organisation["AADHAR_REFERENCE_NO"].ToString();
                            individual_obj.PAN = Organisation["PAN"].ToString();
                            individual_obj.PASSPORT_NO = Organisation["PASSPORT_NO"].ToString();
                            individual_obj.DRIVING_LICENSE_NO = Organisation["DRIVING_LICENSE_NO"].ToString();
                            individual_obj.TIN = Organisation["TIN"].ToString();
                            individual_obj.TAN = Organisation["TAN"].ToString();
                            individual_obj.CIN = Organisation["CIN"].ToString();
                            individual_obj.DIN = Organisation["DIN"].ToString();
                            individual_obj.NREGA = Organisation["NREGA"].ToString();
                            individual_obj.CKYC = Organisation["CKYC"].ToString();
                            individual_obj.VOTER_CARD_NO = Organisation["VOTER_CARD_NO"].ToString();
                            individual_obj.GST_IN = Organisation["GST_IN"].ToString();
                            individual_obj.RATION_CARD = Organisation["RATION_CARD"].ToString();
                            individual_obj.MOBILE_TYPE_0 = Organisation["MOBILE_TYPE_0"].ToString();
                            individual_obj.MOBILE_0 = Organisation["MOBILE_0"].ToString();
                            individual_obj.MOBILE_TYPE_1 = Organisation["MOBILE_TYPE_1"].ToString();
                            individual_obj.MOBILE_1 = Organisation["MOBILE_1"].ToString();
                            individual_obj.MOBILE_TYPE_2 = Organisation["MOBILE_TYPE_2"].ToString();
                            individual_obj.MOBILE_2 = Organisation["MOBILE_2"].ToString();
                            individual_obj.MOBILE_TYPE_3 = Organisation["MOBILE_TYPE_3"].ToString();
                            individual_obj.MOBILE_3 = Organisation["MOBILE_3"].ToString();
                            individual_obj.MOBILE_TYPE_4 = Organisation["MOBILE_TYPE_4"].ToString();
                            individual_obj.MOBILE_4 = Organisation["MOBILE_4"].ToString();
                            individual_obj.RECORD_TYPE = Organisation["RECORD_TYPE"].ToString();
                            individual_obj.MATCH_CRITERIA = Organisation["MATCH_CRITERIA"].ToString();
                            individual_obj.IS_EXACT_MATCH = Organisation["IS_EXACT_MATCH"].ToString();
                            individual_obj.IS_PROBABLE_MATCH = Organisation["IS_PROBABLE_MATCH"].ToString();
                            individual_obj.MATCHED_ID = Organisation["MATCHED_ID"].ToString();
                            individual_obj.UCIC = Organisation["UCIC"].ToString();
                            individual_obj.LEAD_ID = Organisation["LEAD_ID"].ToString();
                            individual_obj.IND_NON_DETAILS = Organisation["IND_NON_DETAILS"].ToString();
                            all_organisations.Add(individual_obj);

                        }

                        responsD.searchOrUpdateDigiDedupeCustomerRes.msgBdy.CUSTOMER_MATCHES = "";
                        OrganisationsData OrganisationsData = new OrganisationsData();
                        OrganisationsData.ORGANISATION_MATCHES = all_organisations;
                        responsD.searchOrUpdateDigiDedupeCustomerRes.msgBdy.CUSTOMER_MATCHES = (JObject)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(OrganisationsData));
                    }

                    if (responsD.searchOrUpdateDigiDedupeCustomerRes != null)
                    {
                        if (Convert.ToInt32(responsD.searchOrUpdateDigiDedupeCustomerRes.msgBdy.EXACT_MATCH_COUNT.ToString()) > 0)
                        {
                            Dictionary<string, string> odatab = new Dictionary<string, string>();
                            //odatab.Add("eqs_leadstatus", await this._queryParser.getOptionSetTextToValue("lead", "eqs_leadstatus", "Not Onboarded"));
                            odatab.Add("eqs_leadstatus", "2"); //Not Onboarded
                            odatab.Add("eqs_assetleadstatus", "868420001"); //Lead Dropped
                            odatab.Add("statecode", "2"); // Disqualified
                            odatab.Add("statuscode", "136980001"); // Duplicate
                            postDataParametr = JsonConvert.SerializeObject(odatab);
                            var LeadAccount_details = await this._queryParser.HttpApiCall($"leads({applicentDtls[0]["_eqs_leadid_value"].ToString()})", HttpMethod.Patch, postDataParametr);
                            ddupdgCustomerReturn.decideNL = responsD;
                        }
                        else
                        {
                            ddupdgCustomerReturn.decideNL = false;
                        }
                        ddupdgCustomerReturn.Message = OutputMSG.Case_Success;
                        ddupdgCustomerReturn.ReturnCode = "CRM-SUCCESS";
                    }
                    else
                    {
                        ddupdgCustomerReturn.ReturnCode = "CRM-ERROR-101";
                        ddupdgCustomerReturn.Message = Lead_details;
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("getCustomerLead", ex.Message);
                ddupdgCustomerReturn.Message = ex.Message;
                ddupdgCustomerReturn.ReturnCode = "CRM-ERROR-102";
            }

            return ddupdgCustomerReturn;
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



        public async Task<string> EncriptRespons(string ResponsData, string Bankcode)
        {
            return await _queryParser.PayloadEncryption(ResponsData, Transaction_ID, Bankcode);
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