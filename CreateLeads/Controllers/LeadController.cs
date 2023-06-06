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
using CRMConnect;
using System.Diagnostics;

namespace CreateLeads.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LeadController : ControllerBase
    {

        private readonly ICreateLeadExecution _createLeadExecution;
        private readonly IQueryParser _queryp;
        private readonly IKeyVaultService _keyVaultService;
        private Stopwatch watch;

        public LeadController(ICreateLeadExecution createLeadExecution) 
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._createLeadExecution = createLeadExecution;
           // this._createLeadExecution._transactionID = "Lead-" + Guid.NewGuid().ToString("N");
        }

        [HttpPost("CreateLead")]        
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
                    _createLeadExecution.Channel_ID = ChannelID;
                }

                if (Request.Headers.TryGetValue("communicationID", out var communicationID))
                {
                    _createLeadExecution.Transaction_ID = communicationID;
                }

                _createLeadExecution.API_Name = "CreateLead";
                _createLeadExecution.Input_payload = request.ToString();


                LeadReturnParam Leadstatus = await _createLeadExecution.ValidateLeade(request, Header_Value);

                watch.Stop();
                Leadstatus.TransactionID = this._createLeadExecution.Transaction_ID;
                Leadstatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";
                string response = await _createLeadExecution.EncriptRespons(JsonConvert.SerializeObject(Leadstatus));

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



                return Ok(response);
                
                    
            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }

        /*
        [HttpPost("UpdateLeadStatus")]
        public async Task<IActionResult> UpdateLeadStatus()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());
                CreateLeadExecution createleadEx = new CreateLeadExecution(this._log, this._queryp, this._keyVaultService);
                LeadReturnParam Leadstatus = await createleadEx.ValidateLeadeStatus(request);
                return Ok(Leadstatus);
            }
            catch (Exception ex)
            {

                return BadRequest();

            }

        }

        */


    }
}
