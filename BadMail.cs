using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Escc.AzureEmailForwarder
{
    /// <summary>
    /// An email which could not be processed from the queue, so is put into table storage for manual checking
    /// </summary>
    public class BadMail : TableEntity
    {
        /// <summary>
        /// Gets or sets the URI of the blob containing the email.
        /// </summary>
        /// <value>
        /// The blob URI.
        /// </value>
        public Uri BlobUri { get; set; }
    }
}
