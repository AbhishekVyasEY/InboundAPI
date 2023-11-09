namespace UpdateAccountLead.Controllers
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
    public class UpdateDigiAccountLeadController : ControllerBase
    {

        private readonly IUpdgAccLeadExecution _updgaccleadExecution;
        private Stopwatch watch;

        public UpdateDigiAccountLeadController(IUpdgAccLeadExecution updgaccleadExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._updgaccleadExecution = updgaccleadExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                
                _updgaccleadExecution.API_Name = "UpdateDigiAccountLead";
                _updgaccleadExecution.Input_payload = request.ToString();
                UpAccountLeadReturn AccountDetails = await _updgaccleadExecution.ValidateLeadtInput(request);

                watch.Stop();
                AccountDetails.TransactionID = this._updgaccleadExecution.Transaction_ID;
                AccountDetails.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _updgaccleadExecution.EncriptRespons(JsonConvert.SerializeObject(AccountDetails));
               

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
