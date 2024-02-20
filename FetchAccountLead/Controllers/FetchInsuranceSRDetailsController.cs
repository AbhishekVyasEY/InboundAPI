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
    public class FetchInsuranceSRDetailsController : ControllerBase
    {

        private readonly IFthInsuranceSRDtlExecution _fthInsuranceSRDtlExecution;
        private Stopwatch watch;

        public FetchInsuranceSRDetailsController(IFthInsuranceSRDtlExecution fthInsuranceSRDtlExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._fthInsuranceSRDtlExecution = fthInsuranceSRDtlExecution;
        }


        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());


                _fthInsuranceSRDtlExecution.API_Name = "FetchInsuranceSRDetails";
                _fthInsuranceSRDtlExecution.Input_payload = request.ToString();
                FthInsuranceSRDtlReturn AccountDetails = await _fthInsuranceSRDtlExecution.ValidateSRDInput(request);

                watch.Stop();
                AccountDetails.TransactionID = this._fthInsuranceSRDtlExecution.Transaction_ID;
                AccountDetails.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _fthInsuranceSRDtlExecution.EncriptRespons(JsonConvert.SerializeObject(AccountDetails), _fthInsuranceSRDtlExecution.Bank_Code);
                

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
