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

namespace CreateLeadsTestProject
{
    [TestClass]
    public class LeadControllerTest
    {
        private Mock<ICreateLeadExecution> _CreateLeadExecution;
        private Fixture _fixture;
        private LeadController _controller;

        public LeadControllerTest()
        {
            _fixture = new Fixture();
            _CreateLeadExecution = new Mock<ICreateLeadExecution>();
            _controller = new LeadController(_CreateLeadExecution.Object);
        }

        [TestMethod]
        public async Task Post_CreateLeads_ReturnOK()
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

            var ftch_Return = _fixture.Create<LeadReturnParam>();

            _CreateLeadExecution.Setup(x => x.ValidateLeade("APIKEY")).ReturnsAsync(ftch_Return);

            _controller.ControllerContext = controllerContext;

            var result = await _controller.Post();
            var obj = result as ObjectResult;
            Assert.AreEqual(200, obj.StatusCode);
        }


    }
}
