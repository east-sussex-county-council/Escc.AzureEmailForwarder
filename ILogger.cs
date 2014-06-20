
using System;

namespace Escc.AzureEmailForwarder
{
    /// <summary>
    /// A way to log information messages and/or exceptions
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        void Log(string message = null, Exception exception = null);
    }
}
