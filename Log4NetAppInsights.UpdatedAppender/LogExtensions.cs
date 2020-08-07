using log4net;
using log4net.Core;
using System;

namespace Log4NetAppInsights.AppInsightsOperationAppender
{
    public static class LogExtensions
    {
        public static class OperationPropertyNames
        {
            public const string OperationId = "ai:operation_id";
            public const string OperationParentId = "ai:operation_parentId";
        }

        // log4net ILog extension method overloads for including correlation parameters (operationId and operationParentId).
        // See https://docs.microsoft.com/en-us/azure/azure-monitor/app/correlation#data-model-for-telemetry-correlation for more info.

        public static void Debug(this ILog log, string message, string operationId, string operationParentId = null, Exception exception = null) =>
            log.Logger.Log(new LoggingEvent(log.GetType(), log.Logger.Repository, log.Logger.Name, Level.Debug, message, exception)
               .AddOperationMetadata(operationId, operationParentId));

        public static void Info(this ILog log, string message, string operationId, string operationParentId = null, Exception exception = null) =>
            log.Logger.Log(new LoggingEvent(log.GetType(), log.Logger.Repository, log.Logger.Name, Level.Info, message, exception)
               .AddOperationMetadata(operationId, operationParentId));

        public static void Warn(this ILog log, string message, string operationId, string operationParentId = null, Exception exception = null) =>
            log.Logger.Log(new LoggingEvent(log.GetType(), log.Logger.Repository, log.Logger.Name, Level.Warn, message, exception)
               .AddOperationMetadata(operationId, operationParentId));

        public static void Error(this ILog log, string message, string operationId, string operationParentId = null, Exception exception = null) =>
            log.Logger.Log(new LoggingEvent(log.GetType(), log.Logger.Repository, log.Logger.Name, Level.Error, message, exception)
               .AddOperationMetadata(operationId, operationParentId));

        public static void Fatal(this ILog log, string message, string operationId, string operationParentId = null, Exception exception = null) =>
            log.Logger.Log(new LoggingEvent(log.GetType(), log.Logger.Repository, log.Logger.Name, Level.Fatal, message, exception)
               .AddOperationMetadata(operationId, operationParentId));

        public static LoggingEvent AddOperationMetadata(this LoggingEvent loggingEvent, string operationId, string operationParentId = null)
        {
            loggingEvent.Properties[OperationPropertyNames.OperationId] = operationId;
            loggingEvent.Properties[OperationPropertyNames.OperationParentId] = operationParentId;

            return loggingEvent;
        }
    }
}
