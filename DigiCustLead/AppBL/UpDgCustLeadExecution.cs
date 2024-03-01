﻿namespace DigiCustLead
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
    using System.Text.RegularExpressions;
    using Newtonsoft.Json.Converters;

    public class UpDgCustLeadExecution : IUpDgCustLeadExecution
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
        public string DDEId, AddressID, FatcaAddressID, FatcaId;
        public List<AddressResponse> Address_Id;
        public List<BOResponse> BO_Id;
        public List<CPResponse> CP_Id;

        private ICommonFunction _commonFunc;

        public UpDgCustLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {

            this._logger = logger;

            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;


        }


        public async Task<UpdateCustLeadReturn> ValidateCustLeadDetls(dynamic RequestData)
        {
            UpdateCustLeadReturn ldRtPrm = new UpdateCustLeadReturn();

            try
            {
                RequestData = await this.getRequestData(RequestData, "UpdateDigiCustLead");

                if (RequestData.ErrorNo != null && RequestData.ErrorNo.ToString() == "Error99")
                {
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "API do not have access permission!";
                    return ldRtPrm;
                }

                if (!string.IsNullOrEmpty(this.appkey) && this.appkey != "" && checkappkey(this.appkey, "UpdateDigiCustLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(this.Transaction_ID) && !string.IsNullOrEmpty(this.Channel_ID))
                    {
                        if (!string.IsNullOrEmpty(RequestData.ApplicantId?.ToString()))
                        {
                            ldRtPrm = await this.createDDE(RequestData);
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateCustLeadDetls", "ApplicantId could not be null.");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "ApplicantId could not be null.";
                            return ldRtPrm;
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateCustLeadDetls", "Transaction_ID or  Channel_ID is incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or  Channel_ID is incorrect.";
                        return ldRtPrm;
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateCustLeadDetls", "appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Appkey is incorrect";
                }
                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateCustLeadDetls", ex.Message);
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

        public async Task<UpdateCustLeadReturn> createDDE(dynamic CustLeadData)
        {
            UpdateCustLeadReturn csRtPrm = new UpdateCustLeadReturn();
            try
            {
                var Applicant_Data = await this._commonFunc.getApplicentData(CustLeadData.ApplicantId?.ToString());
                Applicant_Data = Applicant_Data[0];
                string EntityType = await this._commonFunc.getEntityName(Applicant_Data["_eqs_entitytypeid_value"]?.ToString());
                if (EntityType == "Individual")
                {
                    csRtPrm = await this.createDigiCustLeadIndv(CustLeadData.IndividualDDE, Applicant_Data);
                }
                else
                {
                    csRtPrm = await this.createDigiCustLeadCorp(CustLeadData.corporateDDE, Applicant_Data);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("createDDE", ex.Message);
                throw;
            }
            return csRtPrm;
        }

        public async Task<UpdateCustLeadReturn> createDigiCustLeadIndv(dynamic CustIndvData, dynamic Applicant_Data)
        {
            UpdateCustLeadReturn csRtPrm = new UpdateCustLeadReturn();

            Dictionary<string, string> CRMDDEmappingFields = new Dictionary<string, string>();
            Dictionary<string, bool> CRMDDEmappingFields1 = new Dictionary<string, bool>();
            Dictionary<string, int> CRMDDEmappingFields2 = new Dictionary<string, int>();
            Dictionary<string, bool> CRMDDEupdateTriggerFields = new Dictionary<string, bool>();

            try
            {
                string dd, mm, yyyy;
                this.DDEId = await this._commonFunc.getDDEFinalAccountIndvData(Applicant_Data["eqs_accountapplicantid"]?.ToString());

                if (!string.IsNullOrEmpty(this.DDEId))
                {
                    var ddeIndividualCust = await this._commonFunc.getDDEFinalIndvCustomerId(this.DDEId);
                    if (!string.IsNullOrEmpty(ddeIndividualCust[0]["eqs_customeridcreated"]?.ToString()))
                    {
                        this._logger.LogError("createDigiCustLeadIndv", "Lead can't be onboarded because Customer Id has been already created for this Account Applicant");
                        csRtPrm.ReturnCode = "CRM-ERROR-101";
                        csRtPrm.Message = "Lead can't be onboarded because Customer Id has been already created for this Account Applicant";
                        return csRtPrm;
                    }
                }
                else if (string.IsNullOrEmpty(this.DDEId))
                {
                    if (Applicant_Data["eqs_panform60code"]?.ToString() != "615290000" && !string.IsNullOrEmpty(Applicant_Data["eqs_internalpan"]?.ToString()))
                    {
                        if (Applicant_Data["eqs_panvalidationmode"]?.ToString() != "958570001")
                        {
                            this._logger.LogError("createDigiCustLeadIndv", "Lead details can't be created or updated because PAN has not been verified for this Lead.");
                            csRtPrm.ReturnCode = "CRM-ERROR-101";
                            csRtPrm.Message = "Lead details can't be created or updated because PAN has not been verified for this Lead.";
                            return csRtPrm;
                        }
                    }
                }

                CRMDDEmappingFields.Add("eqs_dataentryoperator", Applicant_Data.eqs_applicantid?.ToString() + "  - Final");
                CRMDDEmappingFields.Add("eqs_entitytypeId@odata.bind", $"eqs_entitytypes({Applicant_Data._eqs_entitytypeid_value?.ToString()})");
                CRMDDEmappingFields.Add("eqs_subentitytypeId@odata.bind", $"eqs_subentitytypes({Applicant_Data._eqs_subentity_value?.ToString()})");
                
                CRMDDEmappingFields.Add("eqs_leadId@odata.bind", $"leads({Applicant_Data["_eqs_leadid_value"]?.ToString()})");

                CRMDDEmappingFields.Add("eqs_accountapplicantid@odata.bind", $"eqs_accountapplicants({Applicant_Data["eqs_accountapplicantid"]?.ToString()})");
                CRMDDEmappingFields.Add("eqs_dataentrystage", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_dataentrystage", "Final"));
                CRMDDEmappingFields.Add("eqs_titleId@odata.bind", $"eqs_titles({Applicant_Data["_eqs_titleid_value"]?.ToString()})");
                CRMDDEmappingFields.Add("eqs_firstname", Applicant_Data["eqs_firstname"]?.ToString());
                CRMDDEmappingFields.Add("eqs_middlename", Applicant_Data["eqs_middlename"]?.ToString());
                CRMDDEmappingFields.Add("eqs_lastname", Applicant_Data["eqs_lastname"]?.ToString());

                if (!string.IsNullOrEmpty(Applicant_Data["eqs_dob"]?.ToString()))
                {
                    DateTime dt = ((DateTime)((JObject)Applicant_Data)["eqs_dob"]);
                    CRMDDEmappingFields.Add("eqs_dob", dt.ToString("yyyy-MM-dd"));
                }

                if (!string.IsNullOrEmpty(Applicant_Data["eqs_leadage"]?.ToString()) && Applicant_Data["eqs_leadage"]?.ToString() != "")
                {
                    CRMDDEmappingFields.Add("eqs_age", Applicant_Data["eqs_leadage"]?.ToString());
                }

                string shname = Applicant_Data["eqs_firstname"]?.ToString() + " " + Applicant_Data["eqs_middlename"]?.ToString() + " " + Applicant_Data["eqs_lastname"]?.ToString();
                CRMDDEmappingFields.Add("eqs_shortname", (shname.Length > 20) ? shname.Substring(0, 20) : shname);
                CRMDDEmappingFields.Add("eqs_mobilenumber", Applicant_Data["eqs_mobilenumber"]?.ToString());

                List<string> fields = new List<string>();
                int haserror = 0;
                if (CustIndvData.Address != null)
                {
                    foreach (var item in CustIndvData.Address)
                    {
                        if (string.IsNullOrEmpty(item.AddressType?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("AddressType");
                        }
                        if (string.IsNullOrEmpty(item.AddressLine1?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("AddressLine1");
                        }
                        if (string.IsNullOrEmpty(item.PinCodeMaster?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("PinCodeMaster");
                        }
                        if (string.IsNullOrEmpty(item.CityId?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("CityId");
                        }
                        if (string.IsNullOrEmpty(item.StateId?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("StateId");
                        }
                        if (string.IsNullOrEmpty(item.CountryId?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("CountryId");
                        }
                    }
                }
                if (CustIndvData.FATCA != null)
                {
                    if (string.IsNullOrEmpty(CustIndvData.FATCA?.OtherIdentificationNumber?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("OtherIdentificationNumber");
                    }
                    if (string.IsNullOrEmpty(CustIndvData.FATCA?.TaxIdentificationNumber?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("TaxIdentificationNumber");
                    }
                    if (string.IsNullOrEmpty(CustIndvData.FATCA?.AddressType?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("AddressType");
                    }
                    if (CustIndvData.FATCA?.Address != null)
                    {
                        if (string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.AddressLine1?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("FATCA AddressLine1");
                        }
                        if (string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.PinCodeMaster?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("FATCA PinCodeMaster");
                        }
                        if (string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.CityId?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("FATCA CityId");
                        }
                        if (string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.StateId?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("FATCA StateId");
                        }
                        if (string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.CountryId?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("FATCA CountryId");
                        }
                    }
                }
                if (!string.IsNullOrEmpty(CustIndvData.IdentificationDetails?.PassportNumber?.ToString()))
                {
                    if (CustIndvData.IdentificationDetails?.PassportNumber?.ToString()?.Length > 10)
                    {
                        haserror = 1;
                        fields.Add("Passport should not be more than 10 characters");
                    }
                }
                if (haserror == 1)
                {
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = string.Join(",", fields) + " fields should not be null!";
                    return csRtPrm;
                }

                /*********** General *********/
                if (CustIndvData.General != null)
                {
                    if (!string.IsNullOrEmpty(CustIndvData.General?.SourceBranch?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_sourcebranchId@odata.bind", $"eqs_branchs({await this._commonFunc.getBranchId(CustIndvData.General?.SourceBranch?.ToString())})");
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.General?.CustomerspreferredBranch?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_custpreferredbranchId@odata.bind", $"eqs_branchs({await this._commonFunc.getBranchId(CustIndvData.General?.CustomerspreferredBranch?.ToString())})");
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.General?.LGCode?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_lgcode", CustIndvData.General?.LGCode?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.General?.LCCode?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_lccode", CustIndvData.General?.LCCode?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.General?.RelationshiptoPrimaryHolder?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_relationshiptoprimaryholder@odata.bind", $"eqs_relationships({await this._commonFunc.getRelationshipID(CustIndvData.General?.RelationshiptoPrimaryHolder?.ToString())})");
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.General?.ResidencyType?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_residencytypecode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_residencytypecode", CustIndvData.General?.ResidencyType?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.General?.AccountRelationship?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_AccountRelationship@odata.bind", $"eqs_accountrelationshipses({await this._commonFunc.getAccRelationshipID(CustIndvData.General?.AccountRelationship?.ToString())})");
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.General?.PurposeofCreation?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_purposeofcreationId@odata.bind", $"eqs_purposeofcreations({await this._commonFunc.getPurposeID(CustIndvData.General?.PurposeofCreation?.ToString())})");
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.General?.PhysicalAOFnumber?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_physicalaornumber", CustIndvData.General?.PhysicalAOFnumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.General?.IsMFICustomer?.ToString()))
                    {
                        CRMDDEmappingFields1.Add("eqs_ismficustomer", Convert.ToBoolean(CustIndvData.General?.IsMFICustomer?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.General?.IsDeferral?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_deferralcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_deferralcode", CustIndvData.General?.IsDeferral?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.General?.Deferral?.ToString()))
                    {
                        CRMDDEmappingFields1.Add("eqs_isdeferral", (await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_isdeferral", CustIndvData.General?.Deferral?.ToString()) == "1") ? true : false);
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.General?.IsPermAddrAndCurrAddrSame?.ToString()))
                    {
                        CRMDDEmappingFields1.Add("eqs_ispermaddrandcurraddrsame", Convert.ToBoolean(CustIndvData.General?.IsPermAddrAndCurrAddrSame?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.General?.InstaKitCustomerID?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_instakitcustomerid", CustIndvData.General?.InstaKitCustomerID?.ToString());
                    }
                }

                /*********** Prospect Details *********/
                if (CustIndvData.ProspectDetails != null)
                {
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.EmailID?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_emailid", CustIndvData.ProspectDetails?.EmailID?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.NationalityID?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_nationalityId@odata.bind", $"eqs_countries({await this._commonFunc.getCountryID(CustIndvData.ProspectDetails?.NationalityID?.ToString())})");
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.CountryIDofbirth?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_countryofbirthId@odata.bind", $"eqs_countries({await this._commonFunc.getCountryID(CustIndvData.ProspectDetails?.CountryIDofbirth?.ToString())})");
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.FathersName?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_fathername", CustIndvData.ProspectDetails?.FathersName?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.MothersMaidenName?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_mothermaidenname", CustIndvData.ProspectDetails?.MothersMaidenName?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.SpouseName?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_spousename", CustIndvData.ProspectDetails?.SpouseName?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.Gender?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_gendercode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_gendercode", CustIndvData.ProspectDetails?.Gender?.ToString()));
                    }
                    else if (!string.IsNullOrEmpty(Applicant_Data["eqs_gendercode"]?.ToString()) && Applicant_Data["eqs_gendercode"]?.ToString() != "")
                    {
                        CRMDDEmappingFields.Add("eqs_gendercode", Applicant_Data["eqs_gendercode"]?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.CountryID?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_countryId@odata.bind", $"eqs_countries({await this._commonFunc.getCountryID(CustIndvData.ProspectDetails?.CountryID?.ToString())})");
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.Program?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_programcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_programcode", CustIndvData.ProspectDetails?.Program?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.Education?.ToString()))
                    {
                        string education = await this._commonFunc.getEducationId(CustIndvData.ProspectDetails?.Education?.ToString());
                        if (!string.IsNullOrEmpty(education))
                        {
                            CRMDDEmappingFields.Add("eqs_qualificationid@odata.bind", $"eqs_qualifications({education})");
                        }
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.MaritalStatus?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_maritalstatuscode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_maritalstatuscode", CustIndvData.ProspectDetails?.MaritalStatus?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.Profession?.ToString()))
                    {
                        string Profession = await this._commonFunc.getProfessionId(CustIndvData.ProspectDetails?.Profession?.ToString());
                        if (!string.IsNullOrEmpty(Profession))
                        {
                            CRMDDEmappingFields.Add("eqs_occupationid@odata.bind", $"eqs_occupations({Profession})");
                        }
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.AnnualIncomeBand?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_annualincomebandcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_annualincomebandcode", CustIndvData.ProspectDetails?.AnnualIncomeBand.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.EmployerType?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_employertypecode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_employertypecode", CustIndvData.ProspectDetails?.EmployerType.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.EmployerName?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_empname", CustIndvData.ProspectDetails?.EmployerName?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.OfficePhone?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_officephone", CustIndvData.ProspectDetails?.OfficePhone?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.EstimatedAgriculturalIncome?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_agriculturalincome", CustIndvData.ProspectDetails?.EstimatedAgriculturalIncome?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.EstimatedNonAgriculturalIncome?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_nonagriculturalincome", CustIndvData.ProspectDetails?.EstimatedNonAgriculturalIncome?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.IsStaff?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_isstaffcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_isstaffcode", CustIndvData.ProspectDetails?.IsStaff.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.EquitasStaffCode?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_equitasstaffcode", CustIndvData.ProspectDetails?.EquitasStaffCode.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.Language?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_languagecode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_languagecode", CustIndvData.ProspectDetails?.Language.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.PolitcallyExposedPerson?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_politicallyexposedpersoncode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_politicallyexposedpersoncode", CustIndvData.ProspectDetails?.PolitcallyExposedPerson.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.LOBCode?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_lobcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_lobcode", CustIndvData.ProspectDetails?.LOBCode.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.AOBusinessOperation?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_aobocode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_aobocode", CustIndvData.ProspectDetails?.AOBusinessOperation.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.Category?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_communitycode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_communitycode", CustIndvData.ProspectDetails?.Category.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.AdditionalInformation?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_additionalinformationcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_additionalinformationcode", CustIndvData.ProspectDetails?.AdditionalInformation.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.WorkplaceAddress?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_corporatecompanyid@odata.bind", $"eqs_corporatemasters({await this._commonFunc.getcorporatemasterID(CustIndvData.ProspectDetails?.WorkplaceAddress.ToString())})");
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.Designation?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_designationid@odata.bind", $"eqs_designationmasters({await this._commonFunc.getdesignationmasterID(CustIndvData.ProspectDetails?.Designation.ToString())})");
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.ProspectDetails?.IsPhysicallyChallenged?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_isphysicallychallenged", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_isphysicallychallenged", CustIndvData.ProspectDetails?.IsPhysicallyChallenged.ToString()));
                    }
                }

                /*********** Identification Details *********/
                if (CustIndvData.IdentificationDetails != null)
                {
                    if (!string.IsNullOrEmpty(CustIndvData.IdentificationDetails?.CKYCreferenceNumber?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_ckycreferencenumber", CustIndvData.IdentificationDetails?.CKYCreferenceNumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.IdentificationDetails?.KYCVerificationMode?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverificationmodecode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_kycverificationmodecode", CustIndvData.IdentificationDetails?.KYCVerificationMode.ToString()));
                    }

                    if (!string.IsNullOrEmpty(CustIndvData.IdentificationDetails?.VerificationDate?.ToString()))
                    {
                        dd = CustIndvData.IdentificationDetails?.VerificationDate.ToString()?.Substring(0, 2);
                        mm = CustIndvData.IdentificationDetails?.VerificationDate.ToString()?.Substring(3, 2);
                        yyyy = CustIndvData.IdentificationDetails?.VerificationDate.ToString()?.Substring(6, 4);
                        CRMDDEmappingFields.Add("eqs_verificationdate", yyyy + "-" + mm + "-" + dd);
                    }


                    CRMDDEmappingFields.Add("eqs_pannumber", Applicant_Data["eqs_pan"]?.ToString());
                    CRMDDEmappingFields.Add("eqs_internalpan", Applicant_Data["eqs_internalpan"]?.ToString());
                    CRMDDEmappingFields.Add("eqs_passportnumber", Applicant_Data["eqs_passportnumber"]?.ToString());
                    CRMDDEmappingFields.Add("eqs_voterid", Applicant_Data["eqs_voterid"]?.ToString());
                    CRMDDEmappingFields.Add("eqs_drivinglicensenumber", Applicant_Data["eqs_dlnumber"]?.ToString());
                    CRMDDEmappingFields.Add("eqs_aadharreference", Applicant_Data["eqs_aadhaarreference"]?.ToString());


                    if (!string.IsNullOrEmpty(CustIndvData.IdentificationDetails?.PanNumber?.ToString()))
                    {
                        CRMDDEmappingFields["eqs_pannumber"] = "**********";
                        CRMDDEmappingFields["eqs_internalpan"] = CustIndvData.IdentificationDetails.PanNumber.ToString();
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.IdentificationDetails?.PassportNumber?.ToString()))
                    {
                        CRMDDEmappingFields["eqs_passportnumber"] = CustIndvData.IdentificationDetails.PassportNumber.ToString();
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.IdentificationDetails?.VoterID?.ToString()))
                    {
                        CRMDDEmappingFields["eqs_voterid"] = CustIndvData.IdentificationDetails.VoterID.ToString();
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.IdentificationDetails?.DrivinglicenseNumber?.ToString()))
                    {
                        CRMDDEmappingFields["eqs_drivinglicensenumber"] = CustIndvData.IdentificationDetails.DrivinglicenseNumber.ToString();
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.IdentificationDetails?.AadharReference?.ToString()))
                    {
                        CRMDDEmappingFields["eqs_aadharreference"] = CustIndvData.IdentificationDetails.AadharReference.ToString();
                    }
                       

                   
                }

                /*********** FATCA *********/
                if (CustIndvData.FATCA != null)
                {
                    if (!string.IsNullOrEmpty(CustIndvData.FATCA?.TaxResident?.ToString()))
                    {
                        CRMDDEmappingFields1.Add("eqs_taxresident", (await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_taxresident", CustIndvData.FATCA?.TaxResident.ToString()) == "1") ? true : false);
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.FATCA?.CityofBirth?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_cityofbirth", CustIndvData.FATCA?.CityofBirth.ToString());
                    }
                }

                /*********** NRI Details *********/
                if (CustIndvData.NRIDetails != null)
                {
                    if (!string.IsNullOrEmpty(CustIndvData.NRIDetails?.VisaType?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_visatypecode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_visatypecode", CustIndvData.NRIDetails?.VisaType?.ToString()));
                    }

                    if (!string.IsNullOrEmpty(CustIndvData.NRIDetails?.VisaIssuedDate?.ToString()))
                    {
                        dd = CustIndvData.NRIDetails?.VisaIssuedDate?.ToString()?.Substring(0, 2);
                        mm = CustIndvData.NRIDetails?.VisaIssuedDate?.ToString()?.Substring(3, 2);
                        yyyy = CustIndvData.NRIDetails?.VisaIssuedDate?.ToString()?.Substring(6, 4);
                        CRMDDEmappingFields.Add("eqs_visaissueddate", yyyy + "-" + mm + "-" + dd);
                    }

                    if (!string.IsNullOrEmpty(CustIndvData.NRIDetails?.VisaExpiryDate?.ToString()))
                    {
                        dd = CustIndvData.NRIDetails?.VisaExpiryDate?.ToString()?.Substring(0, 2);
                        mm = CustIndvData.NRIDetails?.VisaExpiryDate?.ToString()?.Substring(3, 2);
                        yyyy = CustIndvData.NRIDetails?.VisaExpiryDate?.ToString()?.Substring(6, 4);
                        CRMDDEmappingFields.Add("eqs_visaexpirydate", yyyy + "-" + mm + "-" + dd);
                    }

                    if (!string.IsNullOrEmpty(CustIndvData.NRIDetails?.KYCMode?.ToString()))
                    {
                        CRMDDEmappingFields1.Add("eqs_kycmode", (await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_kycmode", CustIndvData.NRIDetails?.KYCMode?.ToString()) == "1") ? true : false);
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.NRIDetails?.Seafarer?.ToString()))
                    {
                        CRMDDEmappingFields1.Add("eqs_seafarer", (await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_seafarer", CustIndvData.NRIDetails?.Seafarer?.ToString()) == "1") ? true : false);
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.NRIDetails?.ResidenceStatus?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_residencestatuscode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_residencestatuscode", CustIndvData.NRIDetails?.ResidenceStatus?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.NRIDetails?.VISAOCICDCNumber?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_visanumber", CustIndvData.NRIDetails?.VISAOCICDCNumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.NRIDetails?.SMSOTPMobilepreference?.ToString()))
                    {
                        CRMDDEmappingFields1.Add("eqs_mobilepreference", (await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_mobilepreference", CustIndvData.NRIDetails?.SMSOTPMobilepreference?.ToString()) == "1") ? true : false);
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.NRIDetails?.PassportIssuedDate?.ToString()))
                    {
                        dd = CustIndvData.NRIDetails?.PassportIssuedDate?.ToString()?.Substring(0, 2);
                        mm = CustIndvData.NRIDetails?.PassportIssuedDate?.ToString()?.Substring(3, 2);
                        yyyy = CustIndvData.NRIDetails?.PassportIssuedDate?.ToString()?.Substring(6, 4);
                        CRMDDEmappingFields.Add("eqs_passportissuedate", yyyy + "-" + mm + "-" + dd);
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.IdentificationDetails?.PassportExpiryDate?.ToString()))
                    {
                        dd = CustIndvData.NRIDetails?.PassportExpiryDate?.ToString()?.Substring(0, 2);
                        mm = CustIndvData.NRIDetails?.PassportExpiryDate?.ToString()?.Substring(3, 2);
                        yyyy = CustIndvData.NRIDetails?.PassportExpiryDate?.ToString()?.Substring(6, 4);
                        CRMDDEmappingFields.Add("eqs_passportexpirydate", yyyy + "-" + mm + "-" + dd);
                    }
                }

                /*********** RM Details *********/
                if (CustIndvData.RMDetails != null)
                {
                    if (!string.IsNullOrEmpty(CustIndvData.RMDetails?.ServiceRMCode?.ToString()))
                    {
                        var smEmployeeId = await this._commonFunc.getRMId(CustIndvData.RMDetails?.ServiceRMCode?.ToString());
                        if (!string.IsNullOrEmpty(smEmployeeId))
                        {
                            CRMDDEmappingFields.Add("eqs_servicermemployeeid@odata.bind", $"eqs_rmemployees({smEmployeeId})");
                        }
                        CRMDDEmappingFields.Add("eqs_servicermcode", CustIndvData.RMDetails?.ServiceRMCode?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.RMDetails?.ServiceRMName?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_servicermname", CustIndvData.RMDetails?.ServiceRMName?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.RMDetails?.ServiceRMRole?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_servicermrole", CustIndvData.RMDetails?.ServiceRMRole?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.RMDetails?.BusinessRMCode?.ToString()))
                    {
                        var bmEmployeeId = await this._commonFunc.getRMId(CustIndvData.RMDetails?.BusinessRMCode?.ToString());
                        if (!string.IsNullOrEmpty(bmEmployeeId))
                        {
                            CRMDDEmappingFields.Add("eqs_businessrmemployeeid@odata.bind", $"eqs_rmemployees({bmEmployeeId})");
                        }
                        CRMDDEmappingFields.Add("eqs_businessrmcode", CustIndvData.RMDetails?.BusinessRMCode?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.RMDetails?.BusinessRMName?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_businessrmname", CustIndvData.RMDetails?.BusinessRMName?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.RMDetails?.BusinessRMRole?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_businessrmrole", CustIndvData.RMDetails?.BusinessRMRole?.ToString());
                    }
                }
                if (!string.IsNullOrEmpty(Applicant_Data._eqs_leadsourceid_value?.ToString()))
                {
                    CRMDDEmappingFields.Add("eqs_leadsourceId@odata.bind", $"eqs_leadsources({Applicant_Data._eqs_leadsourceid_value?.ToString()})");
                }

                CRMDDEmappingFields.Add("eqs_sourcingchannelcode", Applicant_Data["eqs_leadchannelnew"]?.ToString());
                CRMDDEmappingFields.Add("eqs_panform60code", Applicant_Data["eqs_panform60code"]?.ToString());
                CRMDDEmappingFields.Add("eqs_createdfrompartnerchannel", "true");

                string postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);
                string postDataParametr1 = JsonConvert.SerializeObject(CRMDDEmappingFields1);
                postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                if (string.IsNullOrEmpty(this.DDEId))
                {
                    List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_ddeindividualcustomers()?$select=eqs_ddeindividualcustomerid", HttpMethod.Post, postDataParametr);
                    var ddeid = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_ddeindividualcustomerid");
                    this.DDEId = ddeid;
                    if (this.DDEId == null)
                    {
                        this._logger.LogError("createDigiCustLeadIndv", JsonConvert.SerializeObject(DDE_details), postDataParametr);
                        csRtPrm.ReturnCode = "CRM-ERROR-101";
                        csRtPrm.Message = "Can't create Final DDE.";
                        return csRtPrm;
                    }
                }
                else
                {
                    var response = await this._queryParser.HttpApiCall($"eqs_ddeindividualcustomers({this.DDEId})?", HttpMethod.Patch, postDataParametr);
                }

                /*********** KYCVerification *********/
                if (CustIndvData.KYCVerification != null)
                {
                    CRMDDEmappingFields = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(CustIndvData.KYCVerification?.EmpName?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverifiedempname", CustIndvData.KYCVerification?.EmpName?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.KYCVerification?.EmpID?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverifiedempid", CustIndvData.KYCVerification?.EmpID?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.KYCVerification?.EmpDesignation?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverifiedempdesignation", CustIndvData.KYCVerification?.EmpDesignation?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.KYCVerification?.EmpBranch?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverifiedempbranch", CustIndvData.KYCVerification?.EmpBranch?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.KYCVerification?.InstitutionName?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverifiedinstitutename", CustIndvData.KYCVerification?.InstitutionName?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustIndvData.KYCVerification?.InstitutionCode?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverifiedinstitutecode", CustIndvData.KYCVerification?.InstitutionCode?.ToString());
                    }



                    postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);

                    string KYCVerificationID = await this._commonFunc.getKYCVerificationID(this.DDEId, "IND");

                    if (string.IsNullOrEmpty(KYCVerificationID))
                    {
                        List<JObject> KYC_details = await this._queryParser.HttpApiCall("eqs_kycverificationdetailses()?$select=eqs_kycverificationdetailsid", HttpMethod.Post, postDataParametr);
                        var KYC_id = CommonFunction.GetIdFromPostRespons201(KYC_details[0]["responsebody"], "eqs_kycverificationdetailsid");
                        CRMDDEmappingFields = new Dictionary<string, string>();
                        CRMDDEmappingFields.Add("eqs_KYCVerificationDetailId@odata.bind", $"eqs_kycverificationdetailses({KYC_id})");
                        postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);
                        var response = await this._queryParser.HttpApiCall($"eqs_ddeindividualcustomers({this.DDEId})?", HttpMethod.Patch, postDataParametr);

                    }
                    else
                    {
                        var response = await this._queryParser.HttpApiCall($"eqs_kycverificationdetailses({KYCVerificationID})?", HttpMethod.Patch, postDataParametr);
                    }
                }

                /*********** Address *********/
                if (CustIndvData.Address != null)
                {
                    Address_Id = new List<AddressResponse>();
                    foreach (var CustIndvDataItem in CustIndvData.Address)
                    {
                        string AddressUniqueId = string.Empty;
                        CRMDDEmappingFields = new Dictionary<string, string>();
                        CRMDDEmappingFields1 = new Dictionary<string, bool>();
                        if (!string.IsNullOrEmpty(CustIndvDataItem.AddressId?.ToString()))
                        {
                            AddressUniqueId = CustIndvDataItem.AddressId.ToString();
                        }

                        //CRMDDEmappingFields.Add("eqs_name", Applicant_Data.eqs_applicantid?.ToString() + " - " + CustIndvDataItem.AddressType?.ToString());
                        if (!string.IsNullOrEmpty(CustIndvDataItem.ApplicantAddress?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_name", CustIndvDataItem.ApplicantAddress?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.AddressType?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addresstypecode", await this._queryParser.getOptionSetTextToValue("eqs_leadaddress", "eqs_addresstypecode", CustIndvDataItem.AddressType?.ToString()));
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.AddressLine1?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addressline1", CustIndvDataItem.AddressLine1?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.AddressLine2?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addressline2", CustIndvDataItem.AddressLine2?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.AddressLine3?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addressline3", CustIndvDataItem.AddressLine3?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.AddressLine4?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addressline4", CustIndvDataItem.AddressLine4?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.FaxNumber?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_faxnumber", CustIndvDataItem.FaxNumber?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.OverseasMobileNumber?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_overseasmobilenumber", CustIndvDataItem.OverseasMobileNumber?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.PinCodeMaster?.ToString()))
                        {
                            var pincodeMaster = await this._commonFunc.getPincodeID(CustIndvDataItem.PinCodeMaster?.ToString());
                            if (!string.IsNullOrEmpty(pincodeMaster))
                            {
                                CRMDDEmappingFields.Add("eqs_pincodemaster@odata.bind", $"eqs_pincodes({pincodeMaster})");
                            }
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.CityId?.ToString()))
                        {
                            var cityId = await this._commonFunc.getCityID(CustIndvDataItem.CityId?.ToString());
                            if (!string.IsNullOrEmpty(cityId))
                            {
                                CRMDDEmappingFields.Add("eqs_cityid@odata.bind", $"eqs_cities({cityId})");
                            }
                        }

                        if (!string.IsNullOrEmpty(CustIndvDataItem.StateId?.ToString()))
                        {
                            var stateId = await this._commonFunc.getStateID(CustIndvDataItem.StateId?.ToString());
                            if (!string.IsNullOrEmpty(stateId))
                            {
                                CRMDDEmappingFields.Add("eqs_stateid@odata.bind", $"eqs_states({stateId})");
                            }
                        }

                        if (!string.IsNullOrEmpty(CustIndvDataItem.District?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_district", CustIndvDataItem.District?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.CountryId?.ToString()))
                        {
                            var countryId = await this._commonFunc.getCountryID(CustIndvDataItem.CountryId?.ToString());
                            if (!string.IsNullOrEmpty(countryId))
                            {
                                CRMDDEmappingFields.Add("eqs_countryid@odata.bind", $"eqs_countries({countryId})");
                            }
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.PinCode?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_pincode", CustIndvDataItem.PinCode?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.POBox?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_pobox", CustIndvDataItem.POBox?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.Landmark?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_landmark", CustIndvDataItem.Landmark?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.LandlineNumber?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_landlinenumber", CustIndvDataItem.LandlineNumber?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.AlternateMobileNumber?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_alternatemobilenumber", CustIndvDataItem.AlternateMobileNumber?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustIndvDataItem.LocalOverseas?.ToString()))
                        {
                            CRMDDEmappingFields1.Add("eqs_localoverseas", (await this._queryParser.getOptionSetTextToValue("eqs_leadaddress", "eqs_localoverseas", CustIndvDataItem.LocalOverseas?.ToString()) == "1") ? true : false);
                        }


                        CRMDDEmappingFields.Add("eqs_individualdde@odata.bind", $"eqs_ddeindividualcustomers({this.DDEId})");

                        postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);
                        postDataParametr1 = JsonConvert.SerializeObject(CRMDDEmappingFields1);
                        postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                        this.AddressID = await this._commonFunc.getAddressID(this.DDEId, AddressUniqueId, "indv");

                        if (string.IsNullOrEmpty(this.AddressID))
                        {
                            List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_leadaddresses()?$select=eqs_leadaddressid,eqs_applicantaddressid", HttpMethod.Post, postDataParametr);
                            AddressUniqueId = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_applicantaddressid");
                        }
                        else
                        {
                            var response = await this._queryParser.HttpApiCall($"eqs_leadaddresses({this.AddressID})?", HttpMethod.Patch, postDataParametr);
                        }
                        Address_Id.Add(new AddressResponse(CustIndvDataItem.AddressType.ToString(), AddressUniqueId));
                    }
                }

                /*********** FATCA Link *********/
                if (CustIndvData.FATCA != null)
                {
                    CRMDDEmappingFields = new Dictionary<string, string>();
                    if (await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_taxresident", CustIndvData.FATCA?.TaxResident?.ToString()) == "1")
                    {
                        var fatcaCountryId = await this._commonFunc.getCountryID(CustIndvData.FATCA?.CountryCode?.ToString());
                        if (!string.IsNullOrEmpty(fatcaCountryId))
                        {
                            CRMDDEmappingFields.Add("eqs_countryid@odata.bind", $"eqs_countries({fatcaCountryId})");
                        }
                        CRMDDEmappingFields.Add("eqs_otheridentificationnumber", CustIndvData.FATCA?.OtherIdentificationNumber?.ToString());
                        CRMDDEmappingFields.Add("eqs_taxidentificationnumber", CustIndvData.FATCA?.TaxIdentificationNumber?.ToString());
                        CRMDDEmappingFields.Add("eqs_taxtype", CustIndvData.FATCA?.CountryofTaxResidencyTaxType?.ToString());
                        CRMDDEmappingFields.Add("eqs_addresstype", await this._queryParser.getOptionSetTextToValue("eqs_customerfactcaother", "eqs_addresstype", CustIndvData.FATCA?.AddressType?.ToString()));
                        CRMDDEmappingFields.Add("eqs_fatcadeclaration", await this._queryParser.getOptionSetTextToValue("eqs_customerfactcaother", "eqs_fatcadeclaration", CustIndvData.FATCA?.FATCADeclaration?.ToString()));
                        CRMDDEmappingFields.Add("eqs_indivapplicantddeid@odata.bind", $"eqs_ddeindividualcustomers({this.DDEId})");

                        postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);

                        this.FatcaId = await this._commonFunc.getFatcaID(this.DDEId, "indv");
                        string Fatca_Id = "";
                        if (string.IsNullOrEmpty(this.FatcaId))
                        {
                            List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_customerfactcaothers()?$select=eqs_customerfactcaotherid,eqs_name", HttpMethod.Post, postDataParametr);
                            var fatcaid = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_customerfactcaotherid");
                            csRtPrm.FATCAID = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_name");
                            this.FatcaId = fatcaid;
                        }
                        else
                        {
                            var response = await this._queryParser.HttpApiCall($"eqs_customerfactcaothers({this.FatcaId})?", HttpMethod.Patch, postDataParametr);
                            csRtPrm.FATCAID = CommonFunction.GetIdFromPostRespons201(response[0]["responsebody"], "eqs_name");
                        }

                        if (CustIndvData.FATCA?.AddressType?.ToString()?.Contains("Fatca Other Address") && CustIndvData.FATCA?.Address != null)
                        {
                            string AddressUniqueId = string.Empty;
                            CRMDDEmappingFields = new Dictionary<string, string>();
                            CRMDDEmappingFields1 = new Dictionary<string, bool>();

                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.FATCAAddressID?.ToString()))
                            {
                                AddressUniqueId = CustIndvData.FATCA.Address.FATCAAddressID.ToString();
                            }

                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.AddressType?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_name", Fatca_Id + " - " + CustIndvData.FATCA?.AddressType?.ToString());
                            }

                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.AddressType?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_addresstypecode", await this._queryParser.getOptionSetTextToValue("eqs_leadaddress", "eqs_addresstypecode", CustIndvData.FATCA?.AddressType?.ToString()));
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.AddressLine1?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_addressline1", CustIndvData.FATCA?.Address?.AddressLine1?.ToString());
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.AddressLine2?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_addressline2", CustIndvData.FATCA?.Address?.AddressLine2?.ToString());
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.AddressLine3?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_addressline3", CustIndvData.FATCA?.Address?.AddressLine3?.ToString());
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.AddressLine4?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_addressline4", CustIndvData.FATCA?.Address?.AddressLine4?.ToString());
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.FaxNumber?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_faxnumber", CustIndvData.FATCA?.Address?.FaxNumber?.ToString());
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.OverseasMobileNumber?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_overseasmobilenumber", CustIndvData.FATCA?.Address?.OverseasMobileNumber?.ToString());
                            }


                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.PinCodeMaster?.ToString()))
                            {
                                var fatcaPinCode = await this._commonFunc.getPincodeID(CustIndvData.FATCA?.Address?.PinCodeMaster?.ToString());
                                if (!string.IsNullOrEmpty(fatcaPinCode))
                                {
                                    CRMDDEmappingFields.Add("eqs_pincodemaster@odata.bind", $"eqs_pincodes({fatcaPinCode})");
                                }
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.CityId?.ToString()))
                            {
                                var fatcaCityId = await this._commonFunc.getCityID(CustIndvData.FATCA?.Address?.CityId?.ToString());
                                if (!string.IsNullOrEmpty(fatcaCityId))
                                {
                                    CRMDDEmappingFields.Add("eqs_cityid@odata.bind", $"eqs_cities({fatcaCityId})");
                                }
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.StateId?.ToString()))
                            {
                                var fatcaStateId = await this._commonFunc.getStateID(CustIndvData.FATCA?.Address?.StateId?.ToString());
                                if (!string.IsNullOrEmpty(fatcaStateId))
                                {
                                    CRMDDEmappingFields.Add("eqs_stateid@odata.bind", $"eqs_states({fatcaStateId})");
                                }
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.CountryId?.ToString()))
                            {
                                var fatcaCountry = await this._commonFunc.getCountryID(CustIndvData.FATCA?.Address?.CountryId?.ToString());
                                if (!string.IsNullOrEmpty(fatcaCountry))
                                {
                                    CRMDDEmappingFields.Add("eqs_countryid@odata.bind", $"eqs_countries({fatcaCountry})");
                                }
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.District?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_district", CustIndvData.FATCA?.Address?.District?.ToString());
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.PinCode?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_pincode", CustIndvData.FATCA?.Address?.PinCode?.ToString());
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.POBox?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_pobox", CustIndvData.FATCA?.Address?.POBox?.ToString());
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.Landmark?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_landmark", CustIndvData.FATCA?.Address?.Landmark?.ToString());
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.LandlineNumber?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_landlinenumber", CustIndvData.FATCA?.Address?.LandlineNumber?.ToString());
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.AlternateMobileNumber?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_alternatemobilenumber", CustIndvData.FATCA?.Address?.AlternateMobileNumber?.ToString());
                            }
                            if (!string.IsNullOrEmpty(CustIndvData.FATCA?.Address?.LocalOverseas?.ToString()))
                            {
                                CRMDDEmappingFields1.Add("eqs_localoverseas", (await this._queryParser.getOptionSetTextToValue("eqs_leadaddress", "eqs_localoverseas", CustIndvData.FATCA?.Address?.LocalOverseas?.ToString()) == "1") ? true : false);
                            }


                            CRMDDEmappingFields.Add("eqs_applicantfatca@odata.bind", $"eqs_customerfactcaothers({this.FatcaId})");

                            postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);
                            postDataParametr1 = JsonConvert.SerializeObject(CRMDDEmappingFields1);
                            postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                            this.FatcaAddressID = await this._commonFunc.getFatcaAddressID(this.FatcaId, AddressUniqueId);

                            if (string.IsNullOrEmpty(this.FatcaAddressID))
                            {
                                List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_leadaddresses()?$select=eqs_leadaddressid,eqs_applicantaddressid", HttpMethod.Post, postDataParametr);
                                AddressUniqueId = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_applicantaddressid");
                            }
                            else
                            {
                                var response = await this._queryParser.HttpApiCall($"eqs_leadaddresses({this.FatcaAddressID})?", HttpMethod.Patch, postDataParametr);
                            }
                            Address_Id.Add(new AddressResponse("FATCA Other Address", AddressUniqueId));
                        }
                    }
                }

                if (!string.IsNullOrEmpty(this.DDEId))
                {
                    CRMDDEupdateTriggerFields.Add("eqs_triggervalidation", true);
                    postDataParametr = JsonConvert.SerializeObject(CRMDDEupdateTriggerFields);
                    var response = await this._queryParser.HttpApiCall($"eqs_ddeindividualcustomers({this.DDEId})?", HttpMethod.Patch, postDataParametr);
                }

                csRtPrm.Address = Address_Id;
                csRtPrm.Message = OutputMSG.Case_Success;
                csRtPrm.ReturnCode = "CRM-SUCCESS";
            }
            catch (Exception ex)
            {
                this._logger.LogError("createDigiCustLeadIndv", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-101";
                csRtPrm.Message = "Exception occured while creating DDE Final";
            }
            return csRtPrm;
        }

        public async Task<UpdateCustLeadReturn> createDigiCustLeadCorp(dynamic CustCorpData, dynamic Applicant_Data)
        {
            UpdateCustLeadReturn csRtPrm = new UpdateCustLeadReturn();
            CustLeadElement custLeadElement = new CustLeadElement();
            Dictionary<string, string> CRMDDEmappingFields = new Dictionary<string, string>();
            Dictionary<string, bool> CRMDDEmappingFields1 = new Dictionary<string, bool>();
            Dictionary<string, int> CRMDDEmappingFields2 = new Dictionary<string, int>();
            Dictionary<string, string> CRMCustomermappingFields = new Dictionary<string, string>();
            Dictionary<string, bool> CRMDDEupdateTriggerFields = new Dictionary<string, bool>();

            try
            {
                string dd, mm, yyyy;
                this.DDEId = await this._commonFunc.getDDEFinalAccountCorpData(Applicant_Data["eqs_accountapplicantid"]?.ToString());

                if (!string.IsNullOrEmpty(this.DDEId))
                {
                    var ddeCorporateCust = await this._commonFunc.getDDEFinalCorpCustomerId(this.DDEId);
                    if (!string.IsNullOrEmpty(ddeCorporateCust[0]["eqs_customeridcreated"]?.ToString()))
                    {
                        this._logger.LogError("createDigiCustLeadCorp", "Lead can't be onboarded because Customer Id has been already created for this Account Applicant");
                        csRtPrm.ReturnCode = "CRM-ERROR-101";
                        csRtPrm.Message = "Lead can't be onboarded because Customer Id has been already created for this Account Applicant";
                        return csRtPrm;
                    }
                }
                else if (string.IsNullOrEmpty(this.DDEId))
                {
                    if (Applicant_Data["eqs_panform60code"]?.ToString() != "615290000" && !string.IsNullOrEmpty(Applicant_Data["eqs_internalpan"]?.ToString()))
                    {
                        if (Applicant_Data["eqs_panvalidationmode"]?.ToString() != "958570001")
                        {
                            this._logger.LogError("createDigiCustLeadCorp", "Lead details can't be created or updated because PAN has not been verified for this Lead.");
                            csRtPrm.ReturnCode = "CRM-ERROR-101";
                            csRtPrm.Message = "Lead details can't be created or updated because PAN has not been verified for this Lead.";
                            return csRtPrm;
                        }
                    }

                    CRMDDEmappingFields.Add("eqs_dataentryoperator", Applicant_Data.eqs_applicantid?.ToString() + "  - Final");
                    CRMDDEmappingFields.Add("eqs_dataentrystage", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_dataentrystage", "Final"));
                    CRMDDEmappingFields.Add("eqs_entitytypeId@odata.bind", $"eqs_entitytypes({Applicant_Data._eqs_entitytypeid_value?.ToString()})");
                    CRMDDEmappingFields.Add("eqs_subentitytypeId@odata.bind", $"eqs_subentitytypes({Applicant_Data._eqs_subentity_value?.ToString()})");
                    CRMDDEmappingFields.Add("eqs_sourcebranchterritoryId@odata.bind", $"eqs_branchs({await this._commonFunc.getBranchId(CustCorpData.General.SourceBranch?.ToString())})");
                    CRMDDEmappingFields.Add("eqs_preferredhomebranchId@odata.bind", $"eqs_branchs({await this._commonFunc.getBranchId(CustCorpData.General.CustomerpreferredHomebranch?.ToString())})");
                    CRMDDEmappingFields.Add("eqs_isprimaryholder", Applicant_Data.eqs_isprimaryholder?.ToString());
                    if (!string.IsNullOrEmpty(Applicant_Data["_eqs_titleid_value"]?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_titleId@odata.bind", $"eqs_titles({Applicant_Data["_eqs_titleid_value"]?.ToString()})");
                    }
                    CRMDDEmappingFields.Add("eqs_companyname1", Applicant_Data["eqs_companynamepart1"]?.ToString());
                    CRMDDEmappingFields.Add("eqs_companyname2", Applicant_Data["eqs_companynamepart2"]?.ToString());
                    CRMDDEmappingFields.Add("eqs_companyname3", Applicant_Data["eqs_companynamepart3"]?.ToString());
                    string shname = Applicant_Data["eqs_companynamepart1"]?.ToString() + " " + Applicant_Data["eqs_companynamepart2"]?.ToString() + " " + Applicant_Data["eqs_companynamepart3"]?.ToString();
                    CRMDDEmappingFields.Add("eqs_shortname", (shname.Length > 20) ? shname.Substring(0, 20) : shname);

                    CRMDDEmappingFields.Add("eqs_leadid@odata.bind", $"leads({Applicant_Data["_eqs_leadid_value"]?.ToString()})");

                    if (!string.IsNullOrEmpty(Applicant_Data["eqs_dateofincorporation"]?.ToString()))
                    {
                        DateTime dt = ((DateTime)((JObject)Applicant_Data)["eqs_dateofincorporation"]);
                        //dd = Applicant_Data["eqs_dateofincorporation"]?.ToString()?.Substring(0, 2);
                        //mm = Applicant_Data["eqs_dateofincorporation"]?.ToString()?.Substring(3, 2);
                        //yyyy = Applicant_Data["eqs_dateofincorporation"]?.ToString()?.Substring(6, 4);
                        CRMDDEmappingFields.Add("eqs_dateofincorporation", dt.ToString("yyyy-MM-dd"));
                    }

                    CRMDDEmappingFields.Add("eqs_accountapplicantid@odata.bind", $"eqs_accountapplicants({Applicant_Data["eqs_accountapplicantid"]?.ToString()})");

                }

                List<string> fields = new List<string>();
                int haserror = 0;
                if (CustCorpData.Address != null)
                {
                    foreach (var item in CustCorpData.Address)
                    {
                        if (string.IsNullOrEmpty(item.AddressType?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("AddressType");
                        }
                        if (string.IsNullOrEmpty(item.AddressLine1?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("AddressLine1");
                        }
                        if (string.IsNullOrEmpty(item.PinCodeMaster?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("PinCodeMaster");
                        }
                        if (string.IsNullOrEmpty(item.CityId?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("CityId");
                        }
                        if (string.IsNullOrEmpty(item.StateId?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("StateId");
                        }
                        if (string.IsNullOrEmpty(item.CountryId?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("CountryId");
                        }
                    }
                }
                if (CustCorpData.FATCA != null)
                {
                    if (string.IsNullOrEmpty(CustCorpData.FATCA?.OtherIdentificationNumber?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("OtherIdentificationNumber");
                    }
                    if (string.IsNullOrEmpty(CustCorpData.FATCA?.TaxIdentificationNumber?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("TaxIdentificationNumber");
                    }
                    if (string.IsNullOrEmpty(CustCorpData.FATCA?.AddressType?.ToString()))
                    {
                        haserror = 1;
                        fields.Add("AddressType");
                    }
                    if (CustCorpData.FATCA?.Address != null)
                    {
                        if (string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.AddressLine1?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("FATCA AddressLine1");
                        }
                        if (string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.PinCodeMaster?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("FATCA PinCodeMaster");
                        }
                        if (string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.CityId?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("FATCA CityId");
                        }
                        if (string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.StateId?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("FATCA StateId");
                        }
                        if (string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.CountryId?.ToString()))
                        {
                            haserror = 1;
                            fields.Add("FATCA CountryId");
                        }
                    }
                }

                if (haserror == 1)
                {
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = string.Join(",", fields) + " fields should not be null!";
                    return csRtPrm;
                }

                /*********** General *********/
                if (CustCorpData.General != null)
                {
                    if (!string.IsNullOrEmpty(CustCorpData.General.LGCode?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_lccode", CustCorpData.General.LGCode?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.General.LCCode?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_lgcode", CustCorpData.General.LCCode?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.General.PhysicalAOFnumber?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_aofnumber", CustCorpData.General.PhysicalAOFnumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.General.Deferral?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_deferralcode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_deferralcode", CustCorpData.General.Deferral?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.General.Isdeferral?.ToString()))
                    {
                        CRMDDEmappingFields1.Add("eqs_isdeferral", (await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_isdeferral", CustCorpData.General.Isdeferral?.ToString()) == "1") ? true : false);
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.General.PurposeofCreation?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_purposeofcreationId@odata.bind", $"eqs_purposeofcreations({await this._commonFunc.getPurposeID(CustCorpData.General.PurposeofCreation?.ToString())})");
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.General.IsCommAddrSameAsOfficeAddr?.ToString()))
                    {
                        CRMDDEmappingFields1.Add("eqs_ispermaddrandcurraddrsame", Convert.ToBoolean(CustCorpData.General.IsCommAddrSameAsOfficeAddr?.ToString()));
                    }
                }

                if (!string.IsNullOrEmpty(Applicant_Data._eqs_leadsourceid_value?.ToString()))
                {
                    CRMDDEmappingFields.Add("eqs_leadsourceId@odata.bind", $"eqs_leadsources({Applicant_Data._eqs_leadsourceid_value?.ToString()})");
                }

                if (!string.IsNullOrEmpty(Applicant_Data["eqs_leadchannelnew"]?.ToString()))
                {
                    CRMDDEmappingFields.Add("eqs_leadchannelcode", Applicant_Data["eqs_leadchannelnew"]?.ToString());
                }

                CRMDDEmappingFields.Add("eqs_panform60code", Applicant_Data["eqs_panform60code"]?.ToString());
                /***********About Prospect * ********/
                if (CustCorpData.AboutProspect != null)
                {
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.PreferredLanguage?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_preferredlanguagecode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_preferredlanguagecode", CustCorpData.AboutProspect?.PreferredLanguage?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.EmailId?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_emailid", CustCorpData.AboutProspect?.EmailId?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.FaxNumber?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_faxnumber", CustCorpData.AboutProspect?.FaxNumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.CompanyTurnoverValue?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_companyturnovervalue", CustCorpData.AboutProspect?.CompanyTurnoverValue?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.NoofBranchesRegionaOffices?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_noofbranchesregionaloffices", CustCorpData.AboutProspect?.NoofBranchesRegionaOffices?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.CurrentEmployeeStrength?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_currentemployeestrength", CustCorpData.AboutProspect?.CurrentEmployeeStrength?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.AverageSalarytoEmployee?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_averagesalarytoemployee", CustCorpData.AboutProspect?.AverageSalarytoEmployee?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.MinimumSalarytoEmployee?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_minimumsalarytoemployee", CustCorpData.AboutProspect?.MinimumSalarytoEmployee?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.IndustryOthers?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_industryothers", CustCorpData.AboutProspect?.IndustryOthers?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.IsMFIcustomer?.ToString()))
                    {
                        CRMDDEmappingFields1.Add("eqs_ismficustomer", Convert.ToBoolean(CustCorpData.AboutProspect?.IsMFIcustomer?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.Program?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_programcode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_programcode", CustCorpData.AboutProspect?.Program?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.NPOPO?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_npopocode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_npopocode", CustCorpData.AboutProspect?.NPOPO?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.CompanyTurnover?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_companyturnovercode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_companyturnovercode", CustCorpData.AboutProspect?.CompanyTurnover?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.CompanyListed?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_iscompanylisted", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_iscompanylisted", CustCorpData.AboutProspect?.CompanyListed?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.LOB?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_lob", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_lob", CustCorpData.AboutProspect?.LOB?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.AOBO?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_aobocode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_aobocode", CustCorpData.AboutProspect?.AOBO?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.BusinessType?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_businesstypeId@odata.bind", $"eqs_businesstypes({await this._commonFunc.getBusinessTypeId(CustCorpData.AboutProspect?.BusinessType?.ToString())})");
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.AboutProspect?.Industry?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_industryId@odata.bind", $"eqs_businessnatures({await this._commonFunc.getIndustryId(CustCorpData.AboutProspect?.Industry?.ToString())})");
                    }
                }

                /*********** Identification Details *********/
                if (CustCorpData.IdentificationDetails != null)
                {
                    if (!string.IsNullOrEmpty(CustCorpData.IdentificationDetails?.GSTNumber?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_gstnumber", CustCorpData.IdentificationDetails?.GSTNumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.IdentificationDetails?.CKYCRefenceNumber?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_ckycnumber", CustCorpData.IdentificationDetails?.CKYCRefenceNumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.IdentificationDetails?.TANNumber?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_tannumber", CustCorpData.IdentificationDetails?.TANNumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.IdentificationDetails?.VATNumber?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_cstvatnumber", CustCorpData.IdentificationDetails?.VATNumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.IdentificationDetails?.RegisteredNumber?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_cinregisterednumber", CustCorpData.IdentificationDetails?.RegisteredNumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.IdentificationDetails?.KYCVerificationMode?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverificationmodecode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_kycverificationmodecode", CustCorpData.IdentificationDetails?.KYCVerificationMode?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.IdentificationDetails?.CKYCUpdatedDate?.ToString()))
                    {
                        dd = CustCorpData.IdentificationDetails?.CKYCUpdatedDate?.ToString()?.Substring(0, 2);
                        mm = CustCorpData.IdentificationDetails?.CKYCUpdatedDate?.ToString()?.Substring(3, 2);
                        yyyy = CustCorpData.IdentificationDetails?.CKYCUpdatedDate?.ToString()?.Substring(6, 4);
                        CRMDDEmappingFields.Add("eqs_ckycupdateddate", yyyy + "-" + mm + "-" + dd);
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.IdentificationDetails?.KYCVerificationDate?.ToString()))
                    {
                        dd = CustCorpData.IdentificationDetails?.KYCVerificationDate?.ToString()?.Substring(0, 2);
                        mm = CustCorpData.IdentificationDetails?.KYCVerificationDate?.ToString()?.Substring(3, 2);
                        yyyy = CustCorpData.IdentificationDetails?.KYCVerificationDate?.ToString()?.Substring(6, 4);
                        CRMDDEmappingFields.Add("eqs_verificationdate", yyyy + "-" + mm + "-" + dd);
                    }
                }

                /*********** RM Details *********/
                if (CustCorpData.RMDetails != null)
                {
                    if (!string.IsNullOrEmpty(CustCorpData.RMDetails?.ServiceRMCode?.ToString()))
                    {
                        var smEmployeeId = await this._commonFunc.getRMId(CustCorpData.RMDetails?.ServiceRMCode?.ToString());
                        if (!string.IsNullOrEmpty(smEmployeeId))
                        {
                            CRMDDEmappingFields.Add("eqs_servicermemployeeid@odata.bind", $"eqs_rmemployees({smEmployeeId})");
                        }
                        CRMDDEmappingFields.Add("eqs_servicermcode", CustCorpData.RMDetails?.ServiceRMCode?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.RMDetails?.ServiceRMName?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_servicermname", CustCorpData.RMDetails?.ServiceRMName?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.RMDetails?.ServiceRMRole?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_servicermrole", CustCorpData.RMDetails?.ServiceRMRole?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.RMDetails?.BusinessRMCode?.ToString()))
                    {
                        var bmEmployeeId = await this._commonFunc.getRMId(CustCorpData.RMDetails?.BusinessRMCode?.ToString());
                        if (!string.IsNullOrEmpty(bmEmployeeId))
                        {
                            CRMDDEmappingFields.Add("eqs_businessrmemployeeid@odata.bind", $"eqs_rmemployees({bmEmployeeId})");
                        }
                        CRMDDEmappingFields.Add("eqs_businessrmcode", CustCorpData.RMDetails?.BusinessRMCode?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.RMDetails?.BusinessRMName?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_businessrmname", CustCorpData.RMDetails?.BusinessRMName?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.RMDetails?.BusinessRMRole?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_businessrmrole", CustCorpData.RMDetails?.BusinessRMRole?.ToString());
                    }
                }

                /*********** Point Of Contact *********/
                if (CustCorpData.PointOfContact != null)
                {
                    if (!string.IsNullOrEmpty(CustCorpData.PointOfContact?.POCUCIC?.ToString()))
                    {
                        var customer_Dtl = await this._commonFunc.getContactData(CustCorpData.PointOfContact?.POCUCIC?.ToString());
                        if (customer_Dtl.Count > 0)
                        {
                            CRMDDEmappingFields.Add("eqs_contactpersonname", customer_Dtl[0]["fullname"]?.ToString());
                            CRMDDEmappingFields.Add("eqs_pocemailid", customer_Dtl[0]["emailaddress1"]?.ToString());
                            if (!string.IsNullOrEmpty(customer_Dtl[0]["mobilephone"]?.ToString()))
                            {
                                CRMDDEmappingFields.Add("eqs_pocphonenumber", customer_Dtl[0]["mobilephone"]?.ToString());
                            }
                            else
                            {
                                CRMDDEmappingFields.Add("eqs_pocphonenumber", CustCorpData.PointOfContact?.ContactMobilePhone?.ToString());
                            }
                            CRMDDEmappingFields.Add("eqs_pocucic", customer_Dtl[0]["eqs_customerid"]?.ToString());
                            CRMDDEmappingFields.Add("eqs_poclookupId@odata.bind", $"contacts({customer_Dtl[0]["contactid"]?.ToString()})");
                            CRMDDEmappingFields.Add("eqs_contactmobilenumber", CustCorpData.PointOfContact?.ContactMobilePhone?.ToString());
                        }
                        else
                        {
                            CRMDDEmappingFields.Add("eqs_contactpersonname", CustCorpData.PointOfContact?.ContactPersonName?.ToString());
                            CRMDDEmappingFields.Add("eqs_contactmobilenumber", CustCorpData.PointOfContact?.ContactMobilePhone?.ToString());
                            CRMDDEmappingFields.Add("eqs_pocphonenumber", CustCorpData.PointOfContact?.ContactMobilePhone?.ToString());
                        }
                    }
                }

                /*********** FATCA *********/
                if (CustCorpData.FATCA != null)
                {
                    if (!string.IsNullOrEmpty(CustCorpData.FATCA?.FinancialType?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_financialtype", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_financialtype", CustCorpData.FATCA?.FinancialType?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.FATCA?.CityofIncorporation?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_cityofincorporation", CustCorpData.FATCA?.CityofIncorporation?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.FATCA?.TaxResidentType?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_taxresidenttypecode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_taxresidenttypecode", CustCorpData.FATCA?.TaxResidentType?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.FATCA?.CountryofIncorporation?.ToString()))
                    {
                        var countryId = await this._commonFunc.getCountryID(CustCorpData.FATCA?.CountryofIncorporation?.ToString());
                        if (!string.IsNullOrEmpty(countryId))
                        {
                            CRMDDEmappingFields.Add("eqs_countryofincorporationId@odata.bind", $"eqs_countries({countryId})");
                        }
                    }
                }
                CRMDDEmappingFields.Add("eqs_createdfrompartnerchannel", "true");

                string postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);
                string postDataParametr1 = JsonConvert.SerializeObject(CRMDDEmappingFields1);
                postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                if (string.IsNullOrEmpty(this.DDEId))
                {
                    List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_ddecorporatecustomers()?$select=eqs_ddecorporatecustomerid", HttpMethod.Post, postDataParametr);
                    var ddeid = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_ddecorporatecustomerid");
                    this.DDEId = ddeid;
                    if (this.DDEId == null)
                    {
                        this._logger.LogError("createDigiCustLeadCorp", JsonConvert.SerializeObject(DDE_details), postDataParametr);
                        csRtPrm.ReturnCode = "CRM-ERROR-101";
                        csRtPrm.Message = "Can't create Final DDE.";
                        return csRtPrm;
                    }
                }
                else
                {
                    var response = await this._queryParser.HttpApiCall($"eqs_ddecorporatecustomers({this.DDEId})?", HttpMethod.Patch, postDataParametr);
                }

                /*********** KYCVerification *********/
                if (CustCorpData.KYCVerification != null)
                {
                    CRMDDEmappingFields = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(CustCorpData.KYCVerification?.EmpName?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverifiedempname", CustCorpData.KYCVerification?.EmpName?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.KYCVerification?.EmpID?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverifiedempid", CustCorpData.KYCVerification?.EmpID?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.KYCVerification?.EmpDesignation?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverifiedempdesignation", CustCorpData.KYCVerification?.EmpDesignation?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.KYCVerification?.EmpBranch?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverifiedempbranch", CustCorpData.KYCVerification?.EmpBranch?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.KYCVerification?.InstitutionName?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverifiedinstitutename", CustCorpData.KYCVerification?.InstitutionName?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.KYCVerification?.InstitutionCode?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_kycverifiedinstitutecode", CustCorpData.KYCVerification?.InstitutionCode?.ToString());
                    }

                    postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);
                    string KYCVerificationID = await this._commonFunc.getKYCVerificationID(this.DDEId, "Corp");

                    if (string.IsNullOrEmpty(KYCVerificationID))
                    {
                        List<JObject> KYC_details = await this._queryParser.HttpApiCall("eqs_kycverificationdetailses()?$select=eqs_kycverificationdetailsid", HttpMethod.Post, postDataParametr);
                        var KYC_id = CommonFunction.GetIdFromPostRespons201(KYC_details[0]["responsebody"], "eqs_kycverificationdetailsid");
                        CRMDDEmappingFields = new Dictionary<string, string>();
                        CRMDDEmappingFields.Add("eqs_KYCVerificationDetailId@odata.bind", $"eqs_kycverificationdetailses({KYC_id})");
                        postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);
                        var response = await this._queryParser.HttpApiCall($"eqs_ddeindividualcustomers({this.DDEId})?", HttpMethod.Patch, postDataParametr);
                    }
                    else
                    {
                        var response = await this._queryParser.HttpApiCall($"eqs_kycverificationdetailses({KYCVerificationID})?", HttpMethod.Patch, postDataParametr);
                    }
                }

                /*********** Address *********/
                if (CustCorpData.Address != null)
                {
                    Address_Id = new List<AddressResponse>();
                    foreach (var CustCorpDataItem in CustCorpData.Address)
                    {
                        string AddressUniqueId = string.Empty;
                        CRMDDEmappingFields = new Dictionary<string, string>();
                        CRMDDEmappingFields1 = new Dictionary<string, bool>();

                        if (!string.IsNullOrEmpty(CustCorpDataItem.AddressId?.ToString()))
                        {
                            AddressUniqueId = CustCorpDataItem.AddressId.ToString();
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.ApplicantAddress?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_name", CustCorpDataItem.ApplicantAddress?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.AddressType?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addresstypecode", await this._queryParser.getOptionSetTextToValue("eqs_leadaddress", "eqs_addresstypecode", CustCorpDataItem.AddressType?.ToString()));
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.AddressLine1?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addressline1", CustCorpDataItem.AddressLine1?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.AddressLine2?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addressline2", CustCorpDataItem.AddressLine2?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.AddressLine3?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addressline3", CustCorpDataItem.AddressLine3?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.AddressLine4?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addressline4", CustCorpDataItem.AddressLine4?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.FaxNumber?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_faxnumber", CustCorpDataItem.FaxNumber?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.OverseasMobileNumber?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_overseasmobilenumber", CustCorpDataItem.OverseasMobileNumber?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.PinCodeMaster?.ToString()))
                        {
                            var pincodeMasterId = await this._commonFunc.getPincodeID(CustCorpDataItem.PinCodeMaster?.ToString());
                            if (!string.IsNullOrEmpty(pincodeMasterId))
                            {
                                CRMDDEmappingFields.Add("eqs_pincodemaster@odata.bind", $"eqs_pincodes({pincodeMasterId})");
                            }
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.CityId?.ToString()))
                        {
                            var cityId = await this._commonFunc.getCityID(CustCorpDataItem.CityId?.ToString());
                            if (!string.IsNullOrEmpty(cityId))
                            {
                                CRMDDEmappingFields.Add("eqs_cityid@odata.bind", $"eqs_cities({cityId})");
                            }
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.StateId?.ToString()))
                        {
                            var stateId = await this._commonFunc.getStateID(CustCorpDataItem.StateId?.ToString());
                            if (!string.IsNullOrEmpty(stateId))
                            {
                                CRMDDEmappingFields.Add("eqs_stateid@odata.bind", $"eqs_states({stateId})");
                            }
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.CountryId?.ToString()))
                        {
                            var countryId = await this._commonFunc.getCountryID(CustCorpDataItem.CountryId?.ToString());
                            if (!string.IsNullOrEmpty(countryId))
                            {
                                CRMDDEmappingFields.Add("eqs_countryid@odata.bind", $"eqs_countries({countryId})");
                            }
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.District?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_district", CustCorpDataItem.District?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.PinCode?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_pincode", CustCorpDataItem.PinCode?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.POBox?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_pobox", CustCorpDataItem.POBox?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.Landmark?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_landmark", CustCorpDataItem.Landmark?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.LandlineNumber?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_landlinenumber", CustCorpDataItem.LandlineNumber?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.AlternateMobileNumber?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_alternatemobilenumber", CustCorpDataItem.AlternateMobileNumber?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpDataItem.LocalOverseas?.ToString()))
                        {
                            CRMDDEmappingFields1.Add("eqs_localoverseas", (await this._queryParser.getOptionSetTextToValue("eqs_leadaddress", "eqs_localoverseas", CustCorpDataItem.LocalOverseas?.ToString()) == "1") ? true : false);
                        }

                        CRMDDEmappingFields.Add("eqs_corporatedde@odata.bind", $"eqs_ddecorporatecustomers({this.DDEId})");

                        postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);
                        postDataParametr1 = JsonConvert.SerializeObject(CRMDDEmappingFields1);
                        postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                        this.AddressID = await this._commonFunc.getAddressID(this.DDEId, AddressUniqueId, "corp");

                        if (string.IsNullOrEmpty(this.AddressID))
                        {
                            List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_leadaddresses()?$select=eqs_leadaddressid,eqs_applicantaddressid", HttpMethod.Post, postDataParametr);
                            AddressUniqueId = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_applicantaddressid");
                        }
                        else
                        {
                            var response = await this._queryParser.HttpApiCall($"eqs_leadaddresses({this.AddressID})?", HttpMethod.Patch, postDataParametr);
                        }
                        Address_Id.Add(new AddressResponse(CustCorpDataItem.AddressType.ToString(), AddressUniqueId));
                    }
                }

                /*********** FATCA Link *********/
                if (CustCorpData.FATCA != null)
                {
                    CRMDDEmappingFields = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(CustCorpData.FATCA?.CountryCode?.ToString()))
                    {
                        var fatcaCountry = await this._commonFunc.getCountryID(CustCorpData.FATCA?.CountryCode?.ToString());
                        if (!string.IsNullOrEmpty(fatcaCountry))
                        {
                            CRMDDEmappingFields.Add("eqs_countryid@odata.bind", $"eqs_countries({fatcaCountry})");
                        }
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.FATCA?.OtherIdentificationNumber?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_otheridentificationnumber", CustCorpData.FATCA?.OtherIdentificationNumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.FATCA?.TaxIdentificationNumber?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_taxidentificationnumber", CustCorpData.FATCA?.TaxIdentificationNumber?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.FATCA?.AddressType?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_addresstype", await this._queryParser.getOptionSetTextToValue("eqs_customerfactcaother", "eqs_addresstype", CustCorpData.FATCA?.AddressType?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.FATCA?.TypeofNonFinancialEntity?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_typeofnonfinancialentitycode", await this._queryParser.getOptionSetTextToValue("eqs_customerfactcaother", "eqs_typeofnonfinancialentitycode", CustCorpData.FATCA?.TypeofNonFinancialEntity?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.FATCA?.ListingType?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_listingtypecode", await this._queryParser.getOptionSetTextToValue("eqs_customerfactcaother", "eqs_listingtypecode", CustCorpData.FATCA?.ListingType?.ToString()));
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.FATCA?.NameofStockExchange?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_nameofstockexchange", CustCorpData.FATCA?.NameofStockExchange?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.FATCA?.NameofListingCompany?.ToString()))
                    {
                        CRMDDEmappingFields.Add("eqs_nameoflistingcompany", CustCorpData.FATCA?.NameofListingCompany?.ToString());
                    }
                    if (!string.IsNullOrEmpty(CustCorpData.FATCA?.TypeofNonFinancialEntity?.ToString()) && CustCorpData.FATCA?.TypeofNonFinancialEntity?.ToString() == "Passive")
                    {
                        CRMDDEmappingFields.Add("eqs_nameofnonfinancialentity", CustCorpData.FATCA?.NameofNonFinancialEntity?.ToString());

                        dd = CustCorpData.FATCA?.CustomerDOB?.ToString()?.Substring(0, 2);
                        mm = CustCorpData.FATCA?.CustomerDOB?.ToString()?.Substring(3, 2);
                        yyyy = CustCorpData.FATCA?.CustomerDOB?.ToString()?.Substring(6, 4);
                        CRMDDEmappingFields.Add("eqs_customerdob", yyyy + "-" + mm + "-" + dd);

                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.BeneficiaryInterest?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_beneficiaryinterest", CustCorpData.FATCA?.BeneficiaryInterest?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.TaxIdentificationNumberNF?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_taxidentificationnumbernf", CustCorpData.FATCA?.TaxIdentificationNumberNF?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.OtherIdentificationNumberNF?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_otheridentificationnumbernf", CustCorpData.FATCA?.OtherIdentificationNumberNF?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.CustomerUCIC?.ToString()))
                        {
                            var countryCodeId = await this._commonFunc.getContactData(CustCorpData.FATCA?.CustomerUCIC?.ToString());

                            CRMDDEmappingFields.Add("eqs_customerucic", CustCorpData.FATCA?.CustomerUCIC?.ToString());
                            if (countryCodeId.Count > 0)
                            {
                                CRMDDEmappingFields.Add("eqs_customerlookup@odata.bind", $"contacts({countryCodeId[0]["contactid"]?.ToString()})");
                                CRMDDEmappingFields.Add("eqs_customername", countryCodeId[0]["fullname"]?.ToString());
                            }
                        }
                        var countryId = await this._commonFunc.getCountryID(CustCorpData.FATCA?.CountryofTaxResidency?.ToString());
                        if (!string.IsNullOrEmpty(countryId))
                        {
                            CRMDDEmappingFields.Add("eqs_countryoftaxresidencyid@odata.bind", $"eqs_countries({countryId})");
                        }
                        CRMDDEmappingFields.Add("eqs_panofnonfinancialentity", CustCorpData.FATCA?.PanofNonFinancialEntity?.ToString());
                        CRMDDEmappingFields.Add("eqs_nfoccupationtype", await this._queryParser.getOptionSetTextToValue("eqs_customerfactcaother", "eqs_nfoccupationtype", CustCorpData.FATCA?.NFOccupationType?.ToString()));

                    }
                    CRMDDEmappingFields.Add("eqs_ddecorporatecustomerid@odata.bind", $"eqs_ddecorporatecustomers({this.DDEId})");

                    postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);

                    this.FatcaId = await this._commonFunc.getFatcaID(this.DDEId, "corp");
                    string Fatca_Id = "";
                    if (string.IsNullOrEmpty(this.FatcaId))
                    {
                        List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_customerfactcaothers()?$select=eqs_name", HttpMethod.Post, postDataParametr);
                        var fatcaid = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_customerfactcaotherid");
                        csRtPrm.FATCAID = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_name");
                        this.FatcaId = fatcaid;
                    }
                    else
                    {
                        var response = await this._queryParser.HttpApiCall($"eqs_customerfactcaothers({this.FatcaId})?$select=eqs_name", HttpMethod.Patch, postDataParametr);
                        csRtPrm.FATCAID = CommonFunction.GetIdFromPostRespons201(response[0]["responsebody"], "eqs_name");
                    }

                    if (CustCorpData.FATCA?.AddressType?.ToString()?.Contains("Fatca Other Address") && CustCorpData.FATCA?.Address != null)
                    {
                        string AddressUniqueId = string.Empty;
                        CRMDDEmappingFields = new Dictionary<string, string>();
                        CRMDDEmappingFields1 = new Dictionary<string, bool>();

                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.FATCAAddressID?.ToString()))
                        {
                            AddressUniqueId = CustCorpData.FATCA.Address.FATCAAddressID.ToString();
                        }

                        CRMDDEmappingFields.Add("eqs_name", Fatca_Id + " - " + CustCorpData.FATCA?.AddressType?.ToString());

                        CRMDDEmappingFields.Add("eqs_addresstypecode", await this._queryParser.getOptionSetTextToValue("eqs_leadaddress", "eqs_addresstypecode", CustCorpData.FATCA?.AddressType?.ToString()));
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.AddressLine1?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addressline1", CustCorpData.FATCA?.Address?.AddressLine1?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.AddressLine2?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addressline2", CustCorpData.FATCA?.Address?.AddressLine2?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.AddressLine3?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addressline3", CustCorpData.FATCA?.Address?.AddressLine3?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.AddressLine4?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_addressline4", CustCorpData.FATCA?.Address?.AddressLine4?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.FaxNumber?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_faxnumber", CustCorpData.FATCA?.Address?.FaxNumber?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.OverseasMobileNumber?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_overseasmobilenumber", CustCorpData.FATCA?.Address?.OverseasMobileNumber?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.PinCodeMaster?.ToString()))
                        {
                            var fatcaPinCode = await this._commonFunc.getPincodeID(CustCorpData.FATCA?.Address?.PinCodeMaster?.ToString());
                            if (!string.IsNullOrEmpty(fatcaPinCode))
                            {
                                CRMDDEmappingFields.Add("eqs_pincodemaster@odata.bind", $"eqs_pincodes({fatcaPinCode})");
                            }
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.CityId?.ToString()))
                        {
                            var fatcaCity = await this._commonFunc.getCityID(CustCorpData.FATCA?.Address?.CityId?.ToString());
                            if (!string.IsNullOrEmpty(fatcaCity))
                            {
                                CRMDDEmappingFields.Add("eqs_cityid@odata.bind", $"eqs_cities({fatcaCity})");
                            }
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.District?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_district", CustCorpData.FATCA?.Address?.District?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.StateId?.ToString()))
                        {
                            var fatcaState = await this._commonFunc.getStateID(CustCorpData.FATCA?.Address?.StateId?.ToString());
                            if (!string.IsNullOrEmpty(fatcaState))
                            {
                                CRMDDEmappingFields.Add("eqs_stateid@odata.bind", $"eqs_states({fatcaState})");
                            }
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.CountryId?.ToString()))
                        {
                            var fatcaAddCountry = await this._commonFunc.getCountryID(CustCorpData.FATCA?.Address?.CountryId?.ToString());
                            if (!string.IsNullOrEmpty(fatcaAddCountry))
                            {
                                CRMDDEmappingFields.Add("eqs_countryid@odata.bind", $"eqs_countries({fatcaAddCountry})");
                            }
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.PinCode?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_pincode", CustCorpData.FATCA?.Address?.PinCode?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.POBox?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_pobox", CustCorpData.FATCA?.Address?.POBox?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.Landmark?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_landmark", CustCorpData.FATCA?.Address?.Landmark?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.LandlineNumber?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_landlinenumber", CustCorpData.FATCA?.Address?.LandlineNumber?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.AlternateMobileNumber?.ToString()))
                        {
                            CRMDDEmappingFields.Add("eqs_alternatemobilenumber", CustCorpData.FATCA?.Address?.AlternateMobileNumber?.ToString());
                        }
                        if (!string.IsNullOrEmpty(CustCorpData.FATCA?.Address?.LocalOverseas?.ToString()))
                        {
                            CRMDDEmappingFields1.Add("eqs_localoverseas", (await this._queryParser.getOptionSetTextToValue("eqs_leadaddress", "eqs_localoverseas", CustCorpData.FATCA?.Address?.LocalOverseas?.ToString()) == "1") ? true : false);
                        }


                        CRMDDEmappingFields.Add("eqs_applicantfatca@odata.bind", $"eqs_customerfactcaothers({this.FatcaId})");

                        postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);
                        postDataParametr1 = JsonConvert.SerializeObject(CRMDDEmappingFields1);
                        postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                        this.FatcaAddressID = await this._commonFunc.getFatcaAddressID(this.FatcaId, AddressUniqueId);

                        if (string.IsNullOrEmpty(this.FatcaAddressID))
                        {
                            List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_leadaddresses()?$select=eqs_leadaddressid,eqs_applicantaddressid", HttpMethod.Post, postDataParametr);
                            AddressUniqueId = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_applicantaddressid");
                        }
                        else
                        {
                            var response = await this._queryParser.HttpApiCall($"eqs_leadaddresses({this.FatcaAddressID})?", HttpMethod.Patch, postDataParametr);
                        }
                        Address_Id.Add(new AddressResponse("FATCA Other Address", AddressUniqueId));
                    }
                }
                /*********** BO *********/
                if (CustCorpData.BODetails != null)
                {
                    this.BO_Id = new List<BOResponse>();
                    foreach (var CustCorpDataItem in CustCorpData.BODetails)
                    {
                        CRMDDEmappingFields = new Dictionary<string, string>();
                        CRMDDEmappingFields.Add("eqs_name", CustCorpDataItem.BOName?.ToString());
                        var customer_Dtl = await this._commonFunc.getContactData(CustCorpDataItem.BOUCIC?.ToString());
                        if (CustCorpDataItem.BOUCIC.ToString() != "" && customer_Dtl.Count < 1)
                        {
                            this._logger.LogInformation("createDigiCustLeadCorp", "BOUCIC is incorrect.");
                            csRtPrm.ReturnCode = "CRM-ERROR-102";
                            csRtPrm.Message = "BOUCIC is incorrect.";
                            return csRtPrm;
                        }
                        else
                        {
                            CRMDDEmappingFields.Add("eqs_boexistingcustomer@odata.bind", $"contacts({customer_Dtl[0]["contactid"]?.ToString()})");
                            CRMDDEmappingFields.Add("eqs_boucic", customer_Dtl[0]["eqs_customerid"]?.ToString());
                            CRMDDEmappingFields["eqs_name"] = customer_Dtl[0]["fullname"]?.ToString();
                        }

                        CRMDDEmappingFields.Add("eqs_botypecode", await this._queryParser.getOptionSetTextToValue("eqs_customerbo", "eqs_botypecode", CustCorpDataItem.BOType?.ToString()));
                        CRMDDEmappingFields.Add("eqs_bolistingtypecode", await this._queryParser.getOptionSetTextToValue("eqs_customerbo", "eqs_bolistingtypecode", CustCorpDataItem.BOListingType?.ToString()));
                        CRMDDEmappingFields.Add("eqs_bolistingdetails", CustCorpDataItem.BOListingDetails?.ToString());
                        CRMDDEmappingFields.Add("eqs_holding", (Convert.ToInt32(CustCorpDataItem.Holding?.ToString()) > 100) ? "100" : CustCorpDataItem.Holding?.ToString());
                        CRMDDEmappingFields.Add("eqs_ddecorporatecustomerid@odata.bind", $"eqs_ddeindividualcustomers({this.DDEId})");

                        postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);
                        string bo_id = string.Empty;
                        string BOUniqueId = string.Empty;
                        if (!string.IsNullOrEmpty(CustCorpDataItem.BOID?.ToString()))
                        {
                            BOUniqueId = CustCorpDataItem.BOID.ToString();
                            bo_id = await this._commonFunc.getBOId(BOUniqueId);
                        }

                        if (string.IsNullOrEmpty(bo_id))
                        {
                            List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_customerbos()?$select=eqs_boid", HttpMethod.Post, postDataParametr);
                            BOUniqueId = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_boid");
                        }
                        else
                        {
                            var response = await this._queryParser.HttpApiCall($"eqs_customerbos({bo_id})?", HttpMethod.Patch, postDataParametr);
                        }

                        this.BO_Id.Add(new BOResponse(BOUniqueId, CustCorpDataItem.BOListingType?.ToString(), CustCorpDataItem.BOName?.ToString()));
                    }
                }

                /*********** CP *********/
                if (CustCorpData.CPDetails != null)
                {
                    this.CP_Id = new List<CPResponse>();
                    foreach (var CustCorpDataItem in CustCorpData.CPDetails)
                    {
                        CRMDDEmappingFields = new Dictionary<string, string>();
                        CRMDDEmappingFields.Add("eqs_name", CustCorpDataItem.NameofCP?.ToString());
                        var customer_Dtl = await this._commonFunc.getContactData(CustCorpDataItem.CPUCIC?.ToString());
                        if (CustCorpDataItem.CPUCIC?.ToString() != "" && customer_Dtl.Count < 1)
                        {
                            this._logger.LogInformation("createDigiCustLeadCorp", "CPUCIC is incorrect.");
                            csRtPrm.ReturnCode = "CRM-ERROR-102";
                            csRtPrm.Message = "CPUCIC is incorrect.";
                            return csRtPrm;
                        }
                        else
                        {
                            CRMDDEmappingFields.Add("eqs_cpexistingcustomerid@odata.bind", $"contacts({customer_Dtl[0]["contactid"]?.ToString()})");
                            CRMDDEmappingFields.Add("eqs_cpucic", customer_Dtl[0]["eqs_customerid"]?.ToString());
                            CRMDDEmappingFields["eqs_name"] = customer_Dtl[0]["fullname"]?.ToString();
                        }

                        CRMDDEmappingFields.Add("eqs_holding", (Convert.ToInt32(CustCorpDataItem.Holding?.ToString()) > 100) ? "100" : CustCorpDataItem.Holding?.ToString());
                        CRMDDEmappingFields.Add("eqs_ddecorporatecustomerid@odata.bind", $"eqs_ddeindividualcustomers({this.DDEId})");
                        postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);

                        string cp_id = string.Empty;
                        string CPUniqueId = string.Empty;
                        if (CustCorpDataItem.CPID?.ToString() != "")
                        {
                            CPUniqueId = CustCorpDataItem.CPID.ToString();
                            cp_id = await this._commonFunc.getCPId(CPUniqueId);
                        }
                        if (string.IsNullOrEmpty(cp_id))
                        {
                            List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_customercps()?$select=eqs_cpid", HttpMethod.Post, postDataParametr);
                            CPUniqueId = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_cpid");
                        }
                        else
                        {
                            var response = await this._queryParser.HttpApiCall($"eqs_customercps({cp_id})?", HttpMethod.Patch, postDataParametr);
                        }
                        this.CP_Id.Add(new CPResponse(CPUniqueId, CustCorpDataItem.NameofCP?.ToString()));
                    }
                }

                csRtPrm.Address = Address_Id;
                csRtPrm.BOID = BO_Id;
                csRtPrm.CPID = CP_Id;

                if (!string.IsNullOrEmpty(this.DDEId))
                {
                    CRMDDEupdateTriggerFields.Add("eqs_triggervalidation", true);
                    postDataParametr = JsonConvert.SerializeObject(CRMDDEupdateTriggerFields);
                    var response = await this._queryParser.HttpApiCall($"eqs_ddecorporatecustomers({this.DDEId})?", HttpMethod.Patch, postDataParametr);
                }

                csRtPrm.Message = OutputMSG.Case_Success;
                csRtPrm.ReturnCode = "CRM-SUCCESS";
            }
            catch (Exception ex)
            {
                this._logger.LogError("createDigiCustLeadCorp", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-101";
                csRtPrm.Message = "Exception occured while creating DDE Final"; ;
            }
            return csRtPrm;
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
                string BankCode = inputData.req_root.header.cde?.ToString();
                this.Bank_Code = BankCode;
                string xmlData = await this._queryParser.PayloadDecryption(EncryptedData?.ToString(), BankCode, APIname);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlData);
                string xpath = "PIDBlock/payload";
                var nodes = xmlDoc.SelectSingleNode(xpath);
                foreach (XmlNode childrenNode in nodes)
                {
                    JObject rejusetJson1 = (JObject)JsonConvert.DeserializeObject(childrenNode.Value);

                    dynamic payload = rejusetJson1[APIname];

                    this.appkey = payload.msgHdr.authInfo.token?.ToString();
                    this.Transaction_ID = payload.msgHdr.conversationID?.ToString();
                    this.Channel_ID = payload.msgHdr.channelID?.ToString();
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