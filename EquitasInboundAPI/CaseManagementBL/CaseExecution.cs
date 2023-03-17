using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;

namespace EquitasInboundAPI
{
    public class CaseExecution
    {

        public ILogger _logger;
        public IKeyVaultService _keyVault;
        public IQueryParser _queryParser;
        Dictionary<string, int> Channel = new Dictionary<string, int>();
        Dictionary<string, int> LeadStatus = new Dictionary<string, int>();
        private CommonFunction commonFunc;

        public CaseExecution(ILogger logger, IQueryParser queryParser)
        {
            
            this._keyVault = new KeyVaultService();
            this._logger = logger;           
            this._queryParser = queryParser;
            this.commonFunc = new CommonFunction(queryParser);

           

        }


        public async Task<LeadReturnParam> ValidateLeade(dynamic LeadData)
        {
            LeadReturnParam ldRtPrm = new LeadReturnParam();
            try
            {
                string channel = LeadData.ChannelType;

                if (!string.IsNullOrEmpty(channel) && channel != "")
                {
                    int ValidationError = 0;

                   
                    if (string.Equals(LeadData.ChannelType.ToString(), "ChatBot"))
                    {
                        if (LeadData.MobileNumber == null || string.IsNullOrEmpty(LeadData.MobileNumber.ToString()) || LeadData.MobileNumber.ToString() == "")
                        {
                            ValidationError = 1;
                        }
                        
                        if (LeadData.Transcript == null || string.IsNullOrEmpty(LeadData.Transcript.ToString()) || LeadData.Transcript.ToString() == "")
                        {
                            ValidationError = 1;
                        }

                    }
                   

                    

                    if (ValidationError == 1)
                    {
                        ldRtPrm.IsError = 1;
                        ldRtPrm.ErrorMessage = Error.Incorrect_Input;                        
                    }


                    ldRtPrm = await this.CreateCase(LeadData);


                }
                else
                {
                    ldRtPrm.IsError = 1;
                    ldRtPrm.ErrorMessage = Error.Incorrect_Input;
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }

        

        public async Task<LeadReturnParam> CreateCase(dynamic LeadData)
        {
            LeadReturnParam ldRtPrm = new LeadReturnParam();
            LeadMsdProperty lead_Property = new LeadMsdProperty();
            LeadProperty ldProperty = new LeadProperty();
            Dictionary<string,string> odatab= new Dictionary<string,string>();
            string postDataParametr, postDataParametr1;
            List<JObject> Lead_details = new List<JObject>();

            lead_Property.leadsourcecode = this.Channel[LeadData.ChannelType.ToString()];

            if (string.Equals(LeadData.ChannelType.ToString(), "ESFBWebsite"))
            {
                var productDetails  = await this.commonFunc.getProductId(LeadData.ProductCode.ToString());
                ldProperty.ProductId = productDetails["ProductId"];
                ldProperty.Businesscategoryid = productDetails["businesscategoryid"];
                ldProperty.Productcategoryid = productDetails["productcategory"];
                lead_Property.eqs_crmproductcategorycode = productDetails["crmproductcategorycode"];

                if (ldProperty.ProductId!="")
                {
                    lead_Property.firstname = LeadData.FirstName;
                    lead_Property.lastname = LeadData.LastName;
                    lead_Property.mobilephone = LeadData.MobileNumber;
                    lead_Property.emailaddress1 = LeadData.Email;
                    odatab.Add("eqs_productid@odata.bind", $"eqs_products({ldProperty.ProductId})");
                    odatab.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({ldProperty.Productcategoryid})");
                    odatab.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({ldProperty.Businesscategoryid})");

                    ldProperty.CityId = await this.commonFunc.getCityId(LeadData.CityName.ToString());
                    if(ldProperty.CityId!= null && ldProperty.CityId!="")
                        odatab.Add("eqs_cityid@odata.bind", $"eqs_cities({ldProperty.CityId})");

                    ldProperty.BranchId = await this.commonFunc.getBranchId(LeadData.BranchCode.ToString());
                    if (ldProperty.BranchId != null && ldProperty.BranchId != "")
                        odatab.Add("eqs_branchid@odata.bind", $"eqs_branchs({ldProperty.BranchId})");

                    postDataParametr = JsonConvert.SerializeObject(lead_Property);
                    postDataParametr1 = JsonConvert.SerializeObject(odatab);

                    postDataParametr = await this.commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                    Lead_details = await this._queryParser.HttpApiCall("leads", HttpMethod.Post, postDataParametr);
                }
                else
                {
                    ldRtPrm.IsError = 1;
                    ldRtPrm.ErrorMessage = Error.Resource_n_Found;
                }
                
            }
            else if (string.Equals(LeadData.ChannelType.ToString(), "MobileBanking") || string.Equals(LeadData.ChannelType.ToString(), "InternetBanking"))
            {
                var productDetails = await this.commonFunc.getProductId(LeadData.ProductCode.ToString());
                ldProperty.ProductId = productDetails["ProductId"];
                ldProperty.Businesscategoryid = productDetails["businesscategoryid"];
                ldProperty.Productcategoryid = productDetails["productcategory"];
                lead_Property.eqs_crmproductcategorycode = productDetails["crmproductcategorycode"];

                if (ldProperty.ProductId != "")
                {
                    lead_Property.firstname = LeadData.FirstName;
                    lead_Property.lastname = LeadData.LastName;
                    lead_Property.mobilephone = LeadData.MobileNumber;
                    lead_Property.emailaddress1 = LeadData.Email;
                    odatab.Add("eqs_productid@odata.bind", $"eqs_products({ldProperty.ProductId})");
                    odatab.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({ldProperty.Productcategoryid})");
                    odatab.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({ldProperty.Businesscategoryid})");

                    ldProperty.CityId = await this.commonFunc.getCityId(LeadData.CityName.ToString());
                    if (ldProperty.CityId != null && ldProperty.CityId != "")
                        odatab.Add("eqs_cityid@odata.bind", $"eqs_cities({ldProperty.CityId})");

                    ldProperty.BranchId = await this.commonFunc.getBranchId(LeadData.BranchCode.ToString());
                    if (ldProperty.BranchId != null && ldProperty.BranchId != "")
                        odatab.Add("eqs_branchid@odata.bind", $"eqs_branchs({ldProperty.BranchId})");
                  
                    ldProperty.ETBCustomerID = await this.commonFunc.getCustomerId(LeadData.CustomerID.ToString());
                    if (ldProperty.ETBCustomerID != null && ldProperty.ETBCustomerID != "")
                        odatab.Add("eqs_etbcustomerid@odata.bind", $"contacts({ldProperty.ETBCustomerID})");

                    postDataParametr = JsonConvert.SerializeObject(lead_Property);
                    postDataParametr1 = JsonConvert.SerializeObject(odatab);

                    postDataParametr = await this.commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                    Lead_details = await this._queryParser.HttpApiCall("leads", HttpMethod.Post, postDataParametr);
                }
                else
                {
                    ldRtPrm.IsError = 1;
                    ldRtPrm.ErrorMessage = Error.Resource_n_Found;
                }
            }
            else if (string.Equals(LeadData.ChannelType.ToString(), "ChatBot"))
            {
                lead_Property.mobilephone = LeadData.MobileNumber;
                lead_Property.emailaddress1 = LeadData.Email;
                lead_Property.description = LeadData.Transcript;
                postDataParametr = JsonConvert.SerializeObject(lead_Property);
                Lead_details = await this._queryParser.HttpApiCall("leads", HttpMethod.Post, postDataParametr);

            }
            else if (string.Equals(LeadData.ChannelType.ToString(), "Email"))
            {                
                lead_Property.emailaddress1 = LeadData.Email;
                lead_Property.description = LeadData.EmailBody;
                postDataParametr = JsonConvert.SerializeObject(lead_Property);
                Lead_details = await this._queryParser.HttpApiCall("leads", HttpMethod.Post, postDataParametr);
            }

            else if (string.Equals(LeadData.ChannelType.ToString(), "Selfie"))
            {
                lead_Property.mobilephone = LeadData.MobileNumber;

                if (LeadData.ProductCode != null && LeadData.ProductCode.ToString() != "")
                {
                    var productDetails = await this.commonFunc.getProductId(LeadData.ProductCode.ToString());
                    ldProperty.ProductId = productDetails["ProductId"];
                    ldProperty.Businesscategoryid = productDetails["businesscategoryid"];
                    ldProperty.Productcategoryid = productDetails["productcategory"];
                    lead_Property.eqs_crmproductcategorycode = productDetails["crmproductcategorycode"];

                    odatab.Add("eqs_productid@odata.bind", $"eqs_products({ldProperty.ProductId})");
                    odatab.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({ldProperty.Productcategoryid})");
                    odatab.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({ldProperty.Businesscategoryid})");
                }

                if (LeadData.FirstName != null && LeadData.FirstName.ToString() != "")
                {
                    lead_Property.firstname = LeadData.FirstName;
                }
                if (LeadData.LastName != null && LeadData.LastName.ToString() != "")
                {
                    lead_Property.lastname = LeadData.LastName;
                }
                if (LeadData.Email != null && LeadData.Email.ToString() != "")
                {
                    lead_Property.emailaddress1 = LeadData.Email;
                }
                
                if (LeadData.SelfeJourneyStatus != null && LeadData.SelfeJourneyStatus.ToString() != "")
                {
                   // lead_Property.eqs_SelfieJourneyStatus = LeadData.SelfeJourneyStatus;
                }
               

                ldProperty.CityId = await this.commonFunc.getCityId(LeadData.CityName.ToString());
                if (ldProperty.CityId != null && ldProperty.CityId != "")
                    odatab.Add("eqs_cityid@odata.bind", $"eqs_cities({ldProperty.CityId})");

                ldProperty.BranchId = await this.commonFunc.getBranchId(LeadData.BranchCode.ToString());
                if (ldProperty.BranchId != null && ldProperty.BranchId != "")
                    odatab.Add("eqs_branchid@odata.bind", $"eqs_branchs({ldProperty.BranchId})");

                ldProperty.ETBCustomerID = await this.commonFunc.getCustomerId(LeadData.CustomerID.ToString());
                if (ldProperty.ETBCustomerID != null && ldProperty.ETBCustomerID != "")
                    odatab.Add("eqs_etbcustomerid@odata.bind", $"contacts({ldProperty.ETBCustomerID})");

                postDataParametr = JsonConvert.SerializeObject(lead_Property);
                postDataParametr1 = JsonConvert.SerializeObject(odatab);

                postDataParametr = await this.commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                if (LeadData.LeadID != null && LeadData.LeadID.ToString() != "")
                {
                    ldRtPrm.LeadID = LeadData.LeadID.ToString();
                    Lead_details = await this._queryParser.HttpApiCall($"leads({ldRtPrm.LeadID})", HttpMethod.Patch, postDataParametr);
                }
                else
                {                    
                    Lead_details = await this._queryParser.HttpApiCall("leads", HttpMethod.Post, postDataParametr);
                }
               
                
            }




            if (Lead_details.Count >0 )
            {
                dynamic respons_code = Lead_details[0];
                if (respons_code.responsecode == 204)
                {
                    ldRtPrm.LeadID = CommonFunction.GetIdFromPostRespons(respons_code.responsebody.ToString());
                    ldRtPrm.InfoMessage = Error.Lead_Success;
                }
                else
                {
                    ldRtPrm.IsError = 1;
                    ldRtPrm.ErrorMessage = Error.Resource_n_Found;
                }
            }
            else
            {
                ldRtPrm.IsError = 1;
                ldRtPrm.ErrorMessage = Error.Resource_n_Found;
            }


            return ldRtPrm;
        }



      
    }
}
