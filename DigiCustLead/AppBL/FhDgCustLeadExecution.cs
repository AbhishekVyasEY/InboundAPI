namespace DigiCustLead
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
    using System.Reflection.Emit;
    using System.Reflection.Metadata;
    using System.Reflection;
    using static Azure.Core.HttpHeader;
    using System.Diagnostics.Metrics;
    using System.Data;

    public class FhDgCustLeadExecution : IFhDgCustLeadExecution
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
        public string AddressID, FatcaId;

        private ICommonFunction _commonFunc;

        public FhDgCustLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {

            this._logger = logger;

            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;


        }


        public async Task<FetchCustLeadReturn> ValidateFetchLeadDetls(dynamic RequestData)
        {
            FetchCustLeadReturn ldRtPrm = new FetchCustLeadReturn();

            try
            {
                RequestData = await this.getRequestData(RequestData, "FetchDigiCustLead");

                if (!string.IsNullOrEmpty(this.appkey) && this.appkey != "" && checkappkey(this.appkey, "FetchDigiCustLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(this.Transaction_ID) && !string.IsNullOrEmpty(this.Channel_ID))
                    {
                        if (!string.IsNullOrEmpty(RequestData.AccountapplicantID.ToString()))
                        {
                            ldRtPrm = await this.getDDEFinal(RequestData.AccountapplicantID.ToString());
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateFetchLeadDetls", "Account applicant ID is incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "Account applicant ID is incorrect";
                            return ldRtPrm;
                        }


                    }
                    else
                    {
                        this._logger.LogInformation("ValidateFetchLeadDetls", "Transaction_ID or  Channel_ID is incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or  Channel_ID is incorrect.";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateFetchLeadDetls", "appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Appkey is incorrect";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateFetchLeadDetls", ex.Message);
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

        public async Task<FetchCustLeadReturn> getDDEFinal(string AccountapplicantID)
        {
            FetchCustLeadReturn csRtPrm = new FetchCustLeadReturn();
            try
            {
                var Applicant_Data = await this._commonFunc.getApplicentData(AccountapplicantID);
                if (Applicant_Data.Count > 0)
                {
                    string EntityType = await this._commonFunc.getEntityName(Applicant_Data[0]["_eqs_entitytypeid_value"].ToString());
                    if (EntityType == "Individual")
                    {
                        csRtPrm.individual = await this.getDigiCustLeadIndv(Applicant_Data[0]["eqs_accountapplicantid"].ToString());
                        if (csRtPrm.individual == null)
                        {
                            this._logger.LogInformation("getDDEFinal", "Final-DDE Entry not available.");
                            csRtPrm.ReturnCode = "CRM-ERROR-102";
                            csRtPrm.Message = "Final-DDE Entry not available.";
                        }
                        else
                        {
                            csRtPrm.Message = OutputMSG.Case_Success;
                            csRtPrm.ReturnCode = "CRM-SUCCESS";
                        }
                    }
                    else
                    {
                        csRtPrm.corporate = await this.getDigiCustLeadCorp(Applicant_Data[0]["eqs_accountapplicantid"].ToString());
                        if (csRtPrm.corporate == null)
                        {
                            this._logger.LogInformation("getDDEFinal", "Final-DDE Entry not available.");
                            csRtPrm.ReturnCode = "CRM-ERROR-102";
                            csRtPrm.Message = "Final-DDE Entry not available.";
                        }
                        else
                        {
                            csRtPrm.Message = OutputMSG.Case_Success;
                            csRtPrm.ReturnCode = "CRM-SUCCESS";
                        }
                    }
                }
                else
                {
                    this._logger.LogInformation("getDDEFinal", "No Applicant data found.");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = "No Applicant data found.";
                }

            }
            catch (Exception ex)
            {
                this._logger.LogError("getDDEFinal", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-101";
                csRtPrm.Message = OutputMSG.Resource_n_Found;
            }
            return csRtPrm;
        }


        public async Task<Individual> getDigiCustLeadIndv(string applicentID)
        {
            Individual csRtPrm = new Individual();

            try
            {
                dynamic DDEDetails = await this._commonFunc.getDDEFinalIndvDetail(applicentID);
                if (DDEDetails.Count > 0)
                {
                    string dd, mm, yyyy;
                    /*********** General *********/
                    Generalind general = new Generalind();
                    general.DDEId = DDEDetails[0].eqs_dataentryoperator.ToString();
                    general.AccountApplicant = await this._commonFunc.getAccountapplicantName(DDEDetails[0]._eqs_accountapplicantid_value.ToString());
                    general.EntityType = await this._commonFunc.getEntityName(DDEDetails[0]._eqs_entitytypeid_value.ToString());
                    general.SubEntityType = await this._commonFunc.getSubentitytypeText(DDEDetails[0]._eqs_subentitytypeid_value.ToString());
                    general.IsPrimaryHolder = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_isprimaryholder", DDEDetails[0].eqs_isprimaryholder.ToString());
                    general.ResidencyType = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_residencytypecode", DDEDetails[0].eqs_residencytypecode.ToString());
                    general.LGCode = DDEDetails[0].eqs_lgcode.ToString();
                    general.LCCode = DDEDetails[0].eqs_lccode.ToString();

                    general.SourceBranch = await this._commonFunc.getBranchText(DDEDetails[0]._eqs_sourcebranchid_value.ToString());
                    general.CustomerspreferredBranch = await this._commonFunc.getBranchText(DDEDetails[0]._eqs_custpreferredbranchid_value.ToString());

                    general.RelationshiptoPrimaryHolder = await this._commonFunc.getRelationshipText(DDEDetails[0]._eqs_relationshiptoprimaryholder_value.ToString());
                    general.AccountRelationship = await this._commonFunc.getAccRelationshipText(DDEDetails[0]._eqs_accountrelationship_value.ToString());

                    general.PhysicalAOFnumber = DDEDetails[0].eqs_physicalaornumber.ToString();
                    general.IsMFICustomer = DDEDetails[0].eqs_ismficustomer.ToString();
                    general.IsDeferral = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_deferralcode", DDEDetails[0].eqs_deferralcode.ToString());
                    general.PurposeofCreation = await this._commonFunc.getPurposeText(DDEDetails[0]._eqs_purposeofcreationid_value.ToString());
                    general.InstaKitCustomerId = DDEDetails[0].eqs_instakitcustomerid.ToString();

                    general.Deferral = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_isdeferral", DDEDetails[0].eqs_isdeferral.ToString());
                    general.CustomerIdCreated = DDEDetails[0].eqs_customeridcreated.ToString();
                    general.DataValidated = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_datavalidated", DDEDetails[0].eqs_datavalidated.ToString());
                    general.LeadNumber = DDEDetails[0].eqs_leadnumber.ToString();
                    general.LeadChannel = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_sourcingchannelcode", DDEDetails[0].eqs_sourcingchannelcode.ToString());
                    general.SourceofLead = await this._commonFunc.getLeadsourceName(DDEDetails[0]._eqs_leadsourceid_value.ToString());
                    general.Leadcreatedon = DDEDetails[0].eqs_leadcreatedon.ToString();
                    general.Leadcreatedby = await this._commonFunc.getSystemuserName(DDEDetails[0]._eqs_leadcreatedby_value.ToString());

                    csRtPrm.general = general;

                    /*********** Prospect Details *********/
                    ProspectDetails prospectDetails = new ProspectDetails();

                    prospectDetails.Gender = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_gendercode", DDEDetails[0].eqs_gendercode.ToString());
                    prospectDetails.ShortName = DDEDetails[0].eqs_shortname.ToString();
                    prospectDetails.EmailID = DDEDetails[0].eqs_emailid.ToString();
                    prospectDetails.Nationality = await this._commonFunc.getCountryText(DDEDetails[0]._eqs_nationalityid_value.ToString());
                    prospectDetails.Countryofbirth = await this._commonFunc.getCountryText(DDEDetails[0]._eqs_countryofbirthid_value.ToString());

                    prospectDetails.FathersName = DDEDetails[0].eqs_fathername.ToString();
                    prospectDetails.MothersMaidenName = DDEDetails[0].eqs_mothermaidenname.ToString();
                    prospectDetails.SpouseName = DDEDetails[0].eqs_spousename.ToString();

                    prospectDetails.Country = await this._commonFunc.getCountryText(DDEDetails[0]._eqs_countryid_value.ToString());
                    prospectDetails.CityofBirth = DDEDetails[0].eqs_cityofbirth.ToString();
                    prospectDetails.Program = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_programcode", DDEDetails[0].eqs_programcode.ToString());
                    prospectDetails.Education = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_educationcode", DDEDetails[0].eqs_educationcode.ToString());
                    prospectDetails.MaritalStatus = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_maritalstatuscode", DDEDetails[0].eqs_maritalstatuscode.ToString());
                    prospectDetails.Profession = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_professioncode", DDEDetails[0].eqs_professioncode.ToString());
                    prospectDetails.AnnualIncomeBand = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_annualincomebandcode", DDEDetails[0].eqs_annualincomebandcode.ToString());
                    prospectDetails.EmployerType = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_employertypecode", DDEDetails[0].eqs_employertypecode.ToString());
                    prospectDetails.EmployerName = DDEDetails[0].eqs_empname.ToString();
                    prospectDetails.OfficePhone = DDEDetails[0].eqs_officephone.ToString();

                    prospectDetails.EstimatedAgriculturalIncome = DDEDetails[0].eqs_agriculturalincome.ToString();
                    prospectDetails.EstimatedNonAgriculturalIncome = DDEDetails[0].eqs_nonagriculturalincome.ToString();
                    prospectDetails.IsStaff = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_isstaffcode", DDEDetails[0].eqs_isstaffcode.ToString());
                    prospectDetails.EquitasStaffCode = DDEDetails[0].eqs_equitasstaffcode.ToString();
                    prospectDetails.Language = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_languagecode", DDEDetails[0].eqs_languagecode.ToString());
                    prospectDetails.PolitcallyExposedPerson = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_ispep", DDEDetails[0].eqs_ispep.ToString());
                    prospectDetails.LOBCode = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_lobcode", DDEDetails[0].eqs_lobcode.ToString());
                    prospectDetails.AOBusinessOperation = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_aobocode", DDEDetails[0].eqs_aobocode.ToString());

                    prospectDetails.Title = await this._commonFunc.getTitleText(DDEDetails[0]._eqs_titleid_value.ToString());
                    prospectDetails.Firstname = DDEDetails[0].eqs_firstname.ToString();
                    prospectDetails.MiddleName = DDEDetails[0].eqs_middlename.ToString();
                    prospectDetails.LastName = DDEDetails[0].eqs_lastname.ToString();
                    prospectDetails.DateofBirth = DDEDetails[0].eqs_dob.ToString();
                    prospectDetails.Age = DDEDetails[0].eqs_age.ToString();
                    prospectDetails.MobileNumber = DDEDetails[0].eqs_mobilenumber.ToString();
                    prospectDetails.IsPhysicallyChallenged = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_isphysicallychallenged", DDEDetails[0].eqs_isphysicallychallenged.ToString());
                    prospectDetails.WorkPlaceAddress = await this._commonFunc.getcorporatemasterText(DDEDetails[0]._eqs_corporatecompanyid_value.ToString());
                    prospectDetails.Designation = await this._commonFunc.getdesignationmasterText(DDEDetails[0]._eqs_designationid_value.ToString());
                    prospectDetails.LOBType = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_lob", DDEDetails[0].eqs_lob.ToString());

                    csRtPrm.prospectDetails = prospectDetails;


                    /*********** Identification Details *********/
                    IdentificationDetails identification = new IdentificationDetails();

                    identification.PassportNumber = DDEDetails[0].eqs_pannumber.ToString();
                    identification.CKYCreferenceNumber = DDEDetails[0].eqs_ckycreferencenumber.ToString();
                    identification.KYCVerificationMode = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_kycverificationmodecode", DDEDetails[0].eqs_kycverificationmodecode.ToString());
                    identification.VerificationDate = DDEDetails[0].eqs_verificationdate.ToString();

                    identification.PanForm60 = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_panform60code", DDEDetails[0].eqs_panform60code.ToString());
                    identification.PanNumber = DDEDetails[0].eqs_pannumber.ToString();
                    identification.AadharReference = DDEDetails[0].eqs_aadharreference.ToString();
                    identification.InternalPAN = DDEDetails[0].eqs_internalpan.ToString();
                    identification.PanAcknowledgementNumber = DDEDetails[0].eqs_panacknowledgementnumber.ToString();
                    identification.VoterID = DDEDetails[0].eqs_voterid.ToString();
                    identification.DrivinglicenseNumber = DDEDetails[0].eqs_drivinglicensenumber.ToString();

                    csRtPrm.identificationDetails = identification;

                    /*********** KYC Verification *********/
                    KYCVerification kYCVerification = new KYCVerification();
                    dynamic kycverificationDetail = await this._commonFunc.getkycverificationDetail(DDEDetails[0]._eqs_kycverificationdetailid_value.ToString());
                    if (kycverificationDetail.Count > 0)
                    {
                        kYCVerification.KYCVerificationID = kycverificationDetail[0].eqs_kycverificationid.ToString();
                        kYCVerification.EmpName = kycverificationDetail[0].eqs_name.ToString();
                        kYCVerification.EmpID = kycverificationDetail[0].eqs_kycverifiedempid.ToString();
                        kYCVerification.EmpDesignation = kycverificationDetail[0].eqs_kycverifiedempdesignation.ToString();
                        csRtPrm.kycverification = kYCVerification;
                    }

                    /*********** Address *********/


                    dynamic addressDetail = await this._commonFunc.getDDEFinalAddressDetail(DDEDetails[0].eqs_ddeindividualcustomerid.ToString(), "indv");
                    csRtPrm.address = new List<Address>();
                    foreach (var addressItem in addressDetail)
                    {
                        Address address = new Address();
                        address.AddressId = addressItem.eqs_name.ToString();
                        address.ApplicantAddress = addressItem.eqs_applicantaddressid.ToString();
                        address.AddressType = await this._queryParser.getOptionSetValuToText("eqs_leadaddress", "eqs_addresstypecode", addressItem.eqs_addresstypecode.ToString());
                        address.AddressLine1 = addressItem.eqs_addressline1.ToString();
                        address.AddressLine2 = addressItem.eqs_addressline2.ToString();
                        address.AddressLine3 = addressItem.eqs_addressline3.ToString();
                        address.AddressLine4 = addressItem.eqs_addressline4.ToString();
                        address.MobileNumber = addressItem.eqs_mobilenumber.ToString();
                        address.FaxNumber = addressItem.eqs_faxnumber.ToString();
                        address.OverseasMobileNumber = addressItem.eqs_overseasmobilenumber.ToString();

                        address.PinCodeMaster = addressItem.eqs_pincode.ToString();
                        address.City = await this._commonFunc.getCityText(addressItem._eqs_cityid_value.ToString());
                        address.District = addressItem.eqs_district.ToString();
                        address.State = await this._commonFunc.getStateText(addressItem._eqs_stateid_value.ToString());
                        address.Country = await this._commonFunc.getCountryText(addressItem._eqs_countryid_value.ToString());

                        address.PinCode = addressItem.eqs_zipcode.ToString();
                        address.POBox = addressItem.eqs_pobox.ToString();
                        address.Landmark = addressItem.eqs_landmark.ToString();
                        address.LandlineNumber = addressItem.eqs_landlinenumber.ToString();
                        address.AlternateMobileNumber = addressItem.eqs_alternatemobilenumber.ToString();
                        address.LocalOverseas = await this._queryParser.getOptionSetValuToText("eqs_leadaddress", "eqs_localoverseas", addressItem.eqs_localoverseas.ToString());

                        csRtPrm.address.Add(address);
                    }

                    /*********** RM Details *********/
                    RMDetails rm = new RMDetails();

                    rm.ServiceRMCode = DDEDetails[0].eqs_servicermcode.ToString();
                    rm.ServiceRMName = DDEDetails[0].eqs_servicermname.ToString();
                    rm.ServiceRMRole = DDEDetails[0].eqs_servicermrole.ToString();
                    rm.BusinessRMCode = DDEDetails[0].eqs_businessrmcode.ToString();
                    rm.BusinessRMName = DDEDetails[0].eqs_businessrmname.ToString();
                    rm.BusinessRMRole = DDEDetails[0].eqs_businessrmrole.ToString();

                    csRtPrm.RMDetails = rm;

                    /*********** FATCA *********/
                    FATCA fATCAobj = new FATCA();
                    dynamic fatcaDetail = await this._commonFunc.getDDEFinalFatcaDetail(DDEDetails[0].eqs_ddeindividualcustomerid.ToString(), "indv");
                    foreach (var fatcaitem in fatcaDetail)
                    {
                        fATCAobj.FATCAID = fatcaitem.eqs_name.ToString();
                        fATCAobj.TaxResident = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_taxresident", DDEDetails[0].eqs_taxresident.ToString());
                        fATCAobj.CityofBirth = DDEDetails[0].eqs_cityofbirth.ToString();
                        fATCAobj.FATCADeclaration = await this._queryParser.getOptionSetValuToText("eqs_customerfactcaother", "eqs_fatcadeclaration", fatcaitem.eqs_fatcadeclaration.ToString());


                        fATCAobj.Country = await this._commonFunc.getCountryText(fatcaDetail[0]._eqs_countryid_value.ToString());
                        fATCAobj.OtherIdentificationNumber = fatcaitem.eqs_otheridentificationnumber.ToString();
                        fATCAobj.TaxIdentificationNumber = fatcaitem.eqs_taxidentificationnumber.ToString();
                        fATCAobj.AddressType = await this._queryParser.getOptionSetValuToText("eqs_customerfactcaother", "eqs_addresstype", fatcaitem.eqs_addresstype.ToString());

                        addressDetail = await this._commonFunc.getFATCAAddress(fatcaitem.eqs_customerfactcaotherid.ToString());

                        if (addressDetail.Count > 0)
                        {
                            FATCAAddress fATCAAddress = new FATCAAddress();
                            fATCAAddress.FATCAAddressID = addressDetail[0].eqs_name.ToString();


                            fATCAAddress.AddressLine1 = addressDetail[0].eqs_addressline1.ToString();
                            fATCAAddress.AddressLine2 = addressDetail[0].eqs_addressline2.ToString();
                            fATCAAddress.AddressLine3 = addressDetail[0].eqs_addressline3.ToString();
                            fATCAAddress.AddressLine4 = addressDetail[0].eqs_addressline4.ToString();
                            fATCAAddress.MobileNumber = addressDetail[0].eqs_mobilenumber.ToString();
                            fATCAAddress.FaxNumber = addressDetail[0].eqs_faxnumber.ToString();
                            fATCAAddress.OverseasMobileNumber = addressDetail[0].eqs_overseasmobilenumber.ToString();

                            fATCAAddress.PinCodeMaster = addressDetail[0].eqs_pincode.ToString();
                            fATCAAddress.City = await this._commonFunc.getCityText(addressDetail[0]._eqs_cityid_value.ToString());
                            fATCAAddress.District = addressDetail[0].eqs_district.ToString();
                            fATCAAddress.State = await this._commonFunc.getStateText(addressDetail[0]._eqs_stateid_value.ToString());
                            fATCAAddress.Country = await this._commonFunc.getCountryText(addressDetail[0]._eqs_countryid_value.ToString());

                            fATCAAddress.PinCode = addressDetail[0].eqs_zipcode.ToString();
                            fATCAAddress.POBox = addressDetail[0].eqs_pobox.ToString();
                            fATCAAddress.Landmark = addressDetail[0].eqs_landmark.ToString();
                            fATCAAddress.LandlineNumber = addressDetail[0].eqs_landlinenumber.ToString();
                            fATCAAddress.AlternateMobileNumber = addressDetail[0].eqs_alternatemobilenumber.ToString();
                            fATCAAddress.LocalOverseas = await this._queryParser.getOptionSetValuToText("eqs_leadaddress", "eqs_localoverseas", addressDetail[0].eqs_localoverseas.ToString());

                            fATCAobj.Address = fATCAAddress;
                        }

                        csRtPrm.fatca = fATCAobj;
                    }

                    /*********** NRI Details *********/
                    NRIDetails nRIDetails = new NRIDetails();

                    nRIDetails.VisaType = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_visatypecode", DDEDetails[0].eqs_visatypecode.ToString());
                    nRIDetails.VisaIssuedDate = DDEDetails[0].eqs_visaissueddate.ToString();
                    nRIDetails.VisaExpiryDate = DDEDetails[0].eqs_visaexpirydate.ToString();
                    nRIDetails.KYCMode = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_kycmode", DDEDetails[0].eqs_kycmode.ToString());

                    nRIDetails.Seafarer = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_seafarer", DDEDetails[0].eqs_seafarer.ToString());
                    nRIDetails.VISAOCICDCNumber = DDEDetails[0].eqs_visanumber.ToString();
                    nRIDetails.TaxIdentificationNumber = DDEDetails[0].eqs_taxidentificationnumber.ToString();
                    nRIDetails.ResidenceStatus = DDEDetails[0].eqs_residencestatuscode.ToString();
                    nRIDetails.OtherIdentificationNumber = DDEDetails[0].eqs_othertaxnumber.ToString();
                    nRIDetails.SMSOTPMobilepreference = DDEDetails[0].eqs_mobilepreference.ToString();
                    nRIDetails.PassportIssuedAt = DDEDetails[0].eqs_passportissuedat.ToString();
                    nRIDetails.PassportIssuedDate = DDEDetails[0].eqs_passportissuedate.ToString();
                    nRIDetails.TaxType = DDEDetails[0].eqs_taxtype.ToString();
                    nRIDetails.PassportExpiryDate = DDEDetails[0].eqs_passportexpirydate.ToString();

                    csRtPrm.nridetails = nRIDetails;
                }
                else
                {
                    csRtPrm = null;
                }

            }
            catch (Exception ex)
            {
                this._logger.LogError("getDigiCustLeadIndv", ex.Message);
                throw;
            }



            return csRtPrm;
        }

        public async Task<Corporate> getDigiCustLeadCorp(string applicentID)
        {
            Corporate csRtPrm = new Corporate();

            try
            {
                dynamic DDEDetails = await this._commonFunc.getDDEFinalCorpDetail(applicentID);
                if (DDEDetails.Count > 0)
                {
                    string dd, mm, yyyy;
                    /*********** General *********/
                    Generalcorp general = new Generalcorp();

                    general.Name = DDEDetails[0].eqs_dataentryoperator.ToString();
                    general.EntityType = await this._commonFunc.getEntityName(DDEDetails[0]._eqs_entitytypeid_value.ToString());
                    general.SubEntityType = await this._commonFunc.getSubentitytypeText(DDEDetails[0]._eqs_subentitytypeid_value.ToString());
                    general.BankName = await this._commonFunc.getBankName(DDEDetails[0]._eqs_banknameid_value.ToString());

                    general.SourceBranchTerritory = await this._commonFunc.getBranchText(DDEDetails[0]._eqs_sourcebranchterritoryid_value.ToString());
                    general.CustomerspreferredBranch = await this._commonFunc.getBranchText(DDEDetails[0]._eqs_preferredhomebranchid_value.ToString());
                    general.LGCode = DDEDetails[0].eqs_lgcode.ToString();
                    general.LCCode = DDEDetails[0].eqs_lccode.ToString();
                    general.DataEntryStage = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_dataentrystage", DDEDetails[0].eqs_dataentrystage.ToString());
                    general.AccountApplicant = DDEDetails[0]._eqs_accountapplicantid_value.ToString();
                    general.IsPrimaryHolder = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_isprimaryholder", DDEDetails[0].eqs_isprimaryholder.ToString());
                    general.PhysicalAOFnumber = DDEDetails[0].eqs_aofnumber.ToString();
                    general.IsDeferral = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_isdeferral", DDEDetails[0].eqs_isdeferral.ToString());
                    general.PurposeofCreation = await this._commonFunc.getPurposeText(DDEDetails[0]._eqs_purposeofcreationid_value.ToString());
                    general.CustomerIdCreated = DDEDetails[0].eqs_customeridcreated.ToString();
                    general.IsCommAddrRgstOfficeAddrSame = DDEDetails[0].eqs_ispermaddrandcurraddrsame.ToString();

                    csRtPrm.general = general;

                    /***********About Prospect *********/
                    ProspectDetailscorp prospectDetails = new ProspectDetailscorp();

                    prospectDetails.aboutprospect = new Aboutprospect();

                    prospectDetails.aboutprospect.Title = await this._commonFunc.getTitleText(DDEDetails[0]._eqs_titleid_value.ToString());
                    prospectDetails.aboutprospect.CompanyName1 = DDEDetails[0].eqs_companyname1.ToString();
                    prospectDetails.aboutprospect.CompanyNamePart2 = DDEDetails[0].eqs_companyname2.ToString();
                    prospectDetails.aboutprospect.CompanyNamePart3 = DDEDetails[0].eqs_companyname3.ToString();
                    prospectDetails.aboutprospect.ShortName = DDEDetails[0].eqs_shortname.ToString();
                    prospectDetails.aboutprospect.DateofIncorporation = DDEDetails[0].eqs_dateofincorporation.ToString();
                    prospectDetails.aboutprospect.PreferredLanguage = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_preferredlanguagecode", DDEDetails[0].eqs_preferredlanguagecode.ToString());
                    prospectDetails.aboutprospect.IsCompanyListed = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_iscompanylisted", DDEDetails[0].eqs_iscompanylisted.ToString());
                    prospectDetails.aboutprospect.EmailId = DDEDetails[0].eqs_emailid.ToString();
                    prospectDetails.aboutprospect.FaxNumber = DDEDetails[0].eqs_faxnumber.ToString();
                    prospectDetails.aboutprospect.Program = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_programcode", DDEDetails[0].eqs_programcode.ToString());
                    prospectDetails.aboutprospect.NPOPO = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_npopocode", DDEDetails[0].eqs_npopocode.ToString());
                    prospectDetails.aboutprospect.IsAMFIcustomer = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_ismficustomer", DDEDetails[0].eqs_ismficustomer.ToString());
                    prospectDetails.aboutprospect.UCICCreatedOn = DDEDetails[0].eqs_uciccreatedon.ToString();
                    prospectDetails.aboutprospect.LOBCode = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_lob", DDEDetails[0].eqs_lob.ToString());
                    prospectDetails.aboutprospect.AOBusinessOperation = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_aobocode", DDEDetails[0].eqs_aobocode.ToString());

                    prospectDetails.aboutbusiness = new Aboutbusiness();

                    prospectDetails.aboutbusiness.BusinessType = await this._commonFunc.getBusinessTypeText(DDEDetails[0]._eqs_businesstypeid_value.ToString());
                    prospectDetails.aboutbusiness.Industry = await this._commonFunc.getIndustryText(DDEDetails[0]._eqs_industryid_value.ToString());
                    prospectDetails.aboutbusiness.IndustryOthers = DDEDetails[0].eqs_industryothers.ToString();
                    prospectDetails.aboutbusiness.CompanyTurnover = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_companyturnovercode", DDEDetails[0].eqs_companyturnovercode.ToString());
                    prospectDetails.aboutbusiness.CompanyTurnoverValue = DDEDetails[0].eqs_companyturnovervalue.ToString();
                    prospectDetails.aboutbusiness.NoofBranchesRegionalOffices = DDEDetails[0].eqs_noofbranchesregionaloffices.ToString();
                    prospectDetails.aboutbusiness.CurrentEmployeeStrength = DDEDetails[0].eqs_currentemployeestrength.ToString();
                    prospectDetails.aboutbusiness.AverageSalarytoEmployee = DDEDetails[0].eqs_averagesalarytoemployee.ToString();
                    prospectDetails.aboutbusiness.MinimumSalarytoEmployee = DDEDetails[0].eqs_minimumsalarytoemployee.ToString();


                    csRtPrm.prospectDetails = prospectDetails;



                    /*********** Identification Details *********/
                    IdentificationDetailscorp identification = new IdentificationDetailscorp();


                    identification.POCPanForm60 = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_pocpanform60code", DDEDetails[0].eqs_pocpanform60code.ToString());
                    identification.POCPANNumber = DDEDetails[0].eqs_pocpannumber.ToString();
                    identification.TANNumber = DDEDetails[0].eqs_tannumber.ToString();
                    identification.CSTVATnumber = DDEDetails[0].eqs_cstvatnumber.ToString();
                    identification.GSTNumber = DDEDetails[0].eqs_gstnumber.ToString();
                    identification.CKYCRefenceNumber = DDEDetails[0].eqs_ckycnumber.ToString();
                    identification.CINRegisteredNumber = DDEDetails[0].eqs_cinregisterednumber.ToString();
                    identification.CKYCUpdatedDate = DDEDetails[0].eqs_ckycupdateddate.ToString();
                    identification.KYCVerificationMode = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_kycverificationmodecode", DDEDetails[0].eqs_kycverificationmodecode.ToString());
                    identification.KYCVerificationDate = DDEDetails[0].eqs_verificationdate.ToString();


                    csRtPrm.identificationDetails = identification;

                    /*********** KYC Verification *********/
                    KYCVerification kYCVerification = new KYCVerification();
                    dynamic kycverificationDetail = await this._commonFunc.getkycverificationDetail(DDEDetails[0]._eqs_kycverificationdetailid_value.ToString());
                    if (kycverificationDetail.Count > 0)
                    {
                        kYCVerification.KYCVerificationID = kycverificationDetail[0].eqs_kycverificationid.ToString();
                        kYCVerification.EmpName = kycverificationDetail[0].eqs_name.ToString();
                        kYCVerification.EmpID = kycverificationDetail[0].eqs_kycverifiedempid.ToString();
                        kYCVerification.EmpDesignation = kycverificationDetail[0].eqs_kycverifiedempdesignation.ToString();
                        csRtPrm.kycverification = kYCVerification;
                    }

                    /*********** Address *********/


                    dynamic addressDetail = await this._commonFunc.getDDEFinalAddressDetail(DDEDetails[0].eqs_ddecorporatecustomerid.ToString(), "corp");
                    csRtPrm.address = new List<Address>();
                    foreach (var addressItem in addressDetail)
                    {
                        Address address = new Address();
                        address.AddressId = addressItem.eqs_name.ToString();
                        address.ApplicantAddress = addressItem.eqs_applicantaddressid.ToString();
                        address.AddressType = await this._queryParser.getOptionSetValuToText("eqs_leadaddress", "eqs_addresstypecode", addressItem.eqs_addresstypecode.ToString());
                        address.AddressLine1 = addressItem.eqs_addressline1.ToString();
                        address.AddressLine2 = addressItem.eqs_addressline2.ToString();
                        address.AddressLine3 = addressItem.eqs_addressline3.ToString();
                        address.AddressLine4 = addressItem.eqs_addressline4.ToString();
                        address.MobileNumber = addressItem.eqs_mobilenumber.ToString();
                        address.FaxNumber = addressItem.eqs_faxnumber.ToString();
                        address.OverseasMobileNumber = addressItem.eqs_overseasmobilenumber.ToString();

                        address.PinCodeMaster = addressItem.eqs_pincode.ToString();
                        address.City = await this._commonFunc.getCityText(addressItem._eqs_cityid_value.ToString());
                        address.District = addressItem.eqs_district.ToString();
                        address.State = await this._commonFunc.getStateText(addressItem._eqs_stateid_value.ToString());
                        address.Country = await this._commonFunc.getCountryText(addressItem._eqs_countryid_value.ToString());

                        address.PinCode = addressItem.eqs_zipcode.ToString();
                        address.POBox = addressItem.eqs_pobox.ToString();
                        address.Landmark = addressItem.eqs_landmark.ToString();
                        address.LandlineNumber = addressItem.eqs_landlinenumber.ToString();
                        address.AlternateMobileNumber = addressItem.eqs_alternatemobilenumber.ToString();
                        address.LocalOverseas = await this._queryParser.getOptionSetValuToText("eqs_leadaddress", "eqs_localoverseas", addressItem.eqs_localoverseas.ToString());

                        csRtPrm.address.Add(address);
                    }



                    /*********** RM Details *********/

                    RMDetails rm = new RMDetails();

                    rm.ServiceRMCode = DDEDetails[0].eqs_servicermcode.ToString();
                    rm.ServiceRMName = DDEDetails[0].eqs_servicermname.ToString();
                    rm.ServiceRMRole = DDEDetails[0].eqs_servicermrole.ToString();
                    rm.BusinessRMCode = DDEDetails[0].eqs_businessrmcode.ToString();
                    rm.BusinessRMName = DDEDetails[0].eqs_businessrmname.ToString();
                    rm.BusinessRMRole = DDEDetails[0].eqs_businessrmrole.ToString();

                    csRtPrm.RMDetails = rm;



                    /*********** FATCA *********/
                    FATCA fATCAobj = new FATCA();
                    dynamic fatcaDetail = await this._commonFunc.getDDEFinalFatcaDetail(DDEDetails[0].eqs_ddecorporatecustomerid.ToString(), "corp");
                    foreach (var fatcaitem in fatcaDetail)
                    {
                        fATCAobj.FATCAID = fatcaitem.eqs_name.ToString();
                        fATCAobj.TaxResident = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_taxresident", DDEDetails[0].eqs_taxresident.ToString());
                        fATCAobj.CityofBirth = DDEDetails[0].eqs_cityofbirth.ToString();
                        fATCAobj.FATCADeclaration = await this._queryParser.getOptionSetValuToText("eqs_customerfactcaother", "eqs_fatcadeclaration", fatcaitem.eqs_fatcadeclaration.ToString());


                        fATCAobj.Country = await this._commonFunc.getCountryText(fatcaDetail[0]._eqs_countryid_value.ToString());
                        fATCAobj.OtherIdentificationNumber = fatcaitem.eqs_otheridentificationnumber.ToString();
                        fATCAobj.TaxIdentificationNumber = fatcaitem.eqs_taxidentificationnumber.ToString();
                        fATCAobj.AddressType = await this._queryParser.getOptionSetValuToText("eqs_customerfactcaother", "eqs_addresstype", fatcaitem.eqs_addresstype.ToString());

                        addressDetail = await this._commonFunc.getFATCAAddress(fatcaitem.eqs_customerfactcaotherid.ToString());

                        if (addressDetail.Count > 0)
                        {
                            FATCAAddress fATCAAddress = new FATCAAddress();
                            fATCAAddress.FATCAAddressID = addressDetail[0].eqs_name.ToString();


                            fATCAAddress.AddressLine1 = addressDetail[0].eqs_addressline1.ToString();
                            fATCAAddress.AddressLine2 = addressDetail[0].eqs_addressline2.ToString();
                            fATCAAddress.AddressLine3 = addressDetail[0].eqs_addressline3.ToString();
                            fATCAAddress.AddressLine4 = addressDetail[0].eqs_addressline4.ToString();
                            fATCAAddress.MobileNumber = addressDetail[0].eqs_mobilenumber.ToString();
                            fATCAAddress.FaxNumber = addressDetail[0].eqs_faxnumber.ToString();
                            fATCAAddress.OverseasMobileNumber = addressDetail[0].eqs_overseasmobilenumber.ToString();

                            fATCAAddress.PinCodeMaster = addressDetail[0].eqs_pincode.ToString();
                            fATCAAddress.City = await this._commonFunc.getCityText(addressDetail[0]._eqs_cityid_value.ToString());
                            fATCAAddress.District = addressDetail[0].eqs_district.ToString();
                            fATCAAddress.State = await this._commonFunc.getStateText(addressDetail[0]._eqs_stateid_value.ToString());
                            fATCAAddress.Country = await this._commonFunc.getCountryText(addressDetail[0]._eqs_countryid_value.ToString());

                            fATCAAddress.PinCode = addressDetail[0].eqs_zipcode.ToString();
                            fATCAAddress.POBox = addressDetail[0].eqs_pobox.ToString();
                            fATCAAddress.Landmark = addressDetail[0].eqs_landmark.ToString();
                            fATCAAddress.LandlineNumber = addressDetail[0].eqs_landlinenumber.ToString();
                            fATCAAddress.AlternateMobileNumber = addressDetail[0].eqs_alternatemobilenumber.ToString();
                            fATCAAddress.LocalOverseas = await this._queryParser.getOptionSetValuToText("eqs_leadaddress", "eqs_localoverseas", addressDetail[0].eqs_localoverseas.ToString());

                            fATCAobj.Address = fATCAAddress;
                        }

                        csRtPrm.fatca = fATCAobj;
                    }




                    /*********** Point Of Contact *********/
                    PointOfContact pointOfContact = new PointOfContact();

                    pointOfContact.ContactPersonName = DDEDetails[0].eqs_contactpersonname.ToString();
                    pointOfContact.POClookUp = await this._commonFunc.getCustomerText(DDEDetails[0]._eqs_poclookupid_value.ToString());
                    pointOfContact.POCEmailId = DDEDetails[0].eqs_pocemailid.ToString();
                    pointOfContact.ContactMobilePhone = DDEDetails[0].eqs_contactmobilenumber.ToString();
                    pointOfContact.POCUCIC = DDEDetails[0].eqs_pocucic.ToString();
                    pointOfContact.POCPhoneNumber = DDEDetails[0].eqs_pocphonenumber.ToString();

                    csRtPrm.pointOfContact = pointOfContact;




                    /*********** CP Details *********/

                    dynamic cpDetail = await this._commonFunc.getDDEFinalCPDetail(DDEDetails[0].eqs_ddecorporatecustomerid.ToString());

                    foreach (var cpitem in cpDetail)
                    {
                        CPDetails cp = new CPDetails();
                        cp.NameofCP = cpDetail[0].eqs_name.ToString();
                        cp.CPUCIC = cpDetail[0].eqs_cpucic.ToString();
                        cp.Holding = cpDetail[0].eqs_holding.ToString();

                        csRtPrm.cpDetails.Add(cp);
                    }


                    /*********** BO Details *********/


                    dynamic boDetail = await this._commonFunc.getDDEFinalBODetail(DDEDetails[0].eqs_ddecorporatecustomerid.ToString());

                    foreach (var boitem in boDetail)
                    {
                        BODetails bo = new BODetails();
                        bo.BOType = await this._queryParser.getOptionSetValuToText("eqs_customerbo", "eqs_botypecode", boitem.eqs_botypecode.ToString());
                        bo.BOListingType = await this._queryParser.getOptionSetValuToText("eqs_customerbo", "eqs_bolistingtypecode", boitem.eqs_bolistingtypecode.ToString());

                        bo.BOUCIC = boitem.eqs_boucic.ToString();
                        bo.BOName = boitem.eqs_name.ToString();
                        bo.BOListingDetails = boitem.eqs_bolistingdetails.ToString();
                        bo.Holding = boitem.eqs_holding.ToString();

                        csRtPrm.boDetails.Add(bo);
                    }
                }
                else
                {
                    csRtPrm = null;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDigiCustLeadCorp", ex.Message);
                throw;
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