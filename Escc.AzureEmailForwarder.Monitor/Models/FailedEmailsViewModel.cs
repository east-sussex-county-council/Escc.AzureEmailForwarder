using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Escc.Services.Azure;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Escc.AzureEmailForwarder.Monitor.Models
{
    public class FailedEmailsViewModel
    {
        public string StorageAccountName {get;set;}
        public int TotalEmails { get; set; }
        public IList<FailedEmailViewModel> FailedEmails { get; private set; } = new List<FailedEmailViewModel>();
    }
}
