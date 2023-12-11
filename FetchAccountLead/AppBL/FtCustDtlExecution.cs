namespace FetchAccountLead
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

    public class FtCustDtlExecution : IFtCustDtlExecution
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

        private string customerid, accountid;

        private List<string> applicents = new List<string>();


        Dictionary<string, string> genderc = new Dictionary<string, string>();

        private ICommonFunction _commonFunc;

        public FtCustDtlExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {

            this._logger = logger;

            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;



        }


        public async Task<FetchCustomerDtlReturn> ValidateCustInput(dynamic RequestData)
        {
            FetchCustomerDtlReturn ldRtPrm = new FetchCustomerDtlReturn();
            RequestData = await this.getRequestData(RequestData, "FetchCustomerDetails");
            try
            {

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "FetchCustomerDetailsappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {

                        if (!string.IsNullOrEmpty(RequestData.CustomerID.ToString()))
                        {
                            string accountnumber = string.Empty;
                            if (RequestData is JObject && ((JObject)RequestData).ContainsKey("AccountNumber"))
                            {
                                accountnumber = RequestData.AccountNumber.ToString();
                            }
                            ldRtPrm = await this.FetCustomerDtl(RequestData.CustomerID.ToString(), accountnumber);
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateLeadtInput", "Customer ID is incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "Customer ID is incorrect";
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateLeadtInput", "Transaction_ID or Channel_ID is incorrect.");
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


        private async Task<FetchCustomerDtlReturn> FetCustomerDtl(string CustomerID, string AccountNumber)
        {
            FetchCustomerDtlReturn customerDetailReturn = new FetchCustomerDtlReturn();

            var CuatomerDetails = await this._commonFunc.getCustomerDetails("eqs_customerid", CustomerID);
            if (CuatomerDetails.Count > 0)
            {
                customerDetailReturn.General = new GeneralDetails();

                //Logic for Minor
                if (!string.IsNullOrEmpty(CuatomerDetails[0]["birthdate"].ToString()))
                {
                    int years = GetAgeInYears(CuatomerDetails[0]["birthdate"].ToString());
                    if (years < 18)
                        customerDetailReturn.General.IsMinor = "Yes";
                    else
                        customerDetailReturn.General.IsMinor = "No";
                }

                customerDetailReturn.General.IsMFICustomer = CuatomerDetails[0]["eqs_ismficustomer"].ToString();
                customerDetailReturn.General.NPAClassification = CuatomerDetails[0]["eqs_npaclassification"].ToString();
                customerDetailReturn.General.PANVerifiedStatus = CuatomerDetails[0]["eqs_panverifiedstatus"].ToString();
                customerDetailReturn.General.IsDeferral = CuatomerDetails[0]["eqs_isdeferral"].ToString();
                customerDetailReturn.General.PoliticallyExposedPerson = CuatomerDetails[0]["eqs_politicallyexposedperson"].ToString();
                customerDetailReturn.General.NRIKYCMode = CuatomerDetails[0]["eqs_nrikycmode"].ToString();
                customerDetailReturn.General.NRIMobileNoPref = CuatomerDetails[0]["eqs_nrimobilenopref"].ToString();
                customerDetailReturn.General.AlternateMandatoryCheck = CuatomerDetails[0]["eqs_alternatemandatorycheck"].ToString();
                customerDetailReturn.General.NPO = CuatomerDetails[0]["eqs_npoflag"].ToString();
                customerDetailReturn.General.EntityFlag = CuatomerDetails[0]["eqs_entityflag"].ToString();
                customerDetailReturn.General.EntityKey = CuatomerDetails[0]["eqs_subentitykey"].ToString();

                //Lookup
                if (CuatomerDetails[0]["_eqs_purposeofcreationlo_value@OData.Community.Display.V1.FormattedValue"] != null)
                    customerDetailReturn.General.PurposeOfCreation = CuatomerDetails[0]["_eqs_purposeofcreationlo_value@OData.Community.Display.V1.FormattedValue"].ToString();

                //Option set
                if (CuatomerDetails[0]["eqs_nrivisatyype@OData.Community.Display.V1.FormattedValue"] != null)
                    customerDetailReturn.General.NRIVisaType = CuatomerDetails[0]["eqs_nrivisatyype@OData.Community.Display.V1.FormattedValue"].ToString();
                if (CuatomerDetails[0]["eqs_panform60@OData.Community.Display.V1.FormattedValue"] != null)
                    customerDetailReturn.General.Form60 = CuatomerDetails[0]["eqs_panform60@OData.Community.Display.V1.FormattedValue"].ToString();

                if (!string.IsNullOrEmpty(CuatomerDetails[0]["eqs_rmemployeeid"].ToString()))
                {
                    customerDetailReturn.RMDetails = new RMDetails();

                    customerDetailReturn.RMDetails.RMCode = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_rmempidslot"].ToString();
                    customerDetailReturn.RMDetails.RMName = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_name"].ToString();
                    customerDetailReturn.RMDetails.RMRole = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_emprole"].ToString();
                    customerDetailReturn.RMDetails.RMType = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_emprolelabel"].ToString();

                    customerDetailReturn.RMDetails.Area = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_area"].ToString();
                    customerDetailReturn.RMDetails.Branch = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_branch"].ToString();
                    customerDetailReturn.RMDetails.BranchCode = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_branchcodeslot"].ToString();
                    customerDetailReturn.RMDetails.Cluster = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_cluster"].ToString();
                    customerDetailReturn.RMDetails.Department = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_department"].ToString();
                    customerDetailReturn.RMDetails.Divison = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_division"].ToString();
                    customerDetailReturn.RMDetails.EmailID = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_emailid"].ToString();
                    customerDetailReturn.RMDetails.EmpCategory = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_empcategory"].ToString();
                    customerDetailReturn.RMDetails.EmpPhoneNumber = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_empphonenumber"].ToString();
                    customerDetailReturn.RMDetails.EmpRoleLabel = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_emprolelabel"].ToString();
                    customerDetailReturn.RMDetails.EmpStatus = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_empstatus"].ToString();
                    customerDetailReturn.RMDetails.Region = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_region"].ToString();
                    customerDetailReturn.RMDetails.ResignedFlag = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_resignedflagtwo"].ToString();
                    customerDetailReturn.RMDetails.State = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_state"].ToString();
                    customerDetailReturn.RMDetails.SupervisorEmailID = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_supervisoremailid"].ToString();
                    customerDetailReturn.RMDetails.SupervisorEmpID = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_supervisorempidslot"].ToString();
                    customerDetailReturn.RMDetails.SupervisorName = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_supervisorname"].ToString();
                    customerDetailReturn.RMDetails.Zone = CuatomerDetails[0]["eqs_rmemployeeid"]["eqs_zone"].ToString();

                }

                if (!string.IsNullOrEmpty(AccountNumber))
                {
                    var AccountRelDetails = await this._commonFunc.getAccountRelationshipDetails(CustomerID, AccountNumber);
                    if (AccountRelDetails.Count > 0)
                    {
                        customerDetailReturn.CustomerPreferences = new CustomerPreferences();
                        customerDetailReturn.CustomerPreferences.NetBanking = AccountRelDetails[0]["eqs_netbanking"].ToString();
                        customerDetailReturn.CustomerPreferences.MobileBanking = AccountRelDetails[0]["eqs_mobilebanking"].ToString();
                        customerDetailReturn.CustomerPreferences.SMS = AccountRelDetails[0]["eqs_smstwo"].ToString();
                        customerDetailReturn.CustomerPreferences.AllSMSAlerts = (!Convert.ToBoolean(AccountRelDetails[0]["eqs_onlytransactionalerts"].ToString())).ToString();
                        customerDetailReturn.CustomerPreferences.PhysicalStatement = AccountRelDetails[0]["eqs_physicalstatement"].ToString();
                        customerDetailReturn.CustomerPreferences.EmailStatement = AccountRelDetails[0]["eqs_emailstatement"].ToString();

                        this.customerid = AccountRelDetails[0]["_eqs_customeridvalue_value"].ToString();
                        this.accountid = AccountRelDetails[0]["_eqs_accountid_value"].ToString();
                        //Add Beat and OnCall Service Details
                        var ServiceDetails = await this._commonFunc.getServiceDetails(customerid, accountid);
                        if (ServiceDetails.Count > 0)
                        {
                            customerDetailReturn.DSBServiceDetails = new List<ServiceDetails>();
                            var servicedetail = ServiceDetails[0];

                            ServiceDetails item = new ServiceDetails();
                            if (!string.IsNullOrEmpty(servicedetail["eqs_cashpickup"].ToString()))
                            {
                                item.ServiceName = "Cash Pickup";
                                item.IsRegistered = "Yes";
                                item.ServiceType = servicedetail["eqs_cashpickup@OData.Community.Display.V1.FormattedValue"].ToString().Substring(0, 1);
                                item.Limit = servicedetail["_eqs_cashpickuplimit_value@OData.Community.Display.V1.FormattedValue"].ToString();
                                item.VendorID = servicedetail["eqs_VendorCashPickup"]["eqs_vendorid"].ToString();
                                item.VendorName = servicedetail["eqs_VendorCashPickup"]["eqs_name"].ToString();
                                item.Location = servicedetail["_eqs_vendorlocationcashpickup_value@OData.Community.Display.V1.FormattedValue"].ToString();
                            }
                            else
                            {
                                item.ServiceName = "Cash Pickup";
                                item.IsRegistered = "No";
                            }
                            customerDetailReturn.DSBServiceDetails.Add(item);

                            item = new ServiceDetails();
                            if (!string.IsNullOrEmpty(servicedetail["eqs_cashdelivery"].ToString()))
                            {
                                item.ServiceName = "Cash Delivery";
                                item.IsRegistered = "Yes";
                                item.ServiceType = servicedetail["eqs_cashdelivery@OData.Community.Display.V1.FormattedValue"].ToString().Substring(0, 1);
                                item.Limit = servicedetail["_eqs_cashdeliverylimit_value@OData.Community.Display.V1.FormattedValue"].ToString();
                                item.VendorID = servicedetail["eqs_VendorCashDelivery"]["eqs_vendorid"].ToString();
                                item.VendorName = servicedetail["eqs_VendorCashDelivery"]["eqs_name"].ToString();
                                item.Location = servicedetail["_eqs_vendorlocationcashdelivery_value@OData.Community.Display.V1.FormattedValue"].ToString();
                            }
                            else
                            {
                                item.ServiceName = "Cash Delivery";
                                item.IsRegistered = "No";
                            }
                            customerDetailReturn.DSBServiceDetails.Add(item);

                            item = new ServiceDetails();
                            if (!string.IsNullOrEmpty(servicedetail["eqs_chequepickup"].ToString()))
                            {
                                item.ServiceName = "Cheque Pickup";
                                item.IsRegistered = "Yes";
                                item.ServiceType = servicedetail["eqs_chequepickup@OData.Community.Display.V1.FormattedValue"].ToString().Substring(0, 1);
                                item.VendorID = servicedetail["eqs_VendorChequePickup"]["eqs_vendorid"].ToString();
                                item.VendorName = servicedetail["eqs_VendorChequePickup"]["eqs_name"].ToString();
                                item.Location = servicedetail["_eqs_vendorlocationchequepickup_value@OData.Community.Display.V1.FormattedValue"].ToString();
                            }
                            else
                            {
                                item.ServiceName = "Cheque Pickup";
                                item.IsRegistered = "No";
                            }
                            customerDetailReturn.DSBServiceDetails.Add(item);
                        }
                    }
                }

                customerDetailReturn.ReturnCode = "CRM-SUCCESS";
                customerDetailReturn.Message = OutputMSG.Case_Success;
            }
            else
            {
                customerDetailReturn.ReturnCode = "CRM-ERROR-101";
                customerDetailReturn.Message = OutputMSG.Resource_n_Found;
            }

            return customerDetailReturn;
        }

        private int GetAgeInYears(string dobstring)
        {
            int yy = Convert.ToInt32(dobstring.Substring(0, 4));
            int mm = Convert.ToInt32(dobstring.Substring(5, 2));
            int dd = Convert.ToInt32(dobstring.Substring(8, 2));
            DateTime dob = new DateTime(yy, mm, dd);
            TimeSpan diff = DateTime.Today - dob;

            DateTime zerodate = new DateTime(1, 1, 1);
            return (zerodate + diff).Year - 1;
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
