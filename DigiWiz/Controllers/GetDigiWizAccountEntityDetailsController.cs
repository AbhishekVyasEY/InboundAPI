namespace DigiWiz.Controllers
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
    public class GetDigiWizAccountEntityDetailsController : ControllerBase
    {

        private readonly IGetDigiWizAcEntyDetlsExecution _getDigiWizAcEntyDetlsExecution;
        private Stopwatch watch;

        public GetDigiWizAccountEntityDetailsController(IGetDigiWizAcEntyDetlsExecution getDigiWizAcEntyDetlsExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._getDigiWizAcEntyDetlsExecution = getDigiWizAcEntyDetlsExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                
                _getDigiWizAcEntyDetlsExecution.API_Name = "GetDigiWizAccountEntityDetails";
                _getDigiWizAcEntyDetlsExecution.Input_payload = request.ToString();
                WizAcEntyReturn Casetatus = await _getDigiWizAcEntyDetlsExecution.ValidateWizAcEntyDetls(request);

                watch.Stop();
                Casetatus.TransactionID = this._getDigiWizAcEntyDetlsExecution.Transaction_ID;
                Casetatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _getDigiWizAcEntyDetlsExecution.EncriptRespons(JsonConvert.SerializeObject(Casetatus));
               

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
