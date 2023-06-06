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
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                string Header_Value = string.Empty;
                if (Request.Headers.TryGetValue("appkey", out var headerValues))
                {
                    Header_Value = headerValues;
                }

                if (Request.Headers.TryGetValue("ChannelID", out var ChannelID))
                {
                    _ftchDgLdStsExecution.Channel_ID = ChannelID;
                }

                if (Request.Headers.TryGetValue("communicationID", out var communicationID))
                {
                    _ftchDgLdStsExecution.Transaction_ID = communicationID;
                }

                _ftchDgLdStsExecution.API_Name = "FetchDigiLeadStatus";
                _ftchDgLdStsExecution.Input_payload = request.ToString();
                FtchDgLdStsReturn LeadStatus = await _ftchDgLdStsExecution.ValidateFtchDgLdSts(request, Header_Value);

                watch.Stop();
                LeadStatus.TransactionID = this._ftchDgLdStsExecution.Transaction_ID;
                LeadStatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _ftchDgLdStsExecution.EncriptRespons(JsonConvert.SerializeObject(LeadStatus));
                this._ftchDgLdStsExecution.CRMLog(JsonConvert.SerializeObject(request), response, LeadStatus.ReturnCode);

                return Ok(response);


            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }
    }
}
