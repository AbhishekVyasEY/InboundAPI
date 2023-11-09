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
    public class CreateDigiCustomerByLeadController : ControllerBase
    {

        private readonly ICrdgCustomerByLeadExecution _crdgcustleadbyExecution;
        private Stopwatch watch;

        public CreateDigiCustomerByLeadController(ICrdgCustomerByLeadExecution crdgcustbyleadExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._crdgcustleadbyExecution = crdgcustbyleadExecution;
        }


        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());


                _crdgcustleadbyExecution.API_Name = "CreateDigiCustomerByLead";
                _crdgcustleadbyExecution.Input_payload = request.ToString();
                CustomerByLeadReturn AccountDetails = await _crdgcustleadbyExecution.ValidateLeadtInput(request);

                watch.Stop();
                AccountDetails.TransactionID = this._crdgcustleadbyExecution.Transaction_ID;
                AccountDetails.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _crdgcustleadbyExecution.EncriptRespons(JsonConvert.SerializeObject(AccountDetails), _crdgcustleadbyExecution.Bank_Code);
                

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
