namespace encdec
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Xml.Linq;
    using CRMConnect;
    using System.Xml;
    using System.Threading.Channels;
    using System.ComponentModel;
    using System;
    using System.Security.Cryptography.Xml;
    using Azure;
    using Microsoft.VisualBasic;
    using System.Text;

    public class EncDecExecution : IEncDecExecution
    {

        private ILoggers _logger;
        private IQueryParser _queryParser;
        public string Bank_Code { set; get; }

        public string Channel_ID
        {
            set
            {
                _logger.Channel_ID = value;
            }
            get
            {
                return _logger.Channel_ID;
            }
        }
       
        public string appkey { set; get; }

        public string API_Name
        {
            set
            {
                _logger.API_Name = value;
            }
        }
       

        private readonly IKeyVaultService _keyVaultService;

       

        private List<string> applicents = new List<string>();
    

       

        public EncDecExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService)
        {

            this._logger = logger;

            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            

        }


        public async Task<string> EQEncryptionInput(dynamic RequestData)
        {
            return await EncriptRespons(JsonConvert.SerializeObject(RequestData));           
        }

        public async Task<string> EQDecryptionInput(dynamic RequestData)
        {
            string xmlData = await this._queryParser.PayloadDecryption(RequestData.ToString(), "FI0060");
            
            return xmlData;
        }


      

        public async Task<string> EncriptRespons(string ResponsData)
        {
            var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            string response = await _queryParser.PayloadEncryption(ResponsData, timeStamp.ToString(), "FI0060");
            return response;
        }

    }
}
