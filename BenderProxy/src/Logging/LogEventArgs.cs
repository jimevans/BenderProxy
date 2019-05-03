using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BenderProxy.Logging
{
    public class LogEventArgs : EventArgs
    {
        private readonly string logMessage;
        private readonly LogLevel logLevel;
        private readonly Type componentType;

        public LogEventArgs(Type componentType, LogLevel logLevel, string template, params object[] args)
        {
            this.logMessage = string.Format(CultureInfo.InvariantCulture, template, args);
            this.logLevel = logLevel;
            this.componentType = componentType;
        }

        public LogEventArgs(Type componentType, LogLevel logLevel, object logObject)
        {
            if (logObject == null)
            {
                this.logMessage = string.Empty;
            }
            else
            {
                this.logMessage = logObject.ToString();
            }

            this.logLevel = logLevel;
            this.componentType = componentType;
        }

        public string LogMessage { get => this.logMessage; }

        public LogLevel LogLevel { get => this.logLevel; }

        public Type ComponentType { get => this.componentType; }
    }
}
