namespace FetchAccountLead.Controllers
{

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System.Net.Http;
    using Newtonsoft.Json;
    using System.Text;
    using System.Net.Http.Headers;
    using Newtonsoft.Json.Linq;
    using System.Reflection.PortableExecutable;
    using System.Linq;
    using Microsoft.VisualBasic;
    using Microsoft.Extensions.Caching.Memory;
    using CRMConnect;
    using System.Diagnostics;



    [Route("[controller]")]
    [ApiController]
    public class FetchCustomerDetailsController : ControllerBase
    {

        private readonly IFtCustDtlExecution _ftCustDtlExecution;
        private Stopwatch watch;

        public FetchCustomerDetailsController(IFtCustDtlExecution ftCustDtlExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._ftCustDtlExecution = ftCustDtlExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());


                _ftCustDtlExecution.API_Name = "FetchCustomerDetails";
                _ftCustDtlExecution.Input_payload = request.ToString();
                FetchCustomerDtlReturn AccountDetails = await _ftCustDtlExecution.ValidateCustInput(request);

                watch.Stop();
                AccountDetails.TransactionID = this._ftCustDtlExecution.Transaction_ID;
                AccountDetails.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _ftCustDtlExecution.EncriptRespons(JsonConvert.SerializeObject(AccountDetails));
               

                var contentResult = new ContentResult();
                contentResult.Content = response;
                contentResult.ContentType = "application/json";
                return contentResult;


            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }
    }
}
