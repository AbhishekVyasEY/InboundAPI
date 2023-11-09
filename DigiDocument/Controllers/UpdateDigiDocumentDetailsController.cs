namespace DigiDocument.Controllers
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
    public class UpdateDigiDocumentDetailsController : ControllerBase
    {

        private readonly IDgDocDtlExecution _dgdocDtlExecution;
        private Stopwatch watch;

        public UpdateDigiDocumentDetailsController(IDgDocDtlExecution dgdocDtExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._dgdocDtlExecution = dgdocDtExecution;
        }



        [HttpPost]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                
                _dgdocDtlExecution.API_Name = "UpdateDigiDocumentDetails";
                _dgdocDtlExecution.Input_payload = request.ToString();
                UpdateDgDocDtlReturn api_Return = await _dgdocDtlExecution.ValidateDocumentInput(request);

                watch.Stop();
                api_Return.TransactionID = this._dgdocDtlExecution.Transaction_ID;
                api_Return.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _dgdocDtlExecution.EncriptRespons(JsonConvert.SerializeObject(api_Return));
                

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
