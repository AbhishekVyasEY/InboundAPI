using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Linq;
using Microsoft.VisualBasic;
using Microsoft.Extensions.Caching.Memory;
using CRMConnect;

namespace ManageCase.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CaseController : ControllerBase
    {

        private readonly ICreateCaseExecution _createCaseExecution;
        private readonly IQueryParser _queryp;
        private readonly IKeyVaultService _keyVaultService;
        private Stopwatch watch;


        public CaseController(ICreateCaseExecution createCaseExecution) 
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._createCaseExecution = createCaseExecution;  
            //this._createCaseExecution._transactionID = "Case-"+ Guid.NewGuid().ToString("N");
        }

        [HttpPost("CreateCase")]        
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
                    _createCaseExecution.Channel_ID = ChannelID;
                }

                if (Request.Headers.TryGetValue("communicationID", out var communicationID))
                {
                    _createCaseExecution.Transaction_ID = communicationID;
                }

                _createCaseExecution.API_Name = "CreateCase";
                _createCaseExecution.Input_payload= request.ToString();
                CaseReturnParam Casetatus = await _createCaseExecution.ValidateCreateCase(request, Header_Value);
                watch.Stop();
                Casetatus.TransactionID = this._createCaseExecution.Transaction_ID;
                Casetatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";
                string response = await _createCaseExecution.EncriptRespons(JsonConvert.SerializeObject(Casetatus));
                this._createCaseExecution.CRMLog(JsonConvert.SerializeObject(request), response, Casetatus.ReturnCode);
                return Ok(response);
                
                    
            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }

        [HttpPost("getCaseStatus")]
        public async Task<IActionResult> getCaseStatus()
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
                    _createCaseExecution.Channel_ID = ChannelID;
                }

                if (Request.Headers.TryGetValue("communicationID", out var communicationID))
                {
                    _createCaseExecution.Transaction_ID = communicationID;
                }

                _createCaseExecution.API_Name = "getCaseStatus";
                _createCaseExecution.Input_payload = request.ToString();
                CaseStatusRtParam Casetatus = await _createCaseExecution.ValidategetCaseStatus(request, Header_Value);

                watch.Stop();
                Casetatus.TransactionID = this._createCaseExecution.Transaction_ID;
                Casetatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _createCaseExecution.EncriptRespons(JsonConvert.SerializeObject(Casetatus));
                this._createCaseExecution.CRMLog(JsonConvert.SerializeObject(request), response, Casetatus.ReturnCode);

                return Ok(response);
            }
            catch (Exception ex)
            {

                return BadRequest();

            }

        }

        [HttpPost("getCaseList")]
        public async Task<IActionResult> getCaseList()
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
                    _createCaseExecution.Channel_ID = ChannelID;
                }

                if (Request.Headers.TryGetValue("communicationID", out var communicationID))
                {
                    _createCaseExecution.Transaction_ID = communicationID;
                }

                _createCaseExecution.API_Name = "getCaseList";
                _createCaseExecution.Input_payload = request.ToString();
                CaseListParam CaseList = await _createCaseExecution.getCaseList(request, Header_Value);

                watch.Stop();
                CaseList.TransactionID = this._createCaseExecution.Transaction_ID;
                CaseList.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";
                string response = await _createCaseExecution.EncriptRespons(JsonConvert.SerializeObject(CaseList));
                this._createCaseExecution.CRMLog(JsonConvert.SerializeObject(request), response, CaseList.ReturnCode);
                return Ok(response);
            }
            catch (Exception ex)
            {

                return BadRequest();

            }

        }


    }
}
