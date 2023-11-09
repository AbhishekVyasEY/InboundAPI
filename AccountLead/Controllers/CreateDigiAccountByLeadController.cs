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
    public class CreateDigiAccountByLeadController : ControllerBase
    {

        private readonly ICrdgAccByLeadExecution _crdgaccleadbyExecution;
        private Stopwatch watch;

        public CreateDigiAccountByLeadController(ICrdgAccByLeadExecution crdgaccbyleadExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._crdgaccleadbyExecution = crdgaccbyleadExecution;
        }


        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());


                _crdgaccleadbyExecution.API_Name = "CreateDigiAccountByLead";
                _crdgaccleadbyExecution.Input_payload = request.ToString();
                AccountByLeadReturn AccountDetails = await _crdgaccleadbyExecution.ValidateLeadtInput(request);

                watch.Stop();
                AccountDetails.TransactionID = this._crdgaccleadbyExecution.Transaction_ID;
                AccountDetails.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _crdgaccleadbyExecution.EncriptRespons(JsonConvert.SerializeObject(AccountDetails), _crdgaccleadbyExecution.Bank_Code);
                

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
