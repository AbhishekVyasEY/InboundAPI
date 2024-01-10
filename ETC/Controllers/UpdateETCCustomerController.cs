namespace ETC.Controllers
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
    public class UpdateETCCustomerController : ControllerBase
    {

        private readonly IETCExecution _etcExecution;
        private Stopwatch watch;

        public UpdateETCCustomerController(IETCExecution etcExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._etcExecution = etcExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                
                _etcExecution.API_Name = "UpdateETCCustomer";
                _etcExecution.Input_payload = request.ToString();
                ETCReturn ETCCustomerDetails = await _etcExecution.ValidateUpETCCustomertInput(request);

                watch.Stop();
                ETCCustomerDetails.TransactionID = this._etcExecution.Transaction_ID;
                ETCCustomerDetails.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _etcExecution.EncriptRespons(JsonConvert.SerializeObject(ETCCustomerDetails));
               

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
