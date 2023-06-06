using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using AutoFixture;
using DedupeDigiLead;
using DedupeDigiLead.Controllers;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CRMConnect;
using Newtonsoft.Json.Linq;

namespace DedupeDigiLeadTestProject
{
    [TestClass]
    public class GetDedupDgLdNLStatusTest
    {
        private Mock<ICommonFunction> _commonFunction;
        private Mock<ILoggers> _logger;
        private Mock<IQueryParser> _queryParser;
        private Mock<IKeyVaultService> _keyVaultService;
        private Fixture _fixture;
        private DedupDgLdNLExecution _controller;

        public GetDedupDgLdNLStatusTest()
        {
            _fixture = new Fixture();
            _commonFunction = new Mock<ICommonFunction>();
            _logger = new Mock<ILoggers>();
            _queryParser = new Mock<IQueryParser>();
            _keyVaultService = new Mock<IKeyVaultService>();
            _controller = new DedupDgLdNLExecution(_logger.Object, _queryParser.Object, _keyVaultService.Object, _commonFunction.Object);
        }

        [TestMethod]
        public async Task GetDedupDg_LdNL_Status_Test()
        {
            var request_body = _fixture.Create <RequestBody>();
            request_body.LeadID = "LD100345";
            
            var request_body_Json = JsonConvert.SerializeObject(request_body);
            JObject request_body_Jobj = JsonConvert.DeserializeObject<JObject>(request_body_Json);

            string getLeadData_JS = "[{\"fullname\":\"fullname\",\"eqs_passportnumber\":\"eqs_passportnumber\"}]";
            JArray getLeadData_JA = JsonConvert.DeserializeObject<JArray>(getLeadData_JS);
            _commonFunction.Setup(x => x.getLeadData(It.IsAny<string>())).ReturnsAsync(getLeadData_JA);

            string getNLTRData_JS = "[{\"eqs_passports\":\"eqs_passportnumber\"}]";
            JArray getNLTRData_JA = JsonConvert.DeserializeObject<JArray>(getNLTRData_JS);
            _commonFunction.Setup(x => x.getNLTRData(It.IsAny<string>())).ReturnsAsync(getNLTRData_JA);

            
            var result = await _controller.getDedupDgLdNLStatus(request_body_Jobj, "NLTR");
            
            Assert.AreEqual("CRM - SUCCESS", result.ReturnCode);
        }


    }
}
