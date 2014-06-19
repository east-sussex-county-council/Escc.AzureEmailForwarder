using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using Escc.Services;
using Escc.Services.Azure;

namespace Escc.AzureEmailForwarder
{
    /// <summary>
    /// Long-running application to monitor emails queued in Azure
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var emailForwarder = new EmailForwarder(
                new AzureEmailQueue(),
                new AzureBadMailTable(),
                new AzureEmailToBlobSerialiser(new BinaryFormatter()),
                new SmtpEmailSender(),
                new List<ILogger>() { new ConsoleLogger(), new ExceptionlessLogger() });
            emailForwarder.Start();

            // Because the email forwarder creates async tasks, we need to wait for a synchronous event to stop the app from closing.
            Console.ReadLine();
        }


    }
}
