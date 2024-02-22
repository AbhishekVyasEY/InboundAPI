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
    using System.Text;

    public class UpdgAccLeadExecution : IUpdgAccLeadExecution
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
        private LeadAccount _accountLead;
        private LeadDetails _leadParam;
        private List<AccountApplicant> _accountApplicants;
        private List<Preference> Preferences;


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

            if (RequestData.ErrorNo != null && RequestData.ErrorNo.ToString() == "Error99")
            {
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = "API do not have access permission!";
                return ldRtPrm;
            }

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


        private async Task<UpAccountLeadReturn> CreateDDELeadAccount(dynamic RequestData)
        {
            UpAccountLeadReturn accountLeadReturn = new UpAccountLeadReturn();

            var _leadDetails = await this._commonFunc.getLeadAccountDetails(RequestData.General?.AccountLeadId.ToString());
            if (_leadDetails.Count > 0)
            {
                int haserror = 0;
                List<string> fields = new List<string>();
                if (string.IsNullOrEmpty(_leadDetails[0]["eqs_ddefinalid"].ToString()))
                {
                    if (string.IsNullOrEmpty(RequestData.General?.ApplicationDate?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("ApplicationDate");
                    }
                    if (string.IsNullOrEmpty(RequestData.General?.InstaKit?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("InstaKit");
                    }
                    else
                    {
                        if (RequestData.General?.InstaKit?.ToString() == "A/C No kit")
                        {
                            if (string.IsNullOrEmpty(RequestData.General?.InstaKitAccountNumber?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("InstaKitAccountNumber");
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(RequestData.General?.AccountOpeningBranch?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("AccountOpeningBranch");
                    }
                    if (string.IsNullOrEmpty(RequestData.General?.PurposeofOpeningAccount?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("PurposeofOpeningAccount");
                    }
                    else
                    {
                        if (RequestData.General?.PurposeofOpeningAccount?.ToString() == "Others")
                        {
                            if (string.IsNullOrEmpty(RequestData.General?.PurposeOfOpeningAccountOthers?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("PurposeOfOpeningAccountOthers");
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(RequestData.General?.ModeofOperation?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("ModeofOperation");
                    }
                    if (string.IsNullOrEmpty(RequestData.General?.AccountOwnership?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("AccountOwnership");
                    }
                    if (string.IsNullOrEmpty(RequestData.General?.LGCode?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("LGCode");
                    }
                    if (string.IsNullOrEmpty(RequestData.General?.LCCode?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("LCCode");
                    }

                    //if (RequestData.AdditionalDetails != null && string.IsNullOrEmpty(RequestData.AdditionalDetails?.DepositAmount?.ToString()))
                    //{
                    //    haserror = 1;
                    //    fields.Add("DepositAmount");
                    //}

                    if (RequestData.FDRDDetails != null)
                    {
                        if (string.IsNullOrEmpty(RequestData.FDRDDetails?.DepositDetails?.SpecialInterestRateRequired?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("SpecialInterestRateRequired");
                        }
                        if (string.IsNullOrEmpty(RequestData.FDRDDetails?.DepositDetails?.WaivedOffTDS?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("WaivedOffTDS");
                        }
                    }

                    if (RequestData.DirectBanking.Preferences != null)
                    {
                        foreach (var items in RequestData.DirectBanking.Preferences)
                        {
                            if (string.IsNullOrEmpty(items.UCIC?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("UCIC");
                            }
                            if (string.IsNullOrEmpty(items.DebitCardFlag?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("DebitCardFlag");
                            }
                            else
                            {
                                if (items["DebitCardFlag"].ToString().ToLower() == "yes")
                                {
                                    if (string.IsNullOrEmpty(items.NameonCard?.ToString()))
                                    {
                                        haserror = 1;
                                        fields.Add("NameonCard");
                                    }
                                    if (string.IsNullOrEmpty(items.DebitCardID?.ToString()))
                                    {
                                        haserror = 1;
                                        fields.Add("DebitCardID");
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(items.SMS?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("SMS");
                            }
                            if (string.IsNullOrEmpty(items.NetBanking?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("NetBanking");
                            }
                            if (string.IsNullOrEmpty(items.MobileBanking?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("MobileBanking");
                            }
                            if (string.IsNullOrEmpty(items.EmailStatement?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("EmailStatement");
                            }
                            if (string.IsNullOrEmpty(items.InternationalDCLimitAct?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("InternationalDCLimitAct");
                            }
                            if (string.IsNullOrEmpty(items.physicalStatement?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("physicalStatement");
                            }
                        }
                    }


                    if (RequestData.Nominee != null)
                    {
                        if (string.IsNullOrEmpty(RequestData.Nominee?.name?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("name");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.NomineeRelationship?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("NomineeRelationship");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.DOB?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("DOB");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.Address1?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("Address1");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.Pin?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("Pin");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.CityCode?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("CityCode");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.District?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("District");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.CountryCode?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("CountryCode");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.State?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("State");
                        }

                        if (RequestData.Nominee?.Guardian != null)
                        {
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.Name?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian Name");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.RelationshipToMinor?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian RelationshipToMinor");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.GuardianAddress1?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian Address");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.GuardianPin?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian Pin");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.GuardianCityCode?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian CityCode");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.GuardianDistrict?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian District");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.GuardianCountryCode?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian CountryCode");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.GuardianState?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian State");
                            }
                        }

                    }


                }
                else
                {
                    if (RequestData.General != null)
                    {
                        if (string.IsNullOrEmpty(RequestData.General?.ApplicationDate?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("ApplicationDate");
                        }
                        if (string.IsNullOrEmpty(RequestData.General?.InstaKit?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("InstaKit");
                        }
                        else
                        {
                            if (RequestData.General?.InstaKit?.ToString() == "A/C No kit")
                            {
                                if (string.IsNullOrEmpty(RequestData.General?.InstaKitAccountNumber?.ToString()))
                                {
                                    haserror = 1;
                                    fields.Add("InstaKitAccountNumber");
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(RequestData.General?.AccountOpeningBranch?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("AccountOpeningBranch");
                        }
                        if (string.IsNullOrEmpty(RequestData.General?.PurposeofOpeningAccount?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("PurposeofOpeningAccount");
                        }
                        else
                        {
                            if (RequestData.General?.PurposeofOpeningAccount?.ToString() == "Others")
                            {
                                if (string.IsNullOrEmpty(RequestData.General?.PurposeOfOpeningAccountOthers?.ToString()))
                                {
                                    haserror = 1;
                                    fields.Add("PurposeOfOpeningAccountOthers");
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(RequestData.General?.ModeofOperation?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("ModeofOperation");
                        }
                        if (string.IsNullOrEmpty(RequestData.General?.AccountOwnership?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("AccountOwnership");
                        }
                        if (string.IsNullOrEmpty(RequestData.General?.LGCode?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("LGCode");
                        }
                        if (string.IsNullOrEmpty(RequestData.General?.LCCode?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("LCCode");
                        }
                    }

                    if (RequestData.FDRDDetails != null)
                    {
                        if (string.IsNullOrEmpty(RequestData.FDRDDetails?.DepositDetails?.SpecialInterestRateRequired?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("SpecialInterestRateRequired");
                        }
                        if (string.IsNullOrEmpty(RequestData.FDRDDetails?.DepositDetails?.WaivedOffTDS?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("WaivedOffTDS");
                        }
                    }

                    if (RequestData.DirectBanking?.Preferences != null)
                    {
                        foreach (var items in RequestData.DirectBanking.Preferences)
                        {
                            //if (string.IsNullOrEmpty(items.PreferenceID?.ToString()))
                            //{
                            //    haserror = 1;
                            //    fields.Add("PreferenceID");
                            //}
                            if (string.IsNullOrEmpty(items.UCIC?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("UCIC");
                            }
                            if (string.IsNullOrEmpty(items.DebitCardFlag?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("DebitCardFlag");
                            }
                            else
                            {
                                if (items["DebitCardFlag"].ToString().ToLower() == "yes")
                                {
                                    if (string.IsNullOrEmpty(items.NameonCard?.ToString()))
                                    {
                                        haserror = 1;
                                        fields.Add("NameonCard");
                                    }
                                    if (string.IsNullOrEmpty(items.DebitCardID?.ToString()))
                                    {
                                        haserror = 1;
                                        fields.Add("DebitCardID");
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(items.SMS?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("SMS");
                            }
                            if (string.IsNullOrEmpty(items.NetBanking?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("NetBanking");
                            }
                            if (string.IsNullOrEmpty(items.MobileBanking?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("MobileBanking");
                            }
                            if (string.IsNullOrEmpty(items.EmailStatement?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("EmailStatement");
                            }
                            if (string.IsNullOrEmpty(items.InternationalDCLimitAct?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("InternationalDCLimitAct");
                            }
                            if (string.IsNullOrEmpty(items.physicalStatement?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("physicalStatement");
                            }
                        }
                    }


                    if (RequestData.Nominee != null)
                    {

                        if (string.IsNullOrEmpty(RequestData.Nominee?.name?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("name");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.NomineeRelationship?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("NomineeRelationship");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.DOB?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("DOB");
                        }

                        if (string.IsNullOrEmpty(RequestData.Nominee?.Address1?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("Address1");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.Pin?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("Pin");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.CityCode?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("CityCode");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.District?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("District");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.CountryCode?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("CountryCode");
                        }
                        if (string.IsNullOrEmpty(RequestData.Nominee?.State?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("State");
                        }

                        if (RequestData.Nominee?.Guardian != null)
                        {
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.Name?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian Name");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.RelationshipToMinor?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian RelationshipToMinor");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.GuardianAddress1?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian Address");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.GuardianPin?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian Pin");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.GuardianCityCode?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian CityCode");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.GuardianDistrict?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian District");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.GuardianCountryCode?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian CountryCode");
                            }
                            if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.GuardianState?.ToString()))
                            {
                                haserror = 1;
                                fields.Add("Guardian State");
                            }
                        }


                    }
                }


                if (haserror == 1)
                {
                    accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                    accountLeadReturn.Message = string.Join(",", fields) + " fields should not be null!";
                    return accountLeadReturn;
                }


                string errorMessage = await SetLeadAccountDDE(_leadDetails, RequestData);
                if (string.IsNullOrEmpty(errorMessage))
                {

                    if (RequestData.documents != null && RequestData.documents.Count > 0)
                    {
                        await SetDocumentDDE(RequestData.documents);
                    }

                    if (RequestData.DirectBanking?.Preferences != null && RequestData.DirectBanking?.Preferences.Count > 0)
                    {
                        string preferenceValidation = await SetPreferencesDDE(RequestData.DirectBanking?.Preferences);
                        if (string.IsNullOrEmpty(preferenceValidation))
                        {
                            accountLeadReturn.Preferences = Preferences;
                        }
                        else
                        {
                            accountLeadReturn.Preferences = Preferences;
                            accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                            accountLeadReturn.Message = preferenceValidation;
                            return accountLeadReturn;
                        }
                    }

                    if (!string.IsNullOrEmpty(RequestData.Nominee?.ToString()))
                    {
                        await SetNomineeDDE(RequestData.Nominee);
                    }

                    accountLeadReturn.ReturnCode = "CRM-SUCCESS";
                    accountLeadReturn.Message = OutputMSG.Case_Success;

                }
                else
                {
                    this._logger.LogInformation("CreateDDELeadAccount", errorMessage);
                    accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                    accountLeadReturn.Message = errorMessage;
                }
            }
            else
            {
                this._logger.LogInformation("CreateDDELeadAccount", "No details found for given LeadAccount.");
                accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                accountLeadReturn.Message = "No details found for given LeadAccount.";
            }


            return accountLeadReturn;
        }


        private async Task<string> SetLeadAccountDDE(JArray LeadAccount, dynamic ddeData)
        {
            try
            {
                string dd, mm, yyyy;
                Dictionary<string, string> odatab = new Dictionary<string, string>();
                Dictionary<string, double> odatab1 = new Dictionary<string, double>();
                Dictionary<string, bool> odatab2 = new Dictionary<string, bool>();
                Dictionary<string, bool> odataTriggerField = new Dictionary<string, bool>();

                // odatab.Add("eqs_applicationdate", LeadAccount[0]["eqs_applicationdate"].ToString("yyyy-MM-dd"));
                if (!string.IsNullOrEmpty(LeadAccount[0]["eqs_ddefinalid"].ToString()))
                {
                    this.DDEId = LeadAccount[0]["eqs_ddefinalid"].ToString();

                    //var AccountDDE = await this._commonFunc.getAccountLeadData(DDEId);
                    //if (AccountDDE.Count > 0 && !string.IsNullOrEmpty(AccountDDE[0]["eqs_accountnocreated"].ToString()))
                    //{
                    //    return "Lead cannot be onboarded because account has been already created for this Lead Account.";
                    //}
                }

                var leadDetails = await this._commonFunc.getLeadDetails(LeadAccount[0]["_eqs_lead_value"].ToString());

                if (string.IsNullOrEmpty(this.DDEId))
                {
                    odatab.Add("eqs_accountopeningbranchid@odata.bind", $"eqs_branchs({leadDetails[0]["_eqs_branchid_value"].ToString()})");
                    odatab.Add("eqs_leadaccountid@odata.bind", $"eqs_leadaccounts({LeadAccount[0]["eqs_leadaccountid"].ToString()})");
                    odatab.Add("eqs_ddeoperatorname", LeadAccount[0]["eqs_crmleadaccountid"].ToString() + "  - Final");
                    odatab.Add("eqs_primarylead@odata.bind", $"leads({LeadAccount[0]["_eqs_lead_value"].ToString()})");
                }


                this.LeadAccountid = LeadAccount[0]["eqs_leadaccountid"].ToString();



                /****************** General  ***********************/
                if (ddeData.General != null)
                {
                    if (ddeData.General?.AccountNumber?.ToString() != "")
                    {
                        string Account = await this._commonFunc.getAccountId(ddeData.General?.AccountNumber?.ToString());
                        if (!string.IsNullOrEmpty(Account))
                        {
                            odatab.Add("eqs_AccountNumber@odata.bind", $"eqs_accounts({Account})");
                        }

                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.ApplicationDate?.ToString()))
                    {
                        dd = ddeData.General?.ApplicationDate?.ToString()?.Substring(0, 2);
                        mm = ddeData.General?.ApplicationDate?.ToString()?.Substring(3, 2);
                        yyyy = ddeData.General?.ApplicationDate?.ToString()?.Substring(6, 4);
                        odatab.Add("eqs_applicationdate", yyyy + "-" + mm + "-" + dd);
                    }

                    if (!string.IsNullOrEmpty(ddeData.General?.ProductCategory?.ToString()))
                    {
                        string prodCat = await this._commonFunc.getProductCategoryId(ddeData.General?.ProductCategory.ToString());
                        if (!string.IsNullOrEmpty(prodCat))
                        {
                            odatab.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({prodCat})");
                        }
                        else
                        {
                            odatab.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({LeadAccount[0]["_eqs_typeofaccountid_value"].ToString()})");
                        }
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.Product.ToString()))
                    {
                        string prodid = await this._commonFunc.getProductId(ddeData.General?.Product.ToString());
                        if (!string.IsNullOrEmpty(prodid))
                        {
                            odatab.Add("eqs_productid@odata.bind", $"eqs_products({prodid})");
                        }
                        else
                        {
                            odatab.Add("eqs_productid@odata.bind", $"eqs_products({LeadAccount[0]["_eqs_productid_value"].ToString()})");
                        }

                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.InstaKit?.ToString()))
                    {
                        odatab.Add("eqs_instakitcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_instakitcode", ddeData.General?.InstaKit?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.InstaKitAccountNumber?.ToString()))
                    {
                        odatab.Add("eqs_instakitaccountnumber", ddeData.General?.InstaKitAccountNumber?.ToString());
                    }

                    odatab.Add("eqs_leadnumber", leadDetails[0]["eqs_crmleadid"].ToString());
                    odatab.Add("eqs_sourcebranchterritoryid@odata.bind", $"eqs_branchs({leadDetails[0]["_eqs_branchid_value"].ToString()})");

                    if (!string.IsNullOrEmpty(leadDetails[0]["_eqs_leadsourceid_value"].ToString()))
                    {
                        odatab.Add("eqs_leadsourceid@odata.bind", $"eqs_leadsources({leadDetails[0]["_eqs_leadsourceid_value"].ToString()})");
                    }

                    if (!string.IsNullOrEmpty(ddeData.General?.PurposeofOpeningAccount.ToString()))
                    {
                        odatab.Add("eqs_purposeofopeningaccountcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_purposeofopeningaccountcode", ddeData.General?.PurposeofOpeningAccount.ToString()));
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.PurposeOfOpeningAccountOthers?.ToString()))
                    {
                        odatab.Add("eqs_purposeofopeningaaccountothers", ddeData.General?.PurposeOfOpeningAccountOthers?.ToString());
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.ModeofOperation?.ToString()))
                    {
                        odatab.Add("eqs_modeofoperationcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_modeofoperationcode", ddeData.General?.ModeofOperation?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.AccountOwnership?.ToString()))
                    {
                        odatab.Add("eqs_accountownershipcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_accountownershipcode", ddeData.General?.AccountOwnership?.ToString()));  //has prob in convarting
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.InitialDepositMode?.ToString()))
                    {
                        odatab.Add("eqs_initialdepositmodecode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_initialdepositmodecode", ddeData.General?.InitialDepositMode?.ToString()));  //has prob in convarting
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.TransactionDate?.ToString()))
                    {
                        dd = ddeData.General?.TransactionDate?.ToString()?.Substring(0, 2);
                        mm = ddeData.General?.TransactionDate?.ToString()?.Substring(3, 2);
                        yyyy = ddeData.General?.TransactionDate?.ToString()?.Substring(6, 4);
                        odatab.Add("eqs_transactiondate", yyyy + "-" + mm + "-" + dd);
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.TransactionID?.ToString()))
                    {
                        odatab.Add("eqs_transactionid", ddeData.General?.TransactionID?.ToString());
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.Fundingchequebank?.ToString()))
                    {
                        odatab.Add("eqs_fundingchequebank", ddeData.General?.Fundingchequebank?.ToString());
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.FundingchequeNumber?.ToString()))
                    {
                        odatab.Add("eqs_fundingchequenumber", ddeData.General?.FundingchequeNumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.SweepFacility?.ToString()))
                    {
                        odatab2.Add("eqs_sweepfacility", (ddeData.General?.SweepFacility?.ToString() == "Yes") ? true : false);
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.LGCode?.ToString()))
                    {
                        odatab.Add("eqs_lgcode", ddeData.General?.LGCode?.ToString());
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.LCCode?.ToString()))
                    {
                        odatab.Add("eqs_lccode", ddeData.General?.LCCode?.ToString());
                    }



                    //odatab.Add("eqs_isnomineedisplay", (Convert.ToBoolean(ddeData.IsNomineeDisplay)) ? "789030001" : "789030000");
                    //odatab.Add("eqs_isnomineebankcustomer", (Convert.ToBoolean(ddeData.IsNomineeBankCustomer)) ? "789030001" : "789030000");

                    odatab.Add("eqs_leadchannelcode", "789030011");
                    odatab.Add("eqs_dataentrystage", "615290002");
                }


                /****************** Additional Details  ***********************/
                if (ddeData.AdditionalDetails != null)
                {
                    if (!string.IsNullOrEmpty(ddeData.AdditionalDetails?.AccountTitle?.ToString()))
                    {
                        odatab.Add("eqs_accounttitle", ddeData.AdditionalDetails?.AccountTitle?.ToString());
                    }
                    if (!string.IsNullOrEmpty(ddeData.AdditionalDetails?.LOBType?.ToString()))
                    {
                        odatab.Add("eqs_lobtypecode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_lobtypecode", ddeData.AdditionalDetails?.LOBType?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(ddeData.AdditionalDetails?.AOBO?.ToString()))
                    {
                        odatab.Add("eqs_aobotypecode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_aobotypecode", ddeData.AdditionalDetails?.AOBO?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(ddeData.AdditionalDetails?.ModeofOperationRemarks?.ToString()))
                    {
                        odatab.Add("eqs_modeofoperationremarks", ddeData.AdditionalDetails?.ModeofOperationRemarks?.ToString());
                    }
                    if (!string.IsNullOrEmpty(ddeData.AdditionalDetails?.SourceofFund?.ToString()))
                    {
                        odatab.Add("eqs_sourceoffundcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_sourceoffundcode", ddeData.AdditionalDetails?.SourceofFund?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(ddeData.AdditionalDetails?.OtherSourceoffund?.ToString()))
                    {
                        odatab.Add("eqs_othersourceoffund", ddeData.AdditionalDetails?.OtherSourceoffund?.ToString());
                    }
                    if (!string.IsNullOrEmpty(ddeData.AdditionalDetails?.PredefinedAccountNumber?.ToString()))
                    {
                        odatab.Add("eqs_predefinedaccountnumber", ddeData.AdditionalDetails?.PredefinedAccountNumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(ddeData.AdditionalDetails?.DepositAmount?.ToString()))
                    {
                        odatab.Add("eqs_depositamountslot", ddeData.AdditionalDetails?.DepositAmount?.ToString());
                    }
                }



                //odatab.Add("eqs_accountownershipcode", this.AccountType[ddeData.accountType.ToString()]);                
                //odatab.Add("eqs_accounttitle", ddeData.AccountTitle.ToString());

                /****************** FDRD Details  ***********************/
                if (ddeData.FDRDDetails != null)
                {
                    if (ddeData.FDRDDetails?.ProductDetails != null)
                    {
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.ProductDetails?.MinimumDepositAmount?.ToString()))
                        {
                            odatab1.Add("eqs_minimumdepositamount", Convert.ToDouble(ddeData.FDRDDetails?.ProductDetails?.MinimumDepositAmount?.ToString()));
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.ProductDetails?.MaximumDepositAmount?.ToString()))
                        {
                            odatab1.Add("eqs_maximumdepositamount", Convert.ToDouble(ddeData.FDRDDetails?.ProductDetails?.MaximumDepositAmount?.ToString()));
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.ProductDetails?.CompoundingFrequency?.ToString()))
                        {
                            odatab.Add("eqs_compoundingfrequency", ddeData.FDRDDetails?.ProductDetails?.CompoundingFrequency?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.ProductDetails?.MinimumTenureMonths?.ToString()))
                        {
                            odatab.Add("eqs_minimumtenuremonths", ddeData.FDRDDetails?.ProductDetails?.MinimumTenureMonths?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.ProductDetails?.MaximumTenureMonths?.ToString()))
                        {
                            odatab.Add("eqs_maximumtenuremonths", ddeData.FDRDDetails?.ProductDetails?.MaximumTenureMonths?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.ProductDetails?.PayoutFrequency?.ToString()))
                        {
                            odatab.Add("eqs_payoutfrequency", ddeData.FDRDDetails?.ProductDetails?.PayoutFrequency?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.ProductDetails?.MinimumTenureDays?.ToString()))
                        {
                            odatab.Add("eqs_minimumtenuredays", ddeData.FDRDDetails?.ProductDetails?.MinimumTenureDays?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.ProductDetails?.MaximumTenureDays?.ToString()))
                        {
                            odatab.Add("eqs_maximumtenuredays", ddeData.FDRDDetails?.ProductDetails?.MaximumTenureDays?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.ProductDetails?.InterestCompoundFrequency?.ToString()))
                        {
                            odatab.Add("eqs_interestcompoundfrequency", ddeData.FDRDDetails?.ProductDetails?.InterestCompoundFrequency?.ToString());
                        }



                    }

                    if (ddeData.FDRDDetails?.DepositDetails != null)
                    {
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.DepositDetails?.DepositVariancePercentage?.ToString()))
                        {
                            odatab1.Add("eqs_depositvariance", Convert.ToDouble(ddeData.FDRDDetails?.DepositDetails?.DepositVariancePercentage?.ToString()));
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.DepositDetails?.DepositAmount?.ToString()))
                        {
                            odatab["eqs_depositamountslot"] = ddeData.FDRDDetails?.DepositDetails?.DepositAmount?.ToString();
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.DepositDetails?.FromESFBAccountNumber?.ToString()))
                        {
                            odatab.Add("eqs_fromesfbaccountnumber", ddeData.FDRDDetails?.DepositDetails?.FromESFBAccountNumber?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.DepositDetails?.FromESFBGLAccount?.ToString()))
                        {
                            odatab.Add("eqs_fromesfbglaccount", ddeData.FDRDDetails?.DepositDetails?.FromESFBGLAccount?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.DepositDetails?.CurrencyofDeposit?.ToString()))
                        {
                            odatab.Add("eqs_currencyofdepositcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_currencyofdepositcode", ddeData.FDRDDetails?.DepositDetails?.CurrencyofDeposit?.ToString()));
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.DepositDetails?.tenureInDays?.ToString()))
                        {
                            odatab.Add("eqs_tenureindays", ddeData.FDRDDetails?.DepositDetails?.tenureInDays?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.DepositDetails?.SpecialInterestRateRequired?.ToString()))
                        {
                            odatab2.Add("eqs_specialinterestraterequired", (ddeData.FDRDDetails?.DepositDetails?.SpecialInterestRateRequired?.ToString() == "Yes") ? true : false);
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.DepositDetails?.SpecialInterestRate?.ToString()))
                        {
                            odatab.Add("eqs_specialinterestrate", ddeData.FDRDDetails?.DepositDetails?.SpecialInterestRate?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.DepositDetails?.SpecialInterestRequestID?.ToString()))
                        {
                            odatab.Add("eqs_specialinterestrequestid", ddeData.FDRDDetails?.DepositDetails?.SpecialInterestRequestID?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.DepositDetails?.BranchCodeGL?.ToString()))
                        {
                            odatab.Add("eqs_branchcodegl", ddeData.FDRDDetails?.DepositDetails?.BranchCodeGL?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.DepositDetails?.FDValueDate?.ToString()))
                        {
                            dd = ddeData.FDRDDetails?.DepositDetails?.FDValueDate?.ToString()?.Substring(0, 2);
                            mm = ddeData.FDRDDetails?.DepositDetails?.FDValueDate?.ToString()?.Substring(3, 2);
                            yyyy = ddeData.FDRDDetails?.DepositDetails?.FDValueDate?.ToString()?.Substring(6, 4);
                            odatab.Add("eqs_fdvaluedate", yyyy + "-" + mm + "-" + dd);
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.DepositDetails?.TenureInMonths?.ToString()))
                        {
                            odatab.Add("eqs_tenureinmonths", ddeData.FDRDDetails?.DepositDetails?.TenureInMonths?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.DepositDetails?.WaivedOffTDS?.ToString()))
                        {
                            odatab2.Add("eqs_waivedofftds", (ddeData.FDRDDetails?.DepositDetails?.WaivedOffTDS?.ToString() == "Yes") ? true : false);
                        }


                        //odatab.Add("eqs_transactionid", ddeData.FDRDDetails?.DepositDetails?.TransactionID.ToString());
                    }

                    if (ddeData.FDRDDetails?.InterestPayoutDetails != null)
                    {
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.InterestPayoutDetails?.interestPayoutMode?.ToString()))
                        {
                            odatab.Add("eqs_interestpayoutmode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_interestpayoutmode", ddeData.FDRDDetails?.InterestPayoutDetails?.interestPayoutMode?.ToString()));
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToESFBAccountNo?.ToString()))
                        {
                            odatab.Add("eqs_esfbaccountnumber_interest", ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToESFBAccountNo?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToOtherBankAccountNo?.ToString()))
                        {
                            odatab.Add("eqs_otherbankaccountnumber_interest", ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToOtherBankAccountNo?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.InterestPayoutDetails?.BeneficiaryAccountType?.ToString()))
                        {
                            odatab.Add("eqs_beneficiaryaccounttypeinterest", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_beneficiaryaccounttypeinterest", ddeData.FDRDDetails?.InterestPayoutDetails?.BeneficiaryAccountType?.ToString()));
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToOtherBankBenificiaryName?.ToString()))
                        {
                            odatab.Add("eqs_beneficiarynameinterest", ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToOtherBankBenificiaryName?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToOtherBankIFSC?.ToString()))
                        {
                            odatab.Add("eqs_ifsccodeinterest", ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToOtherBankIFSC?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToOtherBankName?.ToString()))
                        {
                            odatab.Add("eqs_banknameinterest", ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToOtherBankName?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToOtherBankBranch?.ToString()))
                        {
                            odatab.Add("eqs_branchinterest", ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToOtherBankBranch?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToOtherBankMICR?.ToString()))
                        {
                            odatab.Add("eqs_micrinterest", ddeData.FDRDDetails?.InterestPayoutDetails?.iPayToOtherBankMICR?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.InterestPayoutDetails?.iPByDDPOIssuerCode?.ToString()))
                        {
                            odatab.Add("eqs_issuercode_interest", ddeData.FDRDDetails?.InterestPayoutDetails?.iPByDDPOIssuerCode?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.InterestPayoutDetails?.iPByDDPOPayeeName?.ToString()))
                        {
                            odatab.Add("eqs_payeename_interest", ddeData.FDRDDetails?.InterestPayoutDetails?.iPByDDPOPayeeName?.ToString());
                        }


                    }

                    if (ddeData.FDRDDetails?.MaturityInstructionDetails != null)
                    {
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.MaturityInstruction?.ToString()))
                        {
                            odatab.Add("eqs_maturityinstruction", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_maturityinstruction", ddeData.FDRDDetails?.MaturityInstructionDetails?.MaturityInstruction?.ToString()));
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.MaturityPayoutMode?.ToString()))
                        {
                            odatab.Add("eqs_maturitypayoutmode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_maturitypayoutmode", ddeData.FDRDDetails?.MaturityInstructionDetails?.MaturityPayoutMode?.ToString()));
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToESFBAccountNo?.ToString()))
                        {
                            odatab.Add("eqs_esfbaccountnumber_maturity", ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToESFBAccountNo?.ToString());
                        }
                        //if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToESFBAccountNo?.ToString()))
                        //{
                        //    odatab.Add("eqs_esfbaccountnumber_maturity", ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToESFBAccountNo?.ToString());
                        //}
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToOtherBankAccountNo?.ToString()))
                        {
                            odatab.Add("eqs_otherbankaccountnumber_maturity", ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToOtherBankAccountNo?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToOtherBankAccountType?.ToString()))
                        {
                            odatab.Add("eqs_beneficiaryaccounttype_maturity", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_beneficiaryaccounttype_maturity", ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToOtherBankAccountType?.ToString()));
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.BeneficiaryName?.ToString()))
                        {
                            odatab.Add("eqs_beneficiaryname_maturity", ddeData.FDRDDetails?.MaturityInstructionDetails?.BeneficiaryName?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToOtherBankIFSC?.ToString()))
                        {
                            odatab.Add("eqs_ifccodematurity", ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToOtherBankIFSC?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToOtherBankName?.ToString()))
                        {
                            odatab.Add("eqs_banknamematurity", ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToOtherBankName?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToOtherBankBranch?.ToString()))
                        {
                            odatab.Add("eqs_branchmaturity", ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToOtherBankBranch?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToOtherBankMICR?.ToString()))
                        {
                            odatab.Add("eqs_micrmaturity", ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToOtherBankMICR?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.MIByDDPOIssuerCode?.ToString()))
                        {
                            odatab.Add("eqs_issuercode_maturity", ddeData.FDRDDetails?.MaturityInstructionDetails?.MIByDDPOIssuerCode?.ToString());
                        }
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.MIByDDPOPayeeName?.ToString()))
                        {
                            odatab.Add("eqs_payeename_maturity", ddeData.FDRDDetails?.MaturityInstructionDetails?.MIByDDPOPayeeName?.ToString());
                        }
                    }


                }


                /****************** Direct Banking  ***********************/

                if (ddeData.DirectBanking != null)
                {
                    if (!string.IsNullOrEmpty(ddeData.DirectBanking?.IssuedInstaKit?.ToString()))
                    {
                        odatab2.Add("eqs_issuedinstakit", (ddeData.DirectBanking?.IssuedInstaKit?.ToString() == "No") ? false : true);
                    }
                    if (!string.IsNullOrEmpty(ddeData.DirectBanking?.ChequeBookRequired?.ToString()))
                    {
                        odatab2.Add("eqs_chequebookrequired", (ddeData.DirectBanking?.ChequeBookRequired?.ToString() == "No") ? false : true);
                    }
                    if (!string.IsNullOrEmpty(ddeData.DirectBanking?.NumberChequeBook?.ToString()))
                    {
                        odatab.Add("eqs_numberofchequebook", ddeData.DirectBanking?.NumberChequeBook?.ToString());
                    }
                    if (!string.IsNullOrEmpty(ddeData.DirectBanking?.NumberofChequeLeaves?.ToString()))
                    {
                        odatab.Add("eqs_numberofchequeleavescode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_numberofchequeleavescode", ddeData.DirectBanking?.NumberofChequeLeaves?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(ddeData.DirectBanking?.DispatchMode?.ToString()))
                    {
                        odatab.Add("eqs_dispatchmodecode", await this._queryParser.getOptionSetTextToValue("eqs_ddeaccount", "eqs_dispatchmodecode", ddeData.DirectBanking?.DispatchMode?.ToString()));
                    }


                }

                odatab.Add("eqs_createdfrompartnerchannel", "true");


                string postDataParametr = JsonConvert.SerializeObject(odatab);
                string postDataParametr1 = string.Empty;
                if (odatab1.Count > 0)
                {
                    postDataParametr1 = JsonConvert.SerializeObject(odatab1);
                    postDataParametr = await _commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);
                }

                if (odatab2.Count > 0)
                {
                    postDataParametr1 = JsonConvert.SerializeObject(odatab2);
                    postDataParametr = await _commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);
                }

                if (string.IsNullOrEmpty(this.DDEId))
                {
                    var LeadAccount_details = await this._queryParser.HttpApiCall("eqs_ddeaccounts()?$select=eqs_ddeaccountid", HttpMethod.Post, postDataParametr);
                    odatab = new Dictionary<string, string>();
                    var ddeid = CommonFunction.GetIdFromPostRespons201(LeadAccount_details[0]["responsebody"], "eqs_ddeaccountid");
                    this.DDEId = ddeid;
                    if (this.DDEId == null)
                    {
                        this._logger.LogError("SetLeadAccountDDE", JsonConvert.SerializeObject(LeadAccount_details), postDataParametr);
                        return "Error occured  while creating AccountDDE";
                    }
                    odatab.Add("eqs_ddefinalid", ddeid);
                    postDataParametr = JsonConvert.SerializeObject(odatab);
                    await this._queryParser.HttpApiCall($"eqs_leadaccounts({this.LeadAccountid})", HttpMethod.Patch, postDataParametr);
                }
                else
                {
                    var LeadAccount_details = await this._queryParser.HttpApiCall($"eqs_ddeaccounts({this.DDEId})?$select=eqs_ddeaccountid", HttpMethod.Patch, postDataParametr);
                }

                if (!string.IsNullOrEmpty(this.DDEId))
                {
                    odataTriggerField.Add("eqs_triggervalidation", true);
                    postDataParametr = JsonConvert.SerializeObject(odataTriggerField);
                    var response = await this._queryParser.HttpApiCall($"eqs_ddeaccounts({this.DDEId})?", HttpMethod.Patch, postDataParametr);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                return "Error occured  while creating AccountDDE";
            }

        }

        private async Task<bool> SetDocumentDDE(dynamic ddeDocuments)
        {
            try
            {
                var res = await this._queryParser.DeleteFromTable("eqs_leaddocuments", "", "_eqs_leadaccountdde_value", this.DDEId, "eqs_leaddocumentid");
                foreach (var item in ddeDocuments)
                {
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
                    if (resp[0]["responsecode"].ToString() == "400")
                    {
                        this._logger.LogError("SetDocumentDDE", JsonConvert.SerializeObject(resp), postDataParametr);
                        return false;
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<string> SetPreferencesDDE(dynamic preferenceData)
        {
            Preferences = new List<Preference>();
            StringBuilder error = new StringBuilder();
            foreach (var item in preferenceData)
            {
                string preferenceGUID = string.Empty, ucic = string.Empty, preferenceID = string.Empty;
                bool process = true;
                Dictionary<string, string> inputItem = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(item["PreferenceID"].ToString()))
                {
                    preferenceID = item["PreferenceID"].ToString();
                    preferenceGUID = await this._commonFunc.getPreferenceID(preferenceID, this.DDEId);
                    if (string.IsNullOrEmpty(preferenceGUID))
                    {
                        error.Append("Invalid PreferenceID '" + preferenceID + "', ");
                        process = false;
                    }
                }
                inputItem.Add("eqs_leadaccountdde@odata.bind", $"eqs_ddeaccounts({this.DDEId})");

                if (!string.IsNullOrEmpty(item["UCIC"].ToString()))
                {
                    ucic = item["UCIC"].ToString();
                    string Applicent_ID = await this._commonFunc.getApplicentID(ucic);
                    if (!string.IsNullOrEmpty(Applicent_ID))
                        inputItem.Add("eqs_applicantid@odata.bind", $"eqs_accountapplicants({Applicent_ID})");
                    else
                    {
                        error.Append("Invalid UCIC '" + ucic + "', ");
                        process = false;
                    }
                }

                if (!process)
                    continue;

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
                    if (item["NameonCard"].ToString().Length > 19)
                    {
                        inputItem.Add("eqs_nameoncard", item["NameonCard"].ToString().Substring(0, 19));
                    }
                    else
                    {
                        inputItem.Add("eqs_nameoncard", item["NameonCard"].ToString());
                    }
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
                if (!string.IsNullOrEmpty(item["physicalStatement"].ToString()))
                {
                    inputItem.Add("eqs_physicalstatement", item["physicalStatement"].ToString());
                }
                if (!string.IsNullOrEmpty(item["mobileBankingNumber"].ToString()))
                {
                    inputItem.Add("eqs_mobilebankingnumber", item["mobileBankingNumber"].ToString());
                }
                if (!string.IsNullOrEmpty(item["InternationalDCLimitAct"].ToString()))
                {
                    inputItem.Add("eqs_internationaldclimitact", item["InternationalDCLimitAct"].ToString());
                }

                string postDataParametr = JsonConvert.SerializeObject(inputItem);

                if (string.IsNullOrEmpty(preferenceID))
                {
                    var resp = await this._queryParser.HttpApiCall("eqs_customerpreferences()?$select=eqs_preferenceid", HttpMethod.Post, postDataParametr);
                    if (resp[0]["responsecode"].ToString() == "400")
                    {
                        this._logger.LogError("SetPreferencesDDE", JsonConvert.SerializeObject(resp), postDataParametr);
                    }
                    preferenceID = CommonFunction.GetIdFromPostRespons201(resp[0]["responsebody"], "eqs_preferenceid");
                }
                else
                {
                    await this._queryParser.HttpApiCall($"eqs_customerpreferences({preferenceGUID})", HttpMethod.Patch, postDataParametr);
                }

                Preferences.Add(new Preference() { PreferenceID = preferenceID, UCIC = ucic });
            }

            if (!string.IsNullOrEmpty(error.ToString()))
            {
                return error.ToString().Trim().Trim(',');
            }

            return string.Empty;
        }


        private async Task<bool> SetNomineeDDE(dynamic ddeNominee)
        {
            try
            {
                string nomineeID = await this._commonFunc.getNomineeID(this.DDEId);

                Dictionary<string, string> odatab = new Dictionary<string, string>();
                string dd, mm, yyyy;
                odatab.Add("eqs_nomineename", ddeNominee?.name?.ToString());
                //odatab.Add("eqs_nomineedob", ddeNominee?.DOB?.ToString());
                string nominRel = await this._commonFunc.getRelationShipId(ddeNominee?.NomineeRelationship?.ToString());
                if (!string.IsNullOrEmpty(nominRel))
                {
                    odatab.Add("eqs_nomineerelationshipwithaccountholder@odata.bind", $"eqs_relationships({nominRel})");
                }
                if (!string.IsNullOrEmpty(ddeNominee?.DOB?.ToString()))
                {
                    dd = ddeNominee?.DOB?.ToString()?.Substring(0, 2);
                    mm = ddeNominee?.DOB?.ToString()?.Substring(3, 2);
                    yyyy = ddeNominee?.DOB?.ToString()?.Substring(6, 4);
                   
                    odatab.Add("eqs_nomineedob", yyyy + "-" + mm + "-" + dd);
                }

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

                string cityC = await this._commonFunc.getCityId(ddeNominee?.CityCode?.ToString());
                if (!string.IsNullOrEmpty(cityC))
                {
                    odatab.Add("eqs_city@odata.bind", $"eqs_cities({cityC})");
                }
                string stateC = await this._commonFunc.getStateId(ddeNominee?.State?.ToString());
                if (!string.IsNullOrEmpty(stateC))
                {
                    odatab.Add("eqs_state@odata.bind", $"eqs_states({stateC})");
                }
                string CountryC = await this._commonFunc.getCuntryId(ddeNominee?.CountryCode?.ToString());
                if (!string.IsNullOrEmpty(CountryC))
                {
                    odatab.Add("eqs_country@odata.bind", $"eqs_countries({CountryC})");
                }


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

                    string Grelation = await this._commonFunc.getRelationShipId(ddeNominee?.Guardian?.RelationshipToMinor?.ToString());
                    if (!string.IsNullOrEmpty(Grelation))
                    {
                        odatab.Add("eqs_guardianrelationshiptominor@odata.bind", $"eqs_relationships({Grelation})");
                    }
                    string GcityC = await this._commonFunc.getCityId(ddeNominee?.Guardian?.GuardianCityCode?.ToString());
                    if (!string.IsNullOrEmpty(GcityC))
                    {
                        odatab.Add("eqs_guardiancity@odata.bind", $"eqs_cities({GcityC})");
                    }
                    string GCuntryC = await this._commonFunc.getCuntryId(ddeNominee?.Guardian?.GuardianCountryCode?.ToString());
                    if (!string.IsNullOrEmpty(GCuntryC))
                    {
                        odatab.Add("eqs_guardiancountry@odata.bind", $"eqs_countries({GCuntryC})");
                    }
                    string GStateC = await this._commonFunc.getStateId(ddeNominee?.Guardian?.GuardianState?.ToString());
                    if (!string.IsNullOrEmpty(GStateC))
                    {
                        odatab.Add("eqs_guardianstate@odata.bind", $"eqs_states({GStateC})");
                    }



                }



                string postDataParametr = JsonConvert.SerializeObject(odatab);
                if (string.IsNullOrEmpty(nomineeID))
                {
                    var result = await this._queryParser.HttpApiCall("eqs_ddeaccountnominees()?$select=eqs_ddeaccountnomineeid", HttpMethod.Post, postDataParametr);
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
