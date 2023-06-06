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

namespace DedupeDigiLead.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DedupeDigiCustomerNLTRController : ControllerBase
    {

        private readonly IDedupDgLdNLExecution _dedupDgLdNLExecution;
        private Stopwatch watch;

        public DedupeDigiCustomerNLTRController(IDedupDgLdNLExecution dedupDgLdNLExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._dedupDgLdNLExecution = dedupDgLdNLExecution;
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
                    _dedupDgLdNLExecution.Channel_ID = ChannelID;
                }
                if (Request.Headers.TryGetValue("communicationID", out var communicationID))
                {
                    _dedupDgLdNLExecution.Transaction_ID = communicationID;
                }

                _dedupDgLdNLExecution.API_Name = "DedupeDigiLeadNLTR";
                _dedupDgLdNLExecution.Input_payload = request.ToString();
                DedupDgLdNLTRReturn Casetatus = await _dedupDgLdNLExecution.ValidateDedupDgLdNL(request, Header_Value,"NLTR");

                watch.Stop();
                Casetatus.TransactionID = this._dedupDgLdNLExecution.Transaction_ID;
                Casetatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";
                string response = await _dedupDgLdNLExecution.EncriptRespons(JsonConvert.SerializeObject(Casetatus));

                return Ok(response);


            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }
    }
}
