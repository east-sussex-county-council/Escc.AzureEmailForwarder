
using System;

namespace Escc.AzureEmailForwarder
{
    public interface ILogger
    {
        void Log(string message = null, Exception exception = null);
    }
}
