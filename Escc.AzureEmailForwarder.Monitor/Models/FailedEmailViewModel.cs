using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Escc.Services.Azure;

namespace Escc.AzureEmailForwarder.Monitor.Models
{
    public class FailedEmailViewModel
    {
        public string BlobName { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public SerializableJsonMailMessage FailedEmail { get; set; }
    }
}
