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
    public class GetDigiCustomerLeadNSDL : ControllerBase
    {

        private readonly ICusLeadExecution _cusLeadExecution;
        private Stopwatch watch;

        public GetDigiCustomerLeadNSDL(ICusLeadExecution cusLeadExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._cusLeadExecution = cusLeadExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                
                _cusLeadExecution.API_Name = "GetDigiCustomerLead";
                _cusLeadExecution.Input_payload = request.ToString();
                CustomerLeadReturn AccountDetails = await _cusLeadExecution.ValidateInput(request);

                watch.Stop();
                AccountDetails.TransactionID = this._cusLeadExecution.Transaction_ID;
                AccountDetails.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _cusLeadExecution.EncriptRespons(JsonConvert.SerializeObject(AccountDetails));
               

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
