using log4net.Appender;
using log4net.Core;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Linq;

namespace Log4NetAppInsights.AppInsightsOperationAppender
{
    public class AppInsightsOperationAppender : AppenderSkeleton
    {
        private TelemetryClient telemetryClient;

        // In this example, the instrumentation key comes from the web.config file where this appender is configured.

        public string InstrumentationKey { get; set; }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            if (string.IsNullOrEmpty(InstrumentationKey))
            {
                throw new LogException($"[instrumentationKey] is required.");
            }

            // Create a new telemetry client...

            telemetryClient = new TelemetryClient(new TelemetryConfiguration(InstrumentationKey));
        }

        protected override bool RequiresLayout => true;

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                throw new ArgumentNullException(nameof(loggingEvent));
            }

            try
            {
                // If we don't have an exception, log it as trace telemetry. Otherwise, log it as exception telemetry.

                if (loggingEvent.ExceptionObject == null)
                {
                    AppendTraceTelemetry(loggingEvent);
                }
                else
                {
                    AppendExceptionTelemetry(loggingEvent);
                }
            }
            catch (Exception ex)
            {
                throw new LogException("An error occurred while attempting to append log4net event to Application Insights. See inner exception for details.", ex);
            }
        }

        private void AppendExceptionTelemetry(LoggingEvent loggingEvent)
        {
            var aiTelemetry = new ExceptionTelemetry
            {
                SeverityLevel = ToAppInsightsSeverityLevel(loggingEvent.Level),
                Message = loggingEvent.RenderedMessage,
                Exception = loggingEvent.ExceptionObject
            };

            UpdateMetadata(loggingEvent, aiTelemetry);

            telemetryClient.TrackException(aiTelemetry);
        }

        private void AppendTraceTelemetry(LoggingEvent loggingEvent)
        {
            var aiTelemetry = new TraceTelemetry
            {
                SeverityLevel = ToAppInsightsSeverityLevel(loggingEvent.Level),
                Message = loggingEvent.RenderedMessage
            };

            UpdateMetadata(loggingEvent, aiTelemetry);

            telemetryClient.TrackTrace(aiTelemetry);
        }

        private void UpdateMetadata<T>(LoggingEvent loggingEvent, T aiTelemetry) where T : ITelemetry, ISupportProperties
        {
            // Add log4net-specific properties as custom properties to the telemetry we send to Application Insights...

            if (!string.IsNullOrEmpty(loggingEvent.LoggerName))
            {
                aiTelemetry.Properties.Add(nameof(loggingEvent.LoggerName), loggingEvent.LoggerName);
            }

            if (!string.IsNullOrEmpty(loggingEvent.ThreadName))
            {
                aiTelemetry.Properties.Add(nameof(loggingEvent.ThreadName), loggingEvent.ThreadName);
            }

            if (!string.IsNullOrEmpty(loggingEvent.Domain))
            {
                aiTelemetry.Properties.Add(nameof(loggingEvent.Domain), loggingEvent.Domain);
            }

            if (!string.IsNullOrEmpty(loggingEvent.Identity))
            {
                aiTelemetry.Properties.Add(nameof(loggingEvent.Identity), loggingEvent.Identity);
            }

            UpdateCustomMetadata(loggingEvent, aiTelemetry);
            UpdateOperationMetadata(loggingEvent, aiTelemetry);
            UpdateLocationMetadata(loggingEvent, aiTelemetry);
        }

        private void UpdateCustomMetadata<T>(LoggingEvent loggingEvent, T aiTelemetry) where T : ITelemetry, ISupportProperties
        {
            // If there were any additional custom properties added to the LoggingEvent, add them to the Application Insights telemetry as well...

            foreach (var propertyKey in loggingEvent.Properties.GetKeys().Where(
                k => !k.StartsWith("ai:", StringComparison.InvariantCultureIgnoreCase) &&
                     !k.StartsWith("log4net", StringComparison.InvariantCultureIgnoreCase)))
            {
                aiTelemetry.Properties.Add(propertyKey, loggingEvent.Properties[propertyKey].ToString());
            }
        }

        private void UpdateLocationMetadata<T>(LoggingEvent loggingEvent, T aiTelemetry) where T : ITelemetry, ISupportProperties
        {
            // If log4net provided location information, add it to the Application Insights telmetry....

            var locationInfo = loggingEvent.LocationInformation;

            if (!string.IsNullOrEmpty(locationInfo?.ClassName))
            {
                aiTelemetry.Properties.Add($"location_{nameof(locationInfo.ClassName)}", locationInfo.ClassName);
            }

            if (!string.IsNullOrEmpty(locationInfo?.FileName))
            {
                aiTelemetry.Properties.Add($"location_{nameof(locationInfo.FileName)}", locationInfo.FileName);
            }

            if (!string.IsNullOrEmpty(locationInfo?.LineNumber))
            {
                aiTelemetry.Properties.Add($"location_{nameof(locationInfo.LineNumber)}", locationInfo.LineNumber);
            }

            if (!string.IsNullOrEmpty(locationInfo?.MethodName))
            {
                aiTelemetry.Properties.Add($"location_{nameof(locationInfo.MethodName)}", locationInfo.MethodName);
            }
        }

        private void UpdateOperationMetadata<T>(LoggingEvent loggingEvent, T aiTelemetry) where T : ITelemetry
        {
            // This is where we add the correlation infomration to the Application Insights telemetry...
            // For more information, see https://docs.microsoft.com/en-us/azure/azure-monitor/app/correlation#data-model-for-telemetry-correlation.

            if (aiTelemetry?.Context?.Operation != null)
            {
                if (loggingEvent.Properties.Contains(LogExtensions.OperationPropertyNames.OperationId))
                {
                    aiTelemetry.Context.Operation.Id = loggingEvent.Properties[LogExtensions.OperationPropertyNames.OperationId]?.ToString();
                }

                if (loggingEvent.Properties.Contains(LogExtensions.OperationPropertyNames.OperationParentId))
                {
                    aiTelemetry.Context.Operation.ParentId = loggingEvent.Properties[LogExtensions.OperationPropertyNames.OperationParentId]?.ToString();
                }
            }
        }

        private SeverityLevel ToAppInsightsSeverityLevel(Level originalLevel)
        {
            // Convert log4net severity to Application Insights severity...

            if ((originalLevel == null) || (originalLevel.Value < Level.Info.Value))
            {
                return SeverityLevel.Verbose;
            }  
            else if (originalLevel.Value < Level.Warn.Value)
            {
                return SeverityLevel.Information;
            }
            else if (originalLevel.Value < Level.Error.Value)
            {
                return SeverityLevel.Warning;
            }
            else if (originalLevel.Value < Level.Severe.Value)
            {
                return SeverityLevel.Error;
            }
            else
            {
                return SeverityLevel.Critical;
            }
        }
    }
}
