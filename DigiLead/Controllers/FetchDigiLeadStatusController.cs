namespace DigiLead.Controllers
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
    public class FetchDigiLeadStatusController : ControllerBase
    {

        private readonly IFtchDgLdStsExecution _ftchDgLdStsExecution;
        private Stopwatch watch;

        public FetchDigiLeadStatusController(IFtchDgLdStsExecution ftchDgLdStsExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._ftchDgLdStsExecution = ftchDgLdStsExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                
                _ftchDgLdStsExecution.API_Name = "FetchDigiLeadStatus";
                _ftchDgLdStsExecution.Input_payload = request.ToString();
                FtchDgLdStsReturn LeadStatus = await _ftchDgLdStsExecution.ValidateFtchDgLdSts(request);

                watch.Stop();
                LeadStatus.TransactionID = this._ftchDgLdStsExecution.Transaction_ID;
                LeadStatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _ftchDgLdStsExecution.EncriptRespons(JsonConvert.SerializeObject(LeadStatus));
               

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
