namespace ManageCase.Controllers
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
    public class UpdateCaseController : ControllerBase
    {

        private readonly IUpdateCaseExecution _updateCaseExecution;
        private Stopwatch watch;

        public UpdateCaseController(IUpdateCaseExecution updateCaseExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._updateCaseExecution = updateCaseExecution;
        }


        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());


                _updateCaseExecution.API_Name = "UpdateCase";
                _updateCaseExecution.Input_payload = request.ToString();
                UpdateCaseReturnParam AccountDetails = await _updateCaseExecution.ValidateUpdateCase(request);

                watch.Stop();
                AccountDetails.TransactionID = this._updateCaseExecution.Transaction_ID;
                AccountDetails.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _updateCaseExecution.EncriptRespons(JsonConvert.SerializeObject(AccountDetails), _updateCaseExecution.Bank_Code);
                

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
