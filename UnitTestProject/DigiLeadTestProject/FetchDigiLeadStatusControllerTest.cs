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

namespace DigiLeadTestProject
{
    [TestClass]
    public class FetchDigiLeadStatusControllerTest
    {
        private Mock<IFtchDgLdStsExecution> _FtchDgLdStsExecution;
        private Fixture _fixture;
        private FetchDigiLeadStatusController _controller;

        public FetchDigiLeadStatusControllerTest()
        {
            _fixture = new Fixture();
            _FtchDgLdStsExecution = new Mock<IFtchDgLdStsExecution>();
            _controller = new FetchDigiLeadStatusController(_FtchDgLdStsExecution.Object);
        }

        [TestMethod]
        public async Task Post_FetchDigiLeadStatus_ReturnOK()
        {
            var request_body = new RequestBody();
            request_body.LeadID = "1000078";
            var json = JsonConvert.SerializeObject(request_body);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var httpContext = new DefaultHttpContext()
            {
                Request = { Body = stream, ContentLength = stream.Length }
            };
            var controllerContext = new ControllerContext { HttpContext = httpContext };

            var ftch_Return = _fixture.Create<FtchDgLdStsReturn>();

            _FtchDgLdStsExecution.Setup(x => x.ValidateFtchDgLdSts(json, "APIKEY")).ReturnsAsync(ftch_Return);

            _controller.ControllerContext = controllerContext;

            var result = await _controller.Post();
            var obj = result as ObjectResult;
            Assert.AreEqual(200, obj.StatusCode);
        }


    }
}
