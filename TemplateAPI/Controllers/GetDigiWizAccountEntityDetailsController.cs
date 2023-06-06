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
                    _getDigiWizAcEntyDetlsExecution.Channel_ID = ChannelID;
                }

                if (Request.Headers.TryGetValue("communicationID", out var communicationID))
                {
                    _getDigiWizAcEntyDetlsExecution.Transaction_ID = communicationID;
                }

                _getDigiWizAcEntyDetlsExecution.API_Name = "GetDigiWizAccountEntityDetails";
                _getDigiWizAcEntyDetlsExecution.Input_payload = request.ToString();
                WizAcEntyReturn Casetatus = await _getDigiWizAcEntyDetlsExecution.ValidateWizAcEntyDetls(request, Header_Value);

                watch.Stop();
                Casetatus.TransactionID = this._getDigiWizAcEntyDetlsExecution.Transaction_ID;
                Casetatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _getDigiWizAcEntyDetlsExecution.EncriptRespons(JsonConvert.SerializeObject(Casetatus));
                this._getDigiWizAcEntyDetlsExecution.CRMLog(JsonConvert.SerializeObject(request), response, Casetatus.ReturnCode);

                return Ok(response);


            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }
    }
}
