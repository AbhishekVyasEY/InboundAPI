namespace AccountLead.Controllers
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
    public class CreateDigiAccountLeadController : ControllerBase
    {

        private readonly ICrdgAccLeadExecution _crdgaccleadExecution;
        private Stopwatch watch;

        public CreateDigiAccountLeadController(ICrdgAccLeadExecution crdgaccleadExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._crdgaccleadExecution = crdgaccleadExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                
                _crdgaccleadExecution.API_Name = "CreateDigiAccountLead";
                _crdgaccleadExecution.Input_payload = request.ToString();
                AccountLeadReturn AccountDetails = await _crdgaccleadExecution.ValidateLeadtInput(request);

                watch.Stop();
                AccountDetails.TransactionID = this._crdgaccleadExecution.Transaction_ID;
                AccountDetails.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _crdgaccleadExecution.EncriptRespons(JsonConvert.SerializeObject(AccountDetails));
               

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
