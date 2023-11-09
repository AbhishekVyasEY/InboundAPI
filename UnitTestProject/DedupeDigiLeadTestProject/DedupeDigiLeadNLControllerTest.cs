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

namespace DedupeDigiLeadTestProject
{
    [TestClass]
    public class DedupeDigiLeadNLControllerTest
    {
        private Mock<IDedupDgLdNLExecution> _DedupDgLdNLExecution;
        private Fixture _fixture;
        private DedupeDigiCustomerNLController _controller;

        public DedupeDigiLeadNLControllerTest()
        {
            _fixture = new Fixture();
            _DedupDgLdNLExecution = new Mock<IDedupDgLdNLExecution>();
            _controller = new DedupeDigiCustomerNLController(_DedupDgLdNLExecution.Object);
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

            var ftch_Return = _fixture.Create<DedupDgLdNLReturn>();

            _DedupDgLdNLExecution.Setup(x => x.ValidateDedupDgLdNL(json, "APIKEY")).ReturnsAsync(ftch_Return);

            _controller.ControllerContext = controllerContext;

            var result = await _controller.Post();
            var obj = result as ObjectResult;
            Assert.AreEqual(200, obj.StatusCode);
        }


    }
}
