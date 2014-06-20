using System;

namespace Escc.AzureEmailForwarder
{
    /// <summary>
    /// Logs messages to the console
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Log(string message = null, Exception exception = null)
        {
            Console.WriteLine(message);
        }
    }
}
