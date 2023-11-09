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
    public class UpdateDigiCustLeadController : ControllerBase
    {

        private readonly IUpDgCustLeadExecution _upDgCustLeadExecution;
        private Stopwatch watch;

        public UpdateDigiCustLeadController(IUpDgCustLeadExecution upDgCustLeadExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._upDgCustLeadExecution = upDgCustLeadExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                
                _upDgCustLeadExecution.API_Name = "UpdateDigiCustLead";
                _upDgCustLeadExecution.Input_payload = request.ToString();
                UpdateCustLeadReturn Casetatus = await _upDgCustLeadExecution.ValidateCustLeadDetls(request);

                watch.Stop();
                Casetatus.TransactionID = this._upDgCustLeadExecution.Transaction_ID;
                Casetatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _upDgCustLeadExecution.EncriptRespons(JsonConvert.SerializeObject(Casetatus));                
               

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
