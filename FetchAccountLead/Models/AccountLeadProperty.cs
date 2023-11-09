﻿using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace FetchAccountLead
{
       
   
    
    public class FtAccountLeadReturn
    {
        public General General { get; set; }
        public AdditionalDetails AdditionalDetails { get; set; }
        public FDRDDetails FDRDDetails { get; set; }
        public DirectBanking DirectBanking { get; set; }
        public Nominee Nominee { get; set; }
        public string ReturnCode { get; set; } 
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class General
    {
        public string AccountLeadId { get; set; }
        public string AccountNumber { get; set; }
        public string ApplicationDate { get; set; }
        public string ProductCategory { get; set; }
        public string Product { get; set; }
        public string InstaKit { get; set; }
        public string InstaKitAccountNumber { get; set; }
        public string AccountOpeningBranch { get; set; }
        public string PurposeofOpeningAccount { get; set; }
        public string PurposeOfOpeningAccountOthers { get; set; }
        public string ModeofOperation { get; set; }
        public string AccountOwnership { get; set; }
        public string InitialDepositMode { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionID { get; set; }
        public string Fundingchequebank { get; set; }
        public string FundingchequeNumber { get; set; }
        public string SourceBranchTerritory { get; set; }
        public string SweepFacility { get; set; }
        public string LGCode { get; set; }
        public string LCCode { get; set; }
    }

    public class AdditionalDetails
    {
        public string AccountTitle { get; set; }
        public string LOBType { get; set; }
        public string AOBO { get; set; }
        public string ModeofOperationRemarks { get; set; }
        public string SourceofFund { get; set; }
        public string OtherSourceoffund { get; set; }
        public string PredefinedAccountNumber { get; set; }
        public string CurrencyofDeposit { get; set; }
        public string DepositAmount { get; set; }
    }

    public class FDRDDetails
    {
        public ProductDetails ProductDetails { get; set; }
        public DepositDetails DepositDetails { get; set; }
        public InterestPayoutDetails InterestPayoutDetails { get; set; }
        public MaturityInstructionDetails MaturityInstructionDetails { get; set; }
    }

    public class ProductDetails
    {
        public string Product { get; set; }
        public string MinimumDepositAmount { get; set; }
        public string MaximumDepositAmount { get; set; }
        public string CompoundingFrequency { get; set; }
        public int MinimumTenureMonths { get; set; }
        public int MaximumTenureMonths { get; set; }
        public string PayoutFrequency { get; set; }
        public int MinimumTenureDays { get; set; }
        public int MaximumTenureDays { get; set; }
        public string InterestCompoundFrequency { get; set; }
    }

    public class DepositDetails
    {
        public string DepositVariancePercentage { get; set; }
        public int DepositAmount { get; set; }
        public string FromESFBAccountNumber { get; set; }
        public string FromESFBGLAccount { get; set; }
        public string CurrencyofDeposit { get; set; }
        public string tenureInDays { get; set; }
        public bool SpecialInterestRateRequired { get; set; }
        public int SpecialInterestRate { get; set; }
        public string SpecialInterestRequestID { get; set; }
        public string BranchCodeGL { get; set; }
        public string FDValueDate { get; set; }
        public int TenureInMonths { get; set; }
        public bool WaivedOffTDS { get; set; }
        public string TransactionID { get; set; }
    }

    public class InterestPayoutDetails
    {
        public string interestPayoutMode { get; set; }
        public string iPayToESFBAccountNo { get; set; }
        public string iPayToOtherBankAccountNo { get; set; }
        public string BeneficiaryAccountType { get; set; }
        public string iPayToOtherBankBenificiaryName { get; set; }
        public string iPayToOtherBankIFSC { get; set; }
        public string iPayToOtherBankName { get; set; }
        public string iPayToOtherBankBranch { get; set; }
        public string iPayToOtherBankMICR { get; set; }
        public string iPByDDPOIssuerCode { get; set; }
        public string iPByDDPOPayeeName { get; set; }
    }
    public class MaturityInstructionDetails
    {
        public string MaturityInstruction { get; set; }
        public string MaturityPayoutMode { get; set; }
        public string MICreditToESFBAccountNo { get; set; }
        public string MICreditToOtherBankAccountNo { get; set; }
        public string MICreditToOtherBankAccountType { get; set; }
        public string BeneficiaryName { get; set; }
        public string MICreditToOtherBankIFSC { get; set; }
        public string MICreditToOtherBankName { get; set; }
        public string MICreditToOtherBankBranch { get; set; }
        public string MICreditToOtherBankMICR { get; set; }
        public string MIByDDPOIssuerCode { get; set; }
        public string MIByDDPOPayeeName { get; set; }
    }

    public class DirectBanking
    {
        public string IssuedInstaKit { get; set; }
        public string ChequeBookRequired { get; set; }
        public string NumberChequeBook { get; set; }
        public string NumberofChequeLeaves { get; set; }
        public string DispatchMode { get; set; }
        public List<Preference> Preferences { get; set; }

    }

    public class Preference
    {
        public string PreferenceID { get; set; }
        public string UCIC { get; set; }
        public string DebitCardFlag { get; set; }
        public string NameonCard { get; set; }
        public string DebitCardID { get; set; }
        public bool SMS { get; set; }
        public bool NetBanking { get; set; }
        public bool MobileBanking { get; set; }
        public bool EmailStatement { get; set; }
        public bool InternationalDCLimitAct { get; set; }
        public bool physicalStatement { get; set; }
        public string mobileBankingNumber { get; set; }
        public List<CustomerDeliverable> customerDeliverables { get; set; }
    }

    public class CustomerDeliverable
    {
        public string mappedCustomer { get; set; }
        public string chequeBook { get; set; }
        public string noOfChequeBooks { get; set; }
        public string noOfChequeLeaves { get; set; }
        public string despatchMode { get; set; }
        public string debitCard { get; set; }
        public string debitCardType { get; set; }
        public string nameOnCard { get; set; }
        public string iKIT { get; set; }
        public string iKITAccountNo { get; set; }
        public string iKITCustomerID { get; set; }
        public string predefinedAccountNo { get; set; }
        public string predefinedUCIC { get; set; }
    }

    public class Nominee
    {
        public string nomineeUCICIfCustomer { get; set; }
        public string name { get; set; }
        public string NomineeRelationship { get; set; }
        public string DOB { get; set; }
        public string NomineeDisplayName { get; set; }
        public bool AddresssameasProspects { get; set; }
        public string email { get; set; }
        public string mobile { get; set; }
        public string Landline { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Pin { get; set; }
        public string CityCode { get; set; }
        public string District { get; set; }
        public string CountryCode { get; set; }
        public string State { get; set; }
        public string PO { get; set; }
        public string Landmark { get; set; }
        public Guardian Guardian { get; set; }
    }

    public class Guardian
    {
        public string Name { get; set; }
        public string RelationshipToMinor { get; set; }
        public string GuardianUCIC { get; set; }
        public string GuardianMobile { get; set; }
        public string GuardianLandline { get; set; }
        public string GuardianAddress1 { get; set; }
        public string GuardianAddress2 { get; set; }
        public string GuardianAddress3 { get; set; }
        public string GuardianPin { get; set; }
        public string GuardianCityCode { get; set; }
        public string GuardianDistrict { get; set; }
        public string GuardianCountryCode { get; set; }
        public string GuardianState { get; set; }
        public string GuardianPO { get; set; }
        public string GuardianLandmark { get; set; }
    }


   



    public class FetchCustomerDtlReturn
    {
        
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }




}