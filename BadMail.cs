using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Escc.AzureEmailForwarder
{
    /// <summary>
    /// An email which could not be processed from the queue, so is put into table storage for manual checking
    /// </summary>
    public class BadMail : TableEntity
    {
        public string Content { get; set; }
    }
}
