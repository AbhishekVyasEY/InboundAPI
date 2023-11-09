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
    public class DedupeDigiAccountNLController : ControllerBase
    {

        private readonly IDedupDgAccountNLExecution _dedupDgAccountNLExecution;
        private Stopwatch watch;

        public DedupeDigiAccountNLController(IDedupDgAccountNLExecution dedupDgAccountNLExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._dedupDgAccountNLExecution = dedupDgAccountNLExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());


                _dedupDgAccountNLExecution.API_Name = "DedupeDigiAccountNL";
                _dedupDgAccountNLExecution.Input_payload = request.ToString();
                DedupDgAccNLReturn Casetatus = await _dedupDgAccountNLExecution.ValidateDedupDgAccNL(request,"NL");

                watch.Stop();
                Casetatus.TransactionID = this._dedupDgAccountNLExecution.Transaction_ID;
                Casetatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";
                string response = await _dedupDgAccountNLExecution.EncriptRespons(JsonConvert.SerializeObject(Casetatus));

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
