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
    public class EQDecryptionController : ControllerBase
    {

        private readonly IEncDecExecution _etcExecution;
       

        public EQDecryptionController(IEncDecExecution etcExecution)
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
                string request = await requestReader.ReadToEndAsync();
                               
                string response = await _etcExecution.EQDecryptionInput(request);

                return Content(response);


            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }
    }
}
