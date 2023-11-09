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
    public class UpdateLeadStatusController : ControllerBase
    {

        private readonly IUpdateLeadExecution _updateLeadExecution;
        private readonly IQueryParser _queryp;
        private readonly IKeyVaultService _keyVaultService;
        private Stopwatch watch;

        public UpdateLeadStatusController(IUpdateLeadExecution updateLeadExecution) 
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._updateLeadExecution = updateLeadExecution;
           
        }

        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());


                _updateLeadExecution.API_Name = "UpdateLeadStatus";
                _updateLeadExecution.Input_payload = request.ToString();

                UpdateLidStatusReturnParam Leadstatus = await _updateLeadExecution.UpdateLeade(request);

                watch.Stop();
                Leadstatus.TransactionID = this._updateLeadExecution.Transaction_ID;
                Leadstatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";
                var response = await _updateLeadExecution.EncriptRespons(JsonConvert.SerializeObject(Leadstatus));

              

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
