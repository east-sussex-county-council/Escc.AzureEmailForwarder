using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using Escc.Services;
using Escc.Services.Azure;
using Exceptionless;
using Modules.JsonNet;

namespace Escc.AzureEmailForwarder
{
    /// <summary>
    /// Long-running application to monitor emails queued in Azure
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            ExceptionlessClient.Current.Startup(); 
            
            var emailForwarder = new EmailForwarder(
                new AzureEmailQueue(),
                new AzureBadMailTable(),
                new AzureEmailToBlobSerialiser(new JsonNetFormatter()),
                new SmtpEmailSender(),
                new List<ILogger>() { new ConsoleLogger(), new ExceptionlessLogger(), new Log4NetLogger() });
            emailForwarder.Start();

            ExceptionlessClient.Current.ProcessQueue();

            // Because the email forwarder creates async tasks, we need to wait for a synchronous event to stop the app from closing.
            Console.ReadLine();
        }


    }
}
