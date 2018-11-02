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
        /// <remarks>This is a <see cref="string"/> rather than a <see cref="Uri"/> because that means it gets serialised to Azure table storage correctly.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public string BlobUri { get; set; }
    }
}
