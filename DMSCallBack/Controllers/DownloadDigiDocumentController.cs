namespace DMSCallBack.Controllers
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
    public class DownloadDigiDocumentController : ControllerBase
    {

        private readonly IDownloadDigiDocExecution _downloadDigiDocExecution;
        private Stopwatch watch;

        public DownloadDigiDocumentController(IDownloadDigiDocExecution downloadDigiDocExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._downloadDigiDocExecution = downloadDigiDocExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());


                _downloadDigiDocExecution.API_Name = "DownloadDigiDocument";
                _downloadDigiDocExecution.Input_payload = request.ToString();
                DMSCallBackReturn dmsreturn = await _downloadDigiDocExecution.ValidateDownloadDocInput(request);

                watch.Stop();
                dmsreturn.TransactionID = this._downloadDigiDocExecution.Transaction_ID;
                dmsreturn.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _downloadDigiDocExecution.EncriptRespons(JsonConvert.SerializeObject(dmsreturn));
               

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
