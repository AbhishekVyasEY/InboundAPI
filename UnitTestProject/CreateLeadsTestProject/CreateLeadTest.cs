using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using AutoFixture;
using CreateLeads;
using CreateLeads.Controllers;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CRMConnect;
using Newtonsoft.Json.Linq;

namespace CreateLeadsTestProject
{
    [TestClass]
    public class CreateLeadTest
    {
        private Mock<ICommonFunction> _commonFunction;
        private Mock<ILoggers> _logger;
        private Mock<IQueryParser> _queryParser;
        private Mock<IKeyVaultService> _keyVaultService;
        private Fixture _fixture;
        private CreateLeadExecution _controller;

        public CreateLeadTest()
        {
            _fixture = new Fixture();
            _commonFunction = new Mock<ICommonFunction>();
            _logger = new Mock<ILoggers>();
            _queryParser = new Mock<IQueryParser>();
            _keyVaultService = new Mock<IKeyVaultService>();
            _controller = new CreateLeadExecution(_logger.Object, _queryParser.Object, _keyVaultService.Object, _commonFunction.Object);
        }

        [TestMethod]
        public async Task CreateLeads_ESFBWebsite()
        {
            var request_body = _fixture.Create <RequestBody>();
            request_body.ChannelType = "ESFBWebsite";
            request_body.ProductCode = "P100889";
            var request_body_Json = JsonConvert.SerializeObject(request_body);
            JObject request_body_Jobj = JsonConvert.DeserializeObject<JObject>(request_body_Json);
            Dictionary<string, string> product_Return = new Dictionary<string, string>();
            product_Return["ProductId"] = "P10001";
            product_Return["businesscategoryid"] = "P10001";
            product_Return["productcategory"] = "P10001";
            product_Return["crmproductcategorycode"] = "P10001";

            _commonFunction.Setup(x => x.getProductId(It.IsAny<string>())).ReturnsAsync(product_Return);

            _commonFunction.Setup(x => x.getCityId(It.IsAny<string>())).ReturnsAsync("City ID");
            _commonFunction.Setup(x => x.getBranchId(It.IsAny<string>())).ReturnsAsync("BR0003");
            _commonFunction.Setup(x => x.getCustomerDetail(It.IsAny<string>())).ReturnsAsync(new JArray());

            _commonFunction.Setup(x => x.MeargeJsonString("Json1","Json 2")).ReturnsAsync("MJson");
            string query_resultST = "[{\"responsecode\":\"204\",\"responsebody\":\"yyuu(78678446)iio\"}]";
            var query_result = JsonConvert.DeserializeObject<List<JObject>>(query_resultST);
            _queryParser.Setup(x => x.HttpApiCall(It.IsAny<string>(), HttpMethod.Post, It.IsAny<string>())).ReturnsAsync(query_result);

            var result = await _controller.CreateLead(request_body_Jobj);
            
            Assert.AreEqual("CRM - SUCCESS", result.ReturnCode);
        }


    }
}
