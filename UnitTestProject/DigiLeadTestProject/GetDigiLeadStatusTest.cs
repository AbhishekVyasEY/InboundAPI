using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using AutoFixture;
using DigiLead;
using DigiLead.Controllers;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CRMConnect;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration.Json;

namespace DigiLeadTestProject
{
    [TestClass]
    public class GetDigiLeadStatusTest
    {
        private Mock<ICommonFunction> _commonFunction;
        private Mock<ILoggers> _logger;
        private Mock<IQueryParser> _queryParser;
        private Mock<IKeyVaultService> _keyVaultService;
        private Fixture _fixture;
        private FtchDgLdStsExecution _controller;

        public GetDigiLeadStatusTest()
        {
            _fixture = new Fixture();
            _commonFunction = new Mock<ICommonFunction>();
            _logger = new Mock<ILoggers>();
            _queryParser = new Mock<IQueryParser>();
            _keyVaultService = new Mock<IKeyVaultService>();
            _controller = new FtchDgLdStsExecution(_logger.Object, _queryParser.Object, _keyVaultService.Object, _commonFunction.Object);
        }

        [TestMethod]
        public async Task getDigiLead_Status_Test()
        {
            var request_body = _fixture.Create <RequestBody>();
            request_body.LeadID = "L1234566";

            string JsonSTr = "[{\"firstname\":\"firstname\",\"lastname\":\"lastname\",\"middlename\":\"middlename\",\"eqs_shortname\":\"eqs_shortname\",\"mobilephone\":\"mobilephone\",\"eqs_dob\":\"eqs_dob\",\"eqs_aadhaarreference\":\"eqs_aadhaarreference\",\"eqs_pan\":\"eqs_pan\",\"eqs_mothermaidenname\":\"eqs_mothermaidenname\",\"eqs_panform60code\":\"eqs_panform60code\",\"eqs_nlmatchcode\":\"eqs_nlmatchcode\",\"eqs_reasonforna\":\"eqs_reasonforna\",\"eqs_voterid\":\"eqs_voterid\",\"eqs_dlnumber\":\"eqs_dlnumber\",\"eqs_passportnumber\":\"eqs_passportnumber\",\"eqs_ckycnumber\":\"eqs_ckycnumber\"}]";
            JArray jArray = JsonConvert.DeserializeObject<JArray>(JsonSTr);


            _queryParser.Setup(x => x.HttpApiCall(It.IsAny<string>(), HttpMethod.Get, It.IsAny<string>())).ReturnsAsync(It.IsAny<List<JObject>>());
            _commonFunction.Setup(x => x.getDataFromResponce(It.IsAny<List<JObject>>())).ReturnsAsync(jArray);

            var request_body_Json = JsonConvert.SerializeObject(request_body);
            JObject request_body_Jobj = JsonConvert.DeserializeObject<JObject>(request_body_Json);
           

            var result = await _controller.getDigiLeadStatus(request_body_Jobj);
            
            Assert.AreEqual("CRM-SUCCESS", result.ReturnCode);
        }


    }
}
