using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace EquitasInboundAPI
{
    public static class ErrorLogger
    {
        public static void LogCustomEvent(string ErrorMessage)
        {
            Dictionary<string, object> message = new Dictionary<string, object>();
            message["Messages"] = ErrorMessage;
            TelemetryClient teleClient = new TelemetryClient();
            teleClient.Context.InstrumentationKey = KeyVaultService.getSecret("APPINSIGHTS_INSTRUMENTATIONKEY");
            var eventTrigger = new EventTelemetry("Info Message");
            foreach (var d in message)
            {
                eventTrigger.Properties.Add(d.Key, d.Value == null ? "" : d.Value.ToString());
            }
            teleClient.TrackEvent(eventTrigger);
        }

        public static void requestPerser(HttpRequestMessage request, HttpResponseMessage ResponsMessage)
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
            LogCustomEvent($"SendAsync : {requestmes}");
        }
        
    }
}