using System;
using Exceptionless;

namespace Escc.AzureEmailForwarder
{
    /// <summary>
    /// Logs errors to Exceptionless
    /// </summary>
    class ExceptionlessLogger : ILogger
    {
        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Log(string message = null, Exception exception = null)
        {
            if (exception != null)
            {
                exception.ToExceptionless().Submit();
            }
        }
    }
}
