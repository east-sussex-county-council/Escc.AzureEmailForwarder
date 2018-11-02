using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Escc.Services;
using Escc.Services.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Polly;

namespace Escc.AzureEmailForwarder
{
    /// <summary>
    /// Gets email stored in Azure blobs and queues and sends it using SMTP
    /// </summary>
    public class EmailForwarder
    {
        private readonly IEnumerable<ILogger> _loggers;
        private readonly AzureEmailQueue _queue;
        private readonly AzureBadMailTable _badMailTable;
        private readonly AzureEmailToBlobSerialiser _blobSerialiser;
        private readonly IEmailSender _emailSender;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailForwarder"/> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="badMailTable">The bad mail table.</param>
        /// <param name="blobSerialiser">The email serialiser.</param>
        /// <param name="emailSender">The email sender.</param>
        /// <param name="loggers">The loggers.</param>
        public EmailForwarder(AzureEmailQueue queue, AzureBadMailTable badMailTable, AzureEmailToBlobSerialiser blobSerialiser, IEmailSender emailSender, IEnumerable<ILogger> loggers)
        {
            _queue = queue;
            _badMailTable = badMailTable;
            _blobSerialiser = blobSerialiser;
            _loggers = loggers;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Starts an asynchronous task to monitor the email queue.
        /// </summary>
        public void Start()
        {
            Task.Factory.StartNew(async () => { await ProcessEmail(); },
              TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Long-running task to monitor for new emails and kick off processing
        /// </summary>
        /// <returns></returns>
        private async Task ProcessEmail()
        {
            try
            {
                const int maxMessagesPerBatch = 32;
                const int maxRetryAttempts = 3;
                var pauseBetweenFailures = TimeSpan.FromSeconds(5);

                var retryPolicy = Policy
                    .Handle<StorageException>()
                    .WaitAndRetryAsync(maxRetryAttempts, i => pauseBetweenFailures);

                while (true)
                {
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        var queueMessages = await _queue.Queue.GetMessagesAsync(maxMessagesPerBatch);
                        var cloudQueueMessages = queueMessages as IList<CloudQueueMessage> ?? queueMessages.ToList();
                        if (cloudQueueMessages.Any())
                        {
                            await ProcessBatchOfQueuedEmails(_queue.Queue, cloudQueueMessages, _badMailTable.Table);
                        }
                    });
                }

            }
            catch (Exception ex)
            {
                Log(ex.Message, ex);
            }
        }

        /// <summary>
        /// Processes a batch of queued emails.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="queueMessages">The batch of emails.</param>
        /// <param name="badMailTable">The table for storing emails that can't be processed.</param>
        /// <returns></returns>
        private async Task ProcessBatchOfQueuedEmails(CloudQueue queue, IEnumerable<CloudQueueMessage> queueMessages, CloudTable badMailTable)
        {
            var tasks = new List<Task>();

            foreach (var queueMessage in queueMessages)
            {
                if (queueMessage.DequeueCount > 3)
                {
                    await ProcessBadMail(queue, queueMessage, badMailTable);
                }
                else
                {

                    tasks.Add(ProcessValidMail(queue, queueMessage));
                }
            }

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Forwards an email
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="queueMessage">The queue message.</param>
        /// <returns></returns>
        private Task ProcessValidMail(CloudQueue queue, CloudQueueMessage queueMessage)
        {
            return Task.Factory.StartNew(() =>
            {
                Uri blobUri = null;

                try
                {
                    blobUri = new Uri(queueMessage.AsString);
                    Log("Dequeued blob URI " + blobUri);
                    var blob = _blobSerialiser.ReadBlobFromUri(blobUri);
                    var email = _blobSerialiser.Deserialise(blob);

                    Log("Sending '" + email.Subject + "' to " + email.To);
                    _emailSender.Send(email);

                    Log("Deleting blob " + blob.Name);
                    blob.Delete();

                    Log("Deleting message from queue " + queueMessage.AsString);
                    queue.DeleteMessage(queueMessage);
                }
                catch (Exception ex)
                {
                    // Report email processing errors and continue to next email.
                    // If the error occurs repeatedly the DequeueCount will increase and the message
                    // will be routed to ProcessBadMail.
                    var message = ex.Message;
                    if (blobUri != null)
                    {
                        ex.Data.Add("Blob URI", blobUri.ToString());
                        message += " " + blobUri.ToString();
                    }
                    Log(message, ex);
                }
            });
        }

        private void Log(string message = null, Exception ex = null)
        {
            if (_loggers == null) return;

            foreach (var logger in _loggers)
            {
                logger.Log(message, ex);
            }
        }

        /// <summary>
        /// Stores and reports an email that can't be processed
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="queueMessage">The email message on the queue.</param>
        /// <param name="badMailTable">The table for storing emails that can't be processed.</param>
        /// <returns></returns>
        private async Task ProcessBadMail(CloudQueue queue, CloudQueueMessage queueMessage, CloudTable badMailTable)
        {
            // Store the email in an Azure table, with a RowKey as a unique id
            try
            {
                var badEmail = new BadMail()
            {
                PartitionKey = DateTime.UtcNow.ToString("yyyyMMdd"),
                RowKey = Guid.NewGuid().ToString(),
                BlobUri = queueMessage.AsString
            };

                var tableOperation = TableOperation.Insert(badEmail);
                await badMailTable.ExecuteAsync(tableOperation);

                queue.DeleteMessage(queueMessage);
            }
            catch (Exception ex)
            {
                // Make sure we're notified about any failure to save bad email. 
                // If it's not deleted it will keep getting dequeued and run through this method.
                ex.Data.Add("Blob URI", queueMessage.AsString);
                Log(ex.Message + " " + queueMessage.AsString, ex);
            }

            var error = "Error deserialising or sending email stored in blob: " + queueMessage.AsString +
                        ". This URI is also logged in the badmail table on Azure.";
            Log(error, new BadMailException(error));
        }


    }
}
