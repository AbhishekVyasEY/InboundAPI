namespace encdec.Controllers
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
    public class EQEncryptionController : ControllerBase
    {

        private readonly IEncDecExecution _etcExecution;      

        public EQEncryptionController(IEncDecExecution etcExecution)
        {          
            this._etcExecution = etcExecution;
        }



        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());
                               
               
                string response = await _etcExecution.EQEncryptionInput(request);


                return Content(response);
               


            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }
    }
}
