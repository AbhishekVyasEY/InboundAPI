using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using AutoFixture;
using ManageCase;
using ManageCase.Controllers;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CRMConnect;
using Newtonsoft.Json.Linq;

namespace ManageCaseTestProject
{
    [TestClass]
    public class CreateCaseTest
    {
        private Mock<ICommonFunction> _commonFunction;
        private Mock<ILoggers> _logger;
        private Mock<IQueryParser> _queryParser;
        private Mock<IKeyVaultService> _keyVaultService;
        private Fixture _fixture;
        private CreateCaseExecution _controller;

        public CreateCaseTest()
        {
            _fixture = new Fixture();
            _commonFunction = new Mock<ICommonFunction>();
            _logger = new Mock<ILoggers>();
            _queryParser = new Mock<IQueryParser>();
            _keyVaultService = new Mock<IKeyVaultService>();
            _controller = new CreateCaseExecution(_logger.Object, _queryParser.Object, _keyVaultService.Object, _commonFunction.Object);
        }

        [TestMethod]
        public async Task CreateCase_MobileBanking()
        {
            var request_body = _fixture.Create <RequestBody>();
            request_body.ChannelType = "MobileBanking";
            request_body.CaseType = "Request";
            request_body.Priority = "Normal";

            var request_body_Json = JsonConvert.SerializeObject(request_body);
            JObject request_body_Jobj = JsonConvert.DeserializeObject<JObject>(request_body_Json);
            Dictionary<string, string> product_Return = new Dictionary<string, string>();
            product_Return["ProductId"] = "P10001";
            product_Return["businesscategoryid"] = "P10001";
            product_Return["productcategory"] = "P10001";
            product_Return["crmproductcategorycode"] = "P10001";

            
            _commonFunction.Setup(x => x.getCategoryId(It.IsAny<string>())).ReturnsAsync("C00987");
            _commonFunction.Setup(x => x.getSubCategoryId(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("C00987");
            _commonFunction.Setup(x => x.getclassificationId(It.IsAny<string>())).ReturnsAsync("C00987");
            _commonFunction.Setup(x => x.getAccountId(It.IsAny<string>())).ReturnsAsync("BR0003");
            _commonFunction.Setup(x => x.getCustomerId(It.IsAny<string>())).ReturnsAsync("Cus445566");

            _commonFunction.Setup(x => x.MeargeJsonString("Json1","Json 2")).ReturnsAsync("MJson");
            string query_resultST = "[{\"responsecode\":\"204\",\"responsebody\":\"yyuu(78678446)iio\"}]";
            var query_result = JsonConvert.DeserializeObject<List<JObject>>(query_resultST);
            _queryParser.Setup(x => x.HttpApiCall(It.IsAny<string>(), HttpMethod.Post, It.IsAny<string>())).ReturnsAsync(query_result);

            var result = await _controller.CreateCase(request_body_Jobj);
            
            Assert.AreEqual("CRM-SUCCESS", result.ReturnCode);
        }


    }
}
