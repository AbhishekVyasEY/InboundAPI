namespace CustomerLead.Controllers
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
    public class DedupeDigiCustomer : ControllerBase
    {

        private readonly IDdpDgCustomerExecution _ddpdgcustomerExecution;
        private Stopwatch watch;

        public DedupeDigiCustomer(IDdpDgCustomerExecution ddpdgcustomerExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._ddpdgcustomerExecution = ddpdgcustomerExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                
                _ddpdgcustomerExecution.API_Name = "DedupeDigiCustomer";
                _ddpdgcustomerExecution.Input_payload = request.ToString();
                DedupeDigiCustomerReturn AccountDetails = await _ddpdgcustomerExecution.ValidateInput(request);

                watch.Stop();
                AccountDetails.TransactionID = this._ddpdgcustomerExecution.Transaction_ID;
                AccountDetails.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _ddpdgcustomerExecution.EncriptRespons(JsonConvert.SerializeObject(AccountDetails), _ddpdgcustomerExecution.Bank_Code);
               

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
