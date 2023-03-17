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

namespace EquitasInboundAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LeadController : ControllerBase
    {

        private readonly ILogger<LeadController> _log;
        private readonly IQueryParser _queryp;

        public LeadController(ILogger<LeadController> log, IQueryParser queryParser) 
        {
            this._log = log;
            this._queryp = queryParser;
        }

        [HttpPost("CreateLead")]        
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());
                CreateLeadExecution createleadEx = new CreateLeadExecution(this._log, this._queryp);

                string Header_Value = string.Empty;
                if (Request.Headers.TryGetValue("Sequeritykey", out var headerValues))
                {
                    Header_Value = headerValues;
                }


                LeadReturnParam Leadstatus = await createleadEx.ValidateLeade(request);


                /*
                CommonFunction commObj = new CommonFunction();
                string token = await commObj.AcquireNewTokenAsync();
                string jsonMessage = JsonConvert.SerializeObject(request);
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage requestMessage = new
                    HttpRequestMessage(HttpMethod.Post, "https://equitasdev.api.crm8.dynamics.com/api/data/v9.2/leads");
                    requestMessage.Content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    // requestMessage.Headers.Add("MSCRM.BypassCustomPluginExceution", "true");
                    HttpResponseMessage response = client.SendAsync(requestMessage).Result;
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        // Do something here
                    }
                    
                }
                */


                
                return Ok(Leadstatus);
                
                    
            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }

        [HttpPost("UpdateLeadStatus")]
        public async Task<IActionResult> UpdateLeadStatus()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());
                CreateLeadExecution createleadEx = new CreateLeadExecution(this._log, this._queryp);
                LeadReturnParam Leadstatus = await createleadEx.ValidateLeadeStatus(request);
                return Ok(Leadstatus);
            }
            catch (Exception ex)
            {

                return BadRequest();

            }

        }


    }
}
