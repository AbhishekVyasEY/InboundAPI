namespace CreateLeads
{
    using Microsoft.Extensions.Logging;
    public static class LoggerExtension
    {
        public static string traceID { get; set; }
        public static void LogInformationWithTraceID(this ILogger logger, string logmsg)
        {
            logger.LogInformation($"[Trace ID: {traceID}] {logmsg}");
        }
        public static void LogErrorWithTraceID(this ILogger logger, string logmsg)
        {
            logger.LogError($"[Trace ID: {traceID}] {logmsg}");
        }
        public static void LogDebugWithTraceID(this ILogger logger, string logmsg)
        {
            logger.LogDebug($"[Trace ID: {traceID}] {logmsg}");
        }
        public static void LogWarningWithTraceID(this ILogger logger, string logmsg)
        {
            logger.LogWarning($"[Trace ID: {traceID}] {logmsg}");
        }
        public static void LogCriticalWithTraceID(this ILogger logger, string logmsg)
        {
            logger.LogCritical($"[Trace ID: {traceID}] {logmsg}");
        }

    }


}
