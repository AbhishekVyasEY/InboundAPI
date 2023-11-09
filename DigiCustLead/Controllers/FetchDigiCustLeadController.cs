namespace DigiCustLead.Controllers
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
    public class FetchDigiCustLeadController : ControllerBase
    {

        private readonly IFhDgCustLeadExecution _fhDgCustLeadExecution;
        private Stopwatch watch;

        public FetchDigiCustLeadController(IFhDgCustLeadExecution fhDgCustLeadExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._fhDgCustLeadExecution = fhDgCustLeadExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                
                _fhDgCustLeadExecution.API_Name = "FetchDigiCustLead";
                _fhDgCustLeadExecution.Input_payload = request.ToString();
                FetchCustLeadReturn Casetatus = await _fhDgCustLeadExecution.ValidateFetchLeadDetls(request);

                watch.Stop();
                Casetatus.TransactionID = this._fhDgCustLeadExecution.Transaction_ID;
                Casetatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _fhDgCustLeadExecution.EncriptRespons(JsonConvert.SerializeObject(Casetatus));
              

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
