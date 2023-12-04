using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace UpdateAccountLead
{
    public partial class UpdgAccLeadExecution : IUpdgAccLeadExecution
    {

        private async Task<UpAccountLeadReturn> CreateDDELeadAccount(dynamic RequestData)
        {
            UpAccountLeadReturn accountLeadReturn = new UpAccountLeadReturn();

            var _leadDetails = await this._commonFunc.getLeadAccountDetails(RequestData.General?.AccountLeadId.ToString());
            if (_leadDetails.Count > 0)
            {
                int haserror = 0;
                List<string> fields = new List<string>();
                if (RequestData.Nominee != null)
                {
                    if (string.IsNullOrEmpty(RequestData.Nominee?.nomineeUCICIfCustomer?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("nomineeUCICIfCustomer");
                    }
                    if (string.IsNullOrEmpty(RequestData.Nominee?.name?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("v");
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
                    if (string.IsNullOrEmpty(RequestData.Nominee?.DOB?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("DOB");
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
                        if (string.IsNullOrEmpty(RequestData.Nominee?.Guardian?.GuardianAddress?.ToString()))
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

                if (haserror == 1)
                {
                    accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                    accountLeadReturn.Message = string.Join(",", fields) + " fields should not be null!";
                    return accountLeadReturn;
                }

                List<string> Preferences;
                string errorMessage = await SetLeadAccountDDE(_leadDetails, RequestData);
                if (string.IsNullOrEmpty(errorMessage))
                {

                    if (RequestData.documents != null && RequestData.documents.Count > 0)
                    {
                        await SetDocumentDDE(RequestData.documents);
                    }

                    if (RequestData.DirectBanking?.Preferences != null && RequestData.DirectBanking?.Preferences.Count > 0)
                    {
                        Preferences = await SetPreferencesDDE(RequestData.DirectBanking?.Preferences);
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

                    var AccountDDE = await this._commonFunc.getAccountLeadData(DDEId);
                    if (AccountDDE.Count > 0 && !string.IsNullOrEmpty(AccountDDE[0]["eqs_accountnocreated"].ToString()))
                    {
                        return "Lead cannot be onboarded because account has been already created for this Lead Account.";
                    }
                }

                var leadDetails = await this._commonFunc.getLeadDetails(LeadAccount[0]["_eqs_lead_value"].ToString());

                if (string.IsNullOrEmpty(this.DDEId))
                {
                    odatab.Add("eqs_accountopeningbranchid@odata.bind", $"eqs_branchs({leadDetails[0]["_eqs_branchid_value"].ToString()})");
                    odatab.Add("eqs_leadaccountid@odata.bind", $"eqs_leadaccounts({LeadAccount[0]["eqs_leadaccountid"].ToString()})");
                    odatab.Add("eqs_ddeoperatorname", LeadAccount[0]["eqs_crmleadaccountid"].ToString() + "  - Final");
                }


                this.LeadAccountid = LeadAccount[0]["eqs_leadaccountid"].ToString();



                /****************** General  ***********************/
                if (ddeData.General != null)
                {
                    if (ddeData.General?.AccountNumber.ToString() != "")
                    {
                        odatab.Add("eqs_AccountNumber@odata.bind", $"eqs_accounts({await this._commonFunc.getAccountId(ddeData.General?.AccountNumber?.ToString())})");
                    }
                    if (!string.IsNullOrEmpty(ddeData.General?.ApplicationDate?.ToString()))
                    {
                        dd = ddeData.General?.ApplicationDate?.ToString()?.Substring(0, 2);
                        mm = ddeData.General?.ApplicationDate?.ToString()?.Substring(3, 2);
                        yyyy = ddeData.General?.ApplicationDate?.ToString()?.Substring(6, 4);
                        odatab.Add("eqs_applicationdate", yyyy + "-" + mm + "-" + dd);
                    }

                    if (!string.IsNullOrEmpty(ddeData.General?.ProductCategory.ToString()))
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
                    
                   
                    
                }



                //odatab.Add("eqs_accountownershipcode", this.AccountType[ddeData.accountType.ToString()]);                
                //odatab.Add("eqs_accounttitle", ddeData.AccountTitle.ToString());

                /****************** FDRD Details  ***********************/
                if (ddeData.FDRDDetails != null)
                {
                    if (ddeData.FDRDDetails?.ProductDetails!=null)
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
                            odatab1.Add("eqs_depositamount", Convert.ToDouble(ddeData.FDRDDetails?.DepositDetails?.DepositAmount?.ToString()));
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
                        if (!string.IsNullOrEmpty(ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToESFBAccountNo?.ToString()))
                        {
                            odatab.Add("eqs_esfbaccountnumber_maturity", ddeData.FDRDDetails?.MaturityInstructionDetails?.MICreditToESFBAccountNo?.ToString());
                        }
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

                if (ddeData.DirectBanking!=null)
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
    }
}
