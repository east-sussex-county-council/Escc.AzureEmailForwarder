using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Escc.AzureEmailForwarder.Monitor.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Escc.Services.Azure;
using Modules.JsonNet;
using System.Runtime.Serialization;
using System.IO;
using System.Net.Mail;

namespace Escc.AzureEmailForwarder.Monitor.Controllers
{
    public class HomeController : Controller
    {
        CloudStorageAccount _cloudStorageAccount;
        CloudBlobClient _blobClient;
        CloudBlobContainer _cloudBlobContainer;

        public async Task<IActionResult> Index([FromServices] IConfiguration configuration)
        {
            ConnectToStorageContainer(configuration);

            var model = new FailedEmailsViewModel() { StorageAccountName = _cloudStorageAccount.BlobEndpoint.Authority };
            BlobContinuationToken continuationToken = null;
            var results = new List<IListBlobItem>();
            do
            {
                var response = await _cloudBlobContainer.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results);
            }
            while (continuationToken != null);
            model.TotalEmails = results.Count;

            var serialisationFormatter = new JsonNetFormatter();
            const int limit = 50;
            var count = 0;
            foreach (CloudBlockBlob blob in results)
            {
                if (count >= limit) break;

                var serialised = await blob.DownloadTextAsync();
                var deserialised = await Deserialise(serialisationFormatter, blob);
                model.FailedEmails.Add(new FailedEmailViewModel()
                {
                    BlobName = blob.Name,
                    DateCreated = blob.Properties.Created.Value,
                    FailedEmail = deserialised
                });
                count++;
            }

            return View(model);
        }

        [HttpGet]
        [Route("ViewEmail/{blob}", Name = "ViewEmail")]
        public async Task<ViewResult> ViewEmail([FromRoute] string blob, [FromServices] IConfiguration configuration)
        {
            var model = new FailedEmailViewModel();

            ConnectToStorageContainer(configuration);
            var blockBlob = _cloudBlobContainer.GetBlockBlobReference(blob);
            if (blockBlob != null)
            {
                var serialised = await blockBlob.DownloadTextAsync();
                var deserialised = await Deserialise(new JsonNetFormatter(), blockBlob);

                model.BlobName = blockBlob.Name;
                model.DateCreated = blockBlob.Properties.Created.Value;
                model.FailedEmail = deserialised;
            }
            return View(model);
        }

        private void ConnectToStorageContainer(IConfiguration configuration)
        {
            if (_cloudBlobContainer == null)
            {
                string _storageConnection = configuration.GetConnectionString("StorageConnectionString");
                _cloudStorageAccount = CloudStorageAccount.Parse(_storageConnection);
                _blobClient = _cloudStorageAccount.CreateCloudBlobClient();
                _cloudBlobContainer = _blobClient.GetContainerReference("email");
            }
        }


        /// <summary>
        /// Deserialises the specified blob.
        /// </summary>
        /// <param name="blob">The blob.</param>
        /// <returns></returns>
        public async Task<SerializableJsonMailMessage> Deserialise(IFormatter serialisationFormatter, CloudBlockBlob blob)
        {
            using (var s = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(s);
                s.Position = 0;

                return (SerializableJsonMailMessage)serialisationFormatter.Deserialize(s);
            }
        }

        [HttpDelete]
        [Route("api/DeleteBlob")]
        public async Task<ActionResult> DeleteBlob([FromQuery] string blob, [FromServices] IConfiguration configuration)
        {
            ConnectToStorageContainer(configuration);
            var blockBlob = _cloudBlobContainer.GetBlockBlobReference(blob);
            if (blockBlob != null)
            {
                var result = await blockBlob.DeleteIfExistsAsync();
                if (result)
                {
                    return new StatusCodeResult(204);
                }
                else
                {
                    return new StatusCodeResult(500);
                }
            }

            return new StatusCodeResult(404);
        }

        [HttpPost]
        [Route("api/SendEmail")]
        public async Task<ActionResult> SendEmail([FromQuery] string blob, [FromServices] IConfiguration configuration)
        {
            ConnectToStorageContainer(configuration);
            var blockBlob = _cloudBlobContainer.GetBlockBlobReference(blob);
            if (blockBlob != null)
            {
                var deserialised = await Deserialise(new JsonNetFormatter(), blockBlob);

                using (var client = new SmtpClient(configuration.GetValue<string>("SmtpServer")))
                {
                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = deserialised.From;
                        foreach (var to in deserialised.To)
                        {
                            mailMessage.To.Add(to);
                        }
                        foreach (var cc in deserialised.CC)
                        {
                            mailMessage.CC.Add(cc);
                        }
                        foreach (var bcc in deserialised.Bcc)
                        {
                            mailMessage.Bcc.Add(bcc);
                        }
                        mailMessage.Body = deserialised.Body;
                        mailMessage.IsBodyHtml = deserialised.IsBodyHtml;
                        mailMessage.Subject = deserialised.Subject;
                        client.Send(mailMessage);
                    }
                }

                return await DeleteBlob(blob, configuration);
            }
            return new StatusCodeResult(404);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
