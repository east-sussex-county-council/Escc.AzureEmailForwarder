using System;

namespace Escc.AzureEmailForwarder
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message = null, Exception exception = null)
        {
            Console.WriteLine(message);
        }
    }
}
