using System;
using log4net;

namespace Escc.AzureEmailForwarder
{
    /// <summary>
    /// Log actions and errors using Log4Net
    /// </summary>
    public class Log4NetLogger : ILogger
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Log(string message = null, Exception exception = null)
        {
            if (exception != null)
            {
                _log.Error(exception.Message, exception);
            }
            else
            {
                _log.Info(message);
            }
        }
    }
}
