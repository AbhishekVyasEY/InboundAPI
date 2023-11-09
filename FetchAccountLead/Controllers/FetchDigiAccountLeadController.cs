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
    public class FetchDigiAccountLeadController : ControllerBase
    {

        private readonly IFtdgAccLeadExecution _ftdgaccleadExecution;
        private Stopwatch watch;

        public FetchDigiAccountLeadController(IFtdgAccLeadExecution ftdgaccleadExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._ftdgaccleadExecution = ftdgaccleadExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                
                _ftdgaccleadExecution.API_Name = "FetchDigiAccountLead";
                _ftdgaccleadExecution.Input_payload = request.ToString();
                FtAccountLeadReturn AccountDetails = await _ftdgaccleadExecution.ValidateLeadtInput(request);

                watch.Stop();
                AccountDetails.TransactionID = this._ftdgaccleadExecution.Transaction_ID;
                AccountDetails.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _ftdgaccleadExecution.EncriptRespons(JsonConvert.SerializeObject(AccountDetails));
               

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
