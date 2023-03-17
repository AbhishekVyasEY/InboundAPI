using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace EquitasInboundAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CaseController : ControllerBase
    {

        private readonly ILogger<LeadController> _log;
        private readonly IQueryParser _queryp;

        public CaseController(ILogger<LeadController> log, IQueryParser queryParser)
        {
            this._log = log;
            this._queryp = queryParser;
        }


        [HttpPost("CreateCase")]        
        public async Task<IActionResult> CreateCase()
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
