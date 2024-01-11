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

    public class FtdgAccLeadExecution : IFtdgAccLeadExecution
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

        private string Leadid, LeadAccountid;
        FtAccountLead_Return AccountLeadReturn;



        private ICommonFunction _commonFunc;

        public FtdgAccLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {

            this._logger = logger;

            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

            this.AccountLeadReturn = new FtAccountLead_Return();

        }


        public async Task<FtAccountLead_Return> ValidateLeadtInput(dynamic RequestData)
        {
            FtAccountLead_Return ldRtPrm = new FtAccountLead_Return();
            RequestData = await this.getRequestData(RequestData, "FetchDigiAccountLead");
            try
            {

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "FetchDigiAccountLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {

                        if (!string.IsNullOrEmpty(RequestData.AccountLeadId.ToString()))
                        {
                            ldRtPrm = await this.FetLeadAccount(RequestData.AccountLeadId.ToString());
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateLeadtInput", "Account LeadId is incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "Account LeadId is incorrect";
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


        private async Task<FtAccountLead_Return> FetLeadAccount(string LeadAccountId)
        {


            var _leadDetails = await this._commonFunc.getLeadAccountDetails(LeadAccountId);
            if (_leadDetails.Count > 0)
            {
                this.LeadAccountid = LeadAccountId;
                if (await getLeadAccount(_leadDetails))
                {
                    this.AccountLeadReturn.ReturnCode = "CRM-SUCCESS";
                    this.AccountLeadReturn.Message = OutputMSG.Case_Success;
                }
            }
            else
            {
                _leadDetails = await this._commonFunc.getLeadAccountDetails(LeadAccountId, "D0");

                if (_leadDetails.Count > 0)
                {
                    this.AccountLeadReturn = await getLeadAccountD0(_leadDetails);
                    this.AccountLeadReturn.ReturnCode = "CRM-SUCCESS";
                    this.AccountLeadReturn.Message = OutputMSG.Case_Success;
                }
                else
                {
                    this._logger.LogInformation("FetLeadAccount", "Lead Account Details not found.");
                    this.AccountLeadReturn.ReturnCode = "CRM-ERROR-102";
                    this.AccountLeadReturn.Message = "Lead Account Details not found.";
                }
            }

            return this.AccountLeadReturn;
        }




        private async Task<bool> getLeadAccount(JArray LeadData)
        {
            FtAccountLeadReturn ftAccountLeadReturn = new FtAccountLeadReturn();
            /****************** General  ***********************/

            General general = new General();

            general.AccountLeadId = this.LeadAccountid;
            general.AccountNumber = LeadData[0]["eqs_predefinedaccountnumber"]?.ToString();
            general.ApplicationDate = LeadData[0]["eqs_applicationdate"]?.ToString();
            general.ProductCategory = await this._commonFunc.getProductCategoryCode(LeadData[0]["_eqs_productcategoryid_value"]?.ToString());
            general.Product = await this._commonFunc.getProductCode(LeadData[0]["_eqs_productid_value"]?.ToString());
            general.InstaKit = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_instakitcode", LeadData[0]["eqs_instakitcode"].ToString());
            general.InstaKitAccountNumber = LeadData[0]["eqs_instakitaccountnumber"]?.ToString();
            general.AccountOpeningBranch = await this._commonFunc.getBranchCode(LeadData[0]["_eqs_accountopeningbranchid_value"].ToString());
            general.PurposeofOpeningAccount = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_purposeofopeningaccountcode", LeadData[0]["eqs_purposeofopeningaccountcode"].ToString());
            general.PurposeOfOpeningAccountOthers = LeadData[0]["eqs_purposeofopeningaaccountothers"]?.ToString();
            general.ModeofOperation = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_modeofoperationcode", LeadData[0]["eqs_modeofoperationcode"].ToString());
            general.AccountOwnership = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_accountownershipcode", LeadData[0]["eqs_accountownershipcode"].ToString());
            general.InitialDepositMode = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_initialdepositmodecode", LeadData[0]["eqs_initialdepositmodecode"].ToString());
            general.TransactionDate = LeadData[0]["eqs_transactiondate"]?.ToString();
            general.TransactionID = LeadData[0]["eqs_transactionid"]?.ToString();
            general.Fundingchequebank = LeadData[0]["eqs_fundingchequebank"]?.ToString();
            general.FundingchequeNumber = LeadData[0]["eqs_fundingchequenumber"]?.ToString();
            general.SourceBranchTerritory = await this._commonFunc.getBranchCode(LeadData[0]["_eqs_sourcebranchterritoryid_value"].ToString());
            general.SweepFacility = LeadData[0]["eqs_sweepfacility"]?.ToString();
            general.LGCode = LeadData[0]["eqs_lgcode"]?.ToString();
            general.LCCode = LeadData[0]["eqs_lccode"]?.ToString();

            ftAccountLeadReturn.General = general;

            /****************** Additional Details  ***********************/

            AdditionalDetails additionalDetails = new AdditionalDetails();

            additionalDetails.AccountTitle = LeadData[0]["eqs_accounttitle"]?.ToString();
            additionalDetails.LOBType = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_lobtypecode", LeadData[0]["eqs_lobtypecode"].ToString());
            additionalDetails.AOBO = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_aobotypecode", LeadData[0]["eqs_aobotypecode"].ToString());
            additionalDetails.ModeofOperationRemarks = LeadData[0]["eqs_modeofoperationremarks"]?.ToString();
            additionalDetails.SourceofFund = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_sourceoffundcode", LeadData[0]["eqs_sourceoffundcode"].ToString());
            additionalDetails.OtherSourceoffund = LeadData[0]["eqs_othersourceoffund"]?.ToString();
            additionalDetails.PredefinedAccountNumber = LeadData[0]["eqs_predefinedaccountnumber"]?.ToString();

            ftAccountLeadReturn.AdditionalDetails = additionalDetails;

            /****************** FDRD Details  ***********************/

            ProductDetails productDetails = new ProductDetails();
            productDetails.MinimumDepositAmount = LeadData[0]["eqs_minimumdepositamount"].ToString();
            productDetails.MaximumDepositAmount = LeadData[0]["eqs_maximumdepositamount"].ToString();
            productDetails.CompoundingFrequency = LeadData[0]["eqs_compoundingfrequency"].ToString();
            productDetails.MinimumTenureMonths = LeadData[0]["eqs_minimumtenuremonths"].ToString();
            productDetails.MaximumTenureMonths = LeadData[0]["eqs_maximumtenuremonths"].ToString();
            productDetails.PayoutFrequency = LeadData[0]["eqs_payoutfrequency"].ToString();
            productDetails.MinimumTenureDays = LeadData[0]["eqs_minimumtenuredays"].ToString();
            productDetails.MaximumTenureDays = LeadData[0]["eqs_maximumtenuredays"].ToString();
            productDetails.InterestCompoundFrequency = LeadData[0]["eqs_interestcompoundfrequency"].ToString();

            DepositDetails depositDetails = new DepositDetails();
            depositDetails.DepositVariancePercentage = LeadData[0]["eqs_depositvariance"]?.ToString();
            depositDetails.DepositAmount = LeadData[0]["eqs_depositamountslot"]?.ToString();
            depositDetails.FromESFBAccountNumber = LeadData[0]["eqs_fromesfbaccountnumber"]?.ToString();
            depositDetails.FromESFBGLAccount = LeadData[0]["eqs_fromesfbglaccount"]?.ToString();
            depositDetails.CurrencyofDeposit = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_currencyofdepositcode", LeadData[0]["eqs_currencyofdepositcode"].ToString());
            depositDetails.tenureInDays = LeadData[0]["eqs_tenureindays"]?.ToString();
            depositDetails.SpecialInterestRateRequired = LeadData[0]["eqs_specialinterestraterequired"]?.ToString();
            depositDetails.SpecialInterestRate = LeadData[0]["eqs_specialinterestrate"]?.ToString();
            depositDetails.SpecialInterestRequestID = LeadData[0]["eqs_specialinterestrequestid"]?.ToString();
            depositDetails.BranchCodeGL = LeadData[0]["eqs_branchcodegl"]?.ToString();
            depositDetails.FDValueDate = LeadData[0]["eqs_fdvaluedate"]?.ToString();
            depositDetails.TenureInMonths = LeadData[0]["eqs_tenureinmonths"]?.ToString();
            depositDetails.WaivedOffTDS = LeadData[0]["eqs_waivedofftds"]?.ToString();


            InterestPayoutDetails interestPayoutDetails = new InterestPayoutDetails();
            interestPayoutDetails.interestPayoutMode = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_interestpayoutmode", LeadData[0]["eqs_interestpayoutmode"].ToString());
            interestPayoutDetails.iPayToESFBAccountNo = LeadData[0]["eqs_esfbaccountnumber_interest"]?.ToString();
            interestPayoutDetails.iPayToOtherBankAccountNo = LeadData[0]["eqs_otherbankaccountnumber_interest"]?.ToString();
            interestPayoutDetails.BeneficiaryAccountType = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_beneficiaryaccounttypeinterest", LeadData[0]["eqs_beneficiaryaccounttypeinterest"].ToString());
            interestPayoutDetails.iPayToOtherBankBenificiaryName = LeadData[0]["eqs_beneficiarynameinterest"]?.ToString();
            interestPayoutDetails.iPayToOtherBankIFSC = LeadData[0]["eqs_ifsccodeinterest"]?.ToString();
            interestPayoutDetails.iPayToOtherBankName = LeadData[0]["eqs_banknameinterest"]?.ToString();
            interestPayoutDetails.iPayToOtherBankBranch = LeadData[0]["eqs_branchinterest"]?.ToString();
            interestPayoutDetails.iPayToOtherBankMICR = LeadData[0]["eqs_micrinterest"]?.ToString();
            interestPayoutDetails.iPByDDPOIssuerCode = LeadData[0]["eqs_issuercode_interest"]?.ToString();
            interestPayoutDetails.iPByDDPOPayeeName = LeadData[0]["eqs_payeename_interest"]?.ToString();

            MaturityInstructionDetails maturityInstructionDetails = new MaturityInstructionDetails();
            maturityInstructionDetails.MaturityInstruction = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_maturityinstruction", LeadData[0]["eqs_maturityinstruction"].ToString());
            maturityInstructionDetails.MaturityPayoutMode = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_maturitypayoutmode", LeadData[0]["eqs_maturitypayoutmode"].ToString());
            maturityInstructionDetails.MICreditToESFBAccountNo = LeadData[0]["eqs_esfbaccountnumber_maturity"]?.ToString();
            maturityInstructionDetails.MICreditToOtherBankAccountNo = LeadData[0]["eqs_otherbankaccountnumber_maturity"]?.ToString();
            maturityInstructionDetails.MICreditToOtherBankAccountType = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_beneficiaryaccounttype_maturity", LeadData[0]["eqs_beneficiaryaccounttype_maturity"].ToString());
            maturityInstructionDetails.BeneficiaryName = LeadData[0]["eqs_beneficiaryname_maturity"]?.ToString();
            maturityInstructionDetails.MICreditToOtherBankIFSC = LeadData[0]["eqs_ifccodematurity"]?.ToString();
            maturityInstructionDetails.MICreditToOtherBankName = LeadData[0]["eqs_banknamematurity"]?.ToString();
            maturityInstructionDetails.MICreditToOtherBankBranch = LeadData[0]["eqs_branchmaturity"]?.ToString();
            maturityInstructionDetails.MICreditToOtherBankMICR = LeadData[0]["eqs_micrmaturity"]?.ToString();
            maturityInstructionDetails.MIByDDPOIssuerCode = LeadData[0]["eqs_issuercode_maturity"]?.ToString();
            maturityInstructionDetails.MIByDDPOPayeeName = LeadData[0]["eqs_payeename_maturity"]?.ToString();



            FDRDDetails fDRDDetails = new FDRDDetails();
            fDRDDetails.ProductDetails = productDetails;
            fDRDDetails.DepositDetails = depositDetails;
            fDRDDetails.InterestPayoutDetails = interestPayoutDetails;
            fDRDDetails.MaturityInstructionDetails = maturityInstructionDetails;
            ftAccountLeadReturn.FDRDDetails = fDRDDetails;


            /****************** Direct Banking  ***********************/

            DirectBanking directBanking = new DirectBanking();

            directBanking.IssuedInstaKit = LeadData[0]["eqs_issuedinstakit"]?.ToString();
            directBanking.ChequeBookRequired = LeadData[0]["eqs_chequebookrequired"]?.ToString();
            directBanking.NumberChequeBook = LeadData[0]["eqs_numberofchequebook"]?.ToString();
            directBanking.NumberofChequeLeaves = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_numberofchequeleavescode", LeadData[0]["eqs_numberofchequeleavescode"].ToString());
            directBanking.DispatchMode = await this._queryParser.getOptionSetValuToText("eqs_ddeaccount", "eqs_dispatchmodecode", LeadData[0]["eqs_dispatchmodecode"].ToString());
            directBanking.Preferences = new List<Preference>();

            var applicentPreferences = await this._commonFunc.getPreferences(LeadData[0]["eqs_ddeaccountid"].ToString());
            foreach (var prefItem in applicentPreferences)
            {
                Preference preference = new Preference();

                preference.PreferenceID = prefItem["eqs_preferenceid"]?.ToString();
                preference.UCIC = await this._commonFunc.getUCIC(prefItem["_eqs_applicantid_value"].ToString());
                preference.DebitCardID = await this._commonFunc.getDebitCard(prefItem["_eqs_debitcard_value"].ToString());

                preference.DebitCardFlag = prefItem["eqs_debitcardflag"]?.ToString();
                preference.NameonCard = prefItem["eqs_nameoncard"]?.ToString();

                preference.SMS = prefItem["eqs_sms"].ToString();
                preference.NetBanking = prefItem["eqs_netbanking"]?.ToString();
                preference.MobileBanking = prefItem["eqs_mobilebanking"]?.ToString();
                preference.EmailStatement = prefItem["eqs_emailstatement"]?.ToString();
                preference.InternationalDCLimitAct = prefItem["eqs_internationaldclimitact"]?.ToString();


                directBanking.Preferences.Add(preference);
            }


            ftAccountLeadReturn.DirectBanking = directBanking;


            /****************** Nominee  ***********************/

            var nomineeobj = await this._commonFunc.getNomineDetails(LeadData[0]["eqs_ddeaccountid"].ToString());

            if (nomineeobj.Count > 0)
            {
                Nominee nominee = new Nominee();
                nominee.nomineeUCICIfCustomer = nomineeobj[0]["eqs_nomineeucic"]?.ToString();
                nominee.name = nomineeobj[0]["eqs_nomineename"]?.ToString();

                nominee.DOB = nomineeobj[0]["eqs_nomineedob"]?.ToString();
                nominee.NomineeDisplayName = nomineeobj[0]["eqs_nomineedisplayname"]?.ToString();
                nominee.AddresssameasProspects = nomineeobj[0]["eqs_guardianaddresssameasprospectaddress"]?.ToString();
                nominee.email = nomineeobj[0]["eqs_emailid"]?.ToString();
                nominee.mobile = nomineeobj[0]["eqs_mobile"]?.ToString();
                nominee.Landline = nomineeobj[0]["eqs_landlinenumber"]?.ToString();
                nominee.Address1 = nomineeobj[0]["eqs_addressline1"]?.ToString();
                nominee.Address2 = nomineeobj[0]["eqs_addressline2"]?.ToString();
                nominee.Address3 = nomineeobj[0]["eqs_addressline3"]?.ToString();
                nominee.Pin = nomineeobj[0]["eqs_pincode"]?.ToString();
                nominee.District = nomineeobj[0]["eqs_district"]?.ToString();
                nominee.PO = nomineeobj[0]["eqs_pobox"]?.ToString();
                nominee.Landmark = nomineeobj[0]["eqs_landmark"]?.ToString();

                nominee.NomineeRelationship = await this._commonFunc.getRelationshipCode(nomineeobj[0]["_eqs_nomineerelationshipwithaccountholder_value"].ToString());
                nominee.CityCode = await this._commonFunc.getCityCode(nomineeobj[0]["_eqs_city_value"].ToString());
                nominee.CountryCode = await this._commonFunc.getCuntryCode(nomineeobj[0]["_eqs_country_value"].ToString());
                nominee.State = await this._commonFunc.getStateCode(nomineeobj[0]["_eqs_state_value"].ToString());

                Guardian guardian = new Guardian();
                guardian.Name = nomineeobj[0]["eqs_guardianname"]?.ToString();
                guardian.RelationshipToMinor = nomineeobj[0]["_eqs_guardianrelationshiptominor_value"]?.ToString();
                guardian.GuardianUCIC = nomineeobj[0]["eqs_guardianucic"]?.ToString();
                guardian.GuardianMobile = nomineeobj[0]["eqs_guardianmobile"]?.ToString();
                guardian.GuardianLandline = nomineeobj[0]["eqs_guardianlandlinenumber"]?.ToString();
                guardian.GuardianAddress1 = nomineeobj[0]["eqs_guardianaddressline1"]?.ToString();
                guardian.GuardianAddress2 = nomineeobj[0]["eqs_guardianaddressline2"]?.ToString();
                guardian.GuardianAddress3 = nomineeobj[0]["eqs_guardianaddressline3"]?.ToString();
                guardian.GuardianPin = nomineeobj[0]["eqs_guardianpincode"]?.ToString();
                guardian.GuardianLandmark = nomineeobj[0]["eqs_guardianlandmark"]?.ToString();
                guardian.GuardianPO = nomineeobj[0]["eqs_guardianpobox"]?.ToString();

                guardian.GuardianCityCode = await this._commonFunc.getCityCode(nomineeobj[0]["_eqs_guardiancity_value"].ToString());
                guardian.GuardianDistrict = nomineeobj[0]["eqs_guardiandistrict"]?.ToString();
                guardian.GuardianCountryCode = await this._commonFunc.getCuntryCode(nomineeobj[0]["_eqs_guardiancountry_value"].ToString());
                guardian.GuardianState = await this._commonFunc.getStateCode(nomineeobj[0]["_eqs_guardianstate_value"].ToString());

                nominee.Guardian = guardian;
                ftAccountLeadReturn.Nominee = nominee;
            }

            this.AccountLeadReturn = ftAccountLeadReturn;
            return true;

        }

        private async Task<CDgAccountLead> getLeadAccountD0(JArray LeadData)
        {
            CDgAccountLead cDgAccountLead = new CDgAccountLead();
            AccountLead accountLead = new AccountLead();

            accountLead.accountType = await this._queryParser.getOptionSetValuToText("eqs_leadaccount", "eqs_accountownershipcode", LeadData[0]["eqs_accountownershipcode"].ToString());
            accountLead.productCategory = LeadData[0]["_eqs_typeofaccountid_value@OData.Community.Display.V1.FormattedValue"].ToString();
            accountLead.productCode = LeadData[0]["_eqs_productid_value@OData.Community.Display.V1.FormattedValue"].ToString();
            if (!string.IsNullOrEmpty(LeadData[0]["eqs_instakitoptioncode"].ToString()))
            {
                accountLead.accountOpeningFlow = LeadData[0]["eqs_instakitoptioncode@OData.Community.Display.V1.FormattedValue"].ToString();
            }

            accountLead.sourceBranch = LeadData[0]["eqs_Lead"]["_eqs_branchid_value@OData.Community.Display.V1.FormattedValue"].ToString();
            if (!string.IsNullOrEmpty(LeadData[0]["_eqs_leadsourceid_value"].ToString()))
            {
                accountLead.leadsource = LeadData[0]["_eqs_leadsourceid_value@OData.Community.Display.V1.FormattedValue"].ToString();
            }
            if (!string.IsNullOrEmpty(LeadData[0]["eqs_initialdepositmodecode"].ToString()))
            {
                accountLead.initialDepositType = LeadData[0]["eqs_initialdepositmodecode@OData.Community.Display.V1.FormattedValue"].ToString();
            }
            accountLead.fieldEmployeeCode = LeadData[0]["eqs_sourcebyemployeecode"]?.ToString();
            accountLead.applicationDate = LeadData[0]["eqs_applicationdate"]?.ToString();
            accountLead.tenureInMonths = LeadData[0]["eqs_tenureinmonths"]?.ToString();
            accountLead.tenureInDays = LeadData[0]["eqs_tenureindays"]?.ToString();
            accountLead.rateOfInterest = LeadData[0]["eqs_rateofinterest"]?.ToString();
            accountLead.fundsTobeDebitedFrom = LeadData[0]["eqs_fundstobedebitedfrom"]?.ToString();
            accountLead.InitialDeposit = await this._queryParser.getOptionSetValuToText("eqs_leadaccount", "eqs_initialdepositamountcode", LeadData[0]["eqs_initialdepositamountcode"].ToString());
            accountLead.depositAmount = LeadData[0]["eqs_depositamountslot"]?.ToString();
            accountLead.mopRemarks = LeadData[0]["eqs_modeofoperationremarks"]?.ToString();
            accountLead.fdAccOpeningDate = LeadData[0]["eqs_fdvaluedate"]?.ToString();
            accountLead.sweepFacility = LeadData[0]["eqs_sweepfacility"]?.ToString();
            cDgAccountLead.accountLead = accountLead;

            cDgAccountLead.CustomerAccLdRelations = new List<CustomerAccLdRelation>();

            var applicentdetails = await this._commonFunc.getApplicentsSetails(LeadData[0]["eqs_leadaccountid"].ToString());
            foreach (var item in applicentdetails)
            {
                CustomerAccLdRelation customerAccLdRelation = new CustomerAccLdRelation();
                customerAccLdRelation.UCIC = item["eqs_ucic"]?.ToString();
                customerAccLdRelation.customerAccountRelation = item["_eqs_accountrelationship_value@OData.Community.Display.V1.FormattedValue"]?.ToString();
                customerAccLdRelation.isPrimaryHolder = item["eqs_isprimaryholder@OData.Community.Display.V1.FormattedValue"]?.ToString();
                customerAccLdRelation.relationToPrimaryHolder = await this._commonFunc.getRelationshipCode(item["_eqs_relationship_value"].ToString());

                cDgAccountLead.CustomerAccLdRelations.Add(customerAccLdRelation);
            }


            return cDgAccountLead;
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
