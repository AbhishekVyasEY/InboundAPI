using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace CRMConnect
{
    public class Loggers : ILoggers
    {
        private readonly IKeyVaultService _keyVaultService;
        private TelemetryClient teleClient;
        public string API_Name { get; set; }        
        public string Input_payload { get; set; }
        public string Channel_ID { get; set; }
        public string Transaction_ID { get; set; }
        public Loggers(IKeyVaultService keyVaultService)
        {
            _keyVaultService = keyVaultService;
            this.teleClient = new TelemetryClient();
            this.teleClient.Context.InstrumentationKey = _keyVaultService.ReadSecret("appinsinstrumentationkey");
        }
        public void LogInformation(string FunctionName, string InfoMessage, string Additionalinfo = "")
        {
            Dictionary<string, object> message = new Dictionary<string, object>();
            message["API"] = this.API_Name;
            message["FunctionName"] = FunctionName;
            message["Messages"] = InfoMessage;
            message["InputPayload"] = this.Input_payload;
            message["ChannelID"] = Channel_ID;
            message["TransactionID"] = Transaction_ID;
            message["Additionalinfo"] = Additionalinfo;
            message["Severity"] = "Medium";

            var eventTrigger = new EventTelemetry("Validation Error");
            foreach (var d in message)
            {
                eventTrigger.Properties.Add(d.Key, d.Value == null ? "" : d.Value.ToString());
            }
            teleClient.TrackEvent(eventTrigger);
        }

        public void LogError(string FunctionName, string? ErrorMessage, string Additionalinfo = "")
        {
            Dictionary<string, object> message = new Dictionary<string, object>();
            message["API"] = this.API_Name;
            message["FunctionName"] = FunctionName;
            message["Messages"] = ErrorMessage;
            message["InputPayload"] = this.Input_payload;
            message["ChannelID"] = Channel_ID;
            message["TransactionID"] = Transaction_ID;
            message["Additionalinfo"] = Additionalinfo;
            message["Severity"] = "High";

            var eventTrigger = new EventTelemetry("Application Error");
            foreach (var d in message)
            {
                eventTrigger.Properties.Add(d.Key, d.Value == null ? "" : d.Value.ToString());
            }
            teleClient.TrackEvent(eventTrigger);
        }

        public void requestPerser(HttpRequestMessage request, HttpResponseMessage ResponsMessage)
        {
            MultipartContent multipartContent = (MultipartContent)request.Content;
            string requestmes = "SendAsync > Request \n ";
            foreach (var contens in multipartContent)
            {
                if (contens.GetType() == typeof(MultipartContent))
                {
                    var newcontent = (MultipartContent)contens;
                    foreach (var ncontens in newcontent)
                    {
                        requestmes += ncontens.ReadAsStringAsync().Result;
                    }
                }
                else if (contens.GetType() == typeof(HttpMessageContent))
                {
                    requestmes += contens.ReadAsStringAsync().Result;
                }
            }

            requestmes += "\n SendAsync > Response " + ResponsMessage.Content.ReadAsStringAsync().Result;
            LogInformation("requestPerser", $"SendAsync : {requestmes}");
        }

    }
}