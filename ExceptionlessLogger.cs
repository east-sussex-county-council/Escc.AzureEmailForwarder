using System;
using Exceptionless;

namespace Escc.AzureEmailForwarder
{
    class ExceptionlessLogger : ILogger
    {
        public void Log(string message = null, Exception exception = null)
        {
            if (exception != null)
            {
                exception.ToExceptionless().Submit();
            }
        }
    }
}
